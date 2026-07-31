# Learnix — ADR: Outbox

> The at-least-once delivery mechanism for durable side-effects raised from domain events — schema,
> background dispatch, the low-latency wake-up, and per-message-type handlers. Split out from
> [INFRA.md](INFRA.md), which still owns persistence, caching, and the domain-events interceptor that
> feeds this pattern (`DomainEventsInterceptor`, ADR-BACK-INFRA-015).

---

## ADR-BACK-OUTBOX-001: Outbox pattern (Schema & Background Worker)

**Decision:** The Outbox pattern is implemented to reliably execute background operations (confirm/delete blob, send email, evaluate achievements). Domain events are dispatched in-process by `DomainEventsInterceptor` from **`SavingChangesAsync` — before the INSERT/UPDATE runs**, not after it (ADR-BACK-INFRA-015). That ordering is the whole point: the `OutboxMessage` rows their handlers write land in the same transaction as the entity change, so either both commit or neither does. A handler consequently cannot query for the change that raised it — the row is not there yet.

**`OutboxMessage` entity:**
- `Id`, `Type` (e.g., `DeleteBlob`, `UnlockAchievement`), `Payload` (JSONB)
- `OccurredAt`, `ProcessedAt?`, `AttemptCount`, `LastAttemptAt?`, `LastError?`, `NextRetryAt?`
- Written by the domain event handler in the same EF transaction as the entity changes.

**Outbox worker (background `IHostedService`):**
- Reads `WHERE ProcessedAt IS NULL AND (NextRetryAt IS NULL OR NextRetryAt <= NOW())`
- Invokes `IOutboxMessageDispatcher.DispatchAsync(message)` which routes to a specific handler.
- Exponential backoff via `NextRetryAt` on errors.
- **See ADR-BACK-OUTBOX-002:** Dispatch mechanism optimized via PostgreSQL LISTEN/NOTIFY.

---

## ADR-BACK-OUTBOX-002: Outbox latency — PostgreSQL LISTEN/NOTIFY instead of polling-only

> Partially supersedes ADR-BACK-OUTBOX-001 regarding the "Outbox worker (background IHostedService)" — the message dispatch mechanism was changed from pure polling to push-first with a polling fallback.

**Context and problem:**

The initial Outbox implementation (ADR-BACK-OUTBOX-001) utilized pure polling: `OutboxProcessorService` with a `PeriodicTimer(10s)` executed a SELECT on the `OutboxMessages` table on every tick. This worked well for blob operations and emails, where a 10s latency was acceptable.

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
  `Learnix.Infrastructure/Persistence/EntityFramework/DatabaseObjects/outbox_notify.sql` and are re-applied on every migrator run
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

## ADR-BACK-OUTBOX-003: Outbox Dispatch — a Handler per Message Type, not a Switch in the Processor

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
- **The processor's job is delivery, not meaning.** Row locking, retry, exponential backoff and the `LISTEN/NOTIFY` wake-up (ADR-BACK-OUTBOX-002) are what it must get right. Every dependency it carried for someone else's side-effect was a reason to touch it — and each touch risked the one thing nobody wants broken.
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
