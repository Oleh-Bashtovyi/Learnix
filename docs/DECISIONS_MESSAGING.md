# Learnix — ADR: Student ↔ Instructor Messaging

> Format: decision → why → rejected alternatives.
> Covers F-26 (chat UI), F-28 (notification bell), and the backend endpoints/hub.

---

## ADR-MSG-001: PostgreSQL over MongoDB for Course Conversations

**Decision:** `CourseConversation` and `CourseMessage` are EF Core entities stored in PostgreSQL, not MongoDB documents.

**Why:**
- Conversations have hard FK relationships to `Course`, `User` (student), and `User` (instructor) — all already in PostgreSQL. Storing the data in MongoDB would require cross-database fetches every time the conversation list is rendered.
- A unique constraint `(CourseId, StudentId)` enforces exactly one thread per student per course at the DB level — trivial in PostgreSQL, awkward to enforce in MongoDB.
- Pagination of messages is a simple `ORDER BY CreatedAt OFFSET/LIMIT` query — no document size concerns.
- The EF Core + Ardalis Specification pattern used everywhere else in the codebase applies without any adaption.

**Rejected alternatives:**
- MongoDB — used for AI chat sessions because those are user-scoped documents with no FK joins. Student-instructor chat is inherently relational; using MongoDB would trade one schema migration for permanent cross-database query impedance.

---

## ADR-MSG-002: Separate `ChatHub` from `AchievementsHub`

**Decision:** A dedicated `ChatHub : Hub<IChatHubClient>` at `/hubs/chat` handles messaging events. It is separate from `AchievementsHub`.

**Why:**
- Clean separation of concerns: achievements push is a one-way notification (server → client only). Chat push is also server → client, but carries different payload types and has different lifecycle requirements.
- Separate hubs are independently deployable and scale independently under Azure SignalR Service.
- Avoids a growing "God hub" with mixed notification types.

**Rejected alternatives:**
- Unified `NotificationsHub` handling both achievements and messages — simpler at first, but merges unrelated domains and complicates client-side handler routing.

---

## ADR-MSG-003: REST for History + SignalR for Real-Time Delivery

**Decision:** Message history is fetched via REST (`GET /api/messages/conversations/{id}/messages`) and cached by TanStack Query. New messages arrive in real-time via SignalR `ReceiveMessage` push, which triggers a React Query cache invalidation.

**Why:**
- REST history is cacheable and paginatable — TanStack Query handles stale-while-revalidate, refetch-on-window-focus, and `skip/take` pagination naturally.
- SignalR delivery is ephemeral — perfect for "new message arrived" events that should not be stored client-side.
- Separating concerns keeps the SignalR payload minimal (just the new message DTO) rather than having the hub stream full conversation history.

**Rejected alternatives:**
- SignalR-only — hub sends full history on connect. Loses React Query caching and makes offline/reconnect handling complex.
- Polling only — adds 30-second latency for message delivery, poor UX for a chat feature.

---

## ADR-MSG-004: 1-on-1 Conversation per Student per Course

**Decision:** Each enrolled student gets exactly one private thread with the instructor, scoped to the course. Enforced via `UNIQUE(CourseId, StudentId)` index.

**Why:**
- Private threads match the tutoring/support mental model — students ask questions privately.
- Unique constraint prevents duplicate threads via race conditions.
- `GetOrStartConversation` query creates the thread on first student interaction with a safe get-or-create pattern.

**Rejected alternatives:**
- Group Q&A per course — all students see each other's questions. More appropriate for a forum feature (separate scope), not for private support.

---

## ADR-MSG-005: Unread Count via Denormalized Fields on Conversation

**Decision:** `CourseConversation` has `StudentUnreadCount` and `InstructorUnreadCount` integer fields. These are incremented by `AddMessage()` and reset to 0 by `MarkReadByStudent()` / `MarkReadByInstructor()`.

**Why:**
- Getting the total unread count for the notification bell requires only `SUM(InstructorUnreadCount) WHERE InstructorId = userId` — one indexed aggregation query, no message table scan.
- Increments and resets happen atomically in the same `SaveChangesAsync` call as the conversation update — no separate "read receipt" table needed.

**Rejected alternatives:**
- Per-message `IsRead` flag — requires a `COUNT(messages WHERE IsRead = false AND recipientId = userId)` scan across the entire messages table. Expensive as message volume grows.
- Separate `UnreadCount` table — adds a third table and complicates the transaction boundary.

---

## ADR-MSG-006: `IChatNotifier` Abstraction for SignalR Push

**Decision:** `IChatNotifier` lives in the Application layer with two methods: `NotifyNewMessageAsync` (pushes `ReceiveMessage` to the recipient's SignalR group) and `NotifyUnreadCountChangedAsync` (pushes `UnreadCountChanged` to the affected user). `SignalRChatNotifier` implements it in Infrastructure.

**Why:**
- Same pattern as `IAchievementNotifier` / `SignalRAchievementNotifier` — consistent with the codebase.
- Application layer handlers call `IChatNotifier` directly after `SaveChangesAsync()` — no domain event needed because the handler already has all context (who sent, who receives, updated counts).
- Testable: handlers can be unit-tested by mocking `IChatNotifier`.

**Rejected alternatives:**
- Domain events for messaging — meaningful for achievements because detection is complex (many conditions evaluated asynchronously). For messaging, the handler already knows the recipient and unread count immediately — domain event overhead adds no value.
