# Learnix — ADR: Student ↔ Instructor Messaging

> Covers F-26 (chat UI), F-28 (notification bell), and the backend endpoints/hub.

> **Endpoints:** see [`docs/backend/ENDPOINTS.md`](../../ENDPOINTS.md) — one generated table for
> the whole API, verified against the controllers in CI. An ADR records a decision; it is not the
> place to keep a copy of the API surface.

---
## ADR-BACK-MSG-001: PostgreSQL over MongoDB for Course Conversations

**Decision:** `CourseConversation` and `CourseMessage` are EF Core entities stored in PostgreSQL, not MongoDB documents.

**Why:**
- Conversations have hard FK relationships to `Course`, `User` (student), and `User` (instructor) — all already in PostgreSQL. Storing the data in MongoDB would require cross-database fetches every time the conversation list is rendered.
- A unique constraint `(CourseId, StudentId)` enforces exactly one thread per student per course at the DB level — trivial in PostgreSQL, awkward to enforce in MongoDB.
- Pagination of messages is a simple `ORDER BY CreatedAt OFFSET/LIMIT` query — no document size concerns.
- The EF Core + Ardalis Specification pattern used everywhere else in the codebase applies without any adaption.

**Rejected alternatives:**
- MongoDB — used for AI chat sessions because those are user-scoped documents with no FK joins. Student-instructor chat is inherently relational; using MongoDB would trade one schema migration for permanent cross-database query impedance.

---

## ADR-BACK-MSG-002: REST for History + SignalR for Real-Time Delivery

**Decision:** Message history is fetched via REST (`GET /api/messages/conversations/{id}/messages`) and cached by TanStack Query. New messages arrive in real-time via SignalR `ReceiveMessage` push, which triggers a React Query cache invalidation.

**Why:**
- REST history is cacheable and paginatable — TanStack Query handles stale-while-revalidate, refetch-on-window-focus, and `skip/take` pagination naturally.
- SignalR delivery is ephemeral — perfect for "new message arrived" events that should not be stored client-side.
- Separating concerns keeps the SignalR payload minimal (just the new message DTO) rather than having the hub stream full conversation history.

**Rejected alternatives:**
- SignalR-only — hub sends full history on connect. Loses React Query caching and makes offline/reconnect handling complex.
- Polling only — adds 30-second latency for message delivery, poor UX for a chat feature.

---

## ADR-BACK-MSG-003: 1-on-1 Conversation per Student per Course

**Decision:** Each enrolled student gets exactly one private thread with the instructor, scoped to the course. Enforced via `UNIQUE(CourseId, StudentId)` index.

**Why:**
- Private threads match the tutoring/support mental model — students ask questions privately.
- Unique constraint prevents duplicate threads via race conditions.
- `GetOrStartConversation` query creates the thread on first student interaction with a safe get-or-create pattern.

**Rejected alternatives:**
- Group Q&A per course — all students see each other's questions. More appropriate for a forum feature (separate scope), not for private support.

---

## ADR-BACK-MSG-004: Unread Count via Denormalized Fields on Conversation

**Decision:** `CourseConversation` has `StudentUnreadCount` and `InstructorUnreadCount` integer fields. These are incremented by `AddMessage()` and reset to 0 by `MarkReadByStudent()` / `MarkReadByInstructor()`.

**Why:**
- Getting the total unread count for the notification bell requires only `SUM(InstructorUnreadCount) WHERE InstructorId = userId` — one indexed aggregation query, no message table scan.
- Increments and resets happen atomically in the same `SaveChangesAsync` call as the conversation update — no separate "read receipt" table needed.

**Rejected alternatives:**
- Per-message `IsRead` flag — requires a `COUNT(messages WHERE IsRead = false AND recipientId = userId)` scan across the entire messages table. Expensive as message volume grows.
- Separate `UnreadCount` table — adds a third table and complicates the transaction boundary.

---

## ADR-BACK-MSG-005: `IChatNotifier` Abstraction for SignalR Push

**Decision:** `IChatNotifier` lives in the Application layer with two methods: `NotifyNewMessageAsync` (pushes `ReceiveMessage` to the recipient's SignalR group) and `NotifyUnreadCountChangedAsync` (pushes `UnreadCountChanged` to the affected user). `SignalRChatNotifier` implements it in Infrastructure.

**Why:**
- Same pattern as `IAchievementNotifier` / `SignalRAchievementNotifier` — consistent with the codebase.
- Application layer handlers call `IChatNotifier` directly after `SaveChangesAsync()` — no domain event needed because the handler already has all context (who sent, who receives, updated counts).
- Testable: handlers can be unit-tested by mocking `IChatNotifier`.

**Rejected alternatives:**
- Domain events for messaging — meaningful for achievements because detection is complex (many conditions evaluated asynchronously). For messaging, the handler already knows the recipient and unread count immediately — domain event overhead adds no value.

---

## ADR-BACK-MSG-006: One `NotificationsHub` for every real-time event, not one hub per domain

**Decision:** All real-time push — messages, achievement unlocks, certificates, unread counts, and the
generic in-app feed — goes through a single `NotificationsHub : Hub<INotificationsHubClient>` at
`/hubs/notifications`. The frontend opens **one** WebSocket connection per session, subscribed through a
single `useNotificationsHub` hook. `INotificationsHubClient` defines one typed method per notification
kind — `ReceiveMessage`, `UnreadCountChanged`, `AchievementUnlocked`, `CertificateIssued`, and
`NotificationReceived` for the generic feed (ADR-BACK-NOTIF-001) — and client-side handler routing is a
plain `connection.on('EventName', handler)`; a sixth method for a future notification kind does not
change that.

**Why one hub, not one per domain — the design this replaced:** messaging originally ran its own
`ChatHub`, separate from an `AchievementsHub` for unlocks, on the reasoning that the two push different
payload types, that mixing them risked a growing "God hub," and that separate hubs scale and deploy
independently. That held up only on paper: both hubs authenticated the same way, grouped connections by
the same `user-{userId}` pattern, and carried the same `[Authorize]` attribute — there was nothing about
them that was actually separate. "Independent deployability" is an argument for a service boundary, and
this was one API process on one Azure Container App; two hubs bought each client a second WebSocket
handshake for a distinction the backend never acted on. What actually keeps a shared hub from becoming a
God hub is `INotificationsHubClient`'s one-method-per-kind shape, not which hub the method lives on — so
merging the two lost nothing the split was protecting.

**Why it doesn't recur as the notification surface grows:** certificates and the generic in-app feed
were added after messaging and achievements, and each became one more method on the same hub rather than
a third and fourth hub. The alternative — a hub per domain, restated at a larger N — gets worse precisely
where the original argument for it was already weak.

**Consequences:**
- `ChatHub` and `AchievementsHub` are gone; `useChatHub`/`useAchievementsHub` were replaced by
  `useNotificationsHub`.
- The independent-deployability argument becomes real again only if a domain is ever extracted into its
  own service — that is the condition to watch, not a reason to revisit this today.
