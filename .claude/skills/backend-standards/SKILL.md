---
name: backend-standards
description: Backend coding standards, architecture rules, CQRS patterns, Result<T> usage, Specification pattern, domain entity conventions, and pre/post task checklists for the Learnix .NET 8 backend. Use when implementing backend features, writing handlers, adding commands/queries, modifying domain entities, or reviewing backend architecture.
when_to_use: backend, C#, .NET, handler, command, query, controller, entity, domain, specification, repository, validator, MediatR, FluentResults, FluentValidation, EF Core, migration
---

# Learnix Backend Standards

## Pre-Task Checklist

Before implementing **any** backend feature, complete these steps in order:

1. **Read `TODO.md`** — identify the exact task(s), their phase, and current status.
2. **Read `docs/FEATURES.md`** — understand the functional spec for the feature being built.
3. **Read `docs/ARCHITECTURE.md`** — review layer rules, request flow, and relevant patterns.
4. **Check relevant ADR files in `docs/`** — read only those that apply to the current task scope (Auth → `DECISIONS_AUTH.md`, Domain → `DECISIONS_DOMAIN.md`, Infra → `DECISIONS_INFRA.md`, etc.).
5. **Check the relevant `Controllers/*.cs`** — verify route prefix, HTTP method, request/response shape, and auth requirements before writing any handler or DTO.
6. **Look at an existing similar handler** — find a handler in the same feature folder or a nearby one that does something similar. Match its style exactly.

---

## Layer Rules (never break these)

```
Learnix.Domain        → no external dependencies
Learnix.Application   → depends only on Domain + interfaces
Learnix.Infrastructure→ implements Application interfaces; depends on Application
Learnix.API           → depends on Application; thin, no business logic
```

- **Domain** has zero NuGet dependencies. No MediatR, no EF, no FluentValidation.
- **Application** never references `DbContext`, `EF Core`, or any Infrastructure type directly — only via interfaces.
- **Controllers** never call repositories, never call domain methods, never contain `if` based on domain state. They delegate to `ISender` and call `result.ToActionResult()`.
- **Infrastructure** never makes business decisions. It only performs technical side-effects (write to DB, call Azure, write Outbox). If Infrastructure code contains an `if` based on a domain entity's state — it's a bug (see ADR-010).

---

## CQRS — File Structure

Every operation lives in its own folder. No exceptions.

### Command
```
Application/{Feature}/Commands/{Name}/
    {Name}Command.cs              — record : IRequest<Result> or IRequest<Result<T>>
    {Name}CommandHandler.cs       — sealed class : IRequestHandler<,>
    {Name}Validator.cs            — sealed class : AbstractValidator<{Name}Command>
```

### Query
```
Application/{Feature}/Queries/{Name}/
    {Name}Query.cs                — record : IRequest<Result<TResponse>>
    {Name}QueryHandler.cs         — sealed class : IRequestHandler<,>
    {Name}Response.cs             — DTO co-located here, NOT in a separate DTOs folder
```

### Specifications
```
Application/{Feature}/Specifications/{Name}Specification.cs
```

### Naming table

| Element | Pattern | Example |
|---|---|---|
| Feature folder | `Application/{Feature}/` | `Application/Courses/` |
| Command folder | `Application/{Feature}/Commands/{Name}/` | `Application/Auth/Commands/Register/` |
| Query folder | `Application/{Feature}/Queries/{Name}/` | `Application/Courses/Queries/GetCourseById/` |
| Handler | `{Name}CommandHandler.cs` / `{Name}QueryHandler.cs` | `RegisterCommandHandler.cs` |
| Validator | `{Name}Validator.cs` | `RegisterValidator.cs` |
| Response/DTO | `{Name}Response.cs` co-located with query | `CourseDetailDto.cs` |
| Specification | `{Name}Specification.cs` | `CourseByIdSpecification.cs` |
| Feature abstractions | `Application/{Feature}/Abstractions/I{Name}.cs` | `Auth/Abstractions/ITokenService.cs` |
| Cross-cutting abstractions | `Application/Common/Abstractions/{Category}/I{Name}.cs` | `Common/Abstractions/Persistence/IUnitOfWork.cs` |

---

## Result Pattern

All handlers return `Result` or `Result<T>` from **FluentResults**. Never use exceptions for business errors.

### Typed errors → HTTP status mapping

| Error type | HTTP | When to use |
|---|---|---|
| `NotFoundError` | 404 | Entity not found |
| `ConflictError` | 409 | Duplicate, invariant violation, already enrolled |
| `ForbiddenError` | 403 | Not owner / wrong role |
| `AuthenticationError` | 401 | Not authenticated |
| `ValidationError` | 400 | Validation (usually from `ValidationBehavior`, rarely manual) |

All error types live in `Application/Common/Errors/`.

### Handler auth check pattern
```csharp
// Always check auth first in command handlers
if (currentUser.UserId is null)
    return Result.Fail(new AuthenticationError("User is not authenticated."));

// Then ownership / role
if (course.InstructorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
    return Result.Fail(new ForbiddenError("You are not the owner of this course."));
```

Authorization decisions belong **in handlers**, not in controllers (see ADR-039).

### Typical command handler shape
```csharp
public sealed class CreateCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCourseCommand, Result<CreateCourseResponse>>
{
    public async Task<Result<CreateCourseResponse>> Handle(
        CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 1. Auth check
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("User is not authenticated."));

        // 2. Role / ownership check
        if (!currentUser.IsInRole(Roles.Instructor) && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only instructors can create courses."));

        // 3. Load dependency via specification
        if (!await categoryRepository.AnyAsync(new CategoryByIdSpecification(request.CategoryId), cancellationToken))
            return Result.Fail(new NotFoundError($"Category '{request.CategoryId}' was not found."));

        // 4. Call domain factory / method (happy path only)
        var course = Course.Create(currentUser.UserId.Value, request.CategoryId, request.Title, ...);

        // 5. Persist
        await courseRepository.AddAsync(course, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return
        return Result.Ok(new CreateCourseResponse(course.Id));
    }
}
```

### Typical query handler shape
```csharp
public sealed class GetCourseByIdQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdQuery, Result<CourseDetailDto>>
{
    public async Task<Result<CourseDetailDto>> Handle(
        GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        // Map to DTO — never return domain entity
        return Result.Ok(new CourseDetailDto(course.Id, course.Title, ...));
    }
}
```

---

## Controller Pattern

Controllers are **thin**. The only logic allowed: route → send → map result.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value =>
            CreatedAtAction(nameof(GetById), new { id = value.CourseId }, value));
    }

    // Void result (204 No Content):
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new PublishCourseCommand(id), ct);
        return result.ToActionResult(); // → 204 on success
    }
}
```

`ToActionResult()` lives in `Learnix.API/Extensions/ResultExtensions.cs`. Always use it — never manually check `result.IsFailed` in controllers.

---

## Validation

FluentValidation validators run automatically via `ValidationBehavior` in the MediatR pipeline.

```csharp
public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(CourseConstants.TitleMaxLength); // Always use Domain Constants, not magic numbers
    }
}
```

- Always reference `Domain/Constants/{Entity}Constants.cs` for length limits — never hardcode numbers.
- Application-layer validation policies (password rules, RFC email) go in `Application/{Feature}/Constants/{Feature}ValidationConstants.cs`.
- Validators are registered automatically via assembly scanning — no manual DI needed.

---

## Specification Pattern

Uses **Ardalis.Specification**. Never write raw LINQ in handlers.

```csharp
public sealed class CourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdSpecification(Guid id, bool includeSections = false, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (includeSections)
            Query.Include(c => c.Sections).ThenInclude(s => s.Lessons);

        if (!forUpdate)
            Query.AsNoTracking(); // Default: always AsNoTracking
    }
}
```

**Key rule:**
- All specifications default to `AsNoTracking()` — read-only.
- Omit `AsNoTracking()` **only** for specifications used in Commands that modify the entity (`forUpdate: true`).
- Location: `Application/{Feature}/Specifications/{Name}Specification.cs`

---

## Repository & Unit of Work

```csharp
// Application layer interface — extends Ardalis IRepositoryBase<T>
public interface ICourseRepository : IRepositoryBase<Course> { }

// Usage in handlers:
var course = await courseRepository.FirstOrDefaultAsync(new CourseByIdSpec(id, forUpdate: true), ct);
// Modify via domain methods...
await unitOfWork.SaveChangesAsync(ct); // Always via IUnitOfWork, never DbContext directly
```

- `SaveChangesAsync` is called **only in handlers**, never inside repositories.
- `IUnitOfWork` and `ApplicationDbContext` resolve to the same scoped instance.
- `ICurrentUserService` is also scoped — inject it in handlers, not controllers.

---

## Domain Entity Conventions

- Properties have `private set` — never public setters.
- State change only via methods that reflect business operations:
  ```csharp
  // Good — one method = one business action
  public void UpdateDetails(string title, string description, decimal price, Guid categoryId) { ... }
  public void Publish() { ... }
  
  // Bad — property-level setters
  public void SetTitle(string title) { ... }
  ```
- Domain methods that violate invariants throw `DomainException` (caught by `DomainExceptionBehavior` → `ConflictError`). Handlers don't need try/catch for these.
- Entities implement marker interfaces: `IAuditable`, `IHasDomainEvents`, `ISoftDeletable` (if applicable), `IOrderable` (if applicable).
- Most entities extend `BaseEntity` (provides `Id`, `CreatedAt`, `UpdatedAt`, domain events). Exception: `User` extends `IdentityUser<Guid>` and implements the interfaces directly.

### Soft delete
- Soft-deletable entities extend `SoftDeletableEntity` (currently: `Course`).
- `SoftDeleteInterceptor` intercepts `Remove()` — sets `IsDeleted + DeletedAt`.
- Global EF query filter auto-excludes soft-deleted records.
- Use `.IgnoreQueryFilters()` only for admin queries that intentionally need deleted records.

---

## Domain Events

Events are raised inside entity methods and dispatched by `DomainEventsInterceptor` **after** `SaveChangesAsync` succeeds.

```csharp
// In Domain entity:
protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

// Example:
public void Publish()
{
    EnsurePublishableInvariants();
    Status = CourseStatus.Published;
    RaiseDomainEvent(new CoursePublishedDomainEvent(Id));
}
```

Event handlers in Application subscribe to the MediatR wrapper:
```csharp
public class CoursePublishedHandler
    : INotificationHandler<DomainEventNotification<CoursePublishedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<CoursePublishedDomainEvent> n, CancellationToken ct)
    {
        // React to the event
    }
}
```

`IDomainEvent` has no MediatR dependency — the `DomainEventNotification<T>` wrapper lives in `Application/Common/Events/` (see ADR-008).

---

## Blob Storage — Upload Pattern

All file uploads follow a 3-step SAS URL flow. **Never use `IFormFile` or server-side uploads** for user-initiated files.

```
1. Client → POST /api/uploads/request-url { target, contentType }
   ← returns { uploadUrl (SAS, 15 min), blobPath }

2. Client → PUT {uploadUrl}   (direct to Azure Blob)

3. Client → PUT /api/{resource}/me { blobPath: "avatars/users/..." }
   Handler → IBlobStorageService.ValidateAsync(blobPath, target)  ← magic byte check
   Handler → entity.SetAvatar(blobPath)  ← raises domain event
   Handler → unitOfWork.SaveChangesAsync()
   DomainEventsInterceptor → handler writes OutboxMessage(MarkBlobConfirmed)
   OutboxWorker → confirms blob in Azure
```

Blob paths (e.g. `avatars/users/{userId}/{uploadId}.jpg`) are stored in entities, **not** full SAS URLs. Read URLs are generated on demand via `IBlobStorageService.GenerateReadUrl()`.

---

## MediatR Pipeline Order

```
LoggingBehavior          ← outermost; logs request name + duration; warns >3s
  ValidationBehavior     ← rejects invalid requests; returns Result.Fail(ValidationError)
    DomainExceptionBehavior ← innermost; catches DomainException → ConflictError
      Handler            ← happy path only; no try/catch for domain rules
```

- Request payload is **never logged** (prevents PII leaks — passwords, tokens).
- `ExceptionHandlingMiddleware` catches only unexpected infrastructure failures (500).

---

## Application Settings

Config POCOs live in `Application/Common/Settings/{Name}Settings.cs`. Registered via `services.Configure<T>(...)` in `Infrastructure/DependencyInjection.cs`. Consumed via `IOptions<T>` in Infrastructure implementations.

Never inject `IConfiguration` directly into Application layer classes.

---

## Database Migrations

```bash
# Add new migration
dotnet ef migrations add {Name} \
    --project Learnix.Infrastructure \
    --startup-project Learnix.API \
    --output-dir Persistence/Migrations

# Apply
dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
```

- Dev: auto-applied on startup (`app.ApplyMigrationsAsync()` when `IsDevelopment()`).
- Staging/Prod: controlled step in CI/CD — never auto-apply.

---

## Anti-Patterns — Never Do These

| Anti-pattern | Correct approach |
|---|---|
| Throw exceptions for business errors | `return Result.Fail(new NotFoundError(...))` |
| Raw LINQ in handlers | Build a `Specification<T>` |
| DTOs in a separate `/DTOs` folder | Co-locate `{Name}Response.cs` with its query handler |
| Business logic in controllers | Delegate to handler via `sender.Send()` |
| Business logic in Infrastructure | Move to Application handler or domain method |
| `currentUser.UserId` used in controller | Inject `ICurrentUserService` into the handler |
| `DbContext` injected directly in Application | Use `IUnitOfWork` / repository interface |
| AutoMapper | Manual `ToDto()` / `ToResponse()` extension methods |
| Public property setters on domain entities | `private set`; state via domain methods |
| `IFormFile` for file uploads | 3-step SAS URL flow via `UploadsController` |
| Entity returned from handler | Map to DTO before returning |
| `INotification` on `IDomainEvent` | Keep `IDomainEvent` clean; wrap in `DomainEventNotification<T>` |

---

## Post-Task Checklist

After completing **any** backend task:

1. **Update `TODO.md`** — mark the task(s) as `[x]`. Add a short note if the implementation deviated from the spec or introduced constraints worth remembering.
2. **Update ADR files** — if a new architectural decision was made, a pattern was changed, or a constraint was added: write a new ADR entry in the appropriate `docs/DECISIONS_*.md` file using `ADR-<SCOPE>-NNN` numbering. If an existing ADR was superseded, mark it `Superseded by ADR-<SCOPE>-NNN`.
