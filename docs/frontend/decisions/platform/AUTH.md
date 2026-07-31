# Learnix — Frontend Architecture Decision Records (Auth)

## ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh

**Decision:**
- **Access Token:** Stored exclusively in-memory (via Zustand). It is *never* saved to `localStorage` (XSS vulnerable).
- **Refresh Token:** Handled by the backend via an `HttpOnly` cookie (set by the backend, inaccessible to JS).
- **Silent Refresh on Load:** An `AuthInitializer` component runs on app startup. It sends a `POST /auth/refresh` request, parses the received token via `parseAccessToken`, and if successful, the user is logged in.
- **On other tabs:** Nothing special. They find out via a 401 error → refresh → continue.
- **Google OAuth:** Token-based flow via `@react-oauth/google` (not a server-side redirect).

**Silent Refresh (AuthInitializer):**
The logic is encapsulated in the `AuthInitializer` component, which is called when the application starts.
```tsx
import { useEffect } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { parseAccessToken } from '@/utils/parseAccessToken';
import { env } from '@/utils/env';

export function AuthInitializer({ children }: { children: React.ReactNode }) {
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        axios
            .post<{ accessToken: string; avatarUrl: string | null }>(
                `${env.API_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            )
            .then(({ data }) => {
                setAccessToken(data.accessToken);
                const user = parseAccessToken(data.accessToken);
                if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
            })
            .catch(() => {
                // No valid refresh token — user is not logged in
            })
            .finally(() => {
                finishInitialization();
            });
    }, []);

    return <>{children}</>;
}
```

**Google OAuth — Token-Based Flow (Actual Implementation):**
The backend implements `POST /api/auth/google` with the body `{ idToken: string }` — it is not a redirect handler.
The frontend receives the `id_token` from Google via the `GoogleLogin` component and immediately sends it to the backend.

**Why token-based and not redirect:**
- The backend validates the `id_token` via `GoogleJsonWebSignature.ValidateAsync` (Google.Apis.Auth) — it doesn't require a server-side OAuth code exchange.
- There is no callback page, making the flow simpler.

**Why:**
- `localStorage` for tokens = XSS vulnerability. HttpOnly cookies are inaccessible to JS.
- **Preventing the Login Flash:** The `isInitializing` state defaults to `true`. In the `RequireRole` guard, we return `null` while `isInitializing` is `true`. The `AuthInitializer` calls `finishInitialization()` in its `finally` block. This guarantees that protected routes will wait for the silent refresh to finish before deciding whether to redirect the user to `/login`, eliminating the "login flash" effect upon reloading the page.

**Alternatives:**
- Access token in `localStorage` — simpler, but insecure.
- Popup OAuth — more complex, often blocked by browsers.

---

## ADR-FRONT-AUTH-002: OTP-Based Email Verification & Auto-Login

**Decision:**
- After registration, the user is immediately redirected to `/verify-email` carrying their email in the React Router state, rather than showing a static "check your email" success screen.
- The verification screen provides a 6-digit OTP input interface (with auto-focus, backspace, and paste support).
- Submitting the correct code automatically updates the global auth state and redirects to the dashboard.

**Why:**
- **Conversion UX:** Improves conversion rates by keeping the user actively engaged in the app flow.
- **Immediate Feedback:** A dedicated OTP interface provides a clearer path forward compared to "check your email and click a link".
- **Auto-Login:** Handling the authentication response exactly like a standard login (`setAccessToken`, `setUser` via the auth store) ensures immediate access to gated resources without requiring the user to type their password again.

**Alternatives:**
- **URL Parameter Verification (Previous Implementation):** Relying on the user to click a link. Causes state fragmentation if clicked on another device (e.g. registered on PC, clicked link on phone, PC remains unauthenticated).

**Consequences:**
- The `/verify-email` route acts as an active verification gateway rather than a passive link receiver.
- The `authApi.verifyEmail` payload expects `{ email, token }` and now returns a `LoginResponse` (AccessToken, Expiration, AvatarUrl).
- The `RegisterPage` no longer handles inline success rendering, relying entirely on the dedicated verification page.

---

## ADR-FRONT-AUTH-003: Token-Based Password Reset Flow

**Decision:**
Unlike the Email Verification process (which utilizes a 6-digit OTP), the Password Reset flow relies on a standard URL query string (`?email=...&token=...`) embedded in the recovery email.

**Why:**
- Password resets often happen across different devices or out of active session context (e.g., requested on desktop, email opened on mobile). A standard link is more universally reliable in these scenarios than requiring the user to manually type an OTP on a secondary device.
- It is an established industry standard for password recovery, creating less friction for users in distress.

**Consequences:**
- The `ResetPasswordPage` must parse `useSearchParams` to extract the `email` and `token` prior to rendering the new password form.
- The UX divergence from registration (OTP vs URL) is intentional.

---

## ADR-FRONT-AUTH-004: Explicit Logout & State Clearing

**Decision:**
Manual logout is performed **only** through the `useLogout()` hook (`hooks/auth/useLogout.ts`), which runs a strict 4-step sequence:
1. `authApi.logout()`: Calls the backend to invalidate and clear the HttpOnly refresh cookie.
2. `useAuthStore().logout()`: Clears the in-memory access token and user profile from Zustand.
3. `queryClient.clear()`: Purges all cached server state from TanStack Query.
4. `navigate(APP_ROUTES.public.home)`: Redirects the user to the public landing page.

**Why:**
- Without `queryClient.clear()`, sensitive data fetched by the previous user would remain in the React Query cache, creating a severe data leak vulnerability if another user logs in on the same device.
- The dual-logout approach (Zustand + Backend) guarantees that even if the backend call fails, the client is still forcefully logged out locally.
- A sequence that every "Sign Out" button had to re-implement by hand was copied into four components, and the copies had already begun to diverge. A hook makes the correct sequence the only reachable one.
- Landing rather than login: the user asked to leave, so answering with a sign-in form is a non sequitur. The landing page is public, is the natural signed-out home, and stays valid whether logout was triggered from a public page or from inside a role panel (where the route guard would bounce them out anyway).

**Consequences:**
- A "Sign Out" button calls `useLogout()`. Re-implementing the sequence inline is a defect.
- Automatic logout on refresh failure (`axios.instance.ts`) is a separate path: it clears the store only, and lets the interceptor's queued-request handling decide what happens next.

---

## ADR-FRONT-AUTH-005: Role-Based Routing & Default Entry Points

**Decision:**
Routing logic for authenticated users is strictly role-dependent:
- **Navigation Guard:** The `RequireRole` component intercepts protected routes, validating that the user's role array intersects with the required roles. If unauthorized, they are redirected to their default home.
- **Default Entry Points:** The `getRoleHome` utility dynamically calculates the fallback URL upon login or unauthorized access (Admin → `/admin`, Instructor → `/instructor`, Student → `/courses`).

**Why:**
- Centralizing the fallback logic inside `getRoleHome` prevents hardcoded redirects across different auth components (Login, Registration, Google Auth, etc.).
- The `RequireRole` wrapper provides a declarative way to restrict layout trees at the React Router level without scattering role-checking logic inside individual page components.

**Consequences:**
- All auth handlers must use `getRoleHome(user.roles)` rather than a static redirect when `location.state.from` is missing.

