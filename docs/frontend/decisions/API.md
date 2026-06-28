# Learnix — Frontend Architecture Decision Records (API & State)

## ADR-FRONT-API-001: API Layer — Axios Instance with Queued Token Refresh

**Decision:**
- A single Axios instance in `src/api/axios.instance.ts`.
- Request interceptor attaches the in-memory JWT from Zustand.
- Response interceptor catches `401 Unauthorized` responses. It performs a silent refresh using the HttpOnly refresh cookie.
- **Concurrent 401 Queue:** If multiple requests fail with 401 simultaneously, they are queued. Only *one* refresh request is sent to the backend. After it succeeds, the queued requests are retried.

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
- **Zustand** manages global Client State (auth tokens, user summary, theme, and global UI state like sidebar open/close).
- **useState / react-hook-form** manages local component/form state.

**Why:**
- React Query handles caching, refetching, and stale-while-revalidate out of the box. Duplicating this in Zustand leads to stale data bugs.
- Zustand is perfect for auth state because it can be accessed outside of React components (e.g., inside Axios interceptors).

---

## ADR-FRONT-API-003: React Query Structure

**Decision:**
- Query keys are defined hierarchically in `src/api/queryKeys.ts` (e.g., `queryKeys.courses.lists()`, `queryKeys.courses.detail(id)`).
- Default stale time is 60 seconds.

**Why:**
- Hierarchical keys allow invalidating entire groups of queries at once (e.g., invalidating all course lists regardless of filter parameters).
- 60s stale time is a good compromise for an LMS to prevent aggressive over-fetching while keeping data reasonably fresh.

---

## ADR-FRONT-API-004: Realtime Communication via SignalR

**Decision:**
Realtime features (Chat, Notifications, Achievements) use **SignalR** over WebSockets, rather than Server-Sent Events (SSE).

**Code Fragment (useChatHub.ts):**
```ts
// src/hooks/useChatHub.ts
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
- Validation is done manually at startup inside `env.ts` by throwing an error if critical variables (like `VITE_API_URL`) are missing.

**Why:**
- Centralizing env access in `env.ts` prevents scattering `import.meta.env` calls throughout the codebase, making it easier to mock in tests or change prefixes later.
- Manual validation with standard JS `throw new Error()` is lightweight and sufficient for our needs, avoiding the necessity of adding `zod` schema parsing strictly for environment variables.

