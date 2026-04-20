# Learnix — Architecture Decision Records

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

## Конвенція статусів

ADR не видаляються. Якщо рішення переглянуто — старий ADR помічається `Superseded by ADR-XXX`, новий — `Supersedes ADR-YYY`. Це зберігає історію мислення і показує як архітектура еволюціонувала.

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
- `IDomainEvent : INotification` — простіше, але порушує dependency rule
- Власний `IDomainEventDispatcher` без MediatR взагалі — більше коду, втрата in-process pub/sub фіч MediatR

---

## ADR-020: CacheKeys в Application layer, не Domain

**Рішення:** `CacheKeys` константи живуть в `Learnix.Application.Common.Constants.CacheKeys`, а не в `Learnix.Domain.Constants`.

**Чому:**
- Кешування — інфраструктурна турбота. Domain не повинен знати що десь є Redis
- Domain має залишатись максимально чистим від крос-cutting concerns

**Альтернативи:**
- Лишити в Domain — працює, але змішує рівні абстракції

---

## ADR-021: DbContext сам реалізує IUnitOfWork

**Рішення:** `ApplicationDbContext` реалізує `IUnitOfWork`. Окремого класу `UnitOfWork` немає. DI: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>())` — резолв в той самий scope instance.

**Чому:**
- Окремий `UnitOfWork` клас просто делегував би `SaveChangesAsync` в DbContext — зайвий шар indirection
- Application шар все одно бачить тільки `IUnitOfWork`, не DbContext — абстракція зберігається
- Менше файлів, менше DI-реєстрацій, менше шансів облажатись з scopes

**Альтернативи:**
- Окремий `UnitOfWork` клас — канонічний підхід, але додає шар без функціональної цінності

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
- Не документуємо outbox в ARCHITECTURE.md зараз — додамо разом з реалізацією

---

## ADR-023: Розщеплення BaseEntity на IAuditable + IHasDomainEvents

**Рішення:** `BaseEntity` декомпозовано на два інтерфейси: `IAuditable` (CreatedAt, UpdatedAt) і `IHasDomainEvents` (DomainEvents, ClearDomainEvents). `BaseEntity` тепер просто абстрактний клас що імплементує обидва + дає `Id : Guid`. `User : IdentityUser<Guid>` імплементує обидва інтерфейси вручну, бо не може успадкувати `BaseEntity` через конфлікт `Id` з `IdentityUser<Guid>`.

**Чому:**
- `User` не може жити з двох баз (`IdentityUser<Guid>` і `BaseEntity` — обидва дають `Id`)
- Дублювати audit/events код в `User` вручну — антипатерн, копіпаста
- `AuditableInterceptor` тепер ловить будь-який `IAuditable` (і `BaseEntity`-нащадків, і `User`), domain events публікація — будь-який `IHasDomainEvents`. Один уніфікований механізм
- Решта entities (Course, Enrollment, …) продовжують успадковувати `BaseEntity` — для них API не змінюється

**Альтернативи:**
- Лишити `BaseEntity` як абстрактний клас, дублювати audit/events у `User` вручну — працює, але копіпаста полів і методів. При додаванні нового аудит-поля треба міняти і `BaseEntity`, і `User`
- `User` без audit і events — втрачаємо trace коли юзер створений/оновлений

---

## ADR-024: Чисті Identity ролі замість UserRole enum

**Рішення:** Видалено `UserRole` enum з Domain. Ролі (Student / Instructor / Admin) живуть тільки в Identity (`AspNetRoles` + `AspNetUserRoles`). `Domain.Constants.Roles` — статичний клас з string-константами для типобезпечного посилання.

**Чому:**
- Дублювання enum-поля на User + Identity ролей — два источника правди, неминучий розсинхрон
- `[Authorize(Roles = "Instructor")]` працює з Identity з коробки
- JWT claims автоматично заповнюються Identity з ролей
- Менше коду, менше шансів на помилку

**Альтернативи:**
- Тільки enum на User, без Identity ролей — втрачаємо `[Authorize(Roles=...)]` і вбудовану підтримку, треба писати свій authorization handler
- Гібрид: enum + Identity, синхронізація через domain method — попередня рекомендація, відкинуто, бо дублювання навіть для 3 значень не вартує. Якщо додасться 4-та роль — треба міняти в двох місцях

---

## ADR-025: IIdentityService як абстракція над UserManager

> **Status:** Superseded by ADR-032 (2026-04-19). Оригінальний принцип "Application не знає про UserManager" лишається в силі, але реалізований через три окремі інтерфейси замість одного `IIdentityService`. Цей ADR лишається для історичного контексту.

**Рішення:** Інтерфейс `IIdentityService` живе в Application, реалізація `IdentityService` в Infrastructure. Application handlers не знають про `UserManager<User>` — викликають тільки `IIdentityService`.

**Чому:**
- `UserManager<User>` залежить від `IUserStore` → EF Core → це Infrastructure concern
- Прямий виклик `UserManager` з handler у Application — порушення Clean Architecture (Application залежить від Infrastructure через MS.AspNetCore.Identity.EntityFrameworkCore)
- Інтерфейс дає чітку межу: Application говорить "зареєструй / підтверди email", Infrastructure знає як саме (через Identity або інакше)

**Альтернативи:**
- Прямий виклик `UserManager` з handler — простіше, але порушує dependency rule
- Загорнути Identity в окремий "Auth module" — overengineering для одного сервісу

**Наслідки:**
- Усі auth-related handlers викликають `IIdentityService`, не `UserManager` напряму
- Тестування handlers — мокаємо `IIdentityService`, не Identity інфраструктуру

---

## ADR-026: IEmailSender + console implementation як тимчасове рішення

**Рішення:** Інтерфейс `IEmailSender` в Application, console-реалізація `ConsoleEmailSender` в Infrastructure (логує лист через `ILogger`). Domain event handlers викликають `IEmailSender` напряму (in-process), без MassTransit.

**Чому:**
- Phase 6 (MassTransit + Azure Service Bus, B-35..B-38) ще не реалізована
- Phase 2 потребує email flow для register/confirm/reset — не може чекати Phase 6
- Console implementation достатня для dev: бачимо що "лист відправлено" в логах, копіюємо link з логу для confirm

**Заміна в Phase 6:**
- `ConsoleEmailSender` → `SmtpEmailSender` (або інша реальна реалізація)
- Domain event handler → публікує integration event (`UserRegisteredIntegrationEvent`) через MassTransit
- Consumer (`SendVerificationEmailConsumer`) викликає `IEmailSender`
- Інтерфейс `IEmailSender` лишається таким самим — нульова зміна для handlers

**Альтернативи:**
- Підключити MassTransit одразу заради одного email — overkill для Phase 2, розтягує scope
- Не реалізовувати email до Phase 6 — Phase 2 не зможе підтвердити email юзера, login не пускатиме нікого

---

## ADR-027: Гібридний поділ констант — Domain для інваріантів entity, Application для політик валідації

**Рішення:** Константи розділені за рівнем і змістом:
- **Domain** (`Learnix.Domain/Constants/{Entity}Constants.cs`) — обмеження які є інваріантами сутності (FirstName max length, Bio max length). Використовуються EF configuration, domain методами (якщо потрібно), Application validators.
- **Application** (`Learnix.Application/{Feature}/Constants/{Feature}ValidationConstants.cs`) — обмеження які є політикою валідації входу: довжина пароля, regex вимоги, технічні стандарти типу email RFC 5321 max length. Використовуються тільки валідаторами в межах фічі.

**Що НЕ виноситься в константи взагалі:**
- Унікальні regex які зустрічаються один раз (`[A-Z]`, `[a-z]` в password validator)
- Повідомлення помилок (`WithMessage("...")`) — лишаються inline до появи локалізації
- Деталі реалізації Identity (PasswordHash length, etc.) — Identity сама керує

**Чому:**
- Single source of truth: max length у валідаторі і в EF configuration читаються з одної константи. Зміна — в одному місці. Розсинхрон неможливий.
- Розділення Domain/Application відображає дві різні відповідальності: "що entity вважає валідним станом" (Domain) vs "що ми готові прийняти на вході в систему" (Application)
- Email max length — не інваріант User, а обмеження SMTP стандарту → Application. Password constraints — не інваріант User (зберігається hash), а політика безпеки реєстрації → Application

**Альтернативи:**
- Усі константи в Application — простіше, але втрачає DDD-аргумент про інваріанти. Для проєкту з 30+ entities стане плутаниною
- Усі константи в Domain — погано: затягує знання про SMTP, password policy в Domain, який має бути про бізнес
- Inline magic numbers — категорично ні, синхронізація валідатор↔EF↔domain метод стає неможливою

**Наслідки:**
- Нова конвенція: створюючи нову entity — створювати `{Entity}Constants` у Domain з усіма обмеженнями що використовуються EF configuration
- Створюючи нову feature з валідацією — створювати `{Feature}ValidationConstants` в Application якщо є feature-specific обмеження (інакше — лишити inline)
- Domain методи не дублюють валідацію (свідоме спрощення — див. вище)

---

## ADR-028: Infrastructure отримує FrameworkReference на Microsoft.AspNetCore.App

**Рішення:** `Learnix.Infrastructure.csproj` декларує `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. Це дає доступ до ASP.NET Core shared framework збірок (`Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.Authentication.*`, etc.) які потрібні для реалізації auth-related сервісів.

**Чому:**
- `AddIdentity<,>()` extension method живе у `Microsoft.AspNetCore.Identity` збірці що є частиною shared framework, а не окремого NuGet
- Class library на `Microsoft.NET.Sdk` (не `.Web`) не має доступу до shared framework за замовчуванням, навіть якщо встановлені пакети `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `FrameworkReference` — стандартний механізм отримання доступу до shared framework з не-Web проектів (документований Microsoft підхід)

**Альтернативи:**
- Винести Identity-related код у окремий проект `Learnix.Infrastructure.Identity` з `Sdk="Microsoft.NET.Sdk.Web"` — штучне розділення, EF configurations User логічно належать до основної Infrastructure, додає DI complexity без користі
- Перенести Identity setup у API проект (де `Sdk` вже Web) — порушує "Infrastructure реалізує всі технічні концерни", розмазує auth логіку між шарами

**Наслідки:**
- `Learnix.Infrastructure` тепер транзитивно має доступ до всього `Microsoft.AspNetCore.App` (MVC, SignalR, Authentication middleware). Це формальне розширення scope, але реально використовуємо тільки Identity та (у майбутньому) JWT bearer authentication
- Runtime overhead нульовий — shared framework вже присутній на хості завдяки API проекту
- Той самий компроміс що й ADR-018 (User : IdentityUser): формально менш чисто, прагматично необхідно

---

## ADR-029: Авто-міграції тільки в Development

**Рішення:** `Database.MigrateAsync()` викликається при старті API через extension `app.ApplyMigrationsAsync()` тільки коли `app.Environment.IsDevelopment()`. У staging/prod міграції застосовуються окремим контрольованим кроком (CI/CD або ручний `dotnet ef database update`).

**Чому:**
- Dev: розробник підняв `docker compose up -d` і `dotnet run` — БД готова без додаткових команд. Прискорення feedback loop.
- Prod: авто-міграції створюють race condition при scale-out (декілька instance стартують одночасно), помилка міграції = API не піднімається, руйнівні зміни схеми проходять без human review.

**Альтернативи:**
- Завжди авто-міграції — небезпечно в prod (див. вище).
- Ніколи авто-міграції, навіть в dev — кожен `git pull` з новою міграцією потребує ручного `dotnet ef database update`. Тертя у щоденній роботі.
- `Database.EnsureCreatedAsync()` — несумісно з міграціями, годиться тільки для тестових БД що створюються з нуля.

**Наслідки:**
- Phase D (Deploy): додати окремий CI step для застосування міграцій у staging/prod, або генерувати idempotent SQL script через `dotnet ef migrations script --idempotent` і застосовувати через міграційний tool (Flyway/власний).
- Розробник має бути готовий що при першому `dotnet run` після `git pull` міграції запустяться автоматично — побачить це в консолі через `LogInformation`.

---

## ADR-030: Структура папок Application — гібрид feature-first + cross-cutting Common/Abstractions

**Рішення:** Інтерфейси Application шару групуються за **областю використання**:

- **Cross-cutting** (використовується більше ніж однією фічею) → `Common/Abstractions/{Category}/`. Категорії: `Persistence`, `Caching`, `Messaging`, `Time` (за потреби), `Identity` (загальні концепти типу `ICurrentUserService`).
- **Feature-specific** (живе в межах однієї фічі) → `{Feature}/Abstractions/`. Приклади: `IUserRegistrationService`, `IUserAuthenticationService`, `ITokenService`, `IRefreshTokenRepository` — усі в `Auth/Abstractions/`.

Папка називається `Abstractions`, не `Interfaces`, бо може містити не тільки інтерфейси (абстрактні класи, делегати).

`Models/` (record-и які повертаються/приймаються інтерфейсами) — теж feature-scoped: `Auth/Models/`, `Courses/Models/`. Cross-cutting `Common/Models/` створюється тільки якщо з'явиться модель яку реально використовують з кількох фіч.

Категоризація в `Common/Abstractions/` створюється одразу (навіть з одним файлом у папці) — переїжджати один файл легко, переїжджати десять болісно після того як папка перетворилась на смітник.

**Правило для нових інтерфейсів:** "Цей інтерфейс має сенс поза однією фічею?"
- Так → `Common/Abstractions/{Category}/`
- Ні → `{Feature}/Abstractions/`

**Чому:**
- Локальність змін: фіча — самодостатня папка зверху донизу. Auth feature видаляється однією операцією
- Зрозумілий dependency graph: `using Learnix.Application.Auth.Abstractions` всередині `Courses/` — явний червоний прапор
- Категорії в `Common/Abstractions/` доповнюють групування за роллю — щоб `IUnitOfWork` (persistence) і `IEmailSender` (messaging) не лежали в одному кошику

**Альтернативи:**
- Flat `Common/Interfaces/` — простіше зараз, ламається на 20+ файлах, неможливо відрізнити репозиторій від зовнішнього сервісу від pipeline marker без відкриття файлу
- Тільки feature-grouping без `Common/Abstractions/` — не вирішує куди класти truly cross-cutting (`IUnitOfWork`, `IEmailSender`)
- Тільки `Common/Abstractions/{Category}/` без feature-folders для інтерфейсів — порушує локальність, ламає правило "одна фіча = одна папка"

---

## ADR-031: JWT секрет — placeholder в base + dev-секрет в Development + env var в production

**Рішення:** `appsettings.json` містить `Jwt.Secret = ""` (placeholder, валідація на старті падає якщо він порожній). `appsettings.Development.json` перевизначає його статичним рандомним рядком (>32 байт). У production значення передається через змінну оточення `JWT__Secret` (double underscore = nested config key в .NET configuration).

**Чому:**
- Розробник підняв `dotnet run` без зайвих кроків — БД мігрується автоматично (ADR-029), JWT не падає на старті
- `appsettings.Development.json` ніколи не йде в production билд — низький ризик витоку
- Production-секрет ніколи не торкається диска чи git — тільки runtime env var (Azure Key Vault → App Service config → env var)
- Перевірка `string.IsNullOrWhiteSpace(jwtSettings.Secret)` в `AddInfrastructure` — fail fast, краще впасти на старті ніж випустити токени підписані порожнім ключем

**Альтернативи:**
- Завжди через env var (включно з dev) — кожен новий розробник має окремо налаштувати `.env` чи user-secrets перед першим запуском. Тертя в onboarding
- User Secrets (`dotnet user-secrets`) для dev — канонічний MS-підхід, але ховає секрет в окремому місці що ускладнює дебаг "звідки взялось значення"
- Hardcoded fallback у коді (`?? "default-dev-secret"`) — небезпечно, легко проґавити в production-збірці

**Наслідки:**
- `appsettings.json`: секція `Jwt` з порожнім `Secret`
- `appsettings.Development.json`: перевизначення `Jwt.Secret` рандомним рядком
- `.env.example`: рядок `JWT__Secret=<generate 64+ char secret>` з коментарем "production only"
- `AddInfrastructure`: явна перевірка наявності секрету з `InvalidOperationException` при порожньому

---

## ADR-032: Декомпозиція Identity сервісу на три ролі за принципом "одна причина змінитись"

> **Supersedes:** ADR-025 (one IIdentityService → three interfaces)

**Рішення:** Замість одного `IIdentityService` — три інтерфейси:
- `IUserRegistrationService` — реєстрація + email-підтвердження (CRUD-life-cycle юзера)
- `IUserAuthenticationService` — валідація креденшелів + вибірка info для побудови токена
- `ITokenService` — генерація JWT + refresh token + хешування (чиста функція без знання про БД чи Identity)

Усі три живуть у `Auth/Abstractions/` (ADR-030). Реалізації — в `Infrastructure/Identity/`.

**Чому:**
- **Different reasons to change.** Заміниш Identity provider (на Auth0/IdentityServer) — переписуєш `UserRegistration` + `UserAuthentication`, `TokenService` не знає. Поміняєш JWT на PASETO або змінити claims — переписуєш `TokenService`, решта не торкається. Single Responsibility у дії
- **Тестабельність.** Login handler у unit-тесті мокаєш три легких інтерфейси, а не один товстий
- **Читабельність handler.** `LoginCommandHandler` явно показує оркестрацію: validate → generate token pair → persist refresh → save. Кожен крок — окрема залежність

**Альтернативи:**
- Один `IIdentityService` з усіма методами — простіше менше файлів, але товстий контракт що змінюється з кожної причини
- `IUserAuthenticationService.LoginAsync` повертає одразу JWT — змішує валідацію credentials з token generation, два різних concern в одному методі

**Наслідки:**
- 3 окремих DI-реєстрації в `AddInfrastructure`
- `IdentityService.cs` (старий) видалено, на його місці `UserRegistrationService.cs` + `UserAuthenticationService.cs` + `JwtTokenService.cs`
- Старі handlers (Register, ConfirmEmail, ResendConfirmationEmail) оновлено — параметр конструктора з `IIdentityService` на `IUserRegistrationService`

---

## ADR-033: Refresh token rotation з replay-attack protection

**Рішення:** При кожному успішному `/api/auth/refresh` — старий refresh token revoked (не видаляється), новий створюється і повертається. Якщо приходить запит з токеном що **уже revoked** — це сигнал компрометації: всі активні токени юзера примусово revoked, юзер вилогінюється з усіх пристроїв, інцидент логується як warning з UserId.

Refresh tokens зберігаються в PostgreSQL у вигляді SHA-256 хешу (`TokenHash`, унікальний індекс). Plain-токен існує тільки в HttpOnly cookie клієнта. Витік БД ≠ компрометація сесій.

Refresh token передається через HttpOnly + Secure + SameSite=Strict cookie з `Path = "/api/auth"`. Контролер відповідає за читання/запис cookie; handlers оперують голим рядком — Application шар нічого не знає про HTTP.

**Чому:**
- Rotation робить вікно компрометації мінімальним — токен живе один запит
- Replay protection ловить сценарій "токен украли, обидва (атакувальник і юзер) ним користуються" — перший хто refresh-ить отримує новий, другий приходить зі старим (revoked) → всіх кидає в logout
- Хешування в БД — захист від витоку дампа БД
- Path-обмежений cookie не відправляється з кожним запитом до API, тільки до auth-endpoints — менше експозиції

**Альтернативи:**
- Refresh без rotation (один довгоживучий токен) — простіше, але втрачаємо replay-detection
- Refresh tokens у Redis — швидше, але втрачаємо durability (перезапуск Redis = всі юзери вилогінено)
- JWT як refresh теж — symmetric з access, але втрачаємо центральний контроль revocation (JWT не можна "забрати назад")

**Наслідки:**
- `RefreshToken` entity з `TokenHash`, `ExpiresAt`, `IsRevoked`, `RevokedAt`
- `IRefreshTokenRepository` + специфікації `RefreshTokenByHashSpecification`, `ActiveRefreshTokensByUserSpecification`
- `RefreshTokenCleanupHostedService` (B-11.5) — фонова задача чистить токени старші `ExpiresAt + 7 днів` раз на 24h
- Контролер `AuthController` керує cookie (`SetRefreshTokenCookie`, `ClearRefreshTokenCookie`) — handlers ні

---

## ADR-034: JWT claims — стандартні OIDC + custom для ролей

**Рішення:** Access token містить:
- `sub` — User Id (Guid)
- `email` — User email
- `jti` — унікальний id токена (для трейсингу/blacklist у майбутньому)
- `given_name` — FirstName
- `family_name` — LastName
- `name` — `"{FirstName} {LastName}"` (повне ім'я для display)
- `role` — повторюваний claim для кожної ролі юзера

`MapInboundClaims = false` у `AddJwtBearer` — щоб у коді API ми бачили claim names такими ж як у JWT (не перетворені на `ClaimTypes.NameIdentifier` тощо). `NameClaimType = "name"`, `RoleClaimType = "role"` — щоб `User.Identity.Name` і `[Authorize(Roles = "Instructor")]` працювали з нашими custom-claim-names.

**Чому:**
- Стандартні OIDC claims (`sub`, `email`, `given_name`, `family_name`, `name`) — фронтенд або сторонні системи що очікують OIDC отримають що очікують
- Окремі `given_name` + `family_name` + composite `name` — фронт може взяти будь-яке поле без додаткового парсингу
- `role` як повторюваний claim — стандартний механізм Identity, працює з `[Authorize(Roles = ...)]` за замовчуванням
- `MapInboundClaims = false` — узгодженість: те що у JWT == те що бачимо у коді. Дебаг простіший

**Альтернативи:**
- Тільки `name` без розбиття — фронту доводиться парсити "First Last", ламається на іменах з пробілами/множинних
- Власні короткі claim names (`uid`, `r`) для зменшення розміру токена — економія мінімальна, втрата сумісності з OIDC tooling
- `ClaimTypes.*` URI-based claim names (.NET default) — multi-kilobyte токени, погана читабельність

**Наслідки:**
- `JwtTokenService.GenerateAccessToken` приймає `firstName, lastName` (не тільки `firstName` як в першій ітерації)
- `UserAuthenticationInfo` містить обидва імені
- `AddJwtBearer` сконфігурований з `MapInboundClaims = false`, `NameClaimType`, `RoleClaimType`

---

## ADR-035: Розділення `AuthenticationError` (401) і `ForbiddenError` (403)

**Рішення:** Створено окремий typed error `AuthenticationError : Error` для 401 Unauthorized.
`ForbiddenError` тепер семантично відповідає 403 Forbidden — "автентифікований, але не має прав".

- `AuthenticationError` — invalid credentials, expired/replay refresh token, missing/invalid access token, locked out, unconfirmed email при логіні. Мапиться на 401.
- `ForbiddenError` — юзер автентифікований, але не має права на операцію (наприклад, Student намагається редагувати чужий курс). Мапиться на 403.

**Чому:**
- HTTP 401 і 403 семантично різні. RFC 9110 розділяє їх чітко: 401 = "treba автентифікуватись", 403 = "автентифікація є, але не допомагає"
- Попередня реалізація мапила `ForbiddenError` на 401 у контролері — це працювало, але плутало читача (ім'я типу → 403, маппінг → 401)
- Окремі типи дозволяють `result.ToActionResult()` extension працювати однозначно без перевизначень на рівні action

**Альтернативи:**
- Залишити один `ForbiddenError` і мапити на різні коди залежно від контексту — магія у mapping, сервіс не знає який код поверне його помилка
- Error codes (enum) в одному типі — менш виразно, втрачає compile-time check

**Наслідки:**
- `ResultExtensions.ToActionResult` має окремі гілки для обох типів
- Існуючі handlers (`UserAuthenticationService`, `RefreshTokenCommandHandler`) мігровано на `AuthenticationError`
- Майбутні authorization checks (роль-базовані) використовуватимуть `ForbiddenError`

---

## ADR-036: Google OAuth через Google Identity Services (ID token) замість OAuth code flow

**Рішення:** Фронтенд отримує Google ID token через Google Identity Services (GIS) SDK прямо в браузері. Бек отримує token на `POST /api/auth/google`, валідує через `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync`) і видає свої JWT+refresh. Authorization Code flow з redirect_uri на беці і Client Secret **не використовуємо**.

**Чому:**
- GIS — sanctioned Google's підхід для SPA з 2022+. Простіший, менше рухомих частин.
- Client Secret не потрібен — ID token це self-contained JWT підписаний Google private key, бек валідує публічним ключем з JWKS. Secret потрібен тільки для обміну authorization code.
- Немає redirect endpoint на беці → нема окремої машинерії для callback, state parameter, CSRF protection на callback.
- ID token уже містить `email`, `email_verified`, `given_name`, `family_name`, `sub` — все що нам треба для find-or-create. Додаткових запитів на Google userinfo endpoint не робимо.

**Альтернативи:**
- **Authorization Code flow** — класика, "виглядає стандартніше" на інтерв'ю, але для SPA це anti-pattern у 2026. Потребує Client Secret, redirect endpoint, обміну code → tokens.
- **Implicit flow** — deprecated Google'ом, не обговорюється.

**Наслідки:**
- `GoogleSettings.ClientId` — єдине що треба сконфігурувати. `ClientId` публічний (виявиться в front-end коді), не secret.
- Endpoint `POST /api/auth/google` приймає `{ idToken }` → валідує → видає пару токенів (та сама `LoginResponse` як regular login).
- Якщо Google колись deprecates GIS — треба буде переписати на Authorization Code flow. Ризик низький: GIS — це їх стратегічний напрямок.

---

## ADR-037: `GoogleId` як денормалізоване поле на User замість `AspNetUserLogins`

**Рішення:** External provider linkage зберігається як `User.GoogleId` (nullable `string?`), не через Identity таблицю `AspNetUserLogins` / `UserManager.AddLoginAsync`.

**Чому:**
- У v1 Learnix тільки один external provider (Google). `AspNetUserLogins` — це таблиця для N провайдерів `(Provider, ProviderKey)`. Для одного — overhead без користі.
- `WHERE GoogleId = ?` — один простий lookup без join на `AspNetUserLogins`.
- Менше EF-конфігурації, менше рухомих частин у Identity schema.

**Альтернативи:**
- **`AspNetUserLogins` через `UserManager.AddLoginAsync`** — канонічний Identity шлях. Переваги: масштабується на N провайдерів з нульовими змінами коду (GitHub, Microsoft). Недолік: join на кожному Google login lookup.
- **Гібрид: `GoogleId` для швидкого lookup + запис в `AspNetUserLogins`** — дублювання, розсинхрон можливий.

**Наслідки:**
- Додавання другого external provider (GitHub, Microsoft) — це міграція `string? GoogleId` → `AspNetUserLogins`-based flow. Помітна робота: нова міграція схеми, перенесення даних, переписування `FindOrCreateGoogleUserAsync` на polymorphic `FindOrCreateExternalUserAsync`.
- Обмежено однопровайдерним сценарієм — документовано як свідомий tradeoff.

**Future work:** при додаванні другого провайдера — рефакторити на `UserManager.AddLoginAsync` + `FindByLoginAsync`. Задача в TODO як `B-XX` (поза v1).

---

## ADR-038: Rate limiting — in-memory FixedWindow per IP, один strict policy

**Рішення:** Sensitive auth endpoints (register, login, google login, forgot-password, reset-password, resend-confirmation, confirm-email) лімітовані вбудованим `Microsoft.AspNetCore.RateLimiting` — **5 запитів на 15 хвилин per IP**, FixedWindowLimiter, `QueueLimit = 0`. Refresh, logout не лімітовані. Перевищення → 429 `ProblemDetails` + `Retry-After` header.

**Чому:**
- `Microsoft.AspNetCore.RateLimiting` — вбудований у .NET 8, нуль додаткових NuGet, підтримується Microsoft. AspNetCoreRateLimit — це legacy з часів .NET Core 2.
- FixedWindow — найпрозоріший для юзера ("5 спроб на 15 хв, потім reset"). SlidingWindow і TokenBucket не дають переваг для sensitive auth де нам треба **strict cap**, а не smooth rate.
- `QueueLimit = 0` — юзер що перебрав ліміт одразу отримує 429, не зависає у черзі.
- Refresh без лімітування — легітимний клієнт з 3 вкладками може зробити 3 одночасних refresh при wake з sleep; strict ліміт створював би false positives без security-переваги (replay detection і так працює в `RefreshTokenCommandHandler` через ADR-033).
- Per-IP партиціонування (не per IP+email) — простіше, достатньо для портфоліо. Per-user партиціонування потребує юзер знайдений на цій стадії — але rate limit застосовується **до** auth, коли юзера ще нема.

**Альтернативи:**
- **AspNetCoreRateLimit (NuGet)** — legacy, більше коду, менше підтримки.
- **Redis-backed distributed rate limiter** — правильно для scale-out. Поза scope v1: монолітний деплой на одному Container App instance → in-memory достатньо. Додавати при переході на multi-instance (Phase 10+).
- **Per IP+email для login** — захищає конкретний акаунт від brute force при розподіленій атаці з багатьох IP. Trade-off: складніше, вимагає нестандартного partitioning key (email з body). Для v1 не виправдано.

**Наслідки:**
- In-memory counters — **при scale-out лічильники розсинхронізовані**. Атакувальник може отримати 5×N спроб на N instance. Документований трейд-оф, поки один instance — не критично.
- `HttpContext.Connection.RemoteIpAddress` за reverse proxy повертає IP проксі, не клієнта. При деплої в Azure App Service / Container Apps **обов'язково** додати `UseForwardedHeaders()` — інакше всі юзери отримують один partition key і rate limit стає одним глобальним лічильником. Задача D-06.5 у TODO.

---

## ADR-039: Authorization checks live in handlers, not controllers

**Рішення:** Перевірки "чи може поточний користувач виконати цю операцію над цим ресурсом" (owner check, role check) виконуються в command/query handlers через `ICurrentUserService`. Контролер не бере на себе цю відповідальність — він робить тільки HTTP-речі (read body, return ToActionResult).

**Чому:**
- Контролер не знає про `course.InstructorId` без fetch'у через repository. Якщо контролер робить fetch — він уже фактично робить частину роботи handler'а → порушення SRP
- ASP.NET `[Authorize(Policy = ...)]` добре працює для статичних правил (роль у claims, claim value). Owner-check вимагає fetch resource → resource-based authorization → динамічно → природне місце — handler
- Handler повертає `Result.Fail(new ForbiddenError(...))` → `ToActionResult()` маппить на 403. Узгоджено з існуючим pipeline (ADR-002, ADR-035)
- Одне місце для look — усі business rules видно в одному шарі

**Альтернативи:**
- Authorization в контролері через кастомний `IAuthorizationRequirement + AuthorizationHandler` — офіційний ASP.NET підхід для resource-based auth. Відкинуто: додає шар непрямої взаємодії без виграшу для соло-проекту з одним типом owner-check (`InstructorId`)
- Authorization в domain entity методі (`course.UpdateDetails(..., requestingUserId)`) — змішує знання про identity з бізнес-логікою entity, порушує SRP

**Наслідки:**
- Кожен mutating handler робить 2 перевірки: `currentUser.UserId is null` (401) + owner/admin (403)
- Один додатковий fetch на mutation для entity що й так буде fetched — прийнятний трейд-оф
- При зростанні кількості handler'ів можна винести в extension method: `ResultExtensions.EnsureOwnership(Guid resourceOwnerId, ICurrentUserService user)` — cosmetic refactor, не blocker

---

## ADR-040: Course lifecycle — three states + invariants for Publish

**Рішення:** Course має три видимих стани + один службовий:
- `Draft` — default на Create; може бути оновлений, secteur'и/уроки додаються; не видно нікому окрім власника і Admin
- `Published` — видно всім, можна enroll'итись
- `Archived` — видно власнику і Admin (read-only), не можна enroll; переходи в інший стан заборонені через Unpublish (тільки Draft ↔ Published + будь-який → Archived)
- soft-deleted (через `ISoftDeletable`) — лише власник і Admin можуть бачити через `IgnoreQueryFilters`

Інваріанти Publish (перевіряються в handler і в domain методі як last-line defence):
1. `CoverImageUrl` != null
2. Має щонайменше одну секцію
3. Хоча б одна секція має щонайменше один урок

Переходи:
- Create → Draft (автоматично)
- Draft → Published (`Publish()`, всі інваріанти)
- Published → Draft (`Unpublish()`, без інваріантів)
- Будь-який → Archived (`Archive()`, без інваріантів)
- Будь-який → soft-deleted (`Delete`, без інваріантів, навіть з активними enrollments — ADR-041 cont.)

**Чому:**
- Без інваріантів Publish — порожні курси з'являться в пошуку. Поганий UX, погана репутація платформи
- `CoverImageUrl` опціональний на Create (драфт без обкладинки — нормально), обов'язковий на Publish. Інструктор може працювати над контентом, потім підключити обкладинку
- Archive без інваріантів — це "прибрати з пошуку, залишити для власника". Не має бути обмежень що блокують це

**Альтернативи:**
- Publish без інваріантів — пробіг по бізнес-правилу, швидко, але платформа показує порожні курси. Не беру.
- Інваріанти як DB CHECK constraint — unenforceable для "хоча б одна секція з хоча б одним уроком" без триггерів. Відкинуто.

**Наслідки:**
- Handler Publish fetch'ить course з повною структурою (`CourseByIdWithStructureSpecification`), інші mutations — тільки course без nav
- До реалізації Section/Lesson CRUD (наступні чати) Publish завжди падає з `ConflictError("Course cannot be published without at least one section.")`. Це очікувана проміжна поведінка
- FEATURES.md оновлено з lifecycle-таблицею

---

## ADR-041: EnrollmentsCount — денормалізоване поле, стратегія оновлення TBD

**Рішення:** `Course.EnrollmentsCount` існує як колонка в БД з default 0. Поле **не оновлюється** в Phase 3. Стратегія оновлення (event handler vs nightly job vs raw SQL update) обирається в Phase 4 разом з реалізацією Enrollment (B-26) — коли буде конкретний сценарій навантаження.

**Чому зараз так:**
- Додавання поля зараз = одна міграція. Додавання пізніше = ще одна міграція + backfill всіх існуючих записів. Дешевше закласти зараз.
- Рішення про стратегію оновлення потребує знання: скільки очікувано enrollments per course per day, чи допустима затримка в отображенні, чи буде sort by EnrollmentsCount в hot path. Це все стане ясно в Phase 4.

**Альтернативи для майбутньої розмови (Phase 4):**
1. **Event handler (in-process) після EnrollInCourse**: синхронно інкрементує колонку через raw SQL `UPDATE "Courses" SET "EnrollmentsCount" = "EnrollmentsCount" + 1 WHERE "Id" = ...`. Плюс: завжди актуально. Мінус: write amplification, тупик при race condition якщо не атомарно.
2. **Integration event через MassTransit** (Phase 6+): async consumer оновлює через raw SQL. Плюс: не блокує enrollment. Мінус: eventual consistency (user щойно enroll'ився — counter ще старий)
3. **Nightly job** (Hangfire / IHostedService): один `UPDATE ... SET EnrollmentsCount = (SELECT COUNT(*) FROM Enrollments WHERE ...)` вночі. Плюс: простий, один запит, завжди correct. Мінус: максимальна затримка 24h у counter.
4. **COUNT() on read**: без денормалізованого поля взагалі. Відкидаємо — сенсу в полі нема.

**Наслідки:**
- Запит `GetCourseById` повертає `EnrollmentsCount = 0` для всіх курсів до реалізації в Phase 4
- Якщо Phase 4 обере варіант 3 (nightly job) — треба записати інтервал і допустиму затримку у цей ADR як update, або супер-ADR (не створювати новий)
- Sort by EnrollmentsCount в B-21 (list with sorting) — використовуватиме це поле в readonly режимі

---

## ADR-042: Category.IsSystem flag — захист seeded категорій від видалення/перейменування

**Рішення:** `Category` має поле `IsSystem: bool`. Seeded через `CategorySeederHostedService` категорії створюються з `IsSystem = true`. Domain метод `Category.Rename` кидає `InvalidOperationException` якщо `IsSystem`. Майбутній `DeleteCategoryCommand` валідуватиме `!IsSystem` перед видаленням. Admin UI приховує кнопки edit/delete для системних.

**Чому:**
- Seeded категорії — частина domain data platform'и. Їх видалення має бути неможливим, не тільки UI-приховуванням
- Flag на entity = перевірка в одному місці (domain), не розмазана по UI + API validation
- Admin міг випадково зробити DELETE через Swagger/curl — flag захищає

**Альтернативи:**
- Окрема таблиця `SystemCategories` — overkill для однобітового концепта
- Hardcoded list seeded slugs у коді + перевірка проти нього — працює, але розсинхрон між seeder і validator можливий
- Без захисту взагалі, "Admin не тупий" — не беру, explicit > implicit

**Наслідки:**
- Додатковий bit per row — незначно
- Категорія яка була створена як user-level (`IsSystem = false`) і її slug потім додали в seeder — залишиться IsSystem=false (seeder пропускає дублікати). Документовано: щоб "підвищити" категорію — треба ручний UPDATE

---

## ADR-043: IsFree як computed property на DTO, не окреме поле на entity

**Рішення:** `Course` має тільки поле `Price: decimal`. Семантика "free course" = `Price == 0`. Жодного окремого `IsFree: bool` на entity. У `CourseDetailDto` є computed поле `IsFree => Price == 0m` для зручності фронтенду.

**Чому:**
- Два поля що мають узгоджуватись — гарантований розсинхрон у довгій перспективі (Price = 10, IsFree = true — баг легкий, ціна виправлення висока)
- Price як single source of truth — явний і прозорий
- Фронтенд все одно рендерить "Free" на основі price, окреме поле не дає value

**Альтернативи:**
- Поле `IsFree: bool` на entity — обмежений upside (швидший фільтр за вільними курсами в SQL — можна додати індекс по Price, стане дешево), гарантований downside (розсинхрон)
- Computed column в DB `IsFree AS (CASE WHEN Price = 0 THEN TRUE ELSE FALSE END)` — можливо у майбутньому, якщо фільтр "тільки безкоштовні" стане hot path. Поки що — не треба.

---

## ADR-044: Course як aggregate root для structure mutations

**Рішення:** Усі структурні операції (create/update/delete/reorder sections, create/update/delete/reorder lessons) проходять через публічні методи `Course`. `Section` і `Lesson` мають `internal` setters/mutators — доступні тільки з Domain assembly (тобто тільки з `Course`). Не створюємо `ISectionRepository` / `ILessonRepository` — єдиний repo `ICourseRepository` вже достатній.

Handler pattern для будь-якої structure mutation:
1. Fetch `Course` через `CourseByIdWithStructureSpecification(id, forUpdate: true)` (з tracking, включає `Sections.Lessons`)
2. Owner check через `ICurrentUserService` + `course.InstructorId`
3. Викликати domain метод (`course.AddSection(...)`, `course.RemoveLesson(...)` тощо)
4. `unitOfWork.SaveChangesAsync()`
5. Catch `InvalidOperationException` → `ConflictError`

**Чому:**
- Invariants (`Published course must have cover + ≥1 section + ≥1 lesson`) мусять завжди лишатись true для Published курсу (ADR-045). Щоб їх перевірити після mutation — треба бачити in-memory стан усієї структури. Це можливо тільки якщо mutation проходить через aggregate root, який володіє цією структурою
- Canonical DDD: aggregate root є єдиним gateway до свого aggregate. Section/Lesson — частина Course aggregate, не окремі aggregates
- Single source of truth для invariants. Вони живуть у `Course.EnsurePublishableInvariants()` і викликаються з кожного mutation-методу який може їх порушити

**Альтернативи:**
- **Section/Lesson як окремі aggregates з окремими репозиторіями.** Простіший код для create/update, але invariant enforcement для delete на Published вимагав би fetch'у Course все одно + ручний виклик invariant checker у handler. Два шляхи замість одного, invariant logic дублюється між domain і handler
- **Hybrid.** Create/Update через Section/Lesson aggregates, Delete/Reorder через Course. Два шляхи для структурно схожих операцій — антипатерн на code review

**Наслідки:**
- Course entity розрісся на ~12 нових methods. Rich domain model — явний сигнал DDD на code review. Зворотний бік — Course.cs стане кандидатом на partial classes якщо перевалить 500 рядків (зараз ~230)
- Кожна structure mutation — fetch повного курсу (Sections + Lessons). Для курсу з 10 секцій × 50 уроків — 510 записів. Прийнятно; операції рідкісні (інструктор редагує курс не у hot path)
- `Section.UpdateTitle`, `Section.SetOrder`, `Section.AddLesson`, `Section.RemoveLesson`, `Section.ReorderLessons`, `Lesson.UpdateTitle`, `Lesson.SetOrder`, `VideoLesson.Create`, `VideoLesson.UpdateVideo`, `PostLesson.Create`, `PostLesson.UpdatePost` — усі тепер `internal`. Зовнішні споживачі (Application / API) не можуть викликати їх напряму — тільки через Course methods
- `InternalsVisibleTo` для test-проекту знадобиться коли дійдемо до Domain unit tests (щоб тестувати internal методи Section/Lesson напряму)

---

## ADR-045: Publish invariants enforced continuously — не тільки на Publish

**Рішення:** Інваріанти публікації (`CoverImageUrl != null`, `≥1 section`, `≥1 lesson across all sections`) мають **завжди** лишатись true поки `Course.Status == Published`. Перевірка триває не тільки при переході Draft → Published (команда Publish), а після **кожної** mutation що може їх порушити. Конкретно:

- `Course.SetCoverImage(null)` на Published → throw
- `Course.RemoveSection(id)` що залишає 0 секцій на Published → throw  
- `Course.RemoveSection(id)` що залишає секції без жодного уроку на Published → throw
- `Course.RemoveLesson(id)` що залишає курс без жодного уроку на Published → throw

Archived — повністю read-only (всі structure mutations reject'яться через `EnsureStructureMutable()`). Draft — дозволено все без invariant checks.

**Чому:**
- Юзер явно обрав варіант 3 у плануванні скоупу: "Все дозволено: інваріанти перевіряються на save, не на Publish"
- UX без тертя для Published курсів: інструктор може додавати секції/уроки без Unpublish → Publish циклу
- Invariants залишаються під захистом. Published курс ніколи не може бути в стані "порожній у пошуку"

**Альтернативи розглянуті:**
- **Draft only (strict).** Будь-які structure mutations на Published заборонені, треба Unpublish. Простіше на один інваріант, гірше для UX
- **Additive only.** Додавання OK на Published, видалення/reorder — ні. Половинчасте правило, довелося б explicitly блокувати кожен delete handler — складніше у коді ніж continuous invariant

**Наслідки:**
- `Course.EnsurePublishableInvariants()` — private method, викликається з `SetCoverImage`, `RemoveSection`, `RemoveLesson`, `Publish`
- `SetCoverImage(null)` на Published вперше отримує invariant check (раніше просто присвоював)
- При виконанні mutation що порушить invariant: domain throw `InvalidOperationException`, handler catch → `ConflictError` (409). In-memory state entity може бути modified, але `SaveChangesAsync` не викликається → в БД без змін. DbContext scoped per request → при наступному запиті новий DbContext з актуальним станом з БД
- Нові mutating operations на Course / Section / Lesson у майбутньому зобов'язані викликати `EnsurePublishableInvariants()` якщо потенційно можуть порушити одну з трьох inarianov. Документовано як конвенцію

---

## ADR-046: Bulk reorder через окремий endpoint + set-equality validation

**Рішення:** Reorder секцій і уроків виконується через окремі endpoints (`POST /api/courses/{id}/sections/reorder`, `POST /api/courses/{id}/sections/{id}/lessons/reorder`), а не через PATCH `/Order` на окремих сутностях. Payload — масив `{ id, order }` пар. Domain вимагає **full set equality**: payload мусить містити рівно всі існуючі секції/уроки — ні більше, ні менше. Validator перевіряє shape (non-empty, cap на кількість, unique IDs per payload, orders ≥ 0), domain перевіряє semantic set equality через `ReorderValidation.EnsureValid`.

**Чому:**
- **Атомарність.** Один transaction. Альтернатива — N окремих PATCH'ів — створює проміжні стани де order дублюється (A.Order=1, B.Order=1 на якусь мить). Неможливо підтримувати унікальність без складних lock'ів
- **Full set equality.** Клієнт посилає повний знімок бажаного порядку. Простіша логіка: "ось як має виглядати — застосуй". Альтернатива (partial reorder з ретельним зсувом) — джерело багів
- **Domain-level validation.** Інваріанти "unique IDs, unique orders, matches existing set" — це aggregate invariants, не shape-checks. Валідатор може лише приблизно перевірити shape, domain гарантує semantics

**Альтернативи:**
- **PATCH /sections/{id}** з полем `Order` — потребує ручного обробника колізій або lock'у. Не робиться в production-grade системах
- **Dedicated `order` fractional indexing** (Lexorank, arbitrary-precision) — уникає перепису всіх Order при вставці. Overkill для LMS де reorder — явна операція користувача, не continuous drag

**Наслідки:**
- Reorder cost: `UPDATE ... SET Order = ... WHERE Id = ...` × N — один `SaveChangesAsync` породить N UPDATE statements в транзакції EF. Прийнятно для десятків секцій/уроків
- `ReorderValidation.EnsureValid` — internal shared helper у `Learnix.Domain.Common`. Переюзабельний для майбутніх reorder'ів (questions в тесті, options в choice question, тощо)
- Validator cap: 500 секцій, 1000 уроків за один reorder. Arbitrary, але захищає від DoS запитів з мільйоном IDs

---

## ADR-101: Custom DomainException для захисту бізнес-інваріантів

**Рішення:** Створено кастомний `DomainException` у `Learnix.Domain.Common.Exceptions`. Усі перевірки інваріантів у сутностях (наприклад, `EnsurePublishableInvariants` у `Course`) кидають саме цей виняток замість стандартного `InvalidOperationException`.

**Чому:**
- Перехоплення базового `InvalidOperationException` в Application-шарі є небезпечним антипатерном. Воно маскує реальні системні баги (наприклад, падіння `.First()` при відсутності елемента, або збої Entity Framework) і перетворює їх на бізнес-помилки.
- Кастомний `DomainException` створює чіткий контракт: хендлер точно знає, що перехоплює виключно свідоме порушення бізнес-правил домену, а не технічний збій.

**Альтернативи:**
- Повертати `Result` з доменних методів — відкинуто. Доменна модель має залишатись чистою і не залежати від бібліотек контролю потоку (FluentResults).
- Ловити `InvalidOperationException` — відкинуто через ризик приховування багів і втрати stack trace.

**Наслідки:**
- Усі мутаційні Command Handlers, що працюють з агрегатом `Course`, обгортають виклики доменних методів у `try-catch (DomainException)` і повертають `Result.Fail(new ConflictError(ex.Message))`.
- Усі інші системні винятки не перехоплюються хендлерами і вільно спливають до `ExceptionHandlingMiddleware` для генерації 500 Internal Server Error.

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
