# Learnix — Frontend Architecture Decision Records (AI Chat)

> Format: Decision → Why → Alternatives.
> The backend side (provider abstraction, MongoDB sessions, outbox-free request/response flow) is in
> `docs/backend/decisions/features/CHAT.md`.

---

## ADR-FRONT-CHAT-001: Fetch-Based SSE Streaming, Not SignalR

**Context:** Realtime push already has one shared channel — the SignalR notifications hub
(ADR-FRONT-API-004) — but the AI assistant is a single request/response conversation with one client,
not a fan-out event a hub is built for.

**Decision:**
`streamAiMessage` (`src/api/aiChat.api.ts`) posts the user's message to `/ai-chat/{scope}/messages`
using Axios' `fetch` adapter with `responseType: 'stream'`, then manually parses the raw byte stream
into SSE `event:`/`data:` blocks and yields them as a typed async generator (`text_delta`,
`tool_use_start`, `tool_use_end`, `message_end`, `error`). `useAiChat` (`hooks/realtime/useAiChat.ts`)
consumes the generator, appending each `text_delta` to a streaming buffer and committing the finished
text to the message list on `message_end`.

**Why:**
- The hub pattern (ADR-FRONT-API-004) exists to fan the same event out to whichever client cares; a chat
  reply only ever matters to the one tab that asked for it, so a hub connection buys nothing here.
- `EventSource` (the browser's native SSE client) can't send a POST body or an `Authorization` header,
  and this request needs both — the fetch-based stream can.

**Alternatives:**
- Route AI replies through the existing notifications hub — rejected for the same reason
  ADR-FRONT-API-004 keeps it off: a push channel for a request the client itself just made.
- `EventSource` — rejected: no POST body, no custom headers.

**Consequences:**
- A new event type from the backend needs a case added to `streamAiMessage`'s block parser is
  unaffected (it's already type-agnostic); the new type needs a branch in `useAiChat`'s consuming loop.

---

## ADR-FRONT-CHAT-002: One Hook Owns a Scope-Keyed Session, Independent of Panel Visibility

**Context:** The chat widget can be closed and reopened without losing the conversation, but the course
player keeps the same hook mounted while the student moves between courses — and course A's tutor
conversation must not bleed into course B's.

**Decision:**
`useAiChat(isOpen, scope, lessonId)` is called from a component that outlives the chat surface itself,
keyed by `ChatScope` (`platform`, or `{ courseId }` for a course tutor). Session history loads via a
React Query call gated on `isOpen && !sessionLoaded` with `staleTime: Infinity`, then lives in local
component state, not the query cache, because streaming deltas need to mutate it far more often than a
cache write is meant for. Closing the panel (`isOpen: false`) leaves the loaded messages alone;
detecting that `scope`'s derived key has changed (comparing against the previous render) clears
messages, the in-flight stream, and `sessionLoaded` — a new scope is a new conversation.

**Why:**
- Reopening the widget shouldn't re-fetch or lose a conversation that's already loaded once.
- Switching scopes is a hard boundary: nothing about course A's history belongs in course B's session,
  so it has to be dropped, not merged.

**Alternatives:**
- Store the message list in the React Query cache like ordinary server state — rejected: streaming
  deltas would mean firing a cache write per token, which is not what the cache is for.

**Consequences:**
- A new chat scope (beyond platform/course) needs a case in `keyOf()`'s key derivation, not new
  reset logic — the reset already triggers on any key change.

---

## ADR-FRONT-CHAT-003: Single Floating Widget Mounted at the App Root

**Context:** The assistant should be reachable from nearly any page, but the dedicated Messages pages
already render a full conversation UI — a floating chat bubble on top of that would be redundant chrome
fighting for the same screen space.

**Decision:**
One `<AiChatWidget />` is mounted once, near the app root, gated on a signed-in `user` and hidden on a
`HIDDEN_ON` path-prefix list (the student/instructor/admin Messages routes). Its open/closed state
lives in `ui.store.isChatOpen` (ADR-FRONT-API-002), so anything in the app can toggle it (e.g. a "chat
with your tutor" call to action), and it force-closes on every route change.

**Why:**
- One instance means one place owning the floating-button chrome and open/close state, instead of every
  page that wants the assistant re-mounting its own copy.
- Hiding it on Messages avoids two chat surfaces competing for the same corner of the screen.
- Closing on navigation matches how a floating panel is expected to behave — it shouldn't survive onto
  a page the user didn't open it from.

**Alternatives:**
- Mount the widget per-page that wants it — rejected: duplicates the toggle button and open state
  wherever it's needed.

**Consequences:**
- A new full-page surface that already has its own chat UI (if one is ever added) is added to
  `HIDDEN_ON`, the same way Messages is today.

---

## ADR-FRONT-CHAT-004: Provider Availability Surfaced as an Independent Polled Query

**Context:** Quota or an outage on the AI provider is a fact about the provider, not about any one
conversation — the composer needs to know before the user types into a channel that's already down, and
needs to find out promptly if it goes down mid-conversation.

**Decision:**
`/ai-chat/status` is its own React Query, shared across every scope (not keyed by one), with a 30s
`staleTime`; its `refetchInterval` only turns on (60s) while the last known status is unavailable.
Whenever a stream ends with an `error` event, or the initial POST itself throws (the 503 the API
answers with when it already knows the provider is down), the handler invalidates this status query so
the UI's "unavailable" state reflects reality as soon as either side notices it.

**Why:**
- Sharing the query across scopes matches reality: a provider outage affects the platform assistant and
  every course tutor identically, so there's nothing to gain from tracking it per scope.
- Polling only while unavailable keeps the steady-state (available) case from polling at all.

**Alternatives:**
- Check availability only just-in-time on send, with no standing status query — rejected: a composer
  that only fails after the user has already typed and sent a message is worse UX than disabling it
  upfront.

**Consequences:**
- Any new failure path that implies an outage should invalidate `queryKeys.aiChat.status` (via
  `refreshStatus`) rather than only surfacing a one-off toast, so the composer's disabled state stays
  accurate for the rest of the session.
