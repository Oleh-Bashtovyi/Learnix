# Learnix — ADR: Архітектурні рішення

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення.

Суміжні файли: [DECISIONS_AUTH.md](DECISIONS_AUTH.md) · [DECISIONS_DOMAIN.md](DECISIONS_DOMAIN.md) · [DECISIONS_INFRA.md](DECISIONS_INFRA.md)

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
- Кастомний `Result<T>` — працює, але доведеться розширювати вручну
- Exceptions для бізнес-помилок — порушує контракт "exceptions = unexpected failures"
- ErrorOr — теж варіант, але FluentResults має ширший API

**Наслідки:**
- Command handlers повертають `Result` або `Result<T>`
- Query handlers повертають `Result<TResponse>`
- Controllers маплять `result.IsFailed` → BadRequest, `result.IsSuccess` → Ok

---

## ADR-003: FluentValidation + FluentResults в pipeline (без exceptions)

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

## ADR-004: Typed errors (FluentResults custom errors) замість string matching

**Рішення:** Для класифікації помилок використовуємо типізовані класи
що наслідують FluentResults.Error, а не string matching по повідомленню.

Базові типи:
- NotFoundError — 404
- ValidationError — 400 (якщо потрібно за межами FluentValidation)
- ConflictError — 409 (already enrolled, duplicate, тощо)
- ForbiddenError — 403
- AuthenticationError — 401 (додано, див. DECISIONS_AUTH.md ADR-009)

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

## ADR-005: ProblemDetails для помилок, чистий DTO для успіху

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

## ADR-006: Монорепо (frontend + backend в одному репозиторії)

**Рішення:** Один репозиторій: `learnix/Learnix.Backend/` + `learnix/learnix-client/`.

**Чому:**
- Соло-проєкт, один release cycle — два репо додають overhead без користі
- Спільний Docker Compose, один PR = end-to-end фіча
- Портфоліо: один лінк — весь проєкт

**Альтернативи:**
- Два окремі репо — має сенс для різних команд з різними deploy cycles, тут нерелевантно

---

## ADR-007: Ручний маппінг без AutoMapper

**Рішення:** Entity → DTO маппінг через extension methods (ToDto(), ToResponse()).
Без AutoMapper чи Mapster.

**Чому:**
- Явний, compile-time safe, легко дебажити
- Для 20-30 DTO overhead мінімальний
- AutoMapper ховає помилки за магією конвенцій

---

## ADR-008: IDomainEvent без залежності від MediatR — адаптер в Application

**Рішення:** Інтерфейс `IDomainEvent` в `Learnix.Domain.Common` — чистий marker без наслідування `INotification`. MediatR-специфічна обгортка `DomainEventNotification<TDomainEvent> : INotification` живе в `Learnix.Application.Common.Events`. `ApplicationDbContext.SaveChangesAsync` публікує domain events через `MakeGenericType` + `Activator.CreateInstance`, обгортаючи кожен event в відповідний `DomainEventNotification<T>`.

**Чому:**
- Domain layer не має знати про MediatR — це інфраструктурна бібліотека
- Змінити mediator (теоретично) — переписати один адаптер, не всі domain events
- Handlers в Application пишуться як `INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>` — трохи більше boilerplate, але явно видно що це reaction on domain event

**Альтернативи:**
- `IDomainEvent : INotification` — простіше, але порушує dependency rule
- Власний `IDomainEventDispatcher` без MediatR взагалі — більше коду, втрата in-process pub/sub фіч MediatR

---

## ADR-009: Структура папок Application — гібрид feature-first + cross-cutting Common/Abstractions

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

## ADR-010: Бізнес-логіка — виключно в Application layer

**Рішення:** Будь-яка бізнес-логіка (правила домену, оркестрація, реакція на доменні події) живе **тільки** в `Learnix.Application`. Шари Infrastructure та API не містять жодної бізнес-логіки.

**Що вважається бізнес-логікою:**
- Рішення "інкрементувати/декрементувати CoursesCount категорії" — ЦЕ бізнес-логіка.
- Рішення "course не може бути published без опису" — бізнес-логіка (у Domain або Application validator).
- Вибір яку сутність завантажити, яке доменне правило застосувати, яке доменне поле оновити — бізнес-логіка.

**Що Infrastructure робити НЕ повинна (окрім технічної реалізації):**
- Завантажувати сутності через `DbContext`/репозиторій і викликати доменні методи → це Application (event handler, command handler).
- Вирішувати, чи треба оновлювати лічильник залежно від стану сутності (`WasPublished`) → це Application.
- Виконувати SQL-запит з бізнес-умовами (`WHERE Status = Published`) без явного делегування з Application → це Application.

**Що Infrastructure робить:** технічні операції, незалежні від бізнес-правил — запис у Outbox, відправка HTTP-запиту, запис у Blob Storage, читання конфігурації, DI-реєстрація. Event handlers у Infrastructure виключно створюють infrastructure side-effects (OutboxMessage, blob-операції), але не приймають бізнес-рішень.

**Що API (контролери) робить:** приймає HTTP-запит, делегує в MediatR, маплить Result на HTTP-відповідь. Жодних умов, жодного звернення до репозиторіїв, жодного виклику доменних методів напряму.

**Антипатерн — порушення, яке спричинило цей ADR:**

```
// WRONG — Infrastructure handler з бізнес-логікою всередині
internal sealed class CoursePublishedCountHandler(OutboxDbContextHolder holder) ...
{
    var category = await ctx.Categories.FirstOrDefaultAsync(...);
    category?.IncrementCoursesCount(); // ← бізнес-рішення в Infrastructure
}

// RIGHT — Application handler через абстракцію
internal sealed class CoursePublishedCountHandler(CategoryCoursesCountUpdater updater) ...
{
    return updater.IncrementAsync(notification.DomainEvent.CategoryId, ct);
}
```

**Чому це важливо:**

1. **Тестованість.** Application handlers тестуються через mock-репозиторії. Infrastructure handlers, що обходять цей шар, тестуються тільки з реальним DbContext.
2. **Pipeline.** Логіка в Application проходить через MediatR pipeline: логування, валідація, error handling. В Infrastructure — ні.
3. **Dependency rule.** Infrastructure залежить від Application, а не навпаки. Якщо бізнес-логіка в Infrastructure — вона залежить від конкретного DbContext, EF Core, поточного провайдера БД. Це робить бізнес-правило неявно прив'язаним до технічного вибору.
4. **Єдине місце пошуку.** Розробник шукає "де вирішується чи треба оновити лічильник" — завжди в Application. Не треба шукати по Infrastructure чи API.

**Практичне правило для перевірки:**
> Якщо код у Infrastructure або API містить `if`, що базується на стані **доменної сутності** (не на технічному стані), або викликає доменний метод — це бізнес-логіка, яка має переїхати в Application.

**Альтернативи, що відкинуті:**
- "Зручніше покласти в Infrastructure, бо там є DbContext" — зручність реалізації не може бути підставою порушувати шарування.
- "Контролер може перевірити умову, він же знає контекст HTTP-запиту" — контролер не знає бізнес-контексту, він знає тільки HTTP. Умови з доменним змістом → Application validator або domain entity.

---

## Шаблон для нових записів

```
## ADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
