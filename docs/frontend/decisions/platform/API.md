# Learnix — Frontend Architecture Decision Records (API & State)

## ADR-FRONT-API-001: API Layer — Axios Instance with Queued Token Refresh

**Decision:**
- A single Axios instance in `src/api/axios.instance.ts`.
- Request interceptor attaches the in-memory JWT from Zustand.
- Response interceptor catches `401 Unauthorized` responses. It performs a silent refresh using the HttpOnly refresh cookie.
- **Concurrent 401 Queue:** If multiple requests fail with 401 simultaneously, they are queued. Only *one* refresh request is sent to the backend. After it succeeds, the queued requests are retried.
- If the refresh itself fails, the user is logged out and redirected to `/login`.

**Why:**
- The 401 queue is critical to prevent race conditions (e.g., 5 failing requests causing 5 simultaneous refresh calls).
- Interceptors centralize token logic, keeping API modules thin and clean.

**Alternatives:**
- `fetch` API: Discarded because Axios interceptors are much more robust and require less boilerplate.

---

## ADR-FRONT-API-002: State Management Boundary

**Decision:**
We strictly separate Server State from Client State:
- **TanStack Query** manages all Server State (courses, users, enrollments, etc.). API data is *never* stored in Zustand.
- **Zustand** manages global Client State. The stores are:

| Store | File | Persisted | Purpose |
|-------|------|-----------|---------|
| Auth | `auth.store.ts` | ❌ (in-memory) | Access token, user summary, `isInitializing` flag |
| Theme | `theme.store.ts` | ✅ `localStorage` | Light/dark mode; applies `.dark` class on `<html>` |
| Locale | `locale.store.ts` | ✅ `localStorage` | Active language (`en` / `uk`) |
| UI | `ui.store.ts` | ❌ | AI chat widget open/close state (`isChatOpen`) |
| Player | `player.store.ts` | ✅ `localStorage` | Video autoplay preference in the course player |
| Onboarding | `onboarding.store.ts` | ✅ `localStorage` | Which one-time UI hints (keyed by id) have already been dismissed |

- **useState / react-hook-form** manages local component/form state.

**Why:**
- React Query handles caching, refetching, and stale-while-revalidate out of the box. Duplicating this in Zustand leads to stale data bugs.
- Zustand is perfect for auth state because it can be accessed outside of React components (e.g., inside Axios interceptors via `useAuthStore.getState()`).

---

## ADR-FRONT-API-003: React Query Structure & Defaults

**Decision:**
- Query keys are defined hierarchically in `src/api/queryKeys.ts` (e.g., `queryKeys.courses.lists()`, `queryKeys.courses.detail(id)`).
- The global `QueryClient` is configured in `main.tsx` with the following defaults:
  - `staleTime: 60s` — data is considered fresh for 1 minute.
  - `gcTime: 5min` — unused cache entries are garbage-collected after 5 minutes.
  - `retry: 1` — failed requests are retried once before surfacing an error.
  - `refetchOnWindowFocus: false` — prevents aggressive re-fetching when the user switches browser tabs.
- **Global mutation error handler:** By default, all failed mutations show a toast via `sonner`. Individual mutations can opt out by setting `mutation.meta.suppressGlobalError = true`.

**Why:**
- Hierarchical keys allow invalidating entire groups of queries at once (e.g., invalidating all course lists regardless of filter parameters).
- 60s stale time is a good compromise for an LMS to prevent aggressive over-fetching while keeping data reasonably fresh.
- The `suppressGlobalError` escape hatch is essential for mutations that handle their own errors inline (e.g., form validation flows where errors are mapped to fields, not toasts).

---

## ADR-FRONT-API-004: Realtime Communication via a Single SignalR Notifications Hub

**Decision:**
Direct messaging, in-app notifications, achievements and certificates share **one SignalR hub
connection** (`${env.HUB_URL}/hubs/notifications`), opened once by `useNotificationsHub` and mounted
near the app root. It listens for `ReceiveMessage`, `UnreadCountChanged`, `AchievementUnlocked`,
`CertificateIssued` and `NotificationReceived`, and reacts per event — invalidating the relevant
React Query cache, or surfacing a toast for achievements/certificates.

The **AI chat assistant is not part of this hub.** It is a single request/response stream, not a
multi-client push channel, so it goes over a plain `fetch`-based SSE-style stream (`useAiChat` +
`streamAiMessage` in `src/api/aiChat.api.ts`) instead of SignalR.

**Code Fragment (useNotificationsHub.tsx, abbreviated):**
```ts
// src/hooks/realtime/useNotificationsHub.tsx
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${env.HUB_URL}/hubs/notifications`, { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveMessage', (notification) => {
    queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
    queryClient.invalidateQueries({ queryKey: queryKeys.messages.messages(notification.conversationId) });
});
connection.on('UnreadCountChanged', (notification) => { /* set unread count */ });
connection.on('AchievementUnlocked', (payload) => { /* toast + invalidate achievements.mine() */ });
connection.on('CertificateIssued', (payload) => { /* toast + invalidate certificates.mine() */ });
connection.on('NotificationReceived', (payload) => {
    /* bump unread count, invalidate notifications.list() */
    if (payload.type === 'RoleAssigned' || payload.type === 'RoleRemoved') {
        refreshSession().catch(() => {}); // see ADR-FRONT-AUTH-006
    }
});

connection.start().catch(() => {});
```

**Why:**
- SignalR provides robust automatic reconnections and fallback transports (Long Polling) if WebSockets fail.
- It integrates seamlessly with the .NET backend.
- We tie SignalR events directly to React Query invalidation, ensuring the UI stays fresh without duplicating state.
- One hub connection per session is simpler to manage (auth, reconnects, cleanup) than one per feature, and none of these four features needs its own connection lifecycle.

**Consequences:**
- A new realtime feature that fits a push-notification shape adds a handler to the existing
  `useNotificationsHub` connection rather than opening a second hub connection.
- A conversational, request/response streaming feature (like the AI assistant) does not belong on
  this hub — it should use the `fetch`-based streaming pattern in `aiChat.api.ts` instead.

---

## ADR-FRONT-API-005: Type Definition Strategy (Manual vs Codegen)

**Decision:**
- DTO (Data Transfer Object) types for API requests and responses are written **manually** in `src/types/` (e.g., `course.types.ts`, `user.types.ts`).
- We specifically **do not** use OpenAPI/Swagger code generators (like `orval` or `openapi-typescript`).

**Why:**
- Given the rapid prototyping phase and frequent backend changes in this project, manual types provide flexibility to map UI structures independently of strict backend contracts.
- Explicit mapping between `FormValues` (Zod) and API DTOs (TypeScript) ensures the frontend doesn't become tightly coupled to backend implementation details.

---

## ADR-FRONT-API-006: Environment Variables Management

**Decision:**
- Environment variables are defined in `.env` (development) and `.env.production` (production).
- We use a centralized utility `src/utils/env.ts` to expose environment variables to the rest of the application.
- Critical variables (like `VITE_API_URL`) are validated at startup by throwing an error if missing:

```ts
// src/utils/env.ts
const apiUrl = import.meta.env.VITE_API_URL;
if (!apiUrl) throw new Error('Missing env variable: VITE_API_URL');

export const env = {
    API_URL: apiUrl,
    HUB_URL: apiUrl.replace(/\/api\/?$/, ''),
    SITE_URL: import.meta.env.VITE_SITE_URL ?? window.location.origin,
    SHOW_PROJECT_BANNER: import.meta.env.VITE_SHOW_PROJECT_BANNER === 'true',
} as const;
```

**Note:** `HUB_URL` is derived automatically from `VITE_API_URL` by stripping the `/api` suffix, so SignalR hubs don't need a separate env variable. `SITE_URL` is the absolute base URL used for canonical links, Open Graph tags and the generated sitemap (see `decisions/platform/I18N_SEO.md`), and falls back to the runtime origin when unset. `SHOW_PROJECT_BANNER` is a non-critical display flag and does not throw when missing.

**Why:**
- Centralizing env access in `env.ts` prevents scattering `import.meta.env` calls throughout the codebase, making it easier to mock in tests or change prefixes later.
- Runtime validation with `throw new Error()` catches misconfigured deployments immediately at app startup instead of silently failing on the first API call.

---

## ADR-FRONT-API-007: Data Fetching Abstraction (Custom Hooks)

**Decision:**
Components must not call `useQuery` or `useMutation` directly with `queryKeys` and `api` methods. Instead, all React Query data fetching logic must be encapsulated in domain-specific custom hooks within the `src/hooks/` directory (e.g., `useCourseDetail.ts`, `useCourseMutations.ts`).

**Why:**
- **Separation of concerns:** Components handle presentation and user interaction; custom hooks handle data fetching, cache invalidation, and optimistic updates.
- **Reusability:** A custom hook can be reused across multiple components without duplicating query keys, dependencies, or error handling logic.
- **Maintainability:** When the backend changes, we update the `api` module and the custom hook, leaving the UI components completely untouched.

**Consequences:**
- When integrating a new API endpoint, a corresponding custom hook must be created (or added to an existing hook file like `useMyCourseMutations`).

---

## ADR-FRONT-API-008: Pagination Strategies

**Decision:**
We utilize two distinct pagination strategies depending on the UX requirements:
- **Standard Pagination (Offset-based):** For tables, grids, and catalogs (e.g., Admin Dashboards, Course Catalog), we use offset-based pagination (`skip` and `take` parameters) mapping to the backend's `PagedResult<T>` structure.
- **Infinite Scrolling:** For highly dynamic, linear data streams (specifically Chat Messages in `MessagesPage`), we use React Query's `useInfiniteQuery` to seamlessly load older items as the user scrolls.

**Why:**
- Offset pagination provides deterministic navigation and total counts, which is crucial for dashboards and catalogs where users want to jump to specific pages.
- Infinite scrolling provides a seamless, frictionless UX for chat histories.
