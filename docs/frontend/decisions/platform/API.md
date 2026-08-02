# Learnix — Frontend Architecture Decision Records (API & State)

## ADR-FRONT-API-001: API Layer — Axios Instance with Queued Token Refresh

**Context:** A short-lived access token means every API module would otherwise need its own 401/refresh
handling, and concurrent requests failing at once would each trigger a separate refresh call.

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

**Context:** Without an explicit rule, server data creeps into Zustand stores and client-only UI state
creeps into React Query, and both caches drift out of sync with each other.

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

**Context:** Query keys and cache defaults need one shared convention, or invalidating "all course lists"
after a mutation becomes a guessing game of which exact key variant is cached.

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

**Context:** Direct messaging, notifications, achievements and certificates each need a server-push
channel; opening one SignalR connection per feature multiplies auth/reconnect/cleanup logic for no
real benefit.

**Decision:**
Direct messaging, in-app notifications, achievements and certificates share **one SignalR hub
connection** (`${env.HUB_URL}/hubs/notifications`), opened once by `useNotificationsHub` and mounted
near the app root. It listens for `ReceiveMessage`, `UnreadCountChanged`, `AchievementUnlocked`,
`CertificateIssued` and `NotificationReceived`, and reacts per event — invalidating the relevant
React Query cache, or surfacing a toast for achievements/certificates. A `NotificationReceived` event
whose `type` is `RoleAssigned`/`RoleRemoved` additionally triggers `refreshSession()` (ADR-FRONT-AUTH-006).

The **AI chat assistant is not part of this hub.** It is a single request/response stream, not a
multi-client push channel, so it goes over a plain `fetch`-based SSE-style stream (`useAiChat` +
`streamAiMessage` in `src/api/aiChat.api.ts`) instead of SignalR.

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

**Context:** The backend contract changes frequently during active development; a codegen step adds a
build dependency on the API being reachable and up to date.

**Decision:**
- DTO (Data Transfer Object) types for API requests and responses are written **manually** in `src/types/` (e.g., `course.types.ts`, `user.types.ts`).
- We specifically **do not** use OpenAPI/Swagger code generators (like `orval` or `openapi-typescript`).

**Why:**
- Given the rapid prototyping phase and frequent backend changes in this project, manual types provide flexibility to map UI structures independently of strict backend contracts.
- Explicit mapping between `FormValues` (Zod) and API DTOs (TypeScript) ensures the frontend doesn't become tightly coupled to backend implementation details.

---

## ADR-FRONT-API-006: Environment Variables Management

**Context:** Reading `import.meta.env` directly from call sites scatters config parsing across the
codebase and defers a missing-variable failure to whichever API call happens to need it first.

**Decision:**
- Environment variables are defined in `.env` (development) and `.env.production` (production).
- A centralized utility, `src/utils/env.ts`, exposes them to the rest of the application: `API_URL`,
  `HUB_URL` (derived from `API_URL` by stripping the `/api` suffix — no separate SignalR env var),
  `SITE_URL` (absolute base URL for canonical links, Open Graph tags and the generated sitemap — see
  `I18N_SEO.md` — falling back to the runtime origin when unset), and `SHOW_PROJECT_BANNER` (a
  display flag for the "portfolio project" notice strip, **on by default** — set to `false` to hide it).
- `API_URL` is validated at startup: missing it throws immediately rather than failing on the first
  API call.

**Why:**
- Centralizing env access in `env.ts` prevents scattering `import.meta.env` calls throughout the codebase, making it easier to mock in tests or change prefixes later.
- Throwing at startup on a missing `API_URL` catches misconfigured deployments immediately instead of silently failing later.
- Defaulting `SHOW_PROJECT_BANNER` to on and making it opt-out means a deployment that forgets to set it still shows the disclosure, rather than silently hiding it.

---

## ADR-FRONT-API-007: Data Fetching Abstraction (Custom Hooks)

**Context:** Calling `useQuery`/`useMutation` directly inside components ties query keys, cache
invalidation and error handling to the presentation layer, duplicating that logic wherever the same
data is needed.

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

**Context:** Tables/catalogs and chat message history have different pagination needs — one wants
jump-to-page navigation, the other a continuous scroll-back.

**Decision:**
We utilize two distinct pagination strategies depending on the UX requirements:
- **Standard Pagination (Offset-based):** For tables, grids, and catalogs (e.g., Admin Dashboards, Course Catalog), we use offset-based pagination (`skip` and `take` parameters) mapping to the backend's `PagedResult<T>` structure.
- **Infinite Scrolling:** For highly dynamic, linear data streams (specifically Chat Messages in `MessagesPage`), we use React Query's `useInfiniteQuery` to seamlessly load older items as the user scrolls.

**Why:**
- Offset pagination provides deterministic navigation and total counts, which is crucial for dashboards and catalogs where users want to jump to specific pages.
- Infinite scrolling provides a seamless, frictionless UX for chat histories.
