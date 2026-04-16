# Learnix — Architecture Decision Records

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

---

## ADR-001: Clean Architecture + CQRS через MediatR

**Рішення:** Clean Architecture з чітким поділом на Domain / Application / Infrastructure / API. Усі операції проходять через MediatR (Command/Query).

**Чому:**
- Чіткий dependency rule — Domain не знає про інфраструктуру
- CQRS дозволяє окремо оптимізувати читання (кеш, проєкції) і запис
- MediatR дає pipeline behaviors (validation, logging, caching) без дублювання коду

**Альтернативи:**
- Vertical Slice Architecture — простіше для маленьких проєктів, але гірше масштабується при 20+ фічах
- Сервісний шар (IService) без медіатора — менше абстракцій, але pipeline behaviors довелося б писати вручну

---

## ADR-002: Result<T> через FluentResults замість кастомної імплементації

**Рішення:** Використовуємо бібліотеку [FluentResults](https://github.com/altmann/FluentResults) для Result pattern в Application layer.

**Чому:**
- Зріла бібліотека з підтримкою `Result`, `Result<T>`, ланцюжків помилок, metadata
- Не потрібно підтримувати власну імплементацію Result<T>
- Підтримує множинні помилки (на відміну від простого `string? Error`)
- Добре інтегрується з FluentValidation

**Альтернативи:**
- Кастомний `Result<T>` (як описано в ARCHITECTURE.md) — працює, але доведеться розширювати вручну (множинні помилки, metadata, typed errors)
- Exceptions для бізнес-помилок — порушує контракт "exceptions = unexpected failures"
- ErrorOr — теж варіант, але FluentResults має ширший API

**Наслідки:**
- Command handlers повертають `Result` або `Result<T>`
- Query handlers повертають `Result<TResponse>`
- Controllers маплять `result.IsFailed` → BadRequest, `result.IsSuccess` → Ok
- ARCHITECTURE.md потребує оновлення секції Result<T>

---

## ADR-003: JWT (short-lived) + Refresh Token (long-lived, HttpOnly cookie)

**Рішення:** Аутентифікація через пару токенів:
- **Access token (JWT):** 15 хвилин, передається в `Authorization: Bearer` header
- **Refresh token:** 7 днів, зберігається в HttpOnly + Secure + SameSite=Strict cookie

**Чому:**
- Короткий JWT мінімізує вікно компрометації — навіть якщо токен вкрадено, він живе 15 хв
- HttpOnly cookie захищає refresh token від XSS (JavaScript не має доступу)
- Rotation: кожен refresh видає нову пару (access + refresh), старий refresh інвалідується
- Хешування refresh token в БД (SHA-256) — навіть при витоку БД токени не compromised

**Альтернативи:**
- Session-based auth — простіше, але погано масштабується горизонтально без sticky sessions
- JWT only (довгоживучий, без refresh) — небезпечно, немає механізму відкликання
- OAuth2 + OpenID Connect (IdentityServer) — overkill для монолітного LMS

**Деталі:**
- Refresh token зберігається в таблиці `RefreshToken` (PostgreSQL), хешований
- При кожному refresh: старий токен ревокується, створюється новий
- Якщо використовується вже ревокований токен → ревокуються ВСІ токени юзера (захист від replay attack)
- Google OAuth: після успішного OAuth flow сервер сам генерує JWT + refresh token пару

---

## ADR-004: PostgreSQL + MongoDB (polyglot persistence)

**Рішення:** Основні реляційні дані в PostgreSQL, неструктуровані — в MongoDB.

**Чому:**
- Більшість даних (Users, Courses, Enrollments, Payments) — строго реляційні, потребують транзакцій і FK constraints
- Chat sessions мають змінну кількість повідомлень, не потребують joins → MongoDB nature fit
- Reviews: гнучка схема, можливість додавати поля без міграцій

**Що в MongoDB:**
- `chat_sessions` — історія AI чату
- `course_reviews` — відгуки з рейтингами

**Альтернативи:**
- Все в PostgreSQL (JSONB для чатів) — можливо, але ускладнює query patterns для document-like даних
- Все в MongoDB — втрата referential integrity для критичних даних (платежі, enrollments)

---

## ADR-005: MassTransit + Azure Service Bus для async processing

**Рішення:** Асинхронні операції (email, PDF generation, achievements) виконуються через MassTransit consumers, підключені до Azure Service Bus.

**Чому:**
- Відв'язує тривалі операції від HTTP request lifecycle
- Retry з backoff — якщо email provider тимчасово недоступний
- Масштабування consumers незалежно від API

**Що йде через bus:**
- Emails (verification, enrollment confirmation, instructor approval)
- Certificate PDF generation
- Achievement checking
- Enrollment activation після оплати

**Що залишається in-process (MediatR):**
- Cache invalidation
- Progress updates
- Permission checks

**Альтернативи:**
- Background tasks (Hangfire / `IHostedService`) — простіше, але немає guaranteed delivery
- RabbitMQ — self-hosted альтернатива, але потребує менеджменту інфраструктури

---

## ADR-006: Specification Pattern для queries

**Рішення:** Усі запити до репозиторіїв використовують Specification<T> для інкапсуляції criteria, includes, ordering, paging.

**Чому:**
- Логіка фільтрації/сортування живе в Application layer, а не в Infrastructure
- Специфікації легко тестувати ізольовано
- Уникаємо дублювання query logic між handlers

**Конвенції:**
- `AsNoTracking = true` за замовчуванням
- Для Commands, що змінюють сутності: явно `AsNoTracking = false`
- Розташування: `Application/{Feature}/Specifications/`

---

## ADR-007: Redis для кешування queries

**Рішення:** Queries, що імплементують `ICacheable`, автоматично кешуються в Redis через `CachingBehavior`.

**Чому:**
- Популярні курси, каталог категорій — read-heavy, рідко змінюються
- Redis дає O(1) lookup і TTL з коробки
- Pipeline behavior — кешування прозоре для handler, без boilerplate

**Інвалідація:**
- Commands, що змінюють дані, явно викликають `ICacheService.RemoveAsync(key)` після SaveChanges

---

## ADR-008: Entity Framework Core TPH для Lesson types

**Рішення:** Video, Post, Test lessons зберігаються в одній таблиці `Lessons` з дискримінатором `LessonType` (Table Per Hierarchy).

**Чому:**
- Спільні поля (Title, Order, SectionId) не дублюються
- Один query для "всі уроки секції" без UNION
- EF Core має найкращу підтримку саме TPH

**Альтернативи:**
- TPT (Table Per Type) — чистіша схема, але N+1 joins на кожен запит
- Окремі таблиці без наслідування — максимальна гнучкість, але дублювання і складні queries

---

## ADR-009: FluentValidation + FluentResults в pipeline (без exceptions)

**Рішення:** ValidationBehavior повертає Result.Fail() з помилками валідації замість кидання ValidationException. Constraint на handler: `TResponse : ResultBase`.

**Чому:**
- Валідація — це бізнес-логіка, не exceptional situation
- Консистентність з ADR-002: всі очікувані помилки через Result
- Controller маплить один тип (Result) замість двох потоків помилок (Result + catch)

**Альтернативи:**
- Throw ValidationException + ловити в middleware — працює, але змішує два підходи до помилок
- Повертати Result з middleware (catch → Result.Fail) — косметичне рішення, exception все одно кидається

**Наслідки:**
- ARCHITECTURE.md: секція "Validation Pipeline" потребує повного переписування
- ExceptionHandlingMiddleware залишається тільки для непередбачених збоїв (DB down, null ref, тощо)
- Всі Command/Query handlers мають повертати тип що наслідує ResultBase

---

## ADR-010: Typed errors (FluentResults custom errors) замість string matching

**Рішення:** Для класифікації помилок використовуємо типізовані класи 
що наслідують FluentResults.Error, а не string matching по повідомленню.

Базові типи:
- NotFoundError — 404
- ValidationError — 400 (якщо потрібно за межами FluentValidation)
- ConflictError — 409 (already enrolled, duplicate, тощо)
- ForbiddenError — 403

**Чому:**
- Compile-time safety: помилка в назві типу = compilation error
- Контролер маплить Result → HTTP статус без магічних рядків
- Легко розширювати: новий тип = новий клас, без зміни існуючого коду

**Альтернативи:**
- String matching (Contains("not found")) — крихке, не рефакториться, 
  легко зламати зміною повідомлення
- Error codes (enum) — працює, але менш виразно ніж типи і не несе 
  додаткових даних

**Приклад маппінгу в контролері:**
```cs
if (result.HasError<NotFoundError>()) return NotFound();
if (result.HasError<ConflictError>()) return Conflict();
if (result.IsFailed) return BadRequest(result.Errors);
return Ok(result.Value);
```

---

## ADR-011: Монорепо (frontend + backend в одному репозиторії)

**Рішення:** Один репозиторій: `learnix/Learnix.Backend/` + `learnix/learnix-client/`.

**Чому:**
- Соло-проєкт, один release cycle — два репо додають overhead без користі
- Спільний Docker Compose, один PR = end-to-end фіча
- Портфоліо: один лінк — весь проєкт

**Альтернативи:**
- Два окремі репо — має сенс для різних команд з різними deploy cycles, тут нерелевантно

---

## ADR-012: Ручний маппінг без AutoMapper

**Рішення:** Entity → DTO маппінг через extension methods (ToDto(), ToResponse()). 
Без AutoMapper чи Mapster.

**Чому:**
- Явний, compile-time safe, легко дебажити
- Для 20-30 DTO overhead мінімальний
- AutoMapper ховає помилки за магією конвенцій

---

## ADR-013: Offset-based пагінація через PaginatedResult<T> + PaginationRequest

**Рішення:** Offset-based пагінація (skip/take). Спільні класи 
PaginatedResult<T> і PaginationRequest в Application.Common.Pagination.

**Чому:**
- Достатньо для LMS без мільйонів записів
- PaginationRequest з Math.Clamp(PageSize, 1, 100) захищає від зловживань
- Cursor-based — overkill для цього проєкту

**Деталі:**
- PageIndex — zero-based
- MaxPageSize = 100, DefaultPageSize = 20
- PaginatedResult містить TotalCount, TotalPages, HasNextPage, HasPreviousPage

---

## ADR-014: Audit fields через EF SaveChanges interceptor

**Рішення:** CreatedAt / UpdatedAt встановлюються автоматично через 
EF SaveChanges interceptor. Properties мають private set — 
interceptor встановлює через EF ChangeTracker (без рефлексії, 
EF нативно підтримує private setters).

**Чому:**
- Жоден handler не забуде встановити дату
- Логіка в одному місці, не розмазана по всіх commands
- Private set — ніхто окрім interceptor не змінить значення випадково

---

## ADR-015: Domain entities — private setters + domain methods

**Рішення:** Entity properties мають private set. Зміна стану — 
через методи що відповідають бізнес-операціям.

**Чому:**
- Інваріанти перевіряються в одному місці (всередині entity)
- Не анемічна модель — entity несе поведінку

**Правила:**
- Один метод = одна бізнес-дія (course.UpdateDetails(), course.Publish())
- НЕ метод на кожне поле (SetTitle, SetPrice — антипатерн)
- Масове оновлення через Update(...) з усіма полями — допустимо

---

## ADR-016: Soft delete для Users і Courses, hard delete для решти

**Рішення:** 
- User: soft delete (ISoftDeletable), фізичне видалення через 30 днів background job
- Course: soft delete (30 днів) або Archive (без видалення, залишається в БД)
- LessonProgress, Likes, інше дрібне: hard delete

**Деталі:**
- ISoftDeletable: IsDeleted + DeletedAt
- EF global query filter: HasQueryFilter(e => !e.IsDeleted)
- SoftDeleteInterceptor встановлює IsDeleted/DeletedAt автоматично
- Background job (IHostedService або Hangfire) видаляє записи старші 30 днів

---

## ADR-017: ProblemDetails для помилок, чистий DTO для успіху

**Рішення:** Без envelope. Success → DTO напряму. 
Error → ProblemDetails (RFC 7807) з errors dictionary для валідації.

**Чому:**
- ASP.NET Core має вбудовану підтримку ProblemDetails
- Фронтенд отримує стандартизовану структуру помилок
- Envelope ({ data, success, errors }) — зайвий boilerplate

**Валідація повертається як:**
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

**Маппінг:** Extension method в API layer: Result.Errors → ProblemDetails.

---

## ADR-018: ASP.NET Identity — наслідувати IdentityUser, свій DbContext

**Рішення:** User entity наслідує IdentityUser<Guid>. 
Використовуємо свій ApplicationDbContext, не IdentityDbContext. 
Instructor-specific дані — НЕ в claims.

**Instructor-specific дані:** Якщо потрібні — nullable поля на User entity. 
Окрема таблиця InstructorProfile — out of scope для v1.

**Чому:**
- Identity дає безкоштовно: password hashing, token generation, 
  lockout, email confirmation, external logins
- Свій DbContext = контроль над схемою, без зайвих таблиць
- Claims — для auth metadata (роль, permissions), не для бізнес-даних

**Альтернативи:**
- Identity повністю з коробки (IdentityDbContext) — тягне зайві таблиці, 
  менше контролю
- Без Identity взагалі — місяці на написання auth вручну без користі

---

## ADR-019: IDomainEvent без залежності від MediatR — адаптер в Application

**Рішення:** Інтерфейс `IDomainEvent` в `Learnix.Domain.Common` — чистий marker без наслідування `INotification`. MediatR-специфічна обгортка `DomainEventNotification<TDomainEvent> : INotification` живе в `Learnix.Application.Common.Events`. `ApplicationDbContext.SaveChangesAsync` публікує domain events через `MakeGenericType` + `Activator.CreateInstance`, обгортаючи кожен event в відповідний `DomainEventNotification<T>`.

**Чому:**
- Domain layer не має знати про MediatR — це інфраструктурна бібліотека
- Змінити mediator (теоретично) — переписати один адаптер, не всі domain events
- Handlers в Application пишуться як `INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>` — трохи більше boilerplate, але явно видно що це reaction on domain event

**Альтернативи:**
- `IDomainEvent : INotification` (як в ARCHITECTURE.md) — простіше, але порушує dependency rule
- Власний `IDomainEventDispatcher` без MediatR взагалі — більше коду, втрата in-process pub/sub фіч MediatR

**Наслідки:**
- ARCHITECTURE.md: секція "Domain Entities" — прибрати `public interface IDomainEvent : INotification { }`
- ARCHITECTURE.md: секція "Domain Event Dispatching" потребує оновлення (див. нижче)

---

## ADR-020: CacheKeys в Application layer, не Domain

**Рішення:** `CacheKeys` константи живуть в `Learnix.Application.Common.Constants.CacheKeys`, а не в `Learnix.Domain.Constants`.

**Чому:**
- Кешування — інфраструктурна турбота. Domain не повинен знати що десь є Redis
- Domain має залишатись максимально чистим від крос-cutting concerns

**Альтернативи:**
- Лишити в Domain (як було в ARCHITECTURE.md) — працює, але змішує рівні абстракції

**Наслідки:**
- ARCHITECTURE.md: секція "Caching Strategy (Redis)" — шлях файлу `Application/Common/Constants/CacheKeys.cs`

---

## ADR-021: DbContext сам реалізує IUnitOfWork

**Рішення:** `ApplicationDbContext` реалізує `IUnitOfWork`. Окремого класу `UnitOfWork` немає. DI: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>())` — резолв в той самий scope instance.

**Чому:**
- Окремий `UnitOfWork` клас просто делегував би `SaveChangesAsync` в DbContext — зайвий шар indirection
- Application шар все одно бачить тільки `IUnitOfWork`, не DbContext — абстракція зберігається
- Менше файлів, менше DI-реєстрацій, менше шансів облажатись з scopes

**Альтернативи:**
- Окремий `UnitOfWork` клас (як в ARCHITECTURE.md) — канонічний підхід, але додає шар без функціональної цінності

**Наслідки:**
- ARCHITECTURE.md: секція "Repository Pattern" / "Unit of Work" — прибрати окремий клас UnitOfWork

---

## ADR-022: Outbox pattern — усвідомлено відкладено до Phase 6

**Рішення:** Публікація domain events відбувається безпосередньо після `SaveChangesAsync` в `ApplicationDbContext`, без outbox таблиці. Ми свідомо приймаємо ризик втрати event'ів якщо процес впаде між SaveChanges і Publish.

**Чому зараз так:**
- Phase 1 — фундамент, не прод. Юзерів/транзакцій нема, консистентність не критична
- Реалізація outbox зараз подвоїла б B-04 (entity + EF config + worker + переписування SaveChanges)
- Природне місце для outbox — поряд з MassTransit конфігурацією (Phase 6, B-35) — одна тема, один контекст

**Коли додавати (B-34.5, перед Phase 6):**
- Створити `OutboxMessage` entity (`Id`, `Type`, `Payload`, `OccurredOnUtc`, `ProcessedOnUtc?`, `Error?`)
- `SaveChangesAsync` серіалізує domain events в OutboxMessage **в ту саму транзакцію** що і дані
- Background worker (`IHostedService`) polling-ом читає непроцесовані, публікує через MediatR/MassTransit, ставить `ProcessedOnUtc`
- Прибрати пряму публікацію з `SaveChangesAsync`

**Альтернативи розглянуті:**
- Зробити outbox одразу в B-04 — коректніше, але розтягує чат і ускладнює fundament без поточної потреби
- Не робити outbox взагалі — неприпустимо для production-claim платформи (платежі → сертифікати → email)

**Наслідки:**
- TODO.md: новий таск B-34.5 (або в окрему секцію tech debt)
- Не документуємо outbox в ARCHITECTURE.md зараз — додамо разом з реалізацією

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
