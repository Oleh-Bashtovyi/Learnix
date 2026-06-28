# Learnix — ADR: Architectural Decisions

> Covers core architectural patterns, Clean Architecture implementation, and cross-cutting application decisions.

---

## ADR-ARCH-001: Clean Architecture + CQRS via MediatR

**Decision:** Clean Architecture with a strict separation into Domain / Application / Infrastructure / API. All operations pass through MediatR (Command/Query).

**Why:**
- Strict dependency rule — the Domain does not know about the infrastructure.
- CQRS allows independent optimization of reads (cache, projections) and writes.
- MediatR provides pipeline behaviors (validation, logging, caching) without code duplication.

**Alternatives:**
- Vertical Slice Architecture — simpler for small projects, but scales worse at 20+ features.
- Service layer (`IService`) without a mediator — fewer abstractions, but pipeline behaviors would have to be written manually.

---

## ADR-ARCH-002: Result<T> via FluentResults instead of custom implementation

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

## ADR-ARCH-003: FluentValidation + FluentResults in pipeline (no exceptions)

**Decision:** `ValidationBehavior` returns `Result.Fail()` with validation errors instead of throwing `ValidationException`. Constraint on handler: `TResponse : ResultBase`.

**Why:**
- Validation is business logic, not an exceptional situation.
- Consistency with ADR-ARCH-002: all expected errors via `Result`.
- The controller maps a single type (`Result`) instead of two error streams (`Result` + `catch`).

**Alternatives:**
- Throw `ValidationException` + catch in middleware — works, but mixes two approaches to errors.
- Return `Result` from middleware (`catch` → `Result.Fail`) — a cosmetic solution, the exception is still thrown.

**Consequences:**
- `ExceptionHandlingMiddleware` remains only for unforeseen failures (DB down, null ref, etc.).
- All Command/Query handlers must return a type that inherits from `ResultBase`.

---

## ADR-ARCH-004: Typed errors (FluentResults custom errors) instead of string matching

**Decision:** To classify errors, we use typed classes that inherit from `FluentResults.Error`, rather than string matching on the message.

Base types:
- `NotFoundError` — 404
- `ValidationError` — 400 (if needed outside FluentValidation)
- `ConflictError` — 409 (already enrolled, duplicate, etc.)
- `ForbiddenError` — 403
- `AuthenticationError` — 401 (added, see DECISIONS_AUTH.md ADR-009)

**Why:**
- Compile-time safety: a typo in the type name = compilation error.
- The controller maps Result → HTTP status without magic strings.
- Easy to extend: new type = new class, without altering existing code.

**Alternatives:**
- String matching (`Contains("not found")`) — fragile, hard to refactor, easily broken by changing the message.
- Error codes (enum) — works, but less expressive than types and carries no additional data.

**Example mapping in the controller:**
```cs
if (result.HasError<NotFoundError>()) return NotFound();
if (result.HasError<ConflictError>()) return Conflict();
if (result.IsFailed) return BadRequest(result.Errors);
return Ok(result.Value);
```

---

## ADR-ARCH-005: ProblemDetails for errors, clean DTO for success

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

## ADR-ARCH-006: Monorepo (frontend + backend in one repository)

**Decision:** One repository: `learnix/Learnix.Backend/` + `learnix/learnix-client/`.

**Why:**
- Solo project, one release cycle — two repos add overhead without benefit.
- Shared Docker Compose, one PR = end-to-end feature.
- Portfolio: one link — the whole project.

**Alternatives:**
- Two separate repos — makes sense for different teams with different deploy cycles, irrelevant here.

---

## ADR-ARCH-007: Manual mapping without AutoMapper

**Decision:** Entity → DTO mapping via extension methods (`ToDto()`, `ToResponse()`).
No AutoMapper or Mapster.

**Why:**
- Explicit, compile-time safe, easy to debug.
- For 20-30 DTOs, the overhead is minimal.
- AutoMapper hides errors behind the magic of conventions.

---

## ADR-ARCH-008: IDomainEvent without dependency on MediatR — adapter in Application

**Decision:** The `IDomainEvent` interface in `Learnix.Domain.Common` — a pure marker without inheriting `INotification`. The MediatR-specific wrapper `DomainEventNotification<TDomainEvent> : INotification` resides in `Learnix.Application.Common.Events`. `ApplicationDbContext.SaveChangesAsync` publishes domain events via `MakeGenericType` + `Activator.CreateInstance`, wrapping each event in the corresponding `DomainEventNotification<T>`.

**Why:**
- The Domain layer shouldn't know about MediatR — it's an infrastructure library.
- Changing the mediator (theoretically) — rewrite one adapter, not all domain events.
- Handlers in Application are written as `INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>` — slightly more boilerplate, but explicitly shows it is a reaction to a domain event.

**Alternatives:**
- `IDomainEvent : INotification` — simpler, but violates the dependency rule.
- Custom `IDomainEventDispatcher` without MediatR entirely — more code, loss of MediatR's in-process pub/sub features.

---

## ADR-ARCH-009: Application folder structure — hybrid feature-first + cross-cutting Common/Abstractions

**Decision:** Interfaces of the Application layer are grouped by **area of usage**:

- **Cross-cutting** (used by more than one feature) → `Common/Abstractions/{Category}/`. Categories: `Persistence`, `Caching`, `Messaging`, `Time` (if needed), `Identity` (general concepts like `ICurrentUserService`).
- **Feature-specific** (lives within one feature) → `{Feature}/Abstractions/`. Examples: `IUserRegistrationService`, `IUserAuthenticationService`, `ITokenService`, `IRefreshTokenRepository` — all in `Auth/Abstractions/`.

The folder is named `Abstractions`, not `Interfaces`, because it may contain more than just interfaces (abstract classes, delegates).

`Models/` (records returned/accepted by interfaces) — also feature-scoped: `Auth/Models/`, `Courses/Models/`. A cross-cutting `Common/Models/` is created only if a model emerges that is truly used across multiple features.

Categorization into `Common/Abstractions/` is established immediately (even with one file in the folder) — moving one file is easy, moving ten is painful after the folder has turned into a dump.

**Rule for new interfaces:** "Does this interface make sense outside of a single feature?"
- Yes → `Common/Abstractions/{Category}/`
- No → `{Feature}/Abstractions/`

**Why:**
- Locality of changes: a feature is a self-contained folder top-to-bottom. The Auth feature is deleted in one operation.
- Clear dependency graph: `using Learnix.Application.Auth.Abstractions` inside `Courses/` is an explicit red flag.
- Categories in `Common/Abstractions/` supplement grouping by role — so that `IUnitOfWork` (persistence) and `IEmailSender` (messaging) do not end up in the same basket.

**Alternatives:**
- Flat `Common/Interfaces/` — simpler now, breaks down at 20+ files, impossible to distinguish a repository from an external service or a pipeline marker without opening the file.
- Feature-grouping only without `Common/Abstractions/` — doesn't solve where to place truly cross-cutting items (`IUnitOfWork`, `IEmailSender`).
- Only `Common/Abstractions/{Category}/` without feature-folders for interfaces — violates locality, breaks the "one feature = one folder" rule.

---

## ADR-ARCH-010: Business logic — exclusively in the Application layer

**Decision:** Any business logic (domain rules, orchestration, reaction to domain events) resides **only** in `Learnix.Application`. Infrastructure and API layers contain no business logic.

**What is considered business logic:**
- The decision to "increment/decrement Category CoursesCount" — THIS is business logic.
- The decision "a course cannot be published without a description" — business logic (in Domain or Application validator).
- Choosing which entity to load, which domain rule to apply, which domain field to update — business logic.

**What Infrastructure MUST NOT do (besides technical implementation):**
- Load entities via `DbContext`/repository and invoke domain methods → this is Application (event handler, command handler).
- Decide whether a counter needs updating depending on the entity's state (`WasPublished`) → this is Application.
- Execute SQL queries with business conditions (`WHERE Status = Published`) without explicit delegation from Application → this is Application.

**What Infrastructure does:** technical operations, independent of business rules — writing to the Outbox, sending HTTP requests, writing to Blob Storage, reading configuration, DI registration. Event handlers in Infrastructure exclusively create infrastructure side-effects (OutboxMessage, blob operations), but do not make business decisions.

**What API (controllers) does:** receives the HTTP request, delegates to MediatR, maps Result to an HTTP response. No conditions, no calls to repositories, no direct invocation of domain methods.

**Anti-pattern — the violation that prompted this ADR:**

```csharp
// WRONG — Infrastructure handler with business logic inside
internal sealed class CoursePublishedCountHandler(OutboxDbContextHolder holder) ...
{
    var category = await ctx.Categories.FirstOrDefaultAsync(...);
    category?.IncrementCoursesCount(); // ← business decision in Infrastructure
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

## ADR-ARCH-011: Specification Pattern for queries

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
- `AsNoTracking = true` by default.
- For Commands that mutate entities: explicitly `AsNoTracking = false`.
- Location: `Application/{Feature}/Specifications/`.

---

## ADR-ARCH-012: Offset-based pagination via PaginatedResult<T> + PaginationRequest

**Decision:** Offset-based pagination (skip/take). Shared classes `PaginatedResult<T>` and `PaginationRequest` reside in `Application.Common.Pagination`.

**Why:**
- Sufficient for an LMS without millions of records.
- `PaginationRequest` utilizing `Math.Clamp(PageSize, 1, 100)` protects against abuse.
- Cursor-based pagination is overkill for this project.

**Implementation Details:**
- `PageIndex` is zero-based.
- `MaxPageSize = 100`, `DefaultPageSize = 20`.
- `PaginatedResult` includes `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`.

**Code Snippet:**
```csharp
public record PaginationRequest
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public PaginationRequest(int pageIndex = 0, int pageSize = 20)
    {
        PageIndex = Math.Max(0, pageIndex);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
    
    public int Skip => PageIndex * PageSize;
    public int Take => PageSize;
}

public record PaginatedResult<TEntity>(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyList<TEntity> Items
) where TEntity : class
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages - 1;
    public bool HasPreviousPage => Page > 0;
}
```

---

## ADR-ARCH-013: CacheKeys in Application layer, not Domain

**Decision:** `CacheKeys` constants reside in `Learnix.Application.Common.Constants.CacheKeys`, not in `Learnix.Domain.Constants`.

**Why:**
- Caching is an infrastructure concern. The Domain should not be aware of Redis.
- The Domain should remain as pure as possible, free from cross-cutting concerns.

**Alternatives:**
- Leave in Domain — works, but mixes levels of abstraction.

## ADR-ARCH-014: Command and Query Structure Rules

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

## ADR-ARCH-015: Domain Exception Pipeline Behavior

**Decision:** DomainExceptionBehavior<TRequest, TResponse> sits closest to the handler in the MediatR pipeline. It catches Learnix.Domain.Common.Exceptions.DomainException and returns Result.Fail(new ConflictError(ex.Message)).

**Pipeline order (critical):**
1. LoggingBehavior — wraps everything for timing.
2. ValidationBehavior — rejects invalid requests before domain is touched.
3. DomainExceptionBehavior — closest to handler, catches invariant violations.

**Why:**
- Handlers contain only the happy path — no 	ry-catch boilerplate for domain invariant violations.
- System exceptions (NullReferenceException, DB failures, etc.) propagate freely to ExceptionHandlingMiddleware to return a 500 status code with a full stack trace.