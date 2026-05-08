# Learnix — ADR: Інфраструктура

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

Суміжні файли: [DECISIONS_ARCHITECTURE.md](DECISIONS_ARCHITECTURE.md) · [DECISIONS_AUTH.md](DECISIONS_AUTH.md) · [DECISIONS_DOMAIN.md](DECISIONS_DOMAIN.md)

## Конвенція статусів

ADR не видаляються. Якщо рішення переглянуто — старий ADR помічається `Superseded by ADR-XXX`, новий — `Supersedes ADR-YYY`. Це зберігає історію мислення і показує як архітектура еволюціонувала.

---

## ADR-001: PostgreSQL + MongoDB (polyglot persistence)

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

## ADR-002: MassTransit + Azure Service Bus для async processing

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

## ADR-003: Specification Pattern для queries

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

## ADR-004: Redis для кешування queries

**Рішення:** Queries, що імплементують `ICacheable`, автоматично кешуються в Redis через `CachingBehavior`.

**Чому:**
- Популярні курси, каталог категорій — read-heavy, рідко змінюються
- Redis дає O(1) lookup і TTL з коробки
- Pipeline behavior — кешування прозоре для handler, без boilerplate

**Інвалідація:**
- Commands, що змінюють дані, явно викликають `ICacheService.RemoveAsync(key)` після SaveChanges

---

## ADR-005: Entity Framework Core TPH для Lesson types

**Рішення:** Video, Post, Test lessons зберігаються в одній таблиці `Lessons` з дискримінатором `LessonType` (Table Per Hierarchy).

**Чому:**
- Спільні поля (Title, Order, SectionId) не дублюються
- Один query для "всі уроки секції" без UNION
- EF Core має найкращу підтримку саме TPH

**Альтернативи:**
- TPT (Table Per Type) — чистіша схема, але N+1 joins на кожен запит
- Окремі таблиці без наслідування — максимальна гнучкість, але дублювання і складні queries

---

## ADR-006: Offset-based пагінація через PaginatedResult<T> + PaginationRequest

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

## ADR-007: Audit fields через EF SaveChanges interceptor

**Рішення:** CreatedAt / UpdatedAt встановлюються автоматично через
EF SaveChanges interceptor. Properties мають private set —
interceptor встановлює через EF ChangeTracker (без рефлексії,
EF нативно підтримує private setters).

**Чому:**
- Жоден handler не забуде встановити дату
- Логіка в одному місці, не розмазана по всіх commands
- Private set — ніхто окрім interceptor не змінить значення випадково

---

## ADR-008: CacheKeys в Application layer, не Domain

**Рішення:** `CacheKeys` константи живуть в `Learnix.Application.Common.Constants.CacheKeys`, а не в `Learnix.Domain.Constants`.

**Чому:**
- Кешування — інфраструктурна турбота. Domain не повинен знати що десь є Redis
- Domain має залишатись максимально чистим від крос-cutting concerns

**Альтернативи:**
- Лишити в Domain — працює, але змішує рівні абстракцій

---

## ADR-009: DbContext сам реалізує IUnitOfWork

**Рішення:** `ApplicationDbContext` реалізує `IUnitOfWork`. Окремого класу `UnitOfWork` немає. DI: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>())` — резолв в той самий scope instance.

**Чому:**
- Окремий `UnitOfWork` клас просто делегував би `SaveChangesAsync` в DbContext — зайвий шар indirection
- Application шар все одно бачить тільки `IUnitOfWork`, не DbContext — абстракція зберігається
- Менше файлів, менше DI-реєстрацій, менше шансів облажатись з scopes

**Альтернативи:**
- Окремий `UnitOfWork` клас — канонічний підхід, але додає шар без функціональної цінності

---

## ADR-010: Outbox pattern — часткова реалізація для blob-операцій (Phase 3)

> **Оновлено** після реалізації. Оригінальне рішення "відкласти до Phase 6" виявилось неправильним: blob storage потребував надійних гарантій вже при першому ж use case (підтвердження аватара / видалення старих blob'ів).

**Рішення (поточний стан):** Outbox реалізовано **цілеспрямовано для blob-операцій**, не як загальний механізм для всіх domain events. Domain events публікуються in-process через `DomainEventsInterceptor` після `SaveChangesAsync` — ризик втрати залишається для подій що ведуть до emails (прийнятно до Phase 6 з MassTransit). Blob-операції (confirm / delete) надійно зберігаються в `OutboxMessage` в тій самій транзакції.

**`OutboxMessage` entity:**
- `Id`, `Type` (`DeleteBlob` / `MarkBlobConfirmed`), `Payload` (JSONB)
- `OccurredAt`, `ProcessedAt?`, `AttemptCount`, `LastAttemptAt?`, `LastError?`, `NextRetryAt?`
- Записується domain event handler в тій самій EF транзакції що і зміни entity

**Outbox worker (background `IHostedService`):**
- Polling-ом читає `WHERE ProcessedAt IS NULL AND (NextRetryAt IS NULL OR NextRetryAt <= NOW())`
- Викликає `IOutboxMessageDispatcher.DispatchAsync(message)` → `IBlobStorageService.MarkConfirmedAsync` / `DeleteAsync`
- Exponential backoff через `NextRetryAt` при помилках

**Чому blob-first, не загальний outbox:**
- Blob-операції є критично важливими вже зараз: якщо `MarkConfirmedAsync` не викличеться — blob видаляється Azure lifecycle policy. Якщо `DeleteAsync` не викличеться — orphaned blob залишається назавжди.
- Email через `IEmailSender` (console mock) — втрата при крашу прийнятна для dev phase. Реальний email (Phase 6) буде через MassTransit з власними гарантіями.
- Загальний outbox для всіх domain events — складніша механіка (серіалізація будь-якого event, десеріалізація назад у конкретний тип, replay через MediatR). Не вартує до Phase 6 коли з'явиться MassTransit.

**Що залишається ризиком (до Phase 6):**
- Якщо процес падає між `SaveChangesAsync` і dispatch domain events через MediatR — email events (`UserRegistered`, `PasswordResetRequested`) втрачаються. Blob-операції НЕ втрачаються (вони в OutboxMessage).

**Плани Phase 6:**
- Загальний outbox для domain events → integration events через MassTransit
- Або видалити in-process dispatch, замінити повністю на outbox + worker + MassTransit

---

## ADR-011: Infrastructure отримує FrameworkReference на Microsoft.AspNetCore.App

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
- Той самий компроміс що й DECISIONS_AUTH.md ADR-002 (User : IdentityUser): формально менш чисто, прагматично необхідно

---

## ADR-012: Авто-міграції тільки в Development

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

## ADR-013: Azure Blob Storage — two-phase upload + outbox for side-effects

**Рішення:** Завантаження файлів відбувається у два кроки: (1) клієнт запитує pre-signed SAS URL через `POST /api/uploads/request-url`; (2) клієнт завантажує файл напряму до Azure, минаючи API. Entity-handler отримує лише blob path і валідує його через `IBlobStorageService.ValidateAsync()` (перевірка існування + magic bytes + size limit). Після `SaveChangesAsync` blob підтверджується через `OutboxMessage(MarkBlobConfirmed)`. Старий blob видаляється через `OutboxMessage(DeleteBlob)`. Непідтверджені blobs видаляються Azure lifecycle policy.

**Чому pre-signed upload:**
- Файли не проходять через API сервер — знімає memory/bandwidth pressure для великих відео (до 2 ГБ)
- API не потребує file streaming middleware чи multipart parsing
- Azure зберігає blob атомарно — або blob є, або нема. Немає stale partial upload

**Чому magic byte validation, не Content-Type header:**
- `Content-Type` header на SAS PUT може бути підроблений клієнтом
- Magic bytes (перші N байт файлу) не підробляються без реального перезапису файлу
- Реалізовано для jpeg (`FF D8 FF`), png (`89 50 4E 47`), webp (`52 49 46 46...57 45 42 50`), mp4 (`ftyp` box), webm (`1A 45 DF A3`), pdf (`%PDF`)

**Чому outbox для blob side-effects (а не direct call після SaveChanges):**
- `MarkConfirmedAsync` і `DeleteAsync` — network calls що можуть впасти. In-process виклик після `SaveChangesAsync` не атомарний з entity persist
- Якщо процес падає між SaveChanges і `MarkConfirmedAsync` — blob видалиться lifecycle policy, entity вже вказує на нього → data corruption
- `OutboxMessage` в тій самій транзакції що і entity — гарантує що операція буде виконана рано чи пізно (ADR-010)

**Blob path naming:**
```
avatars/users/{userId}/{uploadId}.{ext}
course-covers/courses/{courseId}/{uploadId}.{ext}
course-videos/courses/{courseId}/lessons/{lessonId}/{uploadId}.mp4
certificates/{code}.pdf
```

**UploadTarget validation:**
| Target | Max size | Allowed types |
|---|---|---|
| Avatar | 5 MB | jpeg, png, webp |
| CourseCover | 10 MB | jpeg, png, webp |
| LessonVideo | 2 GB | mp4, webm |
| Certificate | 5 MB | pdf |

**Альтернативи:**
- Multipart upload через API — простіше для клієнта, але API стає bottleneck для відео. Відкинуто
- Підтвердження без outbox (пряме `MarkConfirmedAsync` після SaveChanges) — вразливе до crash між операціями. Відкинуто після аналізу ризику

**Наслідки:**
- `IBlobStorageService` живе в `Application/Common/Abstractions/Storage/` — cross-cutting abstraction
- `AzureBlobStorageService` в `Infrastructure/Storage/` — реалізація
- `BlobStorageBootstrapper` (hosted service) — перевіряє/створює Azure containers при старті
- Blob paths зберігаються в entity (не full SAS URL). SAS читання генерується on-demand через `GenerateReadUrl(blobPath, ttl)`

---

## ADR-014: Асинхронна генерація PDF-сертифікатів через BackgroundService

**Рішення:** PDF сертифікат генерується не в момент завершення курсу, а фоновим сервісом (`CertificatePdfGenerationService`) що опитує таблицю `Certificates` з `FileUrl IS NULL` кожні 30 секунд. Після генерації PDF завантажується в Azure Blob Storage, а `Certificate.FileUrl` оновлюється. Клієнт отримує `IsReady: false` допоки PDF не готовий.

**Чому:**
- Генерація PDF (QuestPDF) + завантаження в Blob — блокуючі I/O операції, які неприпустимо тримати в HTTP-запиті (типово 200–500ms+)
- Фоновий сервіс не блокує відповідь `MarkLessonComplete` — студент отримує миттєве підтвердження завершення курсу
- Простота: не потребує MassTransit (ADR-002) чи Outbox pattern (ADR-010) — `BackgroundService` з `PeriodicTimer` вже є в кодовій базі

**Альтернативи:**
- **Inline (sync)** — найпростіше, але ризик таймауту та повільної відповіді API. Відхилено.
- **MassTransit consumer** — найправильніше архітектурно, але ADR-002 ще не імплементований. Відкладено.
- **Outbox pattern** — надійніше (гарантована доставка), але надмірно для поточного етапу. Відкладено разом з ADR-010.

**Наслідки:**
- `GET /api/certificates/courses/{courseId}` може повернути `IsReady: false` одразу після завершення курсу (вікно ~0–30 сек)
- Фронтенд має polling або показувати стан "генерується…"
- При міграції на MassTransit (ADR-002): замінити `CertificatePdfGenerationService` на consumer, решта коду не змінюється

---

## ADR-015: Background job scheduling — IHostedService vs Quartz.NET vs Hangfire

**Рішення:** Для фонових завдань використовуємо `BackgroundService` + `PeriodicTimer` (вбудовано в .NET). Quartz.NET та Hangfire не вводимо поки не виникне конкретна потреба в їхніх можливостях.

**Чому IHostedService достатньо зараз:**
- Всі поточні фонові завдання є idempotent і safe to run on every replica (reconciliation, cleanup, PDF generation, seeding). Паралельний запуск на декількох інстансах не призводить до некоректних результатів.
- Zero additional dependencies — `BackgroundService` є частиною `Microsoft.Extensions.Hosting`.
- Паттерн вже використовується в кодовій базі (RefreshTokenCleanup, CertificatePdfGeneration, OutboxProcessor тощо) — консистентність важливіша за передчасну гнучкість.

**Що вміють Quartz.NET і Hangfire (і чого не вміє IHostedService):**

| Можливість | IHostedService | Quartz.NET | Hangfire |
|---|---|---|---|
| Distributed lock (singleton execution across replicas) | ❌ | ✅ (DB/Redis) | ✅ (DB) |
| Cron-вирази для scheduling | ❌ | ✅ | ✅ |
| Management UI | ❌ | ✅ | ✅ (вбудований) |
| Job persistence (retry after crash) | ❌ | ✅ | ✅ |
| Fire-and-forget з web request | ❌ | ❌ | ✅ |
| Dependency | 0 | Quartz + Quartz.Extensions.Hosting | Hangfire.Core + storage |

**Quartz.NET** — enterprise scheduler, побудований на Unix cron-концепціях. Підходить коли потрібні складні розклади (cron expressions) і distributed locking. Конфігурація вербозна.

**Hangfire** — простіший у налаштуванні. Має вбудований dashboard для моніторингу та ручного перезапуску джобів. Популярний для fire-and-forget завдань з HTTP-запитів (наприклад, надіслати email після реєстрації).

**Ключовий concept — Distributed Lock:**
Якщо API запущений на 3 серверах одночасно (horizontal scaling), `IHostedService` запустить job на ВСІХ 3 серверах паралельно. Quartz.NET та Hangfire вирішують це через distributed lock у спільній БД або Redis: тільки ONE instance виконує job, інші чекають або пропускають тік. Це критично для завдань з side-effects (надсилання email, charge платежу) — дублювання неприпустиме.

**Коли переходити на Quartz.NET або Hangfire:**
- З'являється job, який MUST run exactly once across all replicas (наприклад, надсилання щомісячного дайджесту)
- Потрібен dashboard для моніторингу та ручного retrigger джобів
- Кількість background jobs зростає > ~5–6 і управління ними через `AddHostedService` стає громіздким
- Потрібні складні cron-розклади (перший понеділок місяця, кожен робочий день о 9:00 тощо)

**Наслідки поточного рішення:**
- `CategoryCoursesCountReconciliationService`, `RefreshTokenCleanupHostedService` та інші працюють на кожній репліці паралельно — це прийнятно бо всі вони idempotent.
- При введенні горизонтального масштабування (Phase Deploy) — аудит всіх `IHostedService` на предмет того чи безпечно їх запускати паралельно.
- Якщо MassTransit (ADR-002) буде впроваджено в Phase 6 — частина фонових завдань (email, achievements) перейде до MassTransit consumers. `IHostedService` залишиться для infrastructure-level задач (cleanup, seeding, reconciliation).

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
