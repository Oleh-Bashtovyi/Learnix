# Learnix — Frontend Architecture Decision Records (Auth)

## ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh

**Decision:**
- **Access Token:** Stored exclusively in-memory (via Zustand). It is *never* saved to `localStorage` (XSS vulnerable).
- **Refresh Token:** Handled by the backend via an `HttpOnly` cookie (set by the backend, inaccessible to JS).
- **Silent Refresh on Load:** An `AuthInitializer` component runs on app startup. It sends a `POST /auth/refresh` request, parses the received token via `parseAccessToken`, and if successful, the user is logged in.
- **On other tabs:** Nothing special. They find out via a 401 error → refresh → continue.
- **Google OAuth:** Token-based flow via `@react-oauth/google` (not a server-side redirect).

**Silent Refresh (AuthInitializer):**
`AuthInitializer` no longer owns the refresh request itself — that logic lives in one shared
`refreshSession()` utility (`src/utils/refreshSession.ts`), reused both here and by the role-change
handler (ADR-FRONT-AUTH-006). `AuthInitializer` just calls it once on mount, behind a module-level
`refreshPromise` guard so React's Strict Mode double-invoke (or a second mount) can't fire the
request twice:
```tsx
// src/components/common/auth/AuthInitializer.tsx
let refreshPromise: Promise<unknown> | null = null;

export function AuthInitializer({ children }: AuthInitializerProps) {
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        if (!refreshPromise) {
            refreshPromise = refreshSession()
                .catch(() => {
                    // No valid refresh token — user is not logged in
                })
                .finally(() => {
                    refreshPromise = null;
                });
        } else {
            return;
        }

        refreshPromise.finally(() => {
            finishInitialization();
        });
    }, []);

    return <>{children}</>;
}
```

```ts
// src/utils/refreshSession.ts
export async function refreshSession(): Promise<UserSummary | null> {
    const { data } = await axios.post<RefreshResponse>(
        `${env.API_URL}/auth/refresh`,
        {},
        { withCredentials: true },
    );

    const { setAccessToken, setUser } = useAuthStore.getState();
    setAccessToken(data.accessToken);

    const user = parseAccessToken(data.accessToken);
    if (user) setUser({ ...user, avatarUrl: data.avatarUrl });

    return user;
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
- **Extracting `refreshSession()`:** the role-change handler (ADR-FRONT-AUTH-006) needs to perform the exact same refresh outside of app startup. Duplicating the request/parse/store-update logic would have let the two copies drift; the `refreshPromise` guard in `AuthInitializer` exists because Strict Mode's double-effect-invoke would otherwise fire two concurrent `/auth/refresh` calls on a single page load.

**Alternatives:**
- Access token in `localStorage` — simpler, but insecure.
- Popup OAuth — more complex, often blocked by browsers.

---

## ADR-FRONT-AUTH-002: OTP-Based Email Verification & Auto-Login

**Decision:**
- After registration, the user is redirected to `/verify-email` carrying their email in the React Router state, rather than showing a static "check your email" success screen. The redirect isn't a `navigate()` call inside `RegisterPage` itself — registration sets auth state exactly like a login would, and the `RequireGuest` guard (which wraps `/register`) sees an authenticated-but-unverified user and issues the redirect, attaching `{ email, from }` to the navigation state.
- The verification screen provides a 6-digit OTP input interface (with auto-focus, backspace, and paste support).
- Submitting the correct code automatically updates the global auth state and redirects to the dashboard.

**Why:**
- **Conversion UX:** Improves conversion rates by keeping the user actively engaged in the app flow.
- **Immediate Feedback:** A dedicated OTP interface provides a clearer path forward compared to "check your email and click a link".
- **Auto-Login:** Handling the authentication response exactly like a standard login (`setAccessToken`, `setUser` via the auth store) ensures immediate access to gated resources without requiring the user to type their password again.
- **Guard-driven redirect, not a page-local one:** `RequireGuest` is the single place that already decides where an authenticated user visiting a guest-only page should land; routing the unverified case through it means `RegisterPage` doesn't need its own post-submit navigation logic at all.

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
Manual logout is performed **only** through the `useLogout()` hook (`hooks/auth/useLogout.ts`), which runs a strict, awaited 4-step sequence:
1. `await authApi.logout()`: Calls the backend to invalidate and clear the HttpOnly refresh cookie. Awaited (unlike the rest) — a hard navigation would abort a request still in flight and leave the refresh cookie alive on the server.
2. `useAuthStore().logout()`: Clears the in-memory access token and user profile from Zustand.
3. `queryClient.clear()`: Purges all cached server state from TanStack Query.
4. `window.location.assign(APP_ROUTES.public.home)`: A **full page reload** to the public landing page, deliberately not React Router's `navigate()`.

**Why:**
- Without `queryClient.clear()`, sensitive data fetched by the previous user would remain in the React Query cache, creating a severe data leak vulnerability if another user logs in on the same device.
- The dual-logout approach (Zustand + Backend) guarantees that even if the backend call fails, the client is still forcefully logged out locally.
- A sequence that every "Sign Out" button had to re-implement by hand was copied into four components, and the copies had already begun to diverge. A hook makes the correct sequence the only reachable one.
- Landing rather than login: the user asked to leave, so answering with a sign-in form is a non sequitur. The landing page is public, is the natural signed-out home, and stays valid whether logout was triggered from a public page or from inside a role panel (where the route guard would bounce them out anyway).
- **Hard reload, not `navigate()`:** clearing the store is synchronous, so a router transition would still have the guarded route mounted the instant its guard sees a null user — it renders its own `<Navigate to="/login">` and wins the race against the intended landing-page redirect. Leaving the document entirely also drops anything the two clears above could have missed.

**Consequences:**
- A "Sign Out" button calls `useLogout()`. Re-implementing the sequence inline is a defect.
- Automatic logout on refresh failure (`axios.instance.ts`) is a related but separate path: when the 401-retry's own refresh call fails, it clears the auth store and hard-redirects to `/login` via `window.location.href` directly in the interceptor, rather than going through `useLogout()` — there is no backend call to make (the refresh already failed) and no queued requests to preserve.

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

---

## ADR-FRONT-AUTH-006: Mid-Session Role Change Forces a Token Refresh

**Decision:**
When the realtime notifications hub (see `decisions/platform/API.md` ADR-FRONT-API-004) delivers a
`NotificationReceived` event whose `type` is `RoleAssigned` or `RoleRemoved`, the client calls the
same `refreshSession()` used on app startup, swapping in a fresh access token without waiting for the
current one to expire.

**Why:**
- The JWT is not revoked when an admin grants or removes a role — the access token issued before the
  change keeps carrying the old role claims until it is naturally replaced. Without this, a newly
  promoted instructor (or a demoted one) would only see the effect of the change up to 15 minutes
  later, whenever the access token next expired on its own.
- `refreshSession()` re-derives the user's roles from the database on every call (the refresh endpoint
  always re-reads them), so calling it early is sufficient — no separate "apply this role" client-side
  logic is needed.
- Reusing `refreshSession()` rather than writing a second refresh path keeps there being exactly one
  way to update the stored access token.

**Alternatives:**
- Encode a "roles changed" flag server-side and have the client poll for it — rejected, since the hub
  connection already delivers the event with no added infrastructure.
- Force a full logout/login on role change — rejected as unnecessarily disruptive for a change that a
  token refresh already resolves cleanly.

**Consequences:**
- A role change becomes visible (new dashboard reachable, an old one no longer is) within the latency
  of the realtime hub, not the access token's expiry window.
- Any other server-side change that a fresh token would need to reflect can piggyback on the same
  `refreshSession()` call from the same notification handler, rather than inventing a new refresh
  trigger.

---

## ADR-FRONT-AUTH-007: Persistent Email-Confirmation Banner Gated on the JWT Claim

**Decision:**
Whether a signed-in user has confirmed their email is read directly from the `emailVerified` claim on
the parsed access token (`UserSummary.emailVerified`), not from a per-request error code. A persistent
`EmailConfirmationBanner`, rendered near the app shell, shows a resend action whenever a user is signed
in, unverified, and not already on `/verify-email`. Resending is rate-limited client-side via a cooldown
hook, and success re-navigates to `/verify-email` with the email and current path in state — the same
shape `RequireGuest` uses for the post-registration redirect (ADR-FRONT-AUTH-002).

**Why:**
- The claim is already on the token the client holds — there's no need to special-case a 403 from
  individual endpoints to find out the same fact.
- A persistent banner is visible regardless of which gated action the user tries next, instead of only
  appearing reactively after they hit a blocked request.

**Consequences:**
- Any page or layout that renders the app shell gets the banner for free; individual pages don't each
  need to check `emailVerified` themselves purely to decide whether to nudge the user toward
  verification.
- Because the check is claim-based, the banner can go stale for the length of one access token if the
  user confirms their email in another tab — it clears on the next silent refresh, same as any other
  claim.

