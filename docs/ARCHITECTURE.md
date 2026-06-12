# Learnix — Architecture Specification

> For architectural decision records and rationale, see the `docs/` folder.

## Decision Record Convention

All ADRs (Architectural Decision Records) are written in **English** and use **file-scoped numbering** — each topic file has its own `ADR-<SCOPE>-NNN` sequence starting at `001`:

| File | Scope prefix | Example |
|---|---|---|
| `docs/DECISIONS_ARCHITECTURE.md` | `ADR-ARCH-NNN` | `ADR-ARCH-001` |
| `docs/DECISIONS_AUTH.md` | `ADR-AUTH-NNN` | `ADR-AUTH-001` |
| `docs/DECISIONS_DOMAIN.md` | `ADR-DOMAIN-NNN` | `ADR-DOMAIN-001` |
| `docs/DECISIONS_INFRA.md` | `ADR-INFRA-NNN` | `ADR-INFRA-001` |
| `docs/DECISIONS_CHAT.md` | `ADR-CHAT-NNN` | `ADR-CHAT-001` |
| `docs/DECISIONS_REVIEWS.md` | `ADR-REVIEW-NNN` | `ADR-REVIEW-001` |
| `docs/DECISIONS_MESSAGING.md` | `ADR-MSG-NNN` | `ADR-MSG-001` |

New feature decision files follow the pattern `docs/DECISIONS_<FEATURE>.md` with a matching `ADR-<FEATURE>-NNN` prefix.

ADRs are never deleted. If a decision is superseded, mark the old ADR with `Superseded by ADR-<SCOPE>-NNN` and the new one with `Supersedes ADR-<SCOPE>-NNN`.

---

## Overview

**Pattern:** Clean Architecture + Light DDD + CQRS
**API:** ASP.NET Core 8
**Mediator:** MediatR
**Validation:** FluentValidation (pipeline behavior, returns Result — see ADR-009)
**Result pattern:** FluentResults (see ADR-002, ADR-010)
**ORM:** Entity Framework Core (PostgreSQL via Npgsql)
**Blob storage:** Azure Blob Storage (via Azure SDK, SAS URLs)
**NoSQL:** MongoDB.Driver — **[Planned Phase 7+]**
**Cache:** Redis (StackExchange.Redis) — distributed cache via `ICacheable<TValue>` + `CachingBehavior` (see ADR-INFRA-004)
**Message Broker:** MassTransit + Azure Service Bus — **[Planned Phase 6+]**

---

## Layer Structure

```
Learnix.Domain           — Entities, Value Objects, Domain Events, Enums, Constants
Learnix.Application      — CQRS, Validators, Specifications, Interfaces, Pipeline Behaviors
Learnix.Infrastructure   — EF Core, Azure Blob, External Services, Outbox Worker
Learnix.API              — Controllers, Middleware, DI registration
```

### Dependency rule
```
API → Application → Domain        (allowed)
Infrastructure → Application      (allowed, implements interfaces)
Domain → nothing                  (no dependencies)
Application → Infrastructure      (FORBIDDEN — only via interfaces)
```

---

## Request Flow

```
HTTP Request
    ↓
Controller               (routes to MediatR, returns IActionResult)
    ↓
MediatR Pipeline
    ├── LoggingBehavior         (logs request name + duration, warns >3s)
    ├── ValidationBehavior      (FluentValidation → Result.Fail if invalid, see ADR-009)
    └── DomainExceptionBehavior (catches DomainException → ConflictError, see ADR-101)
    ↓
Command / Query Handler  (business logic, happy path only)
    ↓
    ├── [Query]   → repository.FirstOrDefaultAsync/ListAsync(specification)
    │             → map to DTO → return Result<T>
    │
    └── [Command] → repository.FirstOrDefaultAsync(specification, forUpdate: true)
                  → call entity method (entity raises Domain Event + enqueues Outbox messages)
                  → unitOfWork.SaveChangesAsync()
                       ↓ (DomainEventsInterceptor fires after commit)
                  → Domain Event dispatched via MediatR INotificationHandler (in-process)
                  → Outbox worker (background) processes blob confirm/delete messages
```

---

## CQRS

All operations go through MediatR. No business logic in controllers.

### Command structure
Every feature folder under `Application/{Feature}/Commands/{Name}/` contains:
```
UpdateProfileCommand.cs          — record with input data : IRequest<Result>
UpdateProfileCommandHandler.cs   — implements IRequestHandler<,>
UpdateProfileValidator.cs        — AbstractValidator<UpdateProfileCommand>
```

### Query structure
Every feature folder under `Application/{Feature}/Queries/{Name}/` contains:
```
GetMyProfileQuery.cs             — record with input data : IRequest<Result<TResponse>>
GetMyProfileQueryHandler.cs      — implements IRequestHandler<,>
MyProfileResponse.cs             — DTO returned to controller (co-located with query)
```

### Rules
- Commands return `Result` or `Result<T>` (FluentResults)
- Queries return `Result<TResponse>` (FluentResults)
- Expected errors → `Result.Fail(new NotFoundError(...))` — never throw for business errors (see ADR-010)
- Throw exceptions — only for unexpected infrastructure failures
- Never return domain entities from handlers — always map to DTOs (see ADR-012)

### Naming & file paths convention

| Element | Pattern | Example |
|---|---|---|
| Feature folder | `Application/{Feature}/` | `Application/Auth/` |
| Command folder | `Application/{Feature}/Commands/{Name}/` | `Application/Auth/Commands/Register/` |
| Query folder | `Application/{Feature}/Queries/{Name}/` | `Application/Courses/Queries/GetCourseById/` |
| Validator | `{Name}Validator.cs` (alongside command) | `RegisterValidator.cs` |
| Handler | `{Name}CommandHandler.cs` / `{Name}QueryHandler.cs` | `RegisterCommandHandler.cs` |
| Response/DTO | `{Name}Response.cs` co-located with query | `MyProfileResponse.cs` |
| Specifications | `Application/{Feature}/Specifications/{Name}Specification.cs` | `Application/Courses/Specifications/CourseByIdSpecification.cs` |
| Abstractions (cross-cutting) | `Application/Common/Abstractions/{Category}/I{Name}.cs` | `Application/Common/Abstractions/Persistence/IUnitOfWork.cs` |
| Abstractions (feature) | `Application/{Feature}/Abstractions/I{Name}.cs` | `Application/Auth/Abstractions/ITokenService.cs` |
| Models (feature) | `Application/{Feature}/Models/{Name}.cs` | `Application/Auth/Models/UserAuthenticationInfo.cs` |

Feature names directly under `Application/` — no intermediate `Features/` folder. Alongside them: `Application/Common/` for cross-cutting infrastructure.

**Rule for new interfaces:** "Does this interface make sense outside a single feature?"
- Yes → `Common/Abstractions/{Category}/`
- No → `{Feature}/Abstractions/`

See ADR-030.

---

## Result Pattern (FluentResults)

Application layer uses [FluentResults](https://github.com/altmann/FluentResults) (see ADR-002). No custom Result<T>.

### Typed errors (ADR-010)
```csharp
// Learnix.Application/Common/Errors/
NotFoundError(string message)       // → 404
ConflictError(string message)       // → 409
ForbiddenError(string message)      // → 403
AuthenticationError(string message) // → 401
ValidationError(ValidationResult)   // → 400 with errors dictionary
```

### When to use
- `Result.Fail(new NotFoundError(...))` — expected domain errors
- `Result.Fail(new ConflictError(...))` — duplicates, publish invariant violations, etc.
- Throw exceptions — only for unexpected infrastructure failures (DB unavailable, etc.)

### Controller mapping

Centralised in `Learnix.API/Extensions/ResultExtensions.cs`:

```csharp
[HttpGet("me")]
public async Task<IActionResult> GetMyProfile(CancellationToken ct)
{
    var result = await sender.Send(new GetMyProfileQuery(), ct);
    return result.ToActionResult(); // 200 OK with body, or mapped error
}
```

Error responses use ProblemDetails (RFC 7807) — see ADR-017.

### HTTP status code mapping

| FluentResults error type | HTTP status | Body |
|---|---|---|
| `ValidationError` | 400 Bad Request | `ValidationProblemDetails` with `errors` dictionary |
| `NotFoundError` | 404 Not Found | `ProblemDetails` |
| `ConflictError` | 409 Conflict | `ProblemDetails` |
| `AuthenticationError` | 401 Unauthorized | `ProblemDetails` |
| `ForbiddenError` | 403 Forbidden | `ProblemDetails` |
| Any other `Error` | 400 Bad Request | `ProblemDetails` with aggregated messages |
| `Result.IsSuccess` (void) | 204 No Content | empty |
| `Result<T>.IsSuccess` | 200 OK / 201 Created | DTO |

---

## Validation Pipeline

FluentValidation runs automatically for every Command and Query via `ValidationBehavior<TRequest, TResponse>`.

Returns `Result.Fail()` with `ValidationError` on validation errors — no exceptions thrown (see ADR-009).

`ExceptionHandlingMiddleware` handles only unexpected infrastructure failures.

---

## Domain Exception Pipeline Behavior (ADR-101)

`DomainExceptionBehavior<TRequest, TResponse>` sits closest to the handler in the MediatR pipeline. It catches `Learnix.Domain.Common.Exceptions.DomainException` and returns `Result.Fail(new ConflictError(ex.Message))`.

This means handlers contain only the happy path — no `try-catch` for domain invariant violations.

**Pipeline order (critical):**
1. `LoggingBehavior` — wraps everything for timing
2. `ValidationBehavior` — rejects invalid requests before domain is touched
3. `DomainExceptionBehavior` — closest to handler, catches invariant violations

Only `DomainException` is caught. System exceptions (`NullReferenceException`, DB failures, etc.) propagate freely to `ExceptionHandlingMiddleware` for 500 with full stack trace.

---

## Domain Entities

### Domain primitives — interfaces

```csharp
// Learnix.Domain/Common/IAuditable.cs
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}

// Learnix.Domain/Common/IHasDomainEvents.cs
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

// Learnix.Domain/Common/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}

// Learnix.Domain/Common/IOrderable.cs
public interface IOrderable
{
    int DisplayOrder { get; }
}
```

Interfaces used by EF interceptors and `ApplicationDbContext` for unified cross-cutting concern handling without coupling to a specific base class. See ADR-023.

### BaseEntity — used by most entities

```csharp
// Learnix.Domain/Common/BaseEntity.cs
public abstract class BaseEntity : IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    // virtual hook for cleanup before soft delete
    public virtual void PrepareForDeletion() { }
}
```

`SoftDeletableEntity` extends `BaseEntity` and adds `ISoftDeletable` — used by `Course`.

### User — separate case (inherits IdentityUser)

`User` cannot inherit `BaseEntity` because `IdentityUser<Guid>` already provides `Id`. So `User` implements `IAuditable` and `IHasDomainEvents` directly:

```csharp
public class User : IdentityUser<Guid>, IAuditable, IHasDomainEvents
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? AvatarBlobPath { get; private set; }
    public string? Bio { get; private set; }
    public string? GoogleId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // IHasDomainEvents implemented directly (same code as BaseEntity)
    // Domain methods: UpdateProfile(), SetAvatar(), RaiseUserRegistered(), ...
}
```

The `IAuditable`/`IHasDomainEvents` duplication in `User` is the conscious cost of having one class instead of two (Domain User + Identity User with sync). See ADR-018, ADR-023.

### Soft delete

Entities that support soft delete extend `SoftDeletableEntity` (which extends `BaseEntity`). Currently: `Course`.

- `SoftDeleteInterceptor` intercepts `Remove()` — sets `IsDeleted = true`, `DeletedAt = UtcNow`
- Global query filter in `OnModelCreating` auto-excludes soft-deleted records
- Use `.IgnoreQueryFilters()` for admin queries that need deleted records (see ADR-016)

### Entity design conventions (ADR-015)

- Properties with `private set` — no public setters
- State change through methods that reflect business operations
- One method = one business action

```csharp
// Good
public void UpdateDetails(string title, string description, decimal price, Guid categoryId) { ... }
public void Publish() { ... }
public void Archive() { ... }

// Bad — setter per property
public void SetTitle(string title) { ... }
```

### Value objects — Questions and Answers

`Question`, `QuestionOption`, `TextAnswerConfig` are **value objects** stored as JSONB inside `TestLesson`. `StudentAnswer` is a record stored as JSONB inside `TestAttempt`. No separate tables. Scoring logic lives inside `Question.IsAnsweredCorrectly()`.

### Course aggregate — structure through root (ADR-044)

`Course` is the aggregate root over `Section` and `Lesson`. All structural operations go through `Course` public methods:

- `AddSection()`, `UpdateSectionTitle()`, `RemoveSection()`, `ReorderSections()`
- `AddVideoLesson()`, `AddPostLesson()`, `UpdateVideoLesson()`, `RemoveLesson()`, `ReorderLessons()`
- `SetCoverImage()`, `Publish()`, `Unpublish()`, `Archive()`, `MarkForDeletion()`

`Section` and `Lesson` mutation methods are `internal` — accessible only from Domain assembly, enforcing `Course` as the sole entry point.

**Publish invariants (ADR-045)** checked continuously by `Course.EnsurePublishableInvariants()` — called from every mutation that could violate them. A Published course can never end up without a cover / without sections / without visible lessons.

**Archived** — fully read-only. `EnsureStructureMutable()` blocks any structure mutation on an Archived course.

**Handler pattern for any structure mutation:**
1. Fetch `Course` via specification with `forUpdate: true` (tracking, includes Sections.Lessons)
2. Auth + owner check via `ICurrentUserService`
3. Call domain method
4. `unitOfWork.SaveChangesAsync()`
5. `DomainExceptionBehavior` catches invariant violations automatically → `ConflictError`

### CourseCommandHandler base class

To eliminate boilerplate from structure mutation handlers, `CourseCommandHandler<TCommand, TResult>` provides steps 1–3 automatically:

```csharp
// Learnix.Application/Common/Commands/CourseCommandHandler.cs
internal abstract class CourseCommandHandler<TCommand, TResult>(
    ICourseRepository courseRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<TCommand, TResult>
    where TCommand : IRequest<TResult>, ICommandWithCourseId
    where TResult : ResultBase, new()
{
    // Handles: authentication check, course fetch, ownership check, EnsureStructureMutable()
    // Delegates to: abstract Task<TResult> HandleAsync(TCommand request, Course course, CancellationToken ct)
}
```

`CourseSectionCommandHandler<TCommand, TResult>` extends this with an additional section-exists check.

### Constant conventions (ADR-027)

- **Domain** (`Learnix.Domain/Constants/{Entity}Constants.cs`) — entity invariants (max field lengths). Consumed by EF configurations and Application validators. Single source of truth.
- **Application** (`Learnix.Application/{Feature}/Constants/{Feature}ValidationConstants.cs`) — feature-specific validation policies (password complexity, RFC standards). Not entity invariants.

---

## Domain Event → MediatR Adapter

`IDomainEvent` in Domain is a clean marker interface with no MediatR dependency (see ADR-019).
The MediatR-specific wrapper lives in Application:

```csharp
// Learnix.Application/Common/Events/
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification
    where TDomainEvent : IDomainEvent;
```

Event handlers subscribe to the wrapper, not the raw event:

```csharp
public class UserRegisteredDomainEventHandler
    : INotificationHandler<DomainEventNotification<UserRegisteredDomainEvent>>
{
    public async Task Handle(DomainEventNotification<UserRegisteredDomainEvent> n, CancellationToken ct)
    {
        var evt = n.DomainEvent;
        await emailSender.SendEmailConfirmationAsync(evt.Email, evt.FirstName, link, ct);
    }
}
```

---

## EF Core Interceptors

### AuditableInterceptor (ADR-014)
Sets `CreatedAt` / `UpdatedAt` automatically for all entities implementing `IAuditable`.
Covers both `BaseEntity` descendants and `User` (which implements `IAuditable` directly).

### SoftDeleteInterceptor (ADR-016)
Intercepts `Delete()` calls on `ISoftDeletable` entities — sets `IsDeleted + DeletedAt` instead of removing.

### DomainEventsInterceptor
Fires after `SaveChangesAsync` succeeds. Collects all `IHasDomainEvents` entries with pending events, wraps each in `DomainEventNotification<T>` via reflection, publishes through MediatR, then clears the event lists.

```csharp
// Called after base.SaveChangesAsync in DomainEventsInterceptor
foreach (var domainEvent in pendingEvents)
{
    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
    var notification = Activator.CreateInstance(notificationType, domainEvent)!;
    await publisher.Publish(notification, cancellationToken);
}
```

> **Known risk:** if the process crashes between `SaveChangesAsync` and `Publish`, domain events are lost. Accepted for current phase. Domain events currently drive only emails (via `IEmailSender`) and blob operations (via outbox). Blob operations are durable via `OutboxMessage` (see ADR-047). Email loss is acceptable pre-MassTransit.

### Global query filter for soft delete
Applied in `ApplicationDbContext.OnModelCreating` — automatically excludes `IsDeleted = true` records from all queries on `ISoftDeletable` entities.

---

## Blob Storage (Azure Blob)

Client uploads go through a two-phase flow: **pre-signed upload URL → entity persist**.

### IBlobStorageService
```csharp
// Learnix.Application/Common/Abstractions/Storage/IBlobStorageService.cs
Task<UploadUrlResponse> GenerateUploadUrlAsync(UploadTarget target, string contentType, CancellationToken ct);
Task<Result<BlobMetadata>> ValidateAsync(string blobPath, UploadTarget target, CancellationToken ct);
Task MarkConfirmedAsync(string blobPath, CancellationToken ct);
string GenerateReadUrl(string blobPath, TimeSpan ttl);
Task DeleteAsync(string blobPath, CancellationToken ct);
```

### Upload flow

```
1. Client → POST /api/uploads/request-url { target, contentType }
      ↓
   RequestUploadUrlCommandHandler
      → IBlobStorageService.GenerateUploadUrlAsync()
      → returns { uploadUrl (SAS, 15 min), blobPath }

2. Client → PUT {uploadUrl}   (direct to Azure, bypasses API)

3. Client → PUT /api/users/me  { avatarBlobPath: "avatars/users/..." }
      ↓
   UpdateProfileCommandHandler
      → IBlobStorageService.ValidateAsync(blobPath, Avatar)  ← magic byte check
      → user.SetAvatar(blobPath)    ← raises UserAvatarRemovedDomainEvent (old) + UserAvatarSetDomainEvent (new)
      → unitOfWork.SaveChangesAsync()
      → DomainEventsInterceptor dispatches events
      → event handler writes OutboxMessage(MarkBlobConfirmed) + OutboxMessage(DeleteBlob for old)
      → OutboxWorker (background) calls IBlobStorageService.MarkConfirmedAsync / DeleteAsync
```

Blobs without confirmed tag are automatically cleaned up by Azure lifecycle policy (TTL).

### UploadTarget validation
| Target | Max size | Content types |
|---|---|---|
| `Avatar` | 5 MB | jpeg, png, webp |
| `CourseCover` | 10 MB | jpeg, png, webp |
| `LessonVideo` | 2 GB | mp4, webm |
| `Certificate` | 5 MB | pdf |

Content type validated via **magic bytes**, not `Content-Type` header.

### Blob path structure
```
avatars/users/{userId}/{uploadId}.{ext}
course-covers/courses/{courseId}/{uploadId}.{ext}
course-videos/courses/{courseId}/lessons/{lessonId}/{uploadId}.mp4
certificates/{code}.pdf
```

---

## Outbox Pattern

The outbox is used for **durable side-effects** and **background processing** to ensure reliability without blocking the API response. It was originally introduced for blob storage but has been expanded to cover the achievement system.

`OutboxMessage` records are written **in the same EF transaction** as entity changes (via domain event handlers that write to `OutboxMessage` table through the same `SaveChangesAsync`). A background worker (`OutboxProcessorService`) processes them instantly using PostgreSQL `LISTEN/NOTIFY` push notifications, with a 10-second polling fallback.

**Message types:**
- **Blob Operations:** `MarkBlobConfirmed`, `DeleteBlob` (deletes orphaned blobs)
- **Achievements:** `EvaluateLessonCompleted`, `EvaluateEnrollmentCompleted`, `EvaluateTestSubmitted`, `EvaluateProfileChanged` (evaluates progress), and `NotifyAchievementUnlocked` (sends SignalR push)

**Retry:** exponential backoff via `NextRetryAt`. `AttemptCount` and `LastError` tracked per message.

---

## Pagination (ADR-013)

Shared classes in `Application/Common/Pagination/`.

```csharp
public record PaginationRequest
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public int PageIndex { get; init; }   // zero-based
    public int PageSize { get; init; }    // clamped 1..100

    public int Skip => PageIndex * PageSize;
    public int Take => PageSize;

    public static PaginationRequest FromOffset(int skip, int take) { ... }
}

public record PaginatedResult<TEntity>(
    int PageIndex,
    int PageSize,
    long TotalCount,
    IReadOnlyList<TEntity> Data)
{
    public int TotalPages => ...;
    public bool HasNextPage => ...;
    public bool HasPreviousPage => ...;
}
```

---

## Specification Pattern

Uses **Ardalis.Specification** library — not a custom base class (see ADR-006).

```csharp
// Example specification
public sealed class CourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdSpecification(Guid id, bool includeSections = false, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (includeSections)
            Query.Include(c => c.Sections).ThenInclude(s => s.Lessons);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
```

### AsNoTracking convention
- All specifications default to `AsNoTracking()` — read-only, no change tracking
- Explicitly **omit** `AsNoTracking()` only for specifications used in Commands that modify entities (`forUpdate: true` parameter)

### Specification location
```
Application/{Feature}/Specifications/{Name}Specification.cs
```

---

## Repository Pattern

Specific repository interface per aggregate root, extending `IRepositoryBase<T>` from Ardalis.Specification.

### Interface (Application layer)
```csharp
// Learnix.Application/Courses/Abstractions/ICourseRepository.cs
public interface ICourseRepository : IRepositoryBase<Course>
{
}

// Learnix.Application/Users/Abstractions/IUserRepository.cs
public interface IUserRepository : IRepositoryBase<User>
{
}
```

### Implementation (Infrastructure layer)
```csharp
// Learnix.Infrastructure/Persistence/Repositories/CourseRepository.cs
internal sealed class CourseRepository(ApplicationDbContext context)
    : RepositoryBase<Course>(context), ICourseRepository
{
}
```

`RepositoryBase<T>` from Ardalis provides `FirstOrDefaultAsync`, `ListAsync`, `CountAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` — all accept `Specification<T>`.

### Current repositories
| Interface | Feature | Notes |
|---|---|---|
| `ICourseRepository` | `Courses/Abstractions/` | Aggregate root |
| `ILessonRepository` | `Lessons/Abstractions/` | For lesson-specific queries |
| `ICategoryRepository` | `Courses/Abstractions/` | Category lookup |
| `IUserRepository` | `Users/Abstractions/` | Profile read/update |
| `IRefreshTokenRepository` | `Auth/Abstractions/` | Token rotation |

### Unit of Work
`SaveChanges` is called only in handlers via `IUnitOfWork`, not inside repositories.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

`ApplicationDbContext` implements `IUnitOfWork` (ADR-021). DI resolves both to the same scoped instance:

```csharp
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
```

---

## In-Process Email (Current — Phase 2–5)

Email is sent in-process via `IEmailSender` → `ConsoleEmailSender` (logs to console). Domain event handlers call `IEmailSender` directly without a message bus.

**Planned replacement (Phase 6):** Domain event handlers publish Integration Events via MassTransit → consumer calls `IEmailSender`. The interface stays unchanged; only the dispatch path changes.

---

## Integration Events & MassTransit **[Planned — Phase 6+]**

Domain Event Handlers will publish Integration Events to Azure Service Bus via MassTransit.

### Planned flow
```
DomainEventHandler (Application, in-process)
    → publishes IntegrationEvent via IBus
        → MassTransit → Azure Service Bus
            → Consumer (Infrastructure, async)
```

### Planned consumers
| Integration Event | Consumer | Action |
|---|---|---|
| `UserRegisteredIntegrationEvent` | `SendVerificationEmailConsumer` | Send verification email |
| `CourseEnrolledIntegrationEvent` | `SendEnrollmentEmailConsumer` | Send welcome email |
| `CourseCompletedIntegrationEvent` | `NotifyCertificateAvailableConsumer` | Send certificate available email |
| `PaymentCompletedIntegrationEvent` | `ConfirmEnrollmentConsumer` | Activate enrollment |

---

## Caching (Redis) **[Planned — Phase 7+]**

`ICacheable` interface and `CachingBehavior` in MediatR pipeline are designed but not yet wired up. Queries that implement `ICacheable` will be served from Redis when available.

---

## Security

- JWT 15 min access + 7-day refresh with rotation + replay protection + HttpOnly cookie (ADR-003, ADR-033, ADR-034)
- Email confirmation soft restriction: unconfirmed users can browse freely; write actions (enrollment, payment, reviews, messaging, instructor application) gated by `EmailConfirmed` ASP.NET Core policy on `email_verified` JWT claim (ADR-AUTH-014)
- Refresh tokens: SHA-256 hash in PostgreSQL. Plain token only in HttpOnly client cookie. DB leak does not compromise sessions
- Background cleanup: `RefreshTokenCleanupHostedService` removes revoked/expired tokens older than `ExpiresAt + 7d` daily
- Google OAuth via GIS ID token flow — no Authorization Code flow, no Client Secret (ADR-036)
- Rate limiting on auth endpoints: 5 requests / 15 min per IP, FixedWindow, in-memory (ADR-038)
- Security headers via `SecurityHeadersMiddleware`: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- Blob upload validation: magic byte check + size limit per UploadTarget
- Input validation: FluentValidation (backend) + Zod (frontend)
- Error responses: ProblemDetails (RFC 7807) — see ADR-017
- Authorization checks in handlers, not controllers (ADR-039)

---

## Logging

Serilog with structured logging.

```csharp
Log.Information("User {UserId} enrolled in course {CourseId}", userId, courseId);
```

Sinks:
- Console (development)
- Azure Application Insights (production — planned)

`LoggingBehavior` in MediatR pipeline logs every request name and duration. Requests >3 seconds logged as warnings.

Request payload is intentionally NOT logged — prevents accidental PII leak (passwords in `RegisterCommand`, tokens in `ResetPasswordCommand`).

---

## Authentication

### Token strategy

| Token | Lifetime | Storage | Transport |
|---|---|---|---|
| Access (JWT) | 15 min | Client memory | `Authorization: Bearer` header |
| Refresh | 7 days | HttpOnly cookie + PostgreSQL (SHA-256 hash) | Cookie auto-sent on `/api/auth/*` |

### Flow

```
Login                           Refresh                         Logout
─────                           ───────                         ──────
POST /auth/login                POST /auth/refresh              POST /auth/logout
  ↓                               ↓ (cookie auto-attached)        ↓
ValidateCredentials             Find token by SHA-256 hash      Find token by SHA-256 hash
  ↓                               ↓                               ↓
Generate JWT + refresh          IsRevoked → REPLAY DETECTED     Revoke if active
  ↓                               → revoke ALL active tokens      ↓
Store refresh hash in DB        → 401 + clear cookie            Clear cookie
  ↓                               ↓
Set HttpOnly cookie             Else: revoke old, issue new pair
  ↓                             → set cookie, return access token
Return access in body
```

### Google OAuth (ADR-036)

Frontend gets ID token via Google Identity Services SDK. Backend validates via `GoogleJsonWebSignature.ValidateAsync`, then issues its own JWT+refresh pair (same `LoginResponse` as regular login).

```
Frontend (Google SDK) → id_token
↓
POST /api/auth/google { idToken }
↓
IGoogleTokenValidator.ValidateAsync(idToken)   — signature, iss, aud, exp, email_verified
↓
IUserRegistrationService.FindOrCreateGoogleUserAsync(googleUser)
├── GoogleId found → return existing user
├── Email found, confirmed → link GoogleId
├── Email found, unconfirmed → takeover (wipe password, confirm, link)
└── Not found → create new (PasswordHash null, EmailConfirmed true, Student role)
↓
Generate access + refresh (same path as LoginCommandHandler)
```

### Cookie configuration

```csharp
new CookieOptions
{
    HttpOnly = true,                   // XSS-resistant
    Secure = Request.IsHttps,          // HTTPS-only in production
    SameSite = SameSiteMode.Strict,    // CSRF-resistant
    Path = "/api/auth",                // least exposure
    Expires = refreshTokenExpiresAt
}
```

### Current User Context

`ICurrentUserService` reads JWT claims from `HttpContext`. Registered as scoped.

- `UserId: Guid?` — null for anonymous requests
- `Email: string?`
- `IsAuthenticated: bool`
- `IsEmailConfirmed: bool` — reads `email_verified` claim; used by the `EmailConfirmed` policy (ADR-AUTH-014)
- `GetRoles() : IReadOnlyList<string>`
- `IsInRole(role) : bool`

Controllers do NOT use `ICurrentUserService` — authorization decisions belong in handlers (ADR-039).

### Email Confirmation Gate (ADR-AUTH-014)

After registration users are auto-logged in with `email_verified: false` in their JWT. A persistent frontend banner prompts confirmation. Specific write endpoints are protected by the named policy `EmailConfirmed`:

```csharp
// Learnix.API — registered in AddApiServices
options.AddPolicy("EmailConfirmed", policy =>
    policy.RequireClaim("email_verified", "true"));
```

**Gated endpoints** (`[Authorize(Policy = "EmailConfirmed")]`):

| Endpoint | Reason |
|---|---|
| `POST /api/enrollments` | Free course enrollment — all downstream actions (progress, tests, certificates) cascade from this gate |
| Stripe checkout (Phase 9) | Paid enrollment — creates the same `Enrollment` record on payment success |
| `POST /api/instructor-applications` | Triggers admin review; unverified spam is a moderation risk |
| `POST /api/courses/{id}/reviews` | Public content tied to a verified identity |
| `PUT /api/courses/{id}/reviews/{reviewId}` | Same |
| `POST /api/messages/conversations/start-or-get` | First human contact; requires trust |
| `POST /api/messages/conversations/{id}/messages` | Same |

**Not gated:** catalog browsing, course detail, reading reviews, own profile (read + edit), AI chat. Progress / tests / certificates cascade from enrollment — no separate gate needed.

The check is at the controller (HTTP) level, not inside handlers — "is identity verified?" is an auth concern, not domain logic (consistent with ADR-013 which scopes handler auth checks to resource-based owner decisions).

Typical handler pattern:
```csharp
if (currentUser.UserId is null)
    return Result.Fail(new AuthenticationError("Not authenticated."));

if (course.InstructorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
    return Result.Fail(new ForbiddenError("You are not the owner of this course."));
```

---

## Application Settings

Config sections from `appsettings.json` mapped to typed POCOs in `Application/Common/Settings/{Name}Settings.cs`. Registered via `services.Configure<T>(...)` in `Infrastructure/DependencyInjection.cs`. Consumed via `IOptions<T>`.

Current settings:
- `AppSettings` — `ClientBaseUrl`
- `JwtSettings` — `Issuer`, `Audience`, `Secret`, `AccessTokenExpiryMinutes`, `RefreshTokenExpiryDays`
- `GoogleSettings` — `ClientId`
- `BlobStorageOptions` — container names and configuration

Conventions:
- POCOs in `Application/Common/Settings/` — Application layer knows configuration types, not `IConfiguration` directly
- Registration only in `Infrastructure/DependencyInjection.cs`
- Connection strings separately: `ConnectionStrings:Postgres`, `ConnectionStrings:AzureBlobStorage`

---

## Database Migrations

EF Core Code-First. Migrations in `Learnix.Infrastructure/Persistence/Migrations/`.

```bash
# Create new migration
dotnet ef migrations add {Name} \
    --project Learnix.Infrastructure \
    --startup-project Learnix.API \
    --output-dir Persistence/Migrations

# Apply
dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
```

- **Development:** auto-applied on startup via `app.ApplyMigrationsAsync()` when `IsDevelopment()` (ADR-029)
- **Staging/Production:** separate controlled step in CI/CD

---

## Project Structure Reference

```
Learnix.Domain/
├── Common/             ← BaseEntity, SoftDeletableEntity, interfaces (IAuditable, IHasDomainEvents,
│                         ISoftDeletable, IOrderable, IDomainEvent), DomainEvent base record,
│                         ReorderValidation helper, DomainException
├── Constants/          ← Roles, UserConstants, CourseConstants, LessonConstants, etc.
├── Entities/           ← User, Course, Section, Lesson (Video/Post/Test), Category,
│                         Enrollment, Certificate, CourseReview, LessonProgress,
│                         TestAttempt, RefreshToken, OutboxMessage
├── Events/
│   ├── (root)          ← UserRegisteredDomainEvent, PasswordResetRequestedDomainEvent
│   ├── User/           ← UserAvatarSetDomainEvent, UserAvatarRemovedDomainEvent
│   ├── Course/         ← CourseCreatedDomainEvent, CoursePublishedDomainEvent, etc.
│   └── Lessons/        ← LessonVideoAttachedDomainEvent, LessonVideoReleasedDomainEvent
└── Enums/              ← CourseStatus, LessonType, QuestionType, EnrollmentStatus, etc.

Learnix.Application/
├── Common/
│   ├── Abstractions/
│   │   ├── Identity/       ← ICurrentUserService
│   │   ├── Messaging/      ← IEmailSender
│   │   ├── Persistence/    ← IUnitOfWork
│   │   └── Storage/        ← IBlobStorageService, UploadTarget, UploadUrlResponse, BlobMetadata
│   ├── Behaviors/      ← LoggingBehavior, ValidationBehavior, DomainExceptionBehavior
│   ├── Commands/       ← CourseCommandHandler<,>, CourseSectionCommandHandler<,>
│   ├── Constants/      ← CommonMessages
│   ├── Errors/         ← NotFoundError, ConflictError, ForbiddenError, AuthenticationError, ValidationError
│   ├── Events/         ← DomainEventNotification<T>
│   ├── Extensions/     ← ICurrentUserService extensions (IsOwnerOrAdmin, etc.)
│   ├── Models/         ← ReorderItem
│   ├── Pagination/     ← PaginatedResult<T>, PaginationRequest
│   └── Settings/       ← AppSettings, JwtSettings, BlobStorageOptions
├── Auth/
│   ├── Abstractions/   ← IUserRegistrationService, IUserAuthenticationService, ITokenService,
│   │                     IRefreshTokenRepository, IGoogleTokenValidator, IPasswordResetService
│   ├── Commands/       ← Register, ConfirmEmail, Login, Logout, RefreshToken, GoogleLogin,
│   │                     ForgotPassword, ResetPassword, ResendConfirmationEmail
│   ├── EventHandlers/  ← UserRegisteredDomainEventHandler, PasswordResetRequestedDomainEventHandler
│   ├── Constants/      ← AuthValidationConstants
│   ├── Models/         ← UserAuthenticationInfo, AccessTokenResult, RefreshTokenResult
│   ├── Specifications/ ← RefreshTokenByHashSpecification, ActiveRefreshTokensByUserSpecification
│   └── Validation/     ← PasswordRules (FluentValidation extension)
├── Courses/
│   ├── Abstractions/   ← ICourseRepository, ICategoryRepository, IPublicCourseCatalogSearchService
│   ├── Commands/       ← CreateCourse, UpdateCourseDetails, PublishCourse, UnpublishCourse,
│   │                     ArchiveCourse, DeleteCourse
│   ├── Queries/        ← GetCourseById, GetCourseForEditById, GetPublicCourses (+ instructorId filter),
│   │                     GetInstructorCourses, GetAdminCourses
│   └── Specifications/ ← CourseByIdSpecification, CourseListSpecification, CourseListCountSpecification
├── Sections/
│   └── Commands/       ← CreateSection, UpdateSectionTitle, DeleteSection, ReorderSections
├── Lessons/
│   ├── Abstractions/   ← ILessonRepository
│   └── Commands/       ← CreateVideoLesson, CreatePostLesson, UpdateVideoLesson, UpdatePostLesson,
│                         DeleteLesson, ReorderLessons
├── Tests/
│   ├── Commands/       ← CreateTestLesson, UpdateTestLesson, UpdateTestSettings
│   └── Queries/        ← GetTestLesson, GetTestAttempt
├── Uploads/
│   └── Commands/       ← RequestUploadUrl
├── Enrolment/
│   └── Commands/       ← (enrollment flows)
├── Users/
│   ├── Abstractions/   ← IUserRepository
│   ├── Commands/       ← UpdateProfile
│   ├── Queries/        ← GetMyProfile, GetUserProfile
│   └── Specifications/ ← UserByIdSpecification
└── Categories/
    ├── Commands/       ← CreateCategory, UpdateCategory, DeleteCategory
    └── Queries/        ← GetCategories

Learnix.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/     ← EF entity type configurations
│   ├── Interceptors/       ← AuditableInterceptor, SoftDeleteInterceptor, DomainEventsInterceptor
│   ├── Repositories/       ← CourseRepository, LessonRepository, CategoryRepository,
│   │                         RefreshTokenRepository, UserRepository
│   └── Migrations/
├── Outbox/
│   ├── OutboxMessage.cs    ← (also in Domain/Entities — cross-reference)
│   ├── IOutboxMessageDispatcher.cs
│   ├── OutboxMessageDispatcher.cs
│   └── OutboxMessageTypes.cs
├── Identity/           ← UserRegistrationService, UserAuthenticationService, JwtTokenService,
│                         PasswordResetService, GoogleTokenValidator, CurrentUserService
├── Services/           ← ConsoleEmailSender, PublicCourseCatalogSearchService,
│                         BlobStorageBootstrapper, RefreshTokenCleanupHostedService,
│                         RoleSeederHostedService, CategorySeederHostedService
├── Storage/            ← AzureBlobStorageService
└── DependencyInjection.cs

Learnix.API/
├── Controllers/        ← AuthController, CoursesController, SectionsController, LessonsController,
│                         TestsController, UploadsController, UsersController, CategoriesController
├── Extensions/         ← ResultExtensions (ToActionResult), WebApplicationExtensions
├── Middleware/         ← ExceptionHandlingMiddleware, SecurityHeadersMiddleware
└── RateLimiting/       ← RateLimitPolicies
```

---

## Testing Strategy

Tests deferred to Phase 5 completion. Architecture reserved:

```
Learnix.Backend/
├── Learnix.Domain.UnitTests/        — entity behavior, domain methods, value object logic
├── Learnix.Application.UnitTests/   — handlers (mock IRepository, IIdentityService, ...)
└── Learnix.Integration.Tests/       — full HTTP flow with Testcontainers (Postgres)
```

Stack: xUnit + FluentAssertions + NSubstitute + Testcontainers.
`InternalsVisibleTo` needed for Domain unit tests to access `internal` Section/Lesson mutation methods.
