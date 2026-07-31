# Learnix — ADR: Infrastructure

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where architectural decisions were made.

Related files: [ARCHITECTURE.md](ARCHITECTURE.md) · [AUTH.md](AUTH.md) · [DOMAIN.md](DOMAIN.md) · [MIGRATIONS.md](MIGRATIONS.md) · [OUTBOX.md](OUTBOX.md)

## Status Convention

When a decision is revised, the old ADR is marked `Superseded by ADR-XXX` and the new one `Supersedes ADR-YYY` — the history of thought is worth keeping.

When the mechanism an ADR describes no longer exists at all, the ADR is **removed** rather than kept as a tombstone: a reader looking for how the system works should not have to first work out which half of the file is fiction. The rejected alternative lives on in the ADR that replaced it — that is where "why not this?" belongs — and the full text stays in git history.

Numbers are never reused, so gaps in the sequence are expected. `ADR-BACK-INFRA-006` (auto-migrations on API startup) and `ADR-BACK-INFRA-009` (seed assets embedded in `Learnix.Infrastructure`) were removed this way: migrations and seeding no longer live in this layer at all. See [MIGRATIONS.md](MIGRATIONS.md). `ADR-BACK-INFRA-005`, `-008` and `-013` are gaps for a different reason — the outbox pattern, its LISTEN/NOTIFY dispatch and its per-message-type handlers moved wholesale to [OUTBOX.md](OUTBOX.md) as ADR-BACK-OUTBOX-001 through -003, since together they were half this file and a coherent topic on their own.

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

**See also:** ADR-BACK-INFRA-016 (why `CacheKeys` lives in Application, not Domain) and ADR-BACK-INFRA-017
(why keys and their TTLs are co-located in it) — two narrower decisions about this same mechanism.

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
  makes the side effect atomic with the entity change (ADR-BACK-OUTBOX-001 in `OUTBOX.md`).
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

---

## ADR-BACK-INFRA-016: `CacheKeys` in Application layer, not Domain

**Decision:** `CacheKeys` constants (ADR-BACK-INFRA-002) reside in `Learnix.Application.Common.Constants.CacheKeys`, not in `Learnix.Domain.Constants`.

**Why:**
- Caching is an infrastructure concern. The Domain should not be aware of Redis.
- The Domain should remain as pure as possible, free from cross-cutting concerns.

**Alternatives:**
- Leave in Domain — works, but mixes levels of abstraction.

---

## ADR-BACK-INFRA-017: Cache keys and their TTLs are co-located in `CacheKeys`

**Decision:** Every distributed-cache key (ADR-BACK-INFRA-002) is declared in `CacheKeys`, grouped by feature (`CacheKeys.Courses.ById(id)`), and each key sits next to the TTL it is written with (`CacheKeys.Courses.ByIdTtl`). Query records reference both; they never build a key string inline nor declare a `TimeSpan` literal.

**Why:**
- Previously keys lived in `CacheKeys` while TTLs were magic numbers on the query records, and one key (`courses:public:*`) was built inline. The two could drift, and `GetAllCategoriesQuery` had silently borrowed its TTL from `BlobUrlTtlConstants.CertificateReadUrl` - an unrelated blob-SAS constant. Changing the certificate SAS lifetime would have silently changed the category cache lifetime.
- Invalidation sites and cache-write sites now reference the same symbol, so "which commands invalidate this key" is answerable from one file.
- Grouping by feature keeps names readable as the registry grows (`Courses.Featured` over `CoursesFeatured`).

**Consequences:**
- `CacheKeys` holds TTLs despite its name. Accepted: the coupling it prevents is worth more than the naming purity of a separate `CacheTtl` class, which would reintroduce the exact drift this ADR removes.
- `CacheKeys.Courses.Public(...)` is deliberately **not** invalidated: the key space is unbounded (one entry per filter combination) and `IDistributedCache` offers no prefix or tag deletion. The catalog may lag a publish by up to `PublicTtl` (5 min). If that becomes unacceptable, the fix is Redis tag-based invalidation via `IConnectionMultiplexer`, not a longer list of `RemoveAsync` calls.

**Alternatives:**
- Separate `CacheTtl` static class - rejected, recreates the key/TTL split-brain.
- TTL as a parameter on `ICacheable<T>` implementations only - rejected, that is the status quo that produced the certificate-constant bug.
