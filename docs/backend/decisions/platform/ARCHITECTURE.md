# Learnix - ADR: Architectural Decisions

> Covers core architectural patterns, Clean Architecture implementation, and cross-cutting application decisions.

---

## ADR-BACK-ARCH-001: Clean Architecture + CQRS via MediatR

**Decision:** Clean Architecture with a strict separation into Domain / Application / Infrastructure / API. All operations pass through MediatR (Command/Query).

**Why:**
- Strict dependency rule — the Domain does not know about the infrastructure.
- CQRS allows independent optimization of reads (cache, projections) and writes.
- MediatR provides pipeline behaviors (validation, logging, caching) without code duplication.

**Alternatives:**
- Vertical Slice Architecture — simpler for small projects, but scales worse at 20+ features.
- Service layer (`IService`) without a mediator — fewer abstractions, but pipeline behaviors would have to be written manually.

---

## ADR-BACK-ARCH-002: Result<T> via FluentResults instead of custom implementation

**Decision:** We use the [FluentResults](https://github.com/altmann/FluentResults) library for the Result pattern in the Application layer.

**Why:**
- Mature library with support for `Result`, `Result<T>`, error chaining, and metadata.
- No need to maintain a custom implementation of `Result<T>`.
- Supports multiple errors (unlike a simple `string? Error`).
- Integrates well with FluentValidation.

**Alternatives:**
- Custom `Result<T>` — works, but would have to be expanded manually.
- Exceptions for business errors — violates the "exceptions = unexpected failures" contract.
- ErrorOr — another option, but FluentResults has a richer API.

**Consequences:**
- Command handlers return `Result` or `Result<T>`.
- Query handlers return `Result<TResponse>`.
- Controllers map `result.IsFailed` → BadRequest, `result.IsSuccess` → Ok.

---

## ADR-BACK-ARCH-003: FluentValidation + FluentResults in pipeline (no exceptions)

**Decision:** `ValidationBehavior` returns `Result.Fail()` with validation errors instead of throwing `ValidationException`. Constraint on handler: `TResponse : ResultBase`.

**Why:**
- Validation is business logic, not an exceptional situation.
- Consistency with ADR-BACK-ARCH-002: all expected errors via `Result`.
- The controller maps a single type (`Result`) instead of two error streams (`Result` + `catch`).

**Alternatives:**
- Throw `ValidationException` + catch in middleware works, but mixes two approaches to errors.
- Return `Result` from middleware (`catch` → `Result.Fail`) — a cosmetic solution, the exception is still thrown.

**Consequences:**
- `ExceptionHandlingMiddleware` remains only for unforeseen failures (DB down, null ref, etc.).
- All Command/Query handlers must return a type that inherits from `ResultBase`.

---

## ADR-BACK-ARCH-004: Typed errors (FluentResults custom errors) instead of string matching

**Decision:** To classify errors, we use typed classes that inherit from `FluentResults.Error`, rather than string matching on the message.

Base types:
- `NotFoundError` - 404
- `ValidationError` - 400 (if needed outside FluentValidation)
- `ConflictError` - 409 (already enrolled, duplicate, etc.)
- `ForbiddenError` - 403
- `AuthenticationError` - 401 (added, see AUTH.md ADR-BACK-AUTH-009)

**Why:**
- Compile-time safety: a typo in the type name = compilation error.
- The controller maps Result → HTTP status without magic strings.
- Easy to extend: new type = new class, without altering existing code.

**Alternatives:**
- String matching (`Contains("not found")`) - fragile, hard to refactor, easily broken by changing the message.
- Error codes (enum) - works, but less expressive than types and carries no additional data.

**Example mapping in the controller:**
```cs
if (result.HasError<NotFoundError>()) return NotFound();
if (result.HasError<ConflictError>()) return Conflict();
if (result.IsFailed) return BadRequest(result.Errors);
return Ok(result.Value);
```

---

## ADR-BACK-ARCH-005: ProblemDetails for errors, clean DTO for success

**Decision:** No envelope. Success → DTO directly.
Error → `ProblemDetails` (RFC 7807) with an errors dictionary for validation.

**Why:**
- ASP.NET Core has built-in support for `ProblemDetails`.
- The frontend receives a standardized error structure.
- Envelope (`{ data, success, errors }`) — unnecessary boilerplate.

**Validation is returned as:**
```json
{
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Title": ["Title is required"],
    "Price": ["Price must be >= 0"]
  }
}
```

**Mapping:** Extension method in the API layer: `Result.Errors` → `ProblemDetails`.

---

## ADR-BACK-ARCH-007: Manual mapping without AutoMapper

**Decision:** Entity → DTO mapping via extension methods (`ToDto()`, `ToResponse()`).
No AutoMapper or Mapster.

**Why:**
- Explicit, compile-time safe, easy to debug.
- For 20-30 DTOs, the overhead is minimal.
- AutoMapper hides errors behind the magic of conventions.

---

## ADR-BACK-ARCH-008: IDomainEvent without dependency on MediatR - adapter in Application

**Decision:** The `IDomainEvent` interface in `Learnix.Domain.Common` - a pure marker without inheriting `INotification`. The MediatR-specific wrapper `DomainEventNotification<TDomainEvent> : INotification` resides in `Learnix.Application.Common.Events`. `DomainEventsInterceptor` (Infrastructure) does the wrapping at `SavingChangesAsync` time — `typeof(DomainEventNotification<>).MakeGenericType(...)` — and publishes each one through MediatR, inside the same transaction as the entity change (ADR-BACK-INFRA-015).

**Why:**
- The Domain layer shouldn't know about MediatR - it's an infrastructure library.
- Changing the mediator (theoretically) - rewrite one adapter, not all domain events.
- Handlers in Application are written as `INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>` - slightly more boilerplate, but explicitly shows it is a reaction to a domain event.

**Alternatives:**
- `IDomainEvent : INotification` - simpler, but violates the dependency rule.
- Custom `IDomainEventDispatcher` without MediatR entirely - more code, loss of MediatR's in-process pub/sub features.

---

## ADR-BACK-ARCH-009: Application folder structure - hybrid feature-first + cross-cutting Common/Abstractions

**Decision:** Interfaces of the Application layer are grouped by **area of usage**:

- **Cross-cutting** (used by more than one feature) → `Common/Abstractions/{Category}/`. The categories that exist today: `Persistence`, `Identity`, `Messaging`, `Storage`, `Hubs`.
- **Feature-specific** (lives within one feature) → `{Feature}/Abstractions/`. Examples: `IUserRegistrationService`, `IUserAuthenticationService`, `ITokenService`, `IRefreshTokenRepository` — all in `Auth/Abstractions/`.

The folder is named `Abstractions`, not `Interfaces`, because it may contain more than just interfaces (abstract classes, delegates).

`Models/` (the records those interfaces take and return) is feature-scoped and appears only when a type is shared by more than one use case — `Auth/Models/` holds `LoginResponse`, `UserAuthenticationInfo` and `GoogleUserInfo`, which is exactly why the folder exists there and nowhere else (ADR-BACK-ARCH-017). `Common/Models/` is for the handful of types every feature speaks, not a landing zone.

Categorization into `Common/Abstractions/` is established immediately (even with one file in the folder) — moving one file is easy, moving ten is painful after the folder has turned into a dump.

**Rule for new interfaces:** "Does this interface make sense outside of a single feature?"
- Yes → `Common/Abstractions/{Category}/`
- No → `{Feature}/Abstractions/`

**Why:**
- Locality of changes: a feature is a self-contained folder top-to-bottom. The Auth feature is deleted in one operation.
- Clear dependency graph: `using Learnix.Application.Auth.Abstractions` inside `Courses/` is an explicit red flag.
- Categories in `Common/Abstractions/` supplement grouping by role - so that `IUnitOfWork` (persistence) and `IEmailSender` (messaging) do not end up in the same basket.

**Alternatives:**
- Flat `Common/Interfaces/` - simpler now, breaks down at 20+ files, impossible to distinguish a repository from an external service or a pipeline marker without opening the file.
- Feature-grouping only without `Common/Abstractions/` - doesn't solve where to place truly cross-cutting items (`IUnitOfWork`, `IEmailSender`).
- Only `Common/Abstractions/{Category}/` without feature-folders for interfaces - violates locality, breaks the "one feature = one folder" rule.

---

## ADR-BACK-ARCH-010: Business logic — exclusively in the Application layer

**Decision:** Any business logic (domain rules, orchestration, reaction to domain events) resides **only** in `Learnix.Application`. Infrastructure and API layers contain no business logic.

**What is considered business logic:**
- The decision to "increment/decrement Category CoursesCount" is business logic.
- The decision "a course cannot be published without a description" is business logic (in Domain or Application validator).
- Choosing which entity to load, which domain rule to apply, which domain field to update is business logic.

**What Infrastructure MUST NOT do (besides technical implementation):**
- Load entities via `DbContext`/repository and invoke domain methods → this is Application (event handler, command handler).
- Decide whether a counter needs updating depending on the entity's state (`WasPublished`) → this is Application.
- Execute SQL queries with business conditions (`WHERE Status = Published`) without explicit delegation from Application → this is Application.

**What Infrastructure does:** technical operations, independent of business rules - writing to the Outbox, sending HTTP requests, writing to Blob Storage, reading configuration, DI registration. Event handlers in Infrastructure exclusively create infrastructure side-effects (OutboxMessage, blob operations), but do not make business decisions.

**What API (controllers) does:** receives the HTTP request, delegates to MediatR, maps Result to an HTTP response. No conditions, no calls to repositories, no direct invocation of domain methods.

**Anti-pattern - the violation that prompted this ADR:**

```csharp
// WRONG - Infrastructure handler with business logic inside
internal sealed class CoursePublishedCountHandler(OutboxDbContextHolder holder) ...
{
    var category = await ctx.Categories.FirstOrDefaultAsync(...);
    category?.IncrementCoursesCount(); // < business decision in Infrastructure
}

// RIGHT — Application handler via abstraction
internal sealed class CoursePublishedCountHandler(CategoryCoursesCountUpdater updater) ...
{
    return updater.IncrementAsync(notification.DomainEvent.CategoryId, ct);
}
```

**Why this matters:**

1. **Testability.** Application handlers are tested via mock repositories. Infrastructure handlers bypassing this layer are tested only with a real DbContext.
2. **Pipeline.** Logic in Application passes through the MediatR pipeline: logging, validation, error handling. In Infrastructure — it does not.
3. **Dependency rule.** Infrastructure depends on Application, not vice versa. If business logic is in Infrastructure — it depends on the specific DbContext, EF Core, and the current DB provider. This implicitly ties a business rule to a technical choice.
4. **Single place of lookup.** A developer searching for "where is it decided whether to update the counter" — always in Application. No need to search through Infrastructure or API.

**Practical rule for checking:**
> If code in Infrastructure or API contains an `if` based on the state of a **domain entity** (not technical state), or invokes a domain method — it is business logic that must be moved to Application.

**Alternatives rejected:**
- "It is more convenient to place it in Infrastructure because the DbContext is there" — implementation convenience cannot justify violating layering.
- "The controller can check the condition, it knows the HTTP request context" — the controller does not know the business context, only HTTP. Conditions with domain meaning → Application validator or domain entity.

---

## ADR-BACK-ARCH-011: Specification Pattern for queries

**Decision:** All repository queries use `Specification<T>` (via the `Ardalis.Specification` library) to encapsulate criteria, includes, ordering, and paging.

**Why:**
- Filtering/sorting logic resides in the Application layer, not Infrastructure.
- Specifications are easy to test in isolation.
- Prevents duplication of query logic between handlers.

**Example Implementation:**
```csharp
using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Achievements.Specifications;

public sealed class UserAchievementsByUserSpecification : Specification<UserAchievement>
{
    public UserAchievementsByUserSpecification(Guid userId)
    {
        Query
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.UnlockedAt);
    }
}
```

**Conventions:**
- **Tracking is opt-in per call, not per specification.** A specification that both a query and a command
  need takes a `forUpdate` flag and applies `AsNoTracking()` unless it is set (`CourseByIdSpecification`
  is the canonical example). Reads are therefore untracked by default and a mutation has to *ask* for
  tracking — the safe direction for the mistake to fall.
- Location: `Application/{Feature}/Specifications/` — plural, always (ADR-BACK-ARCH-017).
- No raw LINQ in handlers. If a query is worth running, it is worth naming.

---

## ADR-BACK-ARCH-012: Offset-based pagination via PaginatedResult<T> + PaginationRequest

**Decision:** Offset-based pagination (skip/take). Shared classes `PaginatedResult<T>` and `PaginationRequest` reside in `Application.Common.Pagination`.

**Why:**
- Sufficient for an LMS without millions of records.
- `PaginationRequest` utilizing `Math.Clamp(PageSize, 1, 100)` protects against abuse.
- Cursor-based pagination is overkill for this project.

**Implementation Details:**
- `PageIndex` is zero-based; `PaginationRequest` clamps it and the page size in its constructor
  (`Math.Clamp(pageSize, MinPageSize, MaxPageSize)`), so an out-of-range request cannot exist — there is
  no validation rule to forget. `FromOffset(skip, take)` adapts the `skip`/`take` the controllers accept.
- Bounds live in `PaginationConstants` (ADR-BACK-ARCH-018): `MaxPageSize = 100`, `DefaultPageSize = 20`.
- `PaginatedResult<T>` carries `TotalCount` and derives `TotalPages`, `HasNextPage`, `HasPreviousPage`.

The clamp is the point worth remembering: an anonymous caller asking for `take=1000000` gets 100 rows,
not an `OutOfMemoryException`.

---

## ADR-BACK-ARCH-013: CacheKeys in Application layer, not Domain

**Decision:** `CacheKeys` constants reside in `Learnix.Application.Common.Constants.CacheKeys`, not in `Learnix.Domain.Constants`.

**Why:**
- Caching is an infrastructure concern. The Domain should not be aware of Redis.
- The Domain should remain as pure as possible, free from cross-cutting concerns.

**Alternatives:**
- Leave in Domain — works, but mixes levels of abstraction.

## ADR-BACK-ARCH-014: Command and Query Structure Rules

**Decision:** Commands and Queries are strictly structured within feature folders. Controllers contain no business logic.

**Structure:**
- **Command:** Application/{Feature}/Commands/{Name}/
  - {Name}Command.cs — record with input data implementing IRequest<Result> or IRequest<Result<T>>
  - {Name}CommandHandler.cs — implements IRequestHandler<,>
  - {Name}Validator.cs — AbstractValidator<{Name}Command>
- **Query:** Application/{Feature}/Queries/{Name}/
  - {Name}Query.cs — record with input data implementing IRequest<Result<TResponse>>
  - {Name}QueryHandler.cs — implements IRequestHandler<,>
  - {Name}Response.cs — DTO returned to controller (co-located with query)

**Rules:**
- Commands return Result or Result<T> (FluentResults).
- Queries return Result<TResponse> (FluentResults).
- Expected errors → Result.Fail(new NotFoundError(...)) — never throw for business errors.
- Throw exceptions — only for unexpected infrastructure failures.
- Never return domain entities from handlers — always map to DTOs.

**Why:**
- Strict structure ensures predictability across all features.
- Co-locating DTOs with Queries keeps related files together.
- Returning Result prevents using exceptions for control flow.

---

## ADR-BACK-ARCH-015: Domain Exception Pipeline Behavior

**Decision:** DomainExceptionBehavior<TRequest, TResponse> sits closest to the handler in the MediatR pipeline. It catches Learnix.Domain.Common.Exceptions.DomainException and returns Result.Fail(new ConflictError(ex.Message)).

**Pipeline order (critical):**
1. LoggingBehavior — wraps everything for timing.
2. ValidationBehavior — rejects invalid requests before domain is touched.
3. DomainExceptionBehavior — closest to handler, catches invariant violations.

**Why:**
- Handlers contain only the happy path — no try-catch boilerplate for domain invariant violations.
- System exceptions (NullReferenceException, DB failures, etc.) propagate freely to ExceptionHandlingMiddleware to return a 500 status code with a full stack trace.

---

## ADR-BACK-ARCH-016: Cache keys and their TTLs are co-located in CacheKeys

**Decision:** Every distributed-cache key is declared in `CacheKeys`, grouped by feature (`CacheKeys.Courses.ById(id)`), and each key sits next to the TTL it is written with (`CacheKeys.Courses.ByIdTtl`). Query records reference both; they never build a key string inline nor declare a `TimeSpan` literal.

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

---

## ADR-BACK-ARCH-017: The feature folder — one folder per use case, promotion only when shared

**Decision:** a feature in `Learnix.Application` is a vertical slice. The unit of organization is the
**use case**, not the artifact type.

```
Payments/
  Commands/InitiateMockPayment/     ← one folder per use case
      InitiateMockPaymentCommand.cs
      InitiateMockPaymentCommandHandler.cs
      InitiateMockPaymentValidator.cs
      InitiateMockPaymentResponse.cs
  Queries/GetMyPayments/            ← same for queries; the DTO is co-located
  Abstractions/                     ← feature-scoped interfaces (IPaymentRepository)
  Specifications/                   ← Ardalis specifications
  Constants/                        ← feature constants (see ADR-BACK-ARCH-018)
```

**Everything a use case owns lives in its folder** — command/query, handler, validator, response.
One type per file; the folder is the unit, not the file (see ADR-BACK-ARCH-014).

**An artifact is promoted to the feature level only when a second use case actually uses it:**

| Folder | Holds | Created when |
|---|---|---|
| `Models/` | Types shared across use cases, and the contracts of the feature's `Abstractions/` | A second use case needs the type |
| `Validation/` | Reusable FluentValidation rule sets (`PasswordRules.ValidPassword()`) | A second validator needs the rule |
| `EventHandlers/` | Domain-event handlers belonging to the feature | The feature reacts to a domain event |
| `Services/` | Feature-scoped services that are not use cases (e.g. `PublicCourseCatalogSearchService`) | Logic is genuinely shared and does not fit a handler |

**Not before.** No speculative `Models/` or `Validation/` folder "because it is cleaner".

**Why:**
- **What changes together lives together.** Adding a field to a command touches the command, its
  validator, its handler and its response. In one folder that is one diff in one place. Split across
  `Commands/`, `Validation/` and `Models/` it is a hunt through three trees — and things get left
  behind. Deleting a use case is likewise `rm -r` on one folder, with no orphans.
- **Type-based folders are the thing feature folders exist to escape.** A `Validation/` folder holding
  every validator is layer-by-type organization at a smaller scale — the same mistake, one level down.
- **The promotion rule is the same one the frontend already follows:** a component moves to
  `components/common/` when a *second* page uses it, not in anticipation of one.

**The same rule applies one level up.** When the second consumer is another *feature*, the artifact goes
to `Common/`, not to whichever feature happened to need it first:

- `Common/Validation/UserRules` — `ValidFirstName()` / `ValidLastName()` are used by `Auth/Register` and
  by `Users/UpdateProfile`. Two features, so `Common/`. Had they stayed in `Auth/`, `Users` would be
  reaching into another feature's namespace to validate its own input — the red flag from
  ADR-BACK-ARCH-009.
- `Auth/Validation/{PasswordRules, EmailRule}` stay in `Auth/` because everything that shares them is an
  Auth use case.

**Evidence for the rule, from this codebase:** `LoginResponse` is returned by five use cases (Login,
Register, GoogleLogin, ConfirmEmail, RefreshToken) yet was declared inside `Commands/Login/LoginCommand.cs`
— so four unrelated files carried `using ...Commands.Login;` purely to reach a type that was not
Login's. It now lives in `Auth/Models/`, and the compiler flagged all four usings as unnecessary the
moment it moved. `Auth/Validation/PasswordRules` is the rule working correctly in the other direction:
four validators share it, so it is at the feature level and no higher.

**Alternatives:**
- **Split by artifact type inside the feature** (`Commands/`, `Handlers/`, `Validators/`, `Models/`) —
  rejected: it is the layered structure this architecture replaced, reintroduced at feature scope.
- **One file per use case** (command, validator, handler and response in a single `.cs`) — a legitimate
  slice style, rejected here: it breaks the one-type-per-file convention the rest of the solution and
  its tooling assume, and merges ~200 files for no benefit the folder does not already give.

**Consequences:**
- A new use case is a new folder under `Commands/` or `Queries/`, never a new file dropped into a
  shared bucket.
- When a type gains a second consumer, *move it up* in the same PR — do not copy it, and do not leave
  it where a `using` from another use case has to reach for it.
- Optional folders (`Models/`, `Validation/`, `EventHandlers/`, `Services/`, `Tools/`) are absent from
  most features, and that is correct: their absence means nothing has needed sharing yet.

---

## ADR-BACK-ARCH-018: Constants live in the layer that owns the rule

**Decision:** a constant belongs to the layer whose rule it expresses — not to the layer that happens
to read it first.

| The constant is… | Lives in | Examples |
|---|---|---|
| A **domain invariant** — true no matter who calls, part of what the entity *is* | `Learnix.Domain/Constants/` | `CourseConstants.TitleMaxLength`, `ReviewConstants.MinRating`/`MaxRating`, `LessonConstants.*`, `Roles` |
| An **application rule** — a policy of this system, not of the domain | `Learnix.Application/**/Constants/` | `PaginationConstants.MaxPageSize`, `AuthValidationConstants.PasswordMinLength`, `BlobUrlTtlConstants`, cache TTLs (ADR-BACK-ARCH-016) |
| A **technical detail** of one adapter | `Learnix.Infrastructure/Constants/`, `Learnix.API/` | `BackgroundJobConstants`, blob container names, `RateLimitPolicies` |

Feature-scoped constants live in that feature's `Constants/` folder; only genuinely cross-feature ones
go to `Application/Common/Constants/`.

**Why:**
- **The dependency rule decides it, not taste.** Domain has no dependencies, so a rule the domain
  enforces cannot live above it — `Course` cannot reach `Application` to learn its own maximum title
  length. Put it in Domain and every layer above can read it. Put it in Application and the domain
  either duplicates it or stops enforcing it.
- **A duplicated bound is a bound that will disagree with itself.** `GetPublicCoursesValidator` filtered
  `minRating` against a literal `5m` while `ReviewConstants.MaxRating` sat in the domain. Nothing was
  broken — until the day the rating scale changes and one of the two places remembers.
- **Password length is deliberately *not* a domain invariant.** `User` does not store a password, it
  stores a hash; the minimum length is a policy of the authentication feature, and it lives with it.
  The test is not "does it look domain-ish" but "would the entity be invalid without it".

**Error messages are constants too**, and they follow the same layering:
- Messages shared across features — `Application/Common/Constants/Messages.cs` (`CommonMessages`):
  `CourseNotFound(id)`, `NotOwnerOfCourse`, `NotAuthenticated`.
- Feature-specific ones — `Application/{Feature}/Constants/{Feature}Messages.cs` (`AuthMessages`,
  `CourseMessages`, `CertificateMessages`, …).

The reason is the same as for any other constant: the same "Course not found." is raised from a dozen
handlers, and a message the client asserts on is a contract. Handlers hold that line — 164 of the 165
typed errors in the Application layer take their text from a `Messages` class.

**What deliberately stays inline** — a constant is for a value with a *second reader*. These have none:
- One-off regexes used by a single validator (`[A-Z]`, `[a-z]` in the password rules).
- FluentValidation `WithMessage("...")` texts, which are written where the rule is and read nowhere
  else. This is the pragmatic exception, not a second convention: the day one of them is needed twice,
  it moves to the feature's `{Feature}Messages` like everything else.
- Identity's own implementation details (hash length and the like) — ASP.NET Identity owns those.

**Alternatives:**
- **One global `Constants` class per project** — rejected: it becomes a junk drawer nobody can delete
  from, and it puts the domain's rules and the API's rate-limit names in the same namespace.
- **Everything in Application** — rejected: the domain would stop being able to enforce its own rules.
- **Everything in Domain** — rejected: it drags SMTP limits and password policy into a layer that is
  supposed to be about the business.
- **Magic numbers inline, with a comment** — rejected: a comment cannot be reused by the second caller,
  which is exactly how the two ratings above drifted apart.

**Consequences:**
- Before writing a numeric literal in a validator or a handler, look for the constant. If it is a bound
  the domain enforces, it already exists in `Learnix.Domain/Constants/`.
- A new invariant on an entity means a constant in Domain **and** the entity actually enforcing it —
  a constant nobody checks is documentation, not a rule.

---

## ADR-BACK-ARCH-019: `CourseCommandHandler` — a base class for course structure mutations

**Decision:** `CourseCommandHandler<TCommand, TResult>` (`Application/Common/Commands/`) is an abstract
base class that runs the fixed prelude of every course-structure mutation and then delegates to the
handler's own logic:

1. authenticated?
2. load the `Course` (with as much structure as the operation needs — ADR-BACK-DOMAIN-005), tracked
3. `IsOwnerOrAdmin`?
4. `EnsureStructureMutable()`
5. → `abstract HandleAsync(request, course, ct)`

`CourseSectionCommandHandler<TCommand, TResult>` extends it with a section-existence check and can load
only the targeted section's lessons.

**Why:**
- Fifteen-odd handlers run exactly steps 1–4. Copy-pasted, they are fifteen chances to forget the
  ownership check — and a forgotten ownership check is not a bug, it is a vulnerability.
- Template Method: the base class owns the algorithm, the subclass owns only what is specific to it.
  What a reviewer then reads in a handler is the business step, not the ceremony around it.

**How it is wired:**
- `where TCommand : IRequest<TResult>, ICommandWithCourseId` — the course id is reachable without
  reflection. Commands that also need a section implement `ICommandWithCourseAndSectionId`.
- `where TResult : ResultBase, new()` — lets the base class build a failed `TResult` when auth or
  loading fails, without knowing what `TResult` is.
- MediatR still resolves the concrete handler as `IRequestHandler<TCommand, TResult>`; the base class is
  invisible to it.

**Alternatives:**
- **Inline the prelude in every handler** — the duplication is the *point* of the rejection: it is what
  makes a missing check possible.
- **ASP.NET Core resource-based authorization** — rejected in ADR-BACK-AUTH-013: it would need the
  resource loaded inside an authorization handler, i.e. loading the course twice or threading it through.
- **Extension methods on `ICurrentUserService`** — removes some of the duplication, none of the sequence.

**Consequences:**
- A handler is ~20–30 lines shorter and starts at the interesting line.
- The loading mode is a constructor argument, which means the *decision* about how much of the aggregate
  to load is visible in the handler's declaration rather than buried in its body.

---

## ADR-BACK-ARCH-020: Configuration is bound to typed options, never read as `IConfiguration`

**Decision:** every `appsettings.json` section is bound to a POCO and consumed through `IOptions<T>`.
No layer above Infrastructure ever sees `IConfiguration`.

**Where the POCO lives — the same rule as constants (ADR-BACK-ARCH-018): with the layer that owns it.**

| The section describes… | POCO lives in | Examples |
|---|---|---|
| A policy the Application layer reasons about | `Application/Common/Settings/` | `JwtSettings` (token lifetimes), `GoogleSettings`, `AiChatSettings` (which provider), `AppSettings` (client base URL) |
| A detail of one adapter | next to that adapter in `Infrastructure/` | `SmtpSettings`, `MongoSettings`, `AnthropicSettings`, `GeminiSettings`, `BlobStorageOptions` |

Binding happens in exactly one place — `Infrastructure/DependencyInjection.cs`, via
`services.Configure<T>(configuration.GetSection(...))`. Connection strings stay out of it: they are
read from `ConnectionStrings:*`, because a connection string is not a policy.

**Why:**
- The Application layer must not depend on `Microsoft.Extensions.Configuration`. Reading a magic string
  key inside a handler makes the handler untestable without a configuration provider and unanswerable
  to "what settings does this feature have?".
- A typed POCO turns a config typo into a compile error, and makes the shape of a section reviewable.
- Keeping the binding in one file means there is one place to answer "where does this value come from",
  and one place where a fail-fast check can live.

**Fail-fast, not lazy:** `AddInfrastructure` validates the settings a running system cannot do without
(a missing Google client id, an empty JWT secret) and throws at startup. An `IOptions<T>` that is
missing its section binds happily to a default-constructed object — a JWT signed with an empty key is
worse than a server that refuses to boot. Options are **not** wired through `ValidateOnStart()`; the
checks are explicit code in DI. That is a smaller mechanism, and it is the one in use.

**Alternatives:**
- **`IConfiguration` injected where needed** — magic strings, no compile-time shape, no single place to
  validate.
- **`IOptionsSnapshot` / `IOptionsMonitor` for hot reload** — nothing here benefits: a changed JWT secret
  or blob container mid-process is a deployment, not a runtime event.

**Consequences:**
- The naming is currently inconsistent: eight `*Settings` classes and one `*BlobStorageOptions`. Pick
  one — `*Options` is the .NET convention for a type bound through `IOptions<T>` — and rename in one
  pass rather than growing two vocabularies.
- `ConfigurationSectionNameCaonstants` holds the section names. The typo in the class name is real and
  should be fixed; `AppSettings` bypasses it and binds against a literal `"App"`, which is exactly the
  drift the constants exist to prevent.
