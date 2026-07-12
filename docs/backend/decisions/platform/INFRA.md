# Learnix — ADR: Infrastructure

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where architectural decisions were made.

Related files: [ARCHITECTURE.md](ARCHITECTURE.md) · [AUTH.md](AUTH.md) · [DOMAIN.md](DOMAIN.md) · [MIGRATIONS.md](MIGRATIONS.md)

## Status Convention

When a decision is revised, the old ADR is marked `Superseded by ADR-XXX` and the new one `Supersedes ADR-YYY` — the history of thought is worth keeping.

When the mechanism an ADR describes no longer exists at all, the ADR is **removed** rather than kept as a tombstone: a reader looking for how the system works should not have to first work out which half of the file is fiction. The rejected alternative lives on in the ADR that replaced it — that is where "why not this?" belongs — and the full text stays in git history.

Numbers are never reused, so gaps in the sequence are expected. `ADR-BACK-INFRA-006` (auto-migrations on API startup) and `ADR-BACK-INFRA-009` (seed assets embedded in `Learnix.Infrastructure`) were removed this way: migrations and seeding no longer live in this layer at all. See [MIGRATIONS.md](MIGRATIONS.md).

---

## ADR-BACK-INFRA-001: PostgreSQL + MongoDB (polyglot persistence)

**Decision:** Core relational data in PostgreSQL, unstructured data — in MongoDB.

**Why:**
- Most data (Users, Courses, Enrollments, Payments) — strictly relational, requiring transactions and FK constraints.
- A chat session is an append-only list of messages of unbounded length, read as a whole and joined with nothing. That is a document, and it is the only thing here that is.

**What is in MongoDB:** exactly one collection — `chat_sessions` (`MongoDbContext`).

Reviews were once planned for MongoDB on a "flexible schema" argument. They are in PostgreSQL, and deliberately so: writing a review updates `Course.AverageRating` and `ReviewsCount` in the same transaction, and a second database cannot join that transaction (ADR-BACK-REVIEW-001).

**Alternatives:**
- Everything in PostgreSQL (JSONB for chats) — possible, but a session is rewritten on every turn and grows without bound; the document store earns its place on that one collection.
- Everything in MongoDB — loss of referential integrity for critical data (payments, enrollments).

---

## ADR-BACK-INFRA-002: Redis distributed cache — ICacheable<TValue> + MediatR pipeline behavior

**Decision:** Queries implementing `ICacheable<TValue>` are automatically cached in Redis via `CachingBehavior<TRequest, TValue>`. Commands that mutate cached data explicitly invalidate the corresponding keys after `SaveChangesAsync`.

---

### Implementation

**Interface:**
```csharp
public interface ICacheable<TValue>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
```

**Pipeline behavior** implements `IPipelineBehavior<TRequest, Result<TValue>>`, where `TValue` is the second generic parameter. MediatR closes the type automatically: for `GetAllCategoriesQuery : ICacheable<IReadOnlyList<CategoryListItemDto>>` MediatR infers `TValue = IReadOnlyList<CategoryListItemDto>`. No reflection is used — `response.Value` and `Result.Ok(value)` are strongly typed at compile time.

**Serialization:** Only `Value` from `Result<T>` is cached, not the entire Result wrapper. `FluentResults.Result<T>` doesn't support JSON roundtrip (private setter on `Value`). `System.Text.Json` serializes the payload directly, deserializes it back, and the behavior wraps it in `Result.Ok(value)`.

**Invalidation:** In command handlers, after `SaveChangesAsync`, `IDistributedCache.RemoveAsync(key)` is called. `IDistributedCache` is an official Microsoft abstraction (not an infrastructure detail), so it lives in the Application layer alongside handlers.

---

### Which queries are cached and why

| Query | Key | TTL | Why |
|---|---|---|---|
| `GetAllCategoriesQuery` | `categories:all` | 24 h | The category list changes only through admin actions. Read every time the catalog and filters are opened. Longest TTL — lowest churn. |
| `GetFeaturedCoursesQuery` | `courses:featured` | 30 m | Selection of popular courses — expensive JOIN with sorting by enrollments/rating. The query is identical for all users (public, without per-user context). |
| `GetCourseByIdQuery` | `course:{id}` | 10 m | The course details page is read heavily by students before enrolling. Includes `AverageRating` and `ReviewsCount` — modified by every review. Explicit invalidation upon course and review changes. |
| `GetPublicCoursesQuery` | `courses:public:{all 8 parameters}` | 5 m | The catalog is the most heavily loaded endpoint (search + filters + sort + pagination). Unique key for each parameter combination — impossible to pattern invalidate via `IDistributedCache` without directly depending on `IConnectionMultiplexer`. A short TTL compensates for the lack of explicit invalidation. |

**What is intentionally NOT cached:**
- Per-user queries (`GetMyProfile`, `GetMyEnrollments`, `GetMyAchievements`) — each user has their own state, frequent mutations, the key would include userId → low probability of a cache hit for a specific query.
- Admin queries — low traffic, does not impact performance.
- Real-time data (chat, SignalR notifications) — always up to date.

---

### Invalidation — where and why

**Explicit invalidation of `course:{id}` + `courses:featured`** after every course mutation:
- `PublishCourse`, `UnpublishCourse` — course status changes, it appears or disappears from the catalog.
- `ArchiveCourse`, `UnarchiveCourse` — similarly.
- `UpdateCourseDetails` — title, price, cover, category change — all are present in the cached DTO.
- `DeleteCourse`, `AdminDeleteCourse`, `AdminRecoverCourse`, `AdminUnpublishCourse` — course entirely changes state.

**Explicit invalidation of `course:{id}`** upon review mutations:
- `CreateReview`, `UpdateReview`, `DeleteReview` — all three alter `AverageRating` and `ReviewsCount` on the `Course` entity. `CourseDetailDto` includes these fields — without invalidation, the cached page would display outdated ratings.

**Explicit invalidation of `categories:all`** upon any category change:
- `CreateCategory`, `UpdateCategory`, `DeleteCategory`, `SetCategoryImage`, `DeleteCategoryImage`

**`GetPublicCoursesQuery` — TTL only (5 m):** Since the key includes all 8 filter parameters (search, skip, take, categoryId, instructorId, sortBy, isFree, minRating), there could be hundreds of different combinations. Deleting by prefix `courses:public:*` requires `IConnectionMultiplexer.GetServer().Keys()` — an expensive O(N) operation on Redis. For a catalog, a 5-minute visibility delay after course publication is acceptable.

---

### Why Redis, and not IMemoryCache

`IDistributedCache` (Redis) — the only centralized store, `RemoveAsync(key)` is a Redis `DEL` command. When horizontally scaling (multiple API instances), invalidation on one instance automatically propagates to all: the next request on any instance will yield a cache miss and re-read from the DB.

`IMemoryCache` is per-process. Invalidation on instance A does not affect instances B and C, which continue serving stale data until their TTL expires. This is unacceptable for explicitly invalidated data (category list, course detail).

**Why:**
- Popular courses, category catalog — read-heavy, rarely change.
- Redis provides O(1) lookup and TTL out of the box.
- Pipeline behavior — caching is transparent to the handler, without boilerplate in every query.
- Distributed cache works correctly during scale-out.

**Alternatives:**
- `IMemoryCache` — simpler, but yields stale data with multiple instances. Rejected for public queries.
- Lazy invalidation (TTL only for everything) — simpler, but `CourseDetailDto` with rating would show outdated numbers for minutes after a review. Rejected for `course:{id}`.
- Response caching middleware (`[ResponseCache]`) — HTTP-level cache, doesn't control per-key invalidation. Rejected.

**Consequences:**
- `ICacheable<TValue>` in `Application/Common/Caching/`
- `CachingBehavior<TRequest, TValue>` in `Application/Common/Behaviors/`
- `CacheKeys` static class in `Application/Common/Constants/`
- Redis connection string: `ConnectionStrings:Redis` in `appsettings.json`
- Packages: `Microsoft.Extensions.Caching.StackExchangeRedis` (Infrastructure), `Microsoft.Extensions.Caching.Abstractions` (Application)

---

## ADR-BACK-INFRA-003: Audit fields via EF SaveChanges interceptor

**Decision:** CreatedAt / UpdatedAt are automatically set via the EF SaveChanges interceptor. Properties have a private set — the interceptor sets them through the EF ChangeTracker (without reflection, EF natively supports private setters).

**Why:**
- No handler will forget to set the date.
- The logic resides in one place, not scattered across all commands.
- Private set — nobody except the interceptor can accidentally change the value.

---

## ADR-BACK-INFRA-004: DbContext natively implements IUnitOfWork

**Decision:** `ApplicationDbContext` implements `IUnitOfWork`. There is no separate `UnitOfWork` class. DI: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>())` — resolves to the same scoped instance.

**Why:**
- A separate `UnitOfWork` class would merely delegate `SaveChangesAsync` to the DbContext — an unnecessary layer of indirection.
- The Application layer still only sees `IUnitOfWork`, not the DbContext — the abstraction is preserved.
- Fewer files, fewer DI registrations, fewer chances to mess up scopes.

**Alternatives:**
- A separate `UnitOfWork` class — the canonical approach, but adds a layer without functional value.

---

## ADR-BACK-INFRA-005: Outbox pattern (Schema & Background Worker)

**Decision:** The Outbox pattern is implemented to reliably execute background operations (confirm/delete blob, send email, evaluate achievements). Domain events are dispatched in-process by `DomainEventsInterceptor` from **`SavingChangesAsync` — before the INSERT/UPDATE runs**, not after it (ADR-BACK-INFRA-015). That ordering is the whole point: the `OutboxMessage` rows their handlers write land in the same transaction as the entity change, so either both commit or neither does. A handler consequently cannot query for the change that raised it — the row is not there yet.

**`OutboxMessage` entity:**
- `Id`, `Type` (e.g., `DeleteBlob`, `UnlockAchievement`), `Payload` (JSONB)
- `OccurredAt`, `ProcessedAt?`, `AttemptCount`, `LastAttemptAt?`, `LastError?`, `NextRetryAt?`
- Written by the domain event handler in the same EF transaction as the entity changes.

**Outbox worker (background `IHostedService`):**
- Reads `WHERE ProcessedAt IS NULL AND (NextRetryAt IS NULL OR NextRetryAt <= NOW())`
- Invokes `IOutboxMessageDispatcher.DispatchAsync(message)` which routes to a specific handler.
- Exponential backoff via `NextRetryAt` on errors.
- **See ADR-BACK-INFRA-008:** Dispatch mechanism optimized via PostgreSQL LISTEN/NOTIFY.

---

## ADR-BACK-INFRA-007: Background job scheduling — IHostedService vs Quartz.NET vs Hangfire

**Decision:** For background tasks, we use `BackgroundService` + `PeriodicTimer` (built into .NET). We will not introduce Quartz.NET or Hangfire until there is a specific need for their capabilities.

**Why IHostedService is sufficient for now:**
- All current background tasks are idempotent and safe to run on every replica (reconciliation, cleanup, seeding). Parallel execution on multiple instances does not lead to incorrect results.
- Zero additional dependencies — `BackgroundService` is part of `Microsoft.Extensions.Hosting`.
- The pattern is already utilized in the codebase (RefreshTokenCleanup, OutboxProcessor, etc.) — consistency outweighs premature flexibility.

**What Quartz.NET and Hangfire can do (and IHostedService cannot):**

| Capability | IHostedService | Quartz.NET | Hangfire |
|---|---|---|---|
| Distributed lock (singleton execution across replicas) | ❌ | ✅ (DB/Redis) | ✅ (DB) |
| Cron-expressions for scheduling | ❌ | ✅ | ✅ |
| Management UI | ❌ | ✅ | ✅ (built-in) |
| Job persistence (retry after crash) | ❌ | ✅ | ✅ |
| Fire-and-forget from web request | ❌ | ❌ | ✅ |
| Dependency footprint | 0 | Quartz + extensions | Hangfire.Core + storage |

**Key concept — Distributed Lock:**
If the API runs on 3 servers simultaneously (horizontal scaling), `IHostedService` will start the job on ALL 3 servers in parallel. Quartz.NET and Hangfire solve this via a distributed lock in a shared DB or Redis: only ONE instance executes the job, others wait or skip the tick. This is critical for tasks with side-effects (sending email, charging payments) — duplication is unacceptable.

**When to switch to Quartz.NET or Hangfire:**
- A job emerges that MUST run exactly once across all replicas (e.g., sending a monthly digest).
- A dashboard is required to monitor and manually retrigger jobs.
- The number of background jobs grows > ~5–6 and managing them via `AddHostedService` becomes cumbersome.
- Complex cron schedules are required (first Monday of the month, every workday at 9:00, etc.).

**Consequences of the current decision:**
- `CategoryCoursesCountReconciliationService`, `RefreshTokenCleanupHostedService`, and others run on every replica in parallel — this is acceptable because they are all idempotent.
- Upon introducing horizontal scaling (Phase Deploy) — audit all `IHostedService` instances to ensure they remain safe to run in parallel.
- Background tasks (emails, achievements) transitioned to the Outbox processor which correctly handles concurrency via database locks.

---

## ADR-BACK-INFRA-008: Outbox latency — PostgreSQL LISTEN/NOTIFY instead of polling-only

> Partially supersedes ADR-BACK-INFRA-005 regarding the "Outbox worker (background IHostedService)" — the message dispatch mechanism was changed from pure polling to push-first with a polling fallback.

**Context and problem:**

The initial Outbox implementation (ADR-BACK-INFRA-005) utilized pure polling: `OutboxProcessorService` with a `PeriodicTimer(10s)` executed a SELECT on the `OutboxMessages` table on every tick. This worked well for blob operations and emails, where a 10s latency was acceptable.

The issue became critical with the introduction of chained events in the achievement system (ADR-BACK-ACHIEVEMENT-001, ADR-BACK-ACHIEVEMENT-007):

```text
LessonCompleted → SaveChanges
    → DomainEventsInterceptor → outbox: EvaluateLessonCompleted
    → ⏳ up to 10s (polling)
    → AchievementEvaluator → UserAchievement.Unlock() → SaveChanges
        → DomainEventsInterceptor → outbox: NotifyAchievementUnlocked
        → ⏳ up to 10s more (polling)
        → SignalR push → toast in browser
```

Two polling cycles = **up to 20 seconds** from lesson completion to achievement notification. This is unacceptable for UX.

---

**Decision:** `OutboxProcessorService` now wakes up immediately following an INSERT into `OutboxMessages` utilizing PostgreSQL's native `LISTEN/NOTIFY` mechanism. The 10s interval polling remains as a fallback.

**How PostgreSQL LISTEN/NOTIFY works:**

PostgreSQL features a built-in lightweight pub/sub mechanism, distinct from replication and WAL. It operates at the session (connection) level:

1. **NOTIFY** — any transaction can execute `pg_notify('channel_name', 'optional_payload')`. The message is buffered and sent **only after COMMIT** of the transaction. If the transaction rolls back — the notification is not sent. This provides a key guarantee: the processor only receives a signal regarding committed data.

2. **LISTEN** — the client (`NpgsqlConnection`) registers on the channel. Thereafter, any `NOTIFY` on this channel from any connection is delivered as an event to all LISTEN-subscribers. PostgreSQL guarantees delivery to all active subscribers at the moment of COMMIT.

3. **Limitations:** If a subscriber is disconnected at the moment of NOTIFY — the message is lost. LISTEN/NOTIFY lacks persistence (unlike a message broker). That is precisely why polling remains as a fallback: even if the listener was disconnected, the processor will pick up the message on the next 10-second tick.

**Implementation Architecture (3 components):**

**1. PostgreSQL trigger (database layer):**

```sql
CREATE FUNCTION notify_outbox_insert() RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('outbox_new', '');
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_outbox_notify
  AFTER INSERT ON "OutboxMessages"
  FOR EACH STATEMENT EXECUTE FUNCTION notify_outbox_insert();
```

`FOR EACH STATEMENT` (not `FOR EACH ROW`) — if a single `SaveChanges` writes 5 outbox messages, the trigger fires once. The payload is empty — only the fact "there are new messages" is required; specific IDs are unnecessary because the processor executes its own filtered SELECT.

**2. `OutboxNotificationListener` (Infrastructure BackgroundService):**

A dedicated long-lived `NpgsqlConnection` (not pooled!) listens to the `outbox_new` channel:

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var cmd = new NpgsqlCommand("LISTEN outbox_new", connection);
await cmd.ExecuteNonQueryAsync(ct);

while (!ct.IsCancellationRequested)
    await connection.WaitAsync(ct);  // blocks until notification arrives
```

Why a dedicated connection: PostgreSQL's LISTEN state is bound to a specific session. Connection pooling (`NpgsqlDataSource`) returns the connection to the pool after use — losing the LISTEN state. Thus, the listener opens a distinct connection that persists throughout the application lifetime.

Upon connection drop — automatic reconnect with exponential backoff (1s → 2s → 4s → ... → 30s cap) occurs. During reconnects, the polling fallback ensures delivery.

**3. `OutboxSignal` (in-process bridge):**

A `SemaphoreSlim` singleton that bridges the listener and the processor. The listener invokes `signal.Notify()` upon receiving a PG notification. The processor awaits `signal.WaitAsync(10s, ct)` — returning immediately upon a signal or after 10s (fallback).

**The processor signals itself** (`signal.Notify()`) whenever it processed at least one message. That is what drains a backlog larger than the batch: 14 pending messages are not 10 now and 4 after the next 10-second tick — the self-signal starts the next iteration immediately, and it keeps doing so until a batch comes back empty. It is also what makes cascades instant (processing `EvaluateLessonCompleted` writes `NotifyAchievementUnlocked`, which the very next iteration picks up).

**The processor drains queued signals** (`signal.DrainPending()`) right after waking, before it queries. A semaphore counts, so five commits during one batch leave five permits — and the processor would run five more iterations, each issuing its own `SELECT ... FOR UPDATE SKIP LOCKED`, to be told what the first one already knew. The signal carries one bit — "there is something there" — so N of them mean what one means. Draining before the query is what makes this safe rather than lossy: any row committed before the query is in its result set no matter how many notifications announced it, and any row committed after it raises a fresh notification that arrives after the drain. `OutboxSignalTests` pins exactly that, including the case that would make draining a bug: a notification arriving *after* a drain must still wake the processor.

**Results:**

| Scenario | Polling-only | LISTEN/NOTIFY + fallback |
|---|---|---|
| Single-hop (email, blob) | up to 10s | < 100ms |
| Achievement chain (2 hops) | up to 20s | < 500ms |
| Idle load (no messages) | SELECT every 10s | SELECT every 10s |
| New dependencies | — | 0 (Npgsql is already present) |

---

**Alternatives considered:**

1. **Reduce polling interval to 1s** — simplest, but 1 SELECT/s on an empty table = unnecessary load. During scale-out (N instances) this equals N SELECT/s. Does not scale well.

2. **In-process SemaphoreSlim without PostgreSQL** — signaling from `DomainEventsInterceptor` directly. Works for single-instance, but during horizontal scaling, instance A writes an outbox message, and instance B (running the processor) receives no signal. PG LISTEN/NOTIFY operates cross-connection and cross-process.

3. **Debezium CDC (Change Data Capture)** — Production-grade for microservices. Rejected: requires Kafka + Debezium + Kafka consumers — disproportionate for a monolith.

4. **Wolverine framework** — .NET framework with built-in LISTEN/NOTIFY outbox. Rejected: Wolverine replaces MediatR and employs its own pipeline — migrating the entire architecture.

5. **CAP library** — lightweight event bus with a built-in outbox. Rejected: introduces custom abstractions (`ICapPublisher`), conflicting with the existing outbox implementation.

6. **Hybrid: optimistic dispatch + outbox as safety net** (NServiceBus approach) — Rejected for the current architecture: requires changes in the Application layer (the handler must be aware of dispatch), violating layer separation.

---

**Consequences:**

- The PL/pgSQL function and the trigger are **not** in an EF migration. They live in
  `Learnix.DbMigrator/DatabaseObjects/outbox_notify.sql` and are re-applied on every migrator run
  (ADR-BACK-MIGR-003) — a trigger is a repeatable object, and a migration would only state it until the
  next squash of the history collapsed the file away.

  > **The trigger existed in no database until the audit.** The listener, the signal and the
  > self-signalling loop all shipped; nothing created the trigger — verified against a live database:
  > zero user triggers, no `notify_outbox_insert` function. So `OutboxNotificationListener` was holding
  > a dedicated PostgreSQL connection open to listen on a channel nobody ever published to, and every
  > single-hop message waited for the 10-second polling tick instead of the "< 100ms" in the table
  > above. Nothing looked broken, because the fallback is the same mechanism that would carry the load
  > if the listener died — which is exactly the kind of failure a fallback hides. The achievement chain
  > stayed fast anyway, but for a different reason than the one documented here: the processor signals
  > *itself* after processing a message, and that path never involved the trigger.

- `OutboxNotificationListener` in `Infrastructure/Services/Outbox/` — as a distinct `BackgroundService`.
- `OutboxSignal` in `Infrastructure/Outbox/` — singleton `SemaphoreSlim` wrapper.
- `OutboxProcessorService` modified: `PeriodicTimer` → `outboxSignal.WaitAsync(10s)`.
- One additional PostgreSQL connection (unpooled) for LISTEN — minimal resource footprint.

**Scale-out safety (`FOR UPDATE SKIP LOCKED`):**

The Outbox processor utilizes `SELECT ... FOR UPDATE SKIP LOCKED` instead of a regular SELECT:

```sql
SELECT * FROM "OutboxMessages"
WHERE "ProcessedAt" IS NULL AND "NextRetryAt" <= {now}
ORDER BY "OccurredAt"
LIMIT {batch_size}
FOR UPDATE SKIP LOCKED
```

- `FOR UPDATE` — locks the selected rows at the PostgreSQL transaction level. Other transactions cannot `SELECT FOR UPDATE` them until COMMIT.
- `SKIP LOCKED` — if a row is already locked by another instance, skip it instead of waiting.
- **Timestamp rounding buffer:** `{now}` is calculated as `DateTime.UtcNow.AddSeconds(1)` to circumvent PostgreSQL microsecond rounding issues.
- Result: Instance A grabs messages 1–10, Instance B grabs 11–20. No duplication.

The entire batch is wrapped in an explicit transaction (`BeginTransactionAsync` → `CommitAsync`) to maintain the lock while processing.

---

## ADR-BACK-INFRA-010: PII Masking in Application Logs

**Context:**
During a security audit, it was discovered that the email sending service (`SmtpEmailSender`) logged complete user email addresses at the `Information` level (e.g., `logger.LogInformation("Email sent to Oleh123@gmail.com")`). In a production environment, these logs might be transmitted to centralized systems (ELK, Datadog), accessible to a broad array of developers. Logging Personally Identifiable Information (PII) in plaintext creates security risks and violates compliance (GDPR).

**Decision:**
Implement a PII masking rule across all application logs.
For email addresses, employ partial character obfuscation instead of complete redaction or hashing, as partial obfuscation (e.g., `O***@gmail.com`) retains sufficient context for debugging without exposing the full address.
Any service logging sensitive data (email, phones, IP addresses) must apply masking functions prior to writing to `ILogger`.

**Consequences:**
- Security: Reduces the risk of PII leaks via log aggregation systems.
- Debugging: While full masking complicates debugging, the compromise approach (displaying the first letter and domain) aids troubleshooting.
- Additional effort: Developers must remain vigilant regarding the data they log.


## ADR-BACK-INFRA-011: Repository Pattern via Ardalis.Specification

**Decision:** Specific repository interfaces per aggregate root extending IRepositoryBase<T> from Ardalis.Specification. No custom repository base classes.

**Structure:**
- **Interface (Application layer):** public interface ICourseRepository : IRepositoryBase<Course>
- **Implementation (Infrastructure layer):** internal sealed class CourseRepository : RepositoryBase<Course>, ICourseRepository

**Why:**
- RepositoryBase<T> from Ardalis already provides FirstOrDefaultAsync, ListAsync, CountAsync, AddAsync, UpdateAsync, DeleteAsync accepting specifications.
- Prevents boilerplate repository implementations.
- Keeps Application layer decoupled from Entity Framework while still allowing complex queries via Specifications.

---

## ADR-BACK-INFRA-013: Outbox Dispatch — a Handler per Message Type, not a Switch in the Processor

**Decision:** `OutboxProcessorService` no longer knows what any message *means*. It locks a batch (`FOR UPDATE SKIP LOCKED`), hands each row to `IOutboxMessageDispatcher`, and retries with backoff whatever throws. Every message type is a class:

```csharp
internal sealed class PasswordResetEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendPasswordResetEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.PasswordResetEmail;

    protected override Task HandleAsync(SendPasswordResetEmailPayload payload, CancellationToken ct) =>
        emailSender.SendPasswordResetAsync(payload.ToEmail, payload.FirstName, payload.ResetLink, payload.Language, ct);
}
```

`OutboxMessageHandler<TPayload>` deserializes the payload once, in the base class. Handlers are registered by an assembly scan (`AddOutboxMessageHandlers`), the way MediatR and FluentValidation already are, and `OutboxMessageDispatcher` routes by a dictionary keyed on `MessageType`.

**What the processor used to be:** a 20-case `switch` with seven services injected into a background worker (`IEmailSender`, `IBlobStorageService`, `IAchievementEvaluator`, `IAchievementNotifier`, `ICertificateNotifier`, `INotificationSender`), `JsonSerializer.Deserialize<T>` repeated verbatim in every branch, and the user-facing text of in-app notifications ("Achievement Unlocked", "Certificate Issued") sitting inside the plumbing. Adding an outbox message meant editing the class responsible for not losing messages.

**Why:**
- **The processor's job is delivery, not meaning.** Row locking, retry, exponential backoff and the `LISTEN/NOTIFY` wake-up (ADR-BACK-INFRA-008) are what it must get right. Every dependency it carried for someone else's side-effect was a reason to touch it — and each touch risked the one thing nobody wants broken.
- **Each handler declares only what it needs.** `DeleteBlobHandler` takes `IBlobStorageService` and nothing else. The old switch gave the *whole* processor every dependency in the union.
- **The deserialization lived twenty times.** Now once, in `OutboxMessageHandler<TPayload>`, which also turns an unreadable payload into a proper failure rather than a `null!` waiting to throw somewhere less obvious.
- **The dispatcher can enforce what the switch could not.** At construction it checks the handler set against every constant in `OutboxMessageTypes`, and refuses to start if a type has no handler — or if two handlers claim one. A `default:` branch could only complain *after* a message was already stranded; a set difference complains at boot. There *was* such a stranded case waiting to happen: an unused `OutboxMessageDispatcher` with a lone `DeleteBlob` branch had been left behind in the codebase, registered nowhere.

**Rejected alternatives:**
- *Keeping the switch, extracting only the deserialization.* Removes the duplication and none of the coupling: the processor still depends on every service in the system.
- *MediatR notifications for outbox messages.* The outbox is deliberately outside the request pipeline; routing it back through MediatR would put behaviors (validation, logging, caching) in the path of a retry loop and blur which failures are retriable.
- *A `Dictionary<string, Func<...>>` built in the processor.* Same coupling in a less readable form, and no per-handler dependency injection.

**Consequences:**
- Adding a message type = a payload record + a handler class. Nothing else changes; the scan finds it, the dispatcher validates it.
- Handlers are `internal` and tested through `Learnix.Infrastructure.UnitTests` (new project, `InternalsVisibleTo`) — the first tests this layer has.
- The in-app notification wording moved with the handlers rather than being fixed: it is still English-only while every email is localized. That gap is recorded as TD-003, not silently inherited.

---

## ADR-BACK-INFRA-014: The Migrator Flushes Redis — a Cache Must Not Outlive Its Database

**Decision:** `Learnix.DbMigrator` empties the Redis cache (`FLUSHDB`) as its last step, after migrations and every seeder have run. Failure to reach Redis logs a warning and does not fail the run.

**Why:** the cache outlives the database, and the two then disagree about which world they are in — with the cache winning for up to a day.

Concretely, and this was found the hard way: drop and re-create PostgreSQL (a routine local reset) while the Redis container keeps running. The categories are re-seeded with **new** GUIDs, but `categories:all` still holds the old list for the remainder of its 24-hour TTL (ADR-BACK-INFRA-002 / `CacheKeys.Categories.AllTtl`). The catalog then renders a filter sidebar of categories whose ids no longer exist in any row, and picking one returns **zero courses**. Nothing in the code is wrong. Every layer is behaving exactly as designed, and the result is a page that lies.

**Why flush everything rather than the keys that went stale:**
- `IDistributedCache` cannot enumerate or delete by prefix, so "the keys that went stale" is not a set the migrator can name. `CacheKeys.Courses.Public(...)` alone is an unbounded key space parameterized by search terms.
- **Every key in Redis is derived data**: cached query results, and `ai-chat:outage` (ADR-BACK-CHAT-014), which the next chat turn re-learns anyway. The cost of throwing it all away is a few cold reads. The cost of keeping a stale entry is a silently wrong page.
- Maintaining a list of "caches to invalidate after a seed" is bookkeeping that rots the moment somebody adds a cache and forgets the list exists.

**Why in the migrator and not the API:** the migrator is the only component that knows the data has just changed underneath everyone. The API cannot tell a fresh start from a restart, and flushing on every boot would throw away a warm cache for no reason.

**Consequences:**
- Every `dotnet run --project Learnix.DbMigrator` and every `docker compose --profile init up migrator` leaves Redis empty. In CI/CD that means the first requests after a deploy are cold — which they largely are anyway, since the deploy replaced the containers.
- The migrator now needs `AllowAdmin = true` on its Redis connection (`FLUSHDB` is an admin command). It is the only component that does; the API's client cannot issue one.
- A Redis that is unreachable during a migration leaves stale entries behind, and says so in a warning rather than failing a deployment that has otherwise succeeded.

---

## ADR-BACK-INFRA-015: `DomainEventsInterceptor` does not swallow handler exceptions

**Decision:** there is no `try-catch` around `publisher.Publish(...)` in `DomainEventsInterceptor`. An
exception from a domain-event handler propagates, `SavingChangesAsync` fails, and EF Core rolls the
transaction back.

**Why:**
- The interceptor runs **before** the write (`SavingChangesAsync` → `base.SavingChangesAsync()`), and the
  domain-event handlers it invokes write `OutboxMessage` rows into *the same* DbContext — that is what
  makes the side effect atomic with the entity change (ADR-BACK-INFRA-005).
- A `try-catch` therefore had a very specific failure mode: the Outbox insert throws (a serialization
  bug, say), the exception is swallowed, the entity is written anyway — and the email or notification
  that was supposed to follow simply never exists. Nothing is logged as broken because nothing *looks*
  broken. Silent divergence between what happened and what the system remembers happening.
- Letting it throw turns that into a 500 and an untouched database. A loud failure beats a lost message.

**Alternatives:**
- **Keep the `try-catch` and add a dead-letter path** — a second delivery guarantee bolted onto the one
  the Outbox already provides. And the failures it would catch are bugs, not transient faults: retrying
  a serialization error accomplishes nothing.
- **Dispatch events after the save** — then the Outbox row is in a different transaction from the entity,
  which is precisely the guarantee we are trying to keep.

**Consequences:**
- `ILogger<DomainEventsInterceptor>` was removed with the catch block — there is nothing left to log.
- A domain-event handler doing something non-critical (cache invalidation, say) must **not** throw:
  inside this interceptor, throwing means rolling back the business transaction that caused it. Handle
  and log it locally.
