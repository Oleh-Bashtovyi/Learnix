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

## ADR-004: Redis distributed cache — ICacheable<TValue> + MediatR pipeline behavior

**Рішення:** Queries, що реалізують `ICacheable<TValue>`, автоматично кешуються в Redis через `CachingBehavior<TRequest, TValue>`. Commands, що змінюють кешовані дані, явно інвалідують відповідні ключі після `SaveChangesAsync`.

---

### Реалізація

**Інтерфейс:**
```csharp
public interface ICacheable<TValue>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
```

**Pipeline behavior** реалізує `IPipelineBehavior<TRequest, Result<TValue>>`, де `TValue` — другий generic-параметр. MediatR закриває тип автоматично: для `GetAllCategoriesQuery : ICacheable<IReadOnlyList<CategoryListItemDto>>` MediatR виводить `TValue = IReadOnlyList<CategoryListItemDto>`. Рефлексії немає — `response.Value` і `Result.Ok(value)` типізовані на рівні компілятора.

**Серіалізація:** кешується тільки `Value` з `Result<T>`, не весь Result-wrapper. `FluentResults.Result<T>` не підтримує JSON roundtrip (private setter на `Value`). `System.Text.Json` серіалізує payload напряму, десеріалізує назад, і behavior обгортає в `Result.Ok(value)`.

**Інвалідація:** у command handlers після `SaveChangesAsync` викликається `IDistributedCache.RemoveAsync(key)`. `IDistributedCache` — це офіційна Microsoft abstraction (не інфраструктурна деталь), тому живе в Application layer поруч з handlers.

---

### Які queries кешуються і чому

| Query | Ключ | TTL | Чому |
|---|---|---|---|
| `GetAllCategoriesQuery` | `categories:all` | 24 год | Список категорій змінюється лише через адмін-дії. Читається при кожному відкритті каталогу та фільтрів. Найдовший TTL — найнижчий churn. |
| `GetFeaturedCoursesQuery` | `courses:featured` | 30 хв | Вибірка популярних курсів — expensive JOIN з сортуванням по enrollments/rating. Запит однаковий для всіх користувачів (public, без per-user контексту). |
| `GetCourseByIdQuery` | `course:{id}` | 10 хв | Сторінка деталей курсу читається масово студентами перед записом. Включає `AverageRating` та `ReviewsCount` — змінюються кожним відгуком. Явна інвалідація при зміні курсу та відгуків. |
| `GetPublicCoursesQuery` | `courses:public:{всі 8 параметрів}` | 5 хв | Каталог — найнавантаженіший endpoint (пошук + фільтри + сортування + пагінація). Унікальний ключ на кожну комбінацію параметрів — неможливо інвалідувати за патерном через `IDistributedCache` без підключення `IConnectionMultiplexer` напряму. Короткий TTL компенсує відсутність явної інвалідації. |

**Що навмисно НЕ кешується:**
- Per-user queries (`GetMyProfile`, `GetMyEnrollments`, `GetMyAchievements`) — у кожного користувача свій стан, частих мутацій багато, ключ включав би userId → мала ймовірність cache hit для конкретного запиту.
- Admin queries — низький трафік, не впливає на продуктивність.
- Real-time дані (chat, SignalR notifications) — завжди актуальні.

---

### Інвалідація — де і чому

**Явна інвалідація `course:{id}` + `courses:featured`** після кожної мутації курсу:
- `PublishCourse`, `UnpublishCourse` — статус курсу змінюється, він з'являється або зникає з каталогу
- `ArchiveCourse`, `UnarchiveCourse` — аналогічно
- `UpdateCourseDetails` — змінюється title, price, cover, category — все є в закешованому DTO
- `DeleteCourse`, `AdminDeleteCourse`, `AdminRecoverCourse`, `AdminUnpublishCourse` — курс повністю змінює стан

**Явна інвалідація `course:{id}`** при мутаціях відгуків:
- `CreateReview`, `UpdateReview`, `DeleteReview` — всі три змінюють `AverageRating` та `ReviewsCount` на `Course` entity. `CourseDetailDto` включає ці поля — без інвалідації кешована сторінка показувала б застарілий рейтинг.

**Явна інвалідація `categories:all`** при будь-якій зміні категорій:
- `CreateCategory`, `UpdateCategory`, `DeleteCategory`, `SetCategoryImage`, `DeleteCategoryImage`

**`GetPublicCoursesQuery` — тільки TTL (5 хв):** оскільки ключ включає всі 8 filter-параметрів (search, skip, take, categoryId, instructorId, sortBy, isFree, minRating), різних комбінацій можуть бути сотні. Видалення за префіксом `courses:public:*` потребує `IConnectionMultiplexer.GetServer().Keys()` — дорога O(N) операція на Redis. Для каталогу 5-хвилинна затримка видимості після публікації курсу прийнятна.

---

### Чому Redis, а не IMemoryCache

`IDistributedCache` (Redis) — єдиний centralized store, `RemoveAsync(key)` є Redis `DEL` command. При горизонтальному масштабуванні (декілька API instances) інвалідація на одному instance автоматично поширюється на всі: наступний запит на будь-якому instance отримає cache miss і перечитає з БД.

`IMemoryCache` — per-process. Інвалідація на instance A не впливає на instances B і C, які продовжують роздавати stale дані до свого TTL. Неприйнятно для даних що явно інвалідуються (category list, course detail).

**Чому:**
- Популярні курси, каталог категорій — read-heavy, рідко змінюються
- Redis дає O(1) lookup і TTL з коробки
- Pipeline behavior — кешування прозоре для handler, без boilerplate в кожному query
- Distributed cache коректно працює при scale-out

**Альтернативи:**
- `IMemoryCache` — простіше, але stale data при multiple instances. Відхилено для публічних запитів.
- Lazy invalidation (тільки TTL для всього) — простіше, але `CourseDetailDto` з рейтингом показував би застарілий rating хвилинами після відгуку. Відхилено для `course:{id}`.
- Response caching middleware (`[ResponseCache]`) — HTTP-level cache, не контролює per-key invalidation. Відхилено.

**Наслідки:**
- `ICacheable<TValue>` в `Application/Common/Caching/`
- `CachingBehavior<TRequest, TValue>` в `Application/Common/Behaviors/`
- `CacheKeys` static class в `Application/Common/Constants/`
- Redis connection string: `ConnectionStrings:Redis` в `appsettings.json`
- Пакети: `Microsoft.Extensions.Caching.StackExchangeRedis` (Infrastructure), `Microsoft.Extensions.Caching.Abstractions` (Application)

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
- Читає `WHERE ProcessedAt IS NULL AND (NextRetryAt IS NULL OR NextRetryAt <= NOW())`
- Викликає `IOutboxMessageDispatcher.DispatchAsync(message)` → `IBlobStorageService.MarkConfirmedAsync` / `DeleteAsync`
- Exponential backoff через `NextRetryAt` при помилках
- **Оновлено (ADR-020):** замість чистого polling (PeriodicTimer 10с) processor тепер прокидається одразу через PostgreSQL LISTEN/NOTIFY. Polling залишається як fallback.

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

## ADR-014: On-Demand (синхронна) генерація PDF-сертифікатів

> **Supersedes**: Попереднє рішення "Асинхронна генерація PDF-сертифікатів через BackgroundService".

**Рішення:** PDF сертифікат генерується синхронно на вимогу користувача (On-Demand) через ендпоінт `POST /api/certificates/courses/{courseId}/generate`. Фоновий сервіс `CertificatePdfGenerationService` повністю видалено.

**Чому:**
- Асинхронний фоновий сервіс (який перевіряв базу кожні 30 сек) створював поганий UX: користувачі бачили статус "Generating..." і не мали контролю над процесом.
- У разі збою генерації або ручного очищення посилання в БД, користувач не міг легко перегенерувати сертифікат.
- Генерація QuestPDF в оперативній пам'яті відбувається достатньо швидко (до 50 мс), тому синхронний виклик не створює значного навантаження на HTTP-потік.

**Альтернативи:**
- **Фоновий Worker (старе рішення)** — відкинуто через поганий UX та складність ручної регенерації.
- **MassTransit consumer** — відкинуто як overkill, оскільки генерація On-Demand вирішує всі проблеми миттєво і архітектурно простіше в імплементації.

**Наслідки:**
- Додано єдиний ендпоінт генерації/регенерації `POST /api/certificates/courses/{courseId}/generate`.
- `CertificatePdfGenerationService` повністю видалено з кодової бази та `DependencyInjection.cs`.
- Фронтенд (кнопки "Download Certificate") викликають мутацію, генерують PDF і одразу відкривають згенероване посилання (`window.location.href`). Більше немає статусу очікування `isReady: false`.

---

## ADR-015: Background job scheduling — IHostedService vs Quartz.NET vs Hangfire

**Рішення:** Для фонових завдань використовуємо `BackgroundService` + `PeriodicTimer` (вбудовано в .NET). Quartz.NET та Hangfire не вводимо поки не виникне конкретна потреба в їхніх можливостях.

**Чому IHostedService достатньо зараз:**
- Всі поточні фонові завдання є idempotent і safe to run on every replica (reconciliation, cleanup, seeding). Паралельний запуск на декількох інстансах не призводить до некоректних результатів.
- Zero additional dependencies — `BackgroundService` є частиною `Microsoft.Extensions.Hosting`.
- Паттерн вже використовується в кодовій базі (RefreshTokenCleanup, OutboxProcessor тощо) — консистентність важливіша за передчасну гнучкість.

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

## ADR-016: Email delivery — MailKit (SMTP) + RazorLight (.cshtml templates) + PreMailer.Net

**Рішення:** Для відправки email використовуємо `MailKit` (SMTP-клієнт) та `RazorLight` для рендерингу `.cshtml` шаблонів. Для стилізації використовується `PreMailer.Net`, який автоматично перетворює CSS-класи з `styles.css` (через загальний `_Layout.cshtml`) на inline-стилі (`style="..."`). Локально — Mailpit у Docker (SMTP :1025, Web UI :8025). На Azure — SendGrid SMTP relay (smtp.sendgrid.net:587, username=`apikey`). `ConsoleEmailSender` видалено.

**Чому:**
- MailKit — промислово зрілий SMTP-клієнт для .NET, підтримує TLS/StartTLS, async.
- RazorLight — standalone Razor engine, не потребує повного ASP.NET MVC pipeline; дозволяє рендерити `.cshtml` в Infrastructure layer.
- PreMailer.Net — більшість email-клієнтів блокують зовнішні CSS-файли. PreMailer вирішує цю проблему, парсячи HTML і вбудовуючи класи як inline-стилі під час рендерингу. Це дозволяє мати чисті шаблони та спільний `_Layout.cshtml`.
- Один і той самий `SmtpEmailSender` для всіх середовищ — змінюється тільки конфіг (`Smtp` секція). Немає vendor lock-in у коді.
- Mailpit — легкий Docker-контейнер для локальної розробки (перехоплює всі листи, показує HTML у браузері).
- SendGrid SMTP relay підтримується на free tier Azure та не потребує зміни коду порівняно з іншими SMTP-провайдерами.

**Альтернативи:**
- SendGrid SDK (`SendGrid` NuGet) — потребує окремої реалізації `IEmailSender`, vendor lock-in; перевага — не потрібен SMTP-порт 587.
- Azure Communication Services Email — Azure-native, але вимагає верифікації домену та дорожче в налаштуванні.
- `System.Net.Mail.SmtpClient` — застарілий, не підтримує async належним чином.

**Наслідки:**
- Шаблони у `Learnix.Infrastructure/Email/Templates/*.cshtml` та `.css`, копіюються до output directory (`Content`, `CopyToOutputDirectory=PreserveNewest`).
- HTML-листи гарантовано сумісні з поштовими клієнтами, залишаючись читабельними для розробників.
- `SmtpSettings` в `Learnix.Infrastructure/Settings/` (internal, тільки Infrastructure знає про SMTP).
- При деплої на Azure: встановити `Smtp__Password` через Azure Key Vault / App Service Environment Variables.
- Коли буде впроваджено MassTransit (ADR-002, Phase 6) — `SmtpEmailSender` залишається, змінюється тільки місце виклику (з Outbox → MassTransit consumer).

---

## ADR-017: Email localization — IStringLocalizer + .resx + Language on User

**Рішення:** Email-шаблони локалізовані на англійську (default) та українську мови через `IStringLocalizer<EmailStrings>` та `.resx` ресурсні файли. Мова зберігається у полі `Language` на entity `User` (default `"en"`). При реєстрації береться з `Accept-Language` header. `SmtpEmailSender` встановлює `CultureInfo.CurrentUICulture` перед рендерингом; `IStringLocalizer` підхоплює культуру автоматично.

**Чому:**
- `IStringLocalizer` + `.resx` — стандартний .NET-підхід; без зовнішніх залежностей.
- Мова на `User` entity — єдине місце правди для всіх email-подій (у т.ч. тих, що відправляються async через Outbox).
- `Accept-Language` при реєстрації — без зайвої UX-складності (не потрібен окремий вибір мови).
- Marker class `EmailStrings` у root namespace `Learnix.Infrastructure` + `ResourcesPath = "Email/Resources"` → файли `.resx` лежать у `Email/Resources/` поруч з шаблонами.

**Альтернативи:**
- Додати `Language` до domain events замість DB-запиту в хендлерах — відкинуто: `Language` є UI/infra concern, не доменний факт.
- Окремий вибір мови в UI (profile setting) — планується як майбутнє покращення; зараз встановлюється при реєстрації.
- Inline conditional замість `.resx` — не масштабується, важко підтримувати.

**Наслідки:**
- `User.Language` (varchar 5, default `en`) — нова колонка через міграцію `AddUserLanguage`.
- Outbox payloads несуть `Language`; outbox handlers вибирають його в `SELECT`.
- Application-layer event handlers (`UserRegisteredDomainEventHandler`, `PasswordResetRequestedDomainEventHandler`) роблять один додатковий SELECT для отримання `Language`.
- `SmtpEmailSender` — singleton; `IStringLocalizerFactory` (singleton) ін'єктується, localizer створюється в constructor.
- `.resx` файли: `Email/Resources/EmailStrings.resx` (EN) та `EmailStrings.uk.resx` (UK) — embedded resources, auto-included SDK.

---

## ADR-018: Мок-оплата замість реального Stripe

**Рішення:** Платіжна система реалізована як мок: кнопка "Pay" одразу записує `Payment` зі статусом `Completed` та `PaymentProvider = "Mock"` і активує enrollment без будь-якого зовнішнього сервісу. `Stripe__SecretKey` прибрано з `.env.example`. Stripe SDK не встановлюється.

**Чому:**
- Це pet-проект. Реальний Stripe потребує верифікації бізнесу, додає комісію 2.9% + $0.30, і значну складність: webhooks, declined cards, refunds, PCI compliance.
- Для портфоліо важлива демонстрація **flow і архітектури** (команда `PurchaseCourse`, доменна подія, зарахування), а не реальне стягнення грошей.
- Мок зберігає повну доменну модель: `Payment` entity з `Amount`, `Status`, `Provider`, `TransactionId` — поле `Provider = "Mock"` чітко сигналізує що це не production.
- Підключити реальний провайдер у майбутньому — зміна в одному місці (handler + DI), без перебудови архітектури.

**Альтернативи:**
- Stripe test mode — все одно потребує облікового запису, webhook endpoint, Stripe SDK. Тестові ключі не заряджають картки, але додають ~200 рядків коду без практичної цінності для пет-проекту.
- Stripe повністю — надлишок для демо-проекту, юридичні вимоги для production.

**Наслідки:**
- `Stripe__SecretKey` прибрано з `Learnix.API/.env.example` і `learnix-client/.env.example`.
- `VITE_STRIPE_PUBLISHABLE_KEY` прибрано з frontend `.env.example`.
- `Payment.PaymentProvider` зберігає `"Mock"` — в майбутньому можна додати `"Stripe"`, `"LiqPay"` тощо.
- При переході на реальний провайдер: замінити логіку в `PurchaseCourseCommandHandler`, додати webhook endpoint, оновити `.env.example`.

---

## ADR-019: Генерація QR-кодів через QRCoder

**Рішення:** Для генерації QR-кодів на сертифікатах використовується бібліотека `QRCoder`.

**Чому:**
- QuestPDF не має вбудованого інструменту для генерації QR-кодів, він приймає лише готові зображення (масиви байтів або потоки).
- `QRCoder` — це надійна, популярна та lightweight C#-бібліотека, яка може легко генерувати QR-коди у вигляді масиву байтів (PNG).
- Це дозволяє легко додавати посилання для швидкої верифікації сертифікатів без необхідності звертатися до сторонніх зовнішніх API (наприклад, Google Chart API), що гарантує стабільну роботу офлайн та кращу приватність.

**Альтернативи:**
- Сторонні API (наприклад, `api.qrserver.com`) — потребують інтернет-з'єднання під час генерації PDF і можуть сповільнювати процес або бути недоступними. Відхилено.
- Написання власного генератора — складна математика (алгоритми Ріда-Соломона), що є reinventing the wheel. Відхилено.

**Наслідки:**
- В `Learnix.Infrastructure` додано залежність `QRCoder`.
- У `CertificatePdfDocument` реалізовано метод `GenerateQrCode()`, який використовується для вставки графіки в PDF-макет QuestPDF.

---

## ADR-020: Outbox latency — PostgreSQL LISTEN/NOTIFY замість polling-only

> Частково supersedes ADR-010 в частині «Outbox worker (background IHostedService)» — механізм диспатчу повідомлень змінено з чистого polling на push-first з polling fallback.

**Контекст і проблема:**

Початкова реалізація Outbox (ADR-010) використовувала чистий polling: `OutboxProcessorService` з `PeriodicTimer(10s)` щоразу робив SELECT по таблиці `OutboxMessages`. Це працювало для blob-операцій та emails, де затримка 10с була прийнятною.

Проблема стала критичною з появою ланцюгових подій у системі досягнень (ADR-ACHIEVEMENT-001, ADR-ACHIEVEMENT-007):

```
LessonCompleted → SaveChanges
    → DomainEventsInterceptor → outbox: EvaluateLessonCompleted
    → ⏳ до 10с (polling)
    → AchievementEvaluator → UserAchievement.Unlock() → SaveChanges
        → DomainEventsInterceptor → outbox: NotifyAchievementUnlocked
        → ⏳ ще до 10с (polling)
        → SignalR push → toast у браузері
```

Два цикли polling = **до 20 секунд** від завершення уроку до нотифікації про досягнення. Для UX — неприйнятно.

---

**Рішення:** `OutboxProcessorService` тепер прокидається одразу після INSERT в `OutboxMessages` через нативний PostgreSQL механізм `LISTEN/NOTIFY`. Polling з інтервалом 10с залишається як fallback.

**Як працює PostgreSQL LISTEN/NOTIFY:**

PostgreSQL має вбудований lightweight pub/sub механізм, окремий від реплікації та WAL. Він працює на рівні сесії (connection):

1. **NOTIFY** — будь-яка транзакція може виконати `pg_notify('channel_name', 'optional_payload')`. Повідомлення буферизується і відправляється **тільки після COMMIT** транзакції. Якщо транзакція відкочується — notification не відправляється. Це ключова гарантія: processor отримує сигнал лише про committed дані.

2. **LISTEN** — клієнт (NpgsqlConnection) реєструється на каналі. Після цього будь-який `NOTIFY` на цьому каналі з будь-якого з'єднання доставляється як подія до всіх LISTEN-підписників. PostgreSQL гарантує доставку до всіх активних підписників на момент COMMIT.

3. **Обмеження:** Якщо підписник відключений в момент NOTIFY — повідомлення втрачається. LISTEN/NOTIFY не має persistence (на відміну від message broker). Саме тому polling залишається як fallback: навіть якщо listener був відключений, processor підхопить повідомлення на наступному 10-секундному тіку.

**Архітектура реалізації (3 компоненти):**

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

`FOR EACH STATEMENT` (не `FOR EACH ROW`) — якщо один `SaveChanges` записує 5 outbox-повідомлень, trigger спрацьовує один раз. Payload порожній — потрібен лише факт «є нові повідомлення», конкретні ID не потрібні, бо processor робить свій SELECT з фільтром.

**2. `OutboxNotificationListener` (Infrastructure BackgroundService):**

Dedicated long-lived `NpgsqlConnection` (не з пулу!) слухає канал `outbox_new`:

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var cmd = new NpgsqlCommand("LISTEN outbox_new", connection);
await cmd.ExecuteNonQueryAsync(ct);

while (!ct.IsCancellationRequested)
    await connection.WaitAsync(ct);  // blocks until notification arrives
```

Чому dedicated connection: PostgreSQL LISTEN state прив'язаний до конкретної сесії. Connection pooling (Npgsql `NpgsqlDataSource`) повертає з'єднання в пул після використання — LISTEN state втрачається. Тому listener відкриває окреме з'єднання, яке живе весь lifetime додатку.

При розриві з'єднання — автоматичний reconnect з exponential backoff (1с → 2с → 4с → ... → 30с cap). Під час reconnect polling fallback забезпечує доставку.

**3. `OutboxSignal` (in-process bridge):**

`SemaphoreSlim` singleton, що зв'язує listener і processor. Listener викликає `signal.Notify()` при отриманні PG notification. Processor чекає `signal.WaitAsync(10s, ct)` — повертається одразу при сигналі або через 10с (fallback).

Додатково: processor сам сигналить себе (`signal.Notify()`) якщо обробив **хоча б одне повідомлення** (`messages.Count > 0`). Це гарантує миттєву обробку каскадних подій (наприклад, коли під час обробки одного повідомлення генерується інше — `NotifyAchievementUnlocked`), не чекаючи на новий сигнал від бази чи 10с таймаут.

**Результат:**

| Сценарій | Polling-only | LISTEN/NOTIFY + fallback |
|---|---|---|
| Single-hop (email, blob) | до 10с | < 100ms |
| Achievement chain (2 hops) | до 20с | < 500ms |
| Idle load (немає повідомлень) | SELECT кожні 10с | SELECT кожні 10с |
| Нові залежності | — | 0 (Npgsql вже є) |

---

**Альтернативи що розглядались:**

1. **Зменшити polling interval до 1с** — найпростіше, але 1 SELECT/с на порожній таблиці = зайве навантаження. При scale-out (N instances) це N SELECT/с. Не масштабується.

2. **In-process SemaphoreSlim без PostgreSQL** — сигналити з `DomainEventsInterceptor` напряму (без PG trigger). Працює для single-instance, але при горизонтальному масштабуванні instance A записує outbox message, а instance B (де крутиться processor) не отримає сигнал. PG LISTEN/NOTIFY працює cross-connection і cross-process.

3. **Debezium CDC (Change Data Capture)** — Debezium підключається до PostgreSQL WAL і стрімить зміни в Kafka topic. Це production-grade рішення для мікросервісів. Відхилено: потребує Kafka + Debezium connector + Kafka consumers — disproportionate для моноліту. Правильний вибір при переході на мікросервісну архітектуру.

4. **Wolverine framework** — .NET application framework з вбудованим LISTEN/NOTIFY outbox. Відхилено: Wolverine замінює MediatR і має свій pipeline — це не drop-in рішення, а міграція всієї архітектури.

5. **CAP library** — lightweight event bus з вбудованим outbox. Відхилено: вводить власні абстракції (`ICapPublisher`, `ICapSubscribe`), власну outbox таблицю. Конфлікт з існуючою outbox реалізацією.

6. **Hybrid: optimistic dispatch + outbox as safety net** (NServiceBus підхід) — після COMMIT спробувати одразу відправити повідомлення in-process, outbox як fallback при crash. Відхилено для поточної архітектури: потребує зміни в Application layer (handler повинен знати про dispatch), що суперечить розділенню шарів.

---

**Наслідки:**

- Migration `AddOutboxNotifyTrigger` створює PL/pgSQL функцію і trigger.
- `OutboxNotificationListener` в `Infrastructure/Services/` — окремий `BackgroundService`.
- `OutboxSignal` в `Infrastructure/Outbox/` — singleton `SemaphoreSlim` wrapper.
- `OutboxProcessorService` змінено: `PeriodicTimer` → `outboxSignal.WaitAsync(10s)`.
- Один додатковий PostgreSQL connection (не з пулу) для LISTEN — мінімальний resource footprint.

**Scale-out safety (`FOR UPDATE SKIP LOCKED`):**

Outbox processor використовує `SELECT ... FOR UPDATE SKIP LOCKED` замість звичайного SELECT:

```sql
SELECT * FROM "OutboxMessages"
WHERE "ProcessedAt" IS NULL AND "NextRetryAt" <= {now}
ORDER BY "OccurredAt"
LIMIT {batch_size}
FOR UPDATE SKIP LOCKED
```

- `FOR UPDATE` — лочить вибрані рядки на рівні PostgreSQL транзакції. Інші транзакції не можуть їх SELECT FOR UPDATE до COMMIT.
- `SKIP LOCKED` — якщо рядок вже залочений іншим інстансом, пропустити його замість очікування (на відміну від `NOWAIT`, який кидає помилку).
- **Timestamp rounding buffer:** Змінна `{now}` розраховується як `DateTime.UtcNow.AddSeconds(1)`. Це обходить проблему мікросекундного округлення PostgreSQL (`timestamp` має точність 1us, а `.NET DateTime` — 100ns), яке могло призводити до того, що щойно вставлене повідомлення отримувало `NextRetryAt` на мікросекунду в майбутньому і пропускалося запитом.
- Результат: Instance A бере повідомлення 1–10, Instance B бере 11–20. Ніякого дублювання.
- Саме цей механізм використовують MassTransit, Wolverine, і NServiceBus для своїх outbox реалізацій.

Весь batch обгорнутий в explicit transaction (`BeginTransactionAsync` → `CommitAsync`), щоб лок тримався під час обробки повідомлень. `pg_notify` від нових outbox-повідомлень (створених під час обробки, наприклад `NotifyAchievementUnlocked`) буферизується PostgreSQL і доставляється тільки після COMMIT зовнішньої транзакції.

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
