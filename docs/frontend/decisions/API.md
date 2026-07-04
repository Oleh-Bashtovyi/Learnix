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
- **Zustand** manages global Client State. The five stores are:

| Store | File | Persisted | Purpose |
|-------|------|-----------|---------|
| Auth | `auth.store.ts` | ❌ (in-memory) | Access token, user summary, `isInitializing` flag |
| Theme | `theme.store.ts` | ✅ `localStorage` | Light/dark mode; applies `.dark` class on `<html>` |
| Locale | `locale.store.ts` | ✅ `localStorage` | Active language (`en` / `uk`) |
| UI | `ui.store.ts` | ❌ | AI chat widget open/close state (`isChatOpen`) |
| Player | `player.store.ts` | ✅ `localStorage` | Video autoplay preference in the course player |

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

## ADR-FRONT-API-004: Realtime Communication via SignalR

**Decision:**
Realtime features (Chat, Notifications, Achievements) use **SignalR** over WebSockets, rather than Server-Sent Events (SSE).

**Code Fragment (useChatHub.ts):**
```ts
// src/hooks/realtime/useChatHub.ts
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '@/store/auth.store';
import { queryKeys } from '@/api/queryKeys';
import { env } from '@/utils/env';

export function useChatHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/chat`, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (notification) => {
            // SignalR events trigger React Query invalidation
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
            queryClient.invalidateQueries({
                queryKey: queryKeys.messages.messages(notification.conversationId),
            });
        });

        connection.start().catch(() => {});
        connectionRef.current = connection;

        return () => { connection.stop(); };
    }, [accessToken, queryClient]);
}
```

**Why:**
- SignalR provides robust automatic reconnections and fallback transports (Long Polling) if WebSockets fail.
- It integrates seamlessly with the .NET backend.
- We tie SignalR events directly to React Query invalidation, ensuring the UI stays fresh without duplicating state.

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
} as const;
```

**Note:** `HUB_URL` is derived automatically from `VITE_API_URL` by stripping the `/api` suffix, so SignalR hubs don't need a separate env variable.

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
