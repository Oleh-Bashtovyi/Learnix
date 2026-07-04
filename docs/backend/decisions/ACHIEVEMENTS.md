# Learnix — ADR: Achievements

> Covers Phase 10: achievement system (unlock logic, progress tracking, notifications).

## Endpoints summary

### Achievements

| HTTP Method | Endpoint | Description | Rate Limit | Auth Required |
|---|---|---|---|---|
| `GET` | `/api/achievements/me` | Fetch user's achievements and progress counters | Default | Yes |
| `POST` | `/api/achievements/{id}/seen` | Mark an achievement as seen by the user | Default | Yes |

### Notifications

| HTTP Method | Endpoint | Description | Rate Limit | Auth Required |
|---|---|---|---|---|
| `GET` | `/api/notifications` | Fetch user's notifications history | Default | Yes |
| `GET` | `/api/notifications/unread-count` | Get total unread notifications count | Default | Yes |
| `POST` | `/api/notifications/{id}/read` | Mark specific notification as read | Default | Yes |
| `POST` | `/api/notifications/read-all` | Mark all user's notifications as read | Default | Yes |
| `POST` | `/api/notifications/read-by-type?type=...` | Mark all notifications of a specific type as read | Default | Yes |

---

## ADR-ACHIEVEMENT-001: Outbox-Driven Evaluation over Inline Handler Logic

**Decision:** Achievement evaluation is not performed inside command handlers. Instead, domain events (`LessonCompletedDomainEvent`, `EnrollmentCompletedDomainEvent`, `TestSubmittedDomainEvent`, `UserProfileUpdatedDomainEvent`) are caught by lightweight Infrastructure MediatR handlers that write a single outbox message. The background `OutboxProcessorService` then dispatches the message to `IAchievementEvaluator`, which contains all achievement logic.

**Why:**
- Command handlers have a single responsibility: executing the requested state change. Mixing achievement checks into, say, `MarkLessonCompletedCommandHandler` couples two unrelated concerns and grows the handler with every new achievement added.
- Achievement evaluation can be slow (multiple aggregate queries). Offloading it to a background processor keeps the command latency unaffected by however many achievements need to be checked.
- The outbox gives at-least-once delivery for free. If the server crashes between writing a lesson progress row and evaluating achievements, the outbox message is replayed on the next poll cycle — no achievement is silently skipped.
- Adding a new achievement type requires no changes to any command handler. A new threshold check is added to the evaluator or a new event handler writes to the outbox; existing code is untouched.

**Rejected alternatives:**
- Inline `IAchievementEvaluator` call inside each command handler — simplest to wire, but bloats every handler, ties achievement latency to command latency, and breaks the single-responsibility principle.
- Dedicated MediatR `INotificationHandler<T>` per achievement, running synchronously after `SaveChangesAsync` — avoids an outbox but loses durability: a crash between the primary save and the notification processing silently drops the achievement. Also mixes infrastructure concerns into the Application layer if the handlers need DB access.
- MassTransit / RabbitMQ consumer — correct at scale, but a full message broker is disproportionate for a pet project. The existing outbox infrastructure achieves the same durability without a new dependency.

---

## ADR-ACHIEVEMENT-002: Idempotent SET Semantics for Progress Counters

**Decision:** `UserAchievementProgress` stores counters (`LessonsCompleted`, `CoursesCompleted`, `DistinctCategoriesCompleted`). Each evaluator method recomputes the counter from an aggregate query (`COUNT(*)`) and calls a `SetX(value)` method — it never increments or decrements. The same applies to threshold checks: before unlocking, the current count is compared against a threshold; if already met, the check simply returns.

**Why:**
- The outbox processor delivers messages at-least-once: if processing crashes after `SaveChangesAsync` but before the outbox message is marked processed, the same message is retried. An increment-based counter would be inflated by the duplicate processing; a SET-based counter is a no-op on replay.
- The `HasAchievementAsync` guard before inserting a `UserAchievement` row is also idempotent: a duplicate evaluation that passes all threshold checks still will not create a duplicate row. The unique index `(UserId, Code)` is the final safety net.
- Recomputing from aggregate queries means the counter always reflects the true database state, even if rows were deleted or a migration back-fills existing data.

**Rejected alternatives:**
- Increment on each event, decrement on undo — simpler per-event logic but fundamentally non-idempotent. Would require a separate dedup mechanism (e.g., storing processed event IDs) to prevent double-counting, which is more complexity than the SET approach.
- No stored counters; derive all thresholds from aggregate queries at evaluation time — correct and storage-free, but requires the same aggregate query at every evaluation. The stored counter eliminates redundant queries and makes the achievement progress visible to the API without re-deriving.

---

## ADR-ACHIEVEMENT-003: Dual-Layer Deduplication for Achievement Unlocks

**Decision:** An achievement unlock is protected by two independent guards:

1. **Application-level:** `HasAchievementAsync(userId, code)` is called before `UserAchievement.Unlock(...)`. If the row already exists, the evaluator returns early without inserting or raising a domain event.
2. **Database-level:** A unique index on `(UserId, Code)` on the `UserAchievements` table causes any duplicate insert to throw a database exception, which the outbox processor logs and retries via exponential back-off.

**Why:**
- The application-level check handles the common case (duplicate outbox delivery) cheaply — one `AnyAsync` query that short-circuits before creating the entity or raising a domain event.
- The database-level index is the invariant enforcer. It protects against race conditions (two outbox workers processing the same message concurrently) and against bugs where the application guard is bypassed. Unlike an application check, the index is an atomic constraint.
- Together, the two layers mean no achievement can be awarded twice without requiring distributed locking or complex event deduplication tables.

**Rejected alternatives:**
- Database unique index only (no application guard) — correct but wasteful: the evaluator would always attempt the insert, always fail with a unique violation for duplicates, and the processor would log spurious errors.
- Application guard only (no database index) — removes the atomic invariant. Two concurrent workers can both pass `HasAchievementAsync` before either has committed, resulting in two rows for the same achievement.

---

## ADR-ACHIEVEMENT-004: Frontend Icon Mapping by Stable Code String

**Decision:** Each achievement has a stable string code (e.g., `FIRST_LESSON`, `SPEED_DEMON`) stored as a `varchar(64)` in the database. The backend returns the code in every API response. The frontend maps each code to an SVG asset at build time. No icon path or icon metadata is stored in the database.

**Why:**
- Achievement codes are enum-like identifiers defined by the product, not user-generated content. They change only when a developer intentionally adds a new achievement. Storing an icon path in the database adds write overhead and an admin UI for zero runtime flexibility — no admin needs to change what `SPEED_DEMON` looks like.
- A frontend code-to-SVG map is a build-time asset that ships with the app. It is type-safe (TypeScript exhaustive check catches missing icons for new codes) and zero-latency (no extra API round trip for icon resolution).
- If icon assets are ever redesigned, the change is a frontend PR — no migration, no seed data update.

**Rejected alternatives:**
- Store icon URL or blob path in the database, configurable by admin — adds a `BlobStorageService` dependency to achievement seeding, requires an admin UI and upload flow, and solves a problem that does not currently exist. Can be added later if the product requires it.
- Return icon URLs from the API (backend resolves CDN paths) — couples icon asset management to the backend deployment. The frontend already owns its static assets.

---

## ADR-ACHIEVEMENT-005: Denormalized `UserAchievementProgress` Aggregate Row

**Decision:** A `UserAchievementProgress` table stores one row per user with pre-computed counters: `LessonsCompleted`, `CoursesCompleted`, `DistinctCategoriesCompleted`, `ProfileCompleted`. These counters are updated by the evaluator and exposed directly by the `GET /api/achievements/me` endpoint.

**Why:**
- `GET /api/achievements/me` must return progress counters alongside unlocked achievements. Deriving `LessonsCompleted` by counting `LessonProgress` rows at query time is correct but adds a join across a potentially large table for every progress fetch.
- The evaluator already computes these counts via aggregate queries (to maintain idempotent SET semantics). Writing the result to `UserAchievementProgress` is one additional row write; reading it later is a primary-key lookup.
- The row is created lazily (`GetOrCreateAsync`) on first evaluation, so there is no bootstrap seeding required for existing users.

**Rejected alternatives:**
- Re-derive from source tables on every `GET /api/achievements/me` call — no extra table, but adds N aggregate queries per request. For a dashboard that shows multiple counters, this becomes several queries that could be one row lookup.
- Store only `UserAchievement` rows (unlocked achievements) and compute progress from thresholds on the client — hides partial progress (e.g., "47 / 50 lessons") since a user below a threshold has no row. The progress counters are needed to render progress bars regardless of whether a threshold is reached.

---

## ADR-ACHIEVEMENT-006: Composite Primary Key on `UserCompletedCategory`

**Decision:** `UserCompletedCategory` uses a composite primary key `(UserId, CategoryId)`. There is no surrogate `Id` column. `AddIfMissingAsync` checks `AnyAsync` before inserting; the composite PK enforces the uniqueness invariant at the database level.

**Why:**
- The POLYMATH achievement requires tracking how many distinct course categories a student has completed. The natural identity of this fact is the `(UserId, CategoryId)` pair — there is no meaningful surrogate key to add.
- The composite PK doubles as the uniqueness constraint. A student completing a second course in the same category does not create a new row; the duplicate insert is either blocked by the application-level `AnyAsync` check or rejected by the PK constraint.
- EF Core supports composite PKs via `HasKey(e => new { e.UserId, e.CategoryId })` with no extra configuration.

**Rejected alternatives:**
- Surrogate `Id` + unique index on `(UserId, CategoryId)` — redundant. The pair is already the natural key; adding a surrogate serves no purpose here.
- Store the category set as a JSON array on `UserAchievementProgress` — eliminates the join table but makes querying and deduplication much more complex, and JSON arrays are not indexable without generated columns.

---

## ADR-ACHIEVEMENT-007: `NotifyAchievementUnlocked` via Outbox → SignalR & Persistent Notifications

> **Updated:** Originally written as a Phase-2 placeholder; both SignalR push and persistent Notifications are now implemented.

**Decision:** When a `UserAchievement` is created, `AchievementUnlockedDomainEvent` is raised. An Infrastructure MediatR handler (`AchievementUnlockedNotificationHandler`) writes a `NotifyAchievementUnlocked` outbox message. The `OutboxProcessorService` processes this message by doing two things:
1. It calls `IAchievementNotifier.NotifyAsync`, which pushes a real-time `AchievementUnlocked` event to the user's SignalR group via `NotificationsHub`.
2. It calls `INotificationSender.SendAsync` to save a persistent `Notification` entity (with type `AchievementEarned`) to the database.

**Why:**
- Going through the outbox (rather than calling the notifiers directly in the domain event handler) provides at-least-once delivery: if the server crashes after the achievement row is saved but before the notification fires, the outbox message is retried on the next poll cycle.
- The dual notification approach (SignalR + DB) ensures that active users get immediate UI feedback (a toast via the hub), while offline users will see the missed event in their persistent notifications list when they log in later. 
- `IAchievementNotifier` and `INotificationSender` are injected into `OutboxProcessorService` from the DI scope, decoupling the domain from real-time and persistent notification implementation details.
- The frontend `useNotificationsHub` hook handles the SignalR push by showing a toast and invalidating `queryKeys.achievements.mine()`, keeping the UI perfectly in sync.

**Rejected alternatives:**
- Call `IAchievementNotifier` or `INotificationSender` directly inside the domain event handler (in-process, no outbox) — loses at-least-once delivery. If the process crashes between `SaveChangesAsync` and the notification dispatch, the push is lost forever.
- SignalR only (no persistent notification) — offline users would never know they earned an achievement while away from the platform (e.g., if a background admin action or delayed outbox execution triggered it).
- Persistent notification only (no SignalR) — active users would not get the instant "wow" factor of a toast popping up right as they complete a course or lesson.

**Latency update (see DECISIONS_INFRA.md ADR-020):** The polling-only outbox had a worst-case 20s delay for achievement chains (evaluate → notify = 2 polling cycles). This was resolved by a combination of PostgreSQL `LISTEN/NOTIFY` push notifications (to wake the processor immediately on new events) and an **in-process self-signaling loop**. When the processor evaluates an achievement and inserts a new `NotifyAchievementUnlocked` message, it immediately loops back to process it without waiting for a new DB notification or polling interval. Achievement notification latency is now effectively instantaneous (< 100ms).

---

## Reference: Achievement Catalogue

All 10 achievements are static — defined at build time in `AchievementCodes.cs` (backend) and `ACHIEVEMENT_META` (frontend). Changing a code string is a breaking change for already-unlocked rows in `UserAchievements`.

| Code | Name | Unlock condition |
|---|---|---|
| `FIRST_LESSON` | First Step | Complete any 1 lesson. |
| `LESSONS_50` | Eager Learner | Reach 50 completed lessons in total. |
| `LESSONS_200` | Knowledge Seeker | Reach 200 completed lessons in total. |
| `LESSONS_500` | Scholar | Reach 500 completed lessons in total. |
| `FIRST_COURSE` | Course Graduate | Finish 1 course (enrollment status → Completed). |
| `COURSES_3` | Triple Crown | Finish 3 courses in total. |
| `COURSES_5` | Dedicated Student | Finish 5 courses in total. |
| `SPEED_DEMON` | Speed Demon | Pass a test of ≥ 20 questions in under 5 minutes. |
| `POLYMATH` | Polymath | Complete courses in ≥ 3 distinct categories. |
| `PROFILE_COMPLETE` | All Set | Fill in first name, last name, bio, and set a profile photo. |

**Trigger mapping** (which evaluator method handles each group):

- `OnLessonCompletedAsync` — `FIRST_LESSON`, `LESSONS_50`, `LESSONS_200`, `LESSONS_500`
- `OnEnrollmentCompletedAsync` — `FIRST_COURSE`, `COURSES_3`, `COURSES_5`, `POLYMATH`
- `OnTestSubmittedAsync` — `SPEED_DEMON` (only on a passed attempt)
- `OnProfileChangedAsync` — `PROFILE_COMPLETE`
