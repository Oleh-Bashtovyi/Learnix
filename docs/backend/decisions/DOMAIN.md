# Learnix — ADR: Доменна модель

Суміжні файли: [DECISIONS_ARCHITECTURE.md](DECISIONS_ARCHITECTURE.md) · [DECISIONS_AUTH.md](DECISIONS_AUTH.md) · [DECISIONS_INFRA.md](DECISIONS_INFRA.md)

---

## ADR-001: Domain entities — private setters + domain methods

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

## ADR-002: Soft delete для Users і Courses, hard delete для решти

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

## ADR-003: Розщеплення BaseEntity на IAuditable + IHasDomainEvents

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

## ADR-004: Гібридний поділ констант — Domain для інваріантів entity, Application для політик валідації

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

## ADR-005: Course lifecycle — three states + invariants for Publish

**Рішення:** Course має три видимих стани + один службовий:
- `Draft` — default на Create; може бути оновлений, секції/уроки додаються; не видно нікому окрім власника і Admin
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
- Будь-який → soft-deleted (`Delete`, без інваріантів, навіть з активними enrollments)

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

## ADR-006: EnrollmentsCount — денормалізоване поле, стратегія оновлення TBD

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

## ADR-007: Category.IsSystem flag — захист seeded категорій від видалення/перейменування

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

## ADR-008: IsFree як computed property на DTO, не окреме поле на entity

**Рішення:** `Course` має тільки поле `Price: decimal`. Семантика "free course" = `Price == 0`. Жодного окремого `IsFree: bool` на entity. У `CourseDetailDto` є computed поле `IsFree => Price == 0m` для зручності фронтенду.

**Чому:**
- Два поля що мають узгоджуватись — гарантований розсинхрон у довгій перспективі (Price = 10, IsFree = true — баг легкий, ціна виправлення висока)
- Price як single source of truth — явний і прозорий
- Фронтенд все одно рендерить "Free" на основі price, окреме поле не дає value

**Альтернативи:**
- Поле `IsFree: bool` на entity — обмежений upside (швидший фільтр за вільними курсами в SQL — можна додати індекс по Price, стане дешево), гарантований downside (розсинхрон)
- Computed column в DB `IsFree AS (CASE WHEN Price = 0 THEN TRUE ELSE FALSE END)` — можливо у майбутньому, якщо фільтр "тільки безкоштовні" стане hot path. Поки що — не треба.

---

## ADR-009: Course як aggregate root для structure mutations

**Рішення:** Усі структурні операції (create/update/delete/reorder sections, create/update/delete/reorder lessons) проходять через публічні методи `Course`. `Section` і `Lesson` мають `internal` setters/mutators — доступні тільки з Domain assembly (тобто тільки з `Course`). Не створюємо `ISectionRepository` / `ILessonRepository` — єдиний repo `ICourseRepository` вже достатній.

Handler pattern для будь-якої structure mutation:
1. Fetch `Course` через `CourseByIdWithStructureSpecification(id, forUpdate: true)` (з tracking, включає `Sections.Lessons`)
2. Owner check через `ICurrentUserService` + `course.InstructorId`
3. Викликати domain метод (`course.AddSection(...)`, `course.RemoveLesson(...)` тощо)
4. `unitOfWork.SaveChangesAsync()`
5. Catch `DomainException` → `ConflictError` (див. ADR-014)

**Чому:**
- Invariants (`Published course must have cover + ≥1 section + ≥1 lesson`) мусять завжди лишатись true для Published курсу (ADR-010). Щоб їх перевірити після mutation — треба бачити in-memory стан усієї структури. Це можливо тільки якщо mutation проходить через aggregate root, який володіє цією структурою
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

## ADR-010: Publish invariants enforced continuously — не тільки на Publish

**Рішення:** Інваріанти публікації (`CoverImageUrl != null`, `≥1 section`, `≥1 lesson across all sections`) мають **завжди** лишатись true поки `Course.Status == Published`. Перевірка триває не тільки при переході Draft → Published (команда Publish), а після **кожної** mutation що може їх порушити. Конкретно:

- `Course.SetCoverImage(null)` на Published → throw
- `Course.RemoveSection(id)` що залишає 0 секцій на Published → throw
- `Course.RemoveSection(id)` що залишає секції без жодного уроку на Published → throw
- `Course.RemoveLesson(id)` що залишає курс без жодного уроку на Published → throw

Archived — повністю read-only (всі structure mutations reject'яться через `EnsureStructureMutable()`). Draft — дозволено все без invariant checks.

**Чому:**
- UX без тертя для Published курсів: інструктор може додавати секції/уроки без Unpublish → Publish циклу
- Invariants залишаються під захистом. Published курс ніколи не може бути в стані "порожній у пошуку"

**Альтернативи розглянуті:**
- **Draft only (strict).** Будь-які structure mutations на Published заборонені, треба Unpublish. Простіше на один інваріант, гірше для UX
- **Additive only.** Додавання OK на Published, видалення/reorder — ні. Половинчасте правило, довелося б explicitly блокувати кожен delete handler — складніше у коді ніж continuous invariant

**Наслідки:**
- `Course.EnsurePublishableInvariants()` — private method, викликається з `SetCoverImage`, `RemoveSection`, `RemoveLesson`, `Publish`
- `SetCoverImage(null)` на Published вперше отримує invariant check (раніше просто присвоював)
- При виконанні mutation що порушить invariant: domain throw `DomainException` (ADR-014), handler catch → `ConflictError` (409). In-memory state entity може бути modified, але `SaveChangesAsync` не викликається → в БД без змін. DbContext scoped per request → при наступному запиті новий DbContext з актуальним станом з БД
- Нові mutating operations на Course / Section / Lesson у майбутньому зобов'язані викликати `EnsurePublishableInvariants()` якщо потенційно можуть порушити один з трьох інваріантів. Документовано як конвенцію

---

## ADR-011: Bulk reorder через окремий endpoint + set-equality validation

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

## ADR-012: Question, QuestionOption, TextAnswerConfig — value objects (owned entities), не окремі таблиці

**Рішення:** `Question`, `QuestionOption`, `TextAnswerConfig` є **value objects** що зберігаються як JSONB всередині `TestLesson`. `StudentAnswer` — record що зберігається як JSONB всередині `TestAttempt`. Окремих таблиць для цих типів немає.

**Чому:**
- Питання не мають незалежного life cycle від TestLesson. Питання без тесту — безглуздо
- Варіанти відповіді не мають незалежного life cycle від Question
- Заміна питань відбувається bulk-операцією (`ReplaceQuestions`) — не patch окремого питання. JSONB це підтримує природно
- Студентські відповіді прив'язані до конкретної спроби і ніколи не реюзаються — JSONB ідеально
- Складна EF schema (Question → QuestionOption + TextAnswerConfig з FK, cascade delete, ordering) vs простий JSON array — значна різниця у складності міграцій і query logic

**Scoring logic в value object:**
`Question.IsAnsweredCorrectly(StudentAnswer)` — повна логіка скорингу живе всередині value object, включаючи Levenshtein distance для fuzzy match текстових відповідей

**Альтернативи:**
- Окремі таблиці `Questions`, `QuestionOptions`, `TextAnswerConfigs` — стандартний реляційний підхід. Відкинуто: join-heavy query для читання тесту, складний cascade delete, bulk replace вимагав би delete-all + insert-all в транзакції
- JSONB для питань, окрема таблиця для відповідей — гібрид, складніший без виграшу

**Наслідки:**
- EF конфігурація: `OwnsMany<Question>` → `OwnsMany<QuestionOption>` + `OwnsOne<TextAnswerConfig>` (JSONB)
- `TestAttempt` owns `IReadOnlyList<StudentAnswer>` через `OwnsMany` (JSONB)
- Scoring: `testLesson.Score(attempt.Answers)` — метод на TestLesson обчислює результат
- При рефакторингу питань (додавання поля) — не потрібна окрема міграція таблиці, тільки JSONB schema зміна (backward compatible через nullable fields)

---

## ADR-013: CourseCommandHandler як base class для скорочення boilerplate structure mutations

**Рішення:** `CourseCommandHandler<TCommand, TResult>` — абстрактний base class в `Application/Common/Commands/`. Автоматично виконує стандартну послідовність для structure mutations: перевірка автентифікації → fetch course з tracking → перевірка власника/адміна → `EnsureStructureMutable()` → делегує до `abstract HandleAsync()`. `CourseSectionCommandHandler<TCommand, TResult>` розширює цей клас додатковою перевіркою існування секції.

**Чому:**
- Кожен з 10+ structure mutation handlers (CreateSection, DeleteLesson, ReorderLessons, etc.) виконує ідентичні кроки 1-4. Без base class — copy-paste в кожному handler
- Помилка в одному handler (пропущена ownership check) = security bug. Base class гарантує що ці перевірки не пропустити
- Template Method pattern: base клас визначає алгоритм, subclass надає тільки бізнес-логіку

**Технічні деталі:**
- Generic constraints: `where TCommand : IRequest<TResult>, ICommandWithCourseId` — дає доступ до `CourseId` без reflection
- `where TResult : ResultBase, new()` — дозволяє конструювати failed result типу TResult через `new()` коли auth/fetch fails
- Handler реєструється в DI через `IRequestHandler<TCommand, TResult>` — MediatR не знає про base class

**Альтернативи:**
- Inline в кожному handler — повторення, ризик security bug. Відкинуто
- Authorization policy через ASP.NET Core resource-based authorization — складніший механізм, потребує fetch resource в authorization handler. Відкинуто (DECISIONS_AUTH.md ADR-013)
- Extension methods на `ICurrentUserService` + shared static helper — частково вирішує, але не виключає дублювання самої послідовності кроків

**Наслідки:**
- Команди що потребують ownership check і structure mutability реалізують `ICommandWithCourseId`
- Commands що додатково потребують section context реалізують `ICommandWithCourseAndSectionId`
- Handler успадковує `CourseCommandHandler` замість прямого `IRequestHandler` — розмір handler файлу скорочується на ~20-30 рядків boilerplate
- `InternalsVisibleTo` для тестів: base class може знадобитись для unit testing через `CourseCommandHandler` напряму (з mock ICourseRepository)

---

## ADR-014: Custom DomainException для захисту бізнес-інваріантів

**Рішення:** Створено кастомний `DomainException` у `Learnix.Domain.Common.Exceptions`. Усі перевірки інваріантів у сутностях (наприклад, `EnsurePublishableInvariants` у `Course`, ADR-009/ADR-010) кидають саме цей виняток замість стандартного `InvalidOperationException`.

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

## ADR-015: DomainEventsInterceptor — видалення try-catch навколо publisher.Publish()

**Рішення:** `try-catch` навколо `await publisher.Publish(notification, ct)` у `DomainEventsInterceptor` видалено. Будь-який виняток з domain event handler тепер вільно спливає, що перериває `SavingChangesAsync` і відкочує транзакцію.

**Чому:**
- `DomainEventsInterceptor` викликається **до** фактичного запису в БД (`SavingChangesAsync` → `base.SavingChangesAsync()`). Відповідальність Outbox-хендлера — записати `OutboxMessage` у **той самий** DbContext в рамках тієї ж транзакції.
- Якщо Outbox-хендлер падає (наприклад, помилка серіалізації), `try-catch` приховував цей збій і продовжував зберігати зміни в БД. Результат: дані в PostgreSQL записані, але `OutboxMessage` не існує → side-effect (email, нотифікація) ніколи не відбудеться. Silent data inconsistency.
- Без `try-catch`: виняток → `SavingChangesAsync` кидає → EF Core відкочує транзакцію → клієнт отримує 500. Явна помилка краща за приховану втрату повідомлення.

**Альтернативи:**
- Залишити `try-catch`, але додати dead-letter механізм для missed events — надмірна складність. Outbox вже є гарантованою доставкою; якщо він сам падає — це баг серіалізації, а не transient збій.
- Перемістити dispatch подій **після** `base.SavingChangesAsync()` (post-save) — не підходить: запис в Outbox мусить бути атомарним разом із змінами entity в тій же транзакції.

**Наслідки:**
- `ILogger<DomainEventsInterceptor>` більше не потрібен і видалений з конструктора.
- Event handlers, що виконують некритичні side-effects (наприклад, кеш-інвалідація), не повинні кидати виключення — треба обробляти помилки всередині самого хендлера і логувати.

---

## ADR-016: Гібридна стратегія завантаження агрегату Course — "AR для інваріантів" vs "AR для авторизації"

**Рішення:** Операції над `Course` поділені на дві категорії із різними вимогами до завантаження агрегату:

### Категорія A — Повне завантаження (`includeLessons: true`)
Операції, що можуть порушити lifecycle-інваріанти `EnsurePublishableInvariants()`. Агрегат завантажується з усіма секціями і уроками.

| Операція | Причина |
|---|---|
| `Publish` | Перевіряє `≥1 section`, `≥1 visible lesson`, `CoverImageUrl != null` |
| `DeleteLesson` | Може залишити курс без видимих уроків |
| `ToggleLessonVisibility` | `isHidden=true` може залишити курс без видимих уроків |
| `RemoveSection` | Може залишити курс без секцій або без уроків |

### Категорія Б — Точкове завантаження (hybrid, AR тільки для авторизації)
Операції над вмістом уроку, що не впливають на lifecycle-стан курсу. Агрегат завантажується лише для перевірки власника та `EnsureStructureMutable()`. Мутація уроку відбувається через окремий `ILessonRepository`.

| Операція | Чому hybrid безпечний |
|---|---|
| `UpdateVideoLesson` | Зміна title/video/description не торкається `CourseStatus` |
| `UpdatePostLesson` | Зміна markdown-контенту не торкається `CourseStatus` |
| `UpdateTestLesson` | Зміна питань не торкається `CourseStatus` |

**Чому:**
- Canonical DDD вимагає усі мутації через AR. Але завантажувати 10 секцій × 50 уроків заради зміни `DurationSeconds` у відео — waste. Операції категорії Б не мають жодних аргументів за повне завантаження.
- Hybrid підхід є свідомим компромісом: AR використовується там де він дає цінність (enforcement інваріантів), а не скрізь як догма.
- Безпека операцій категорії Б підтверджується кодом: `CourseCommandHandler` завжди перевіряє `IsOwnerOrAdmin` і `EnsureStructureMutable()` перед делегацією до `HandleAsync`. Ownership і mutable-check не обходяться.

**Альтернативи:**
- Завжди завантажувати повний агрегат — безпечно, але додає ~500 зайвих SQL рядків на кожен UPDATE вмісту уроку. Відхилено як передчасна коректність без реального захисту.
- Денормалізувати `CourseId` у `Lesson` для прямого доступу без JOIN — не вирішує проблему: `InstructorId` все одно живе тільки в `Course`. Потрібен JOIN у будь-якому разі. Відхилено.

**Наслідки:**
- `CourseCommandHandler<TCommand, TResult>` має параметр `includeLessons` (default `false`). Handlers категорії А передають `includeLessons: true`.
- Новий handler що оновлює вміст уроку → категорія Б (hybrid). Новий handler що може вплинути на видимість або видалити уроки/секції → категорія А (повне завантаження).
- Це задокументована конвенція, а не автоматичний enforcement. При review нових handlers — перевіряти правильність категорії.

---

## ADR-017: Виправлення дірки в enforcement інваріантів — DeleteLesson і ToggleLessonVisibility

**Рішення:** `DeleteLessonCommandHandler` і `ToggleLessonVisibilityCommandHandler` змінено: тепер передають `includeLessons: true` до базового `CourseCommandHandler`. Це гарантує, що при перевірці `EnsurePublishableInvariants()` агрегат бачить реальний стан уроків курсу.

**Контекст (знайдена дірка):**
- Обидва handlers успадковували `CourseCommandHandler` з `includeLessons: false` (default).
- `EnsurePublishableInvariants()` перевіряє `_sections.Any(s => s.Lessons.Any(l => !l.IsHidden))`.
- Якщо `Lessons` не завантажені → колекція порожня → `_sections.Any(...)` повертає `false` → метод **завжди** кинув би `DomainException("Published course must have at least one visible lesson.")` навіть для курсу з 50 видимими уроками.
- На практиці: `EnsurePublishableInvariants()` взагалі не викликається ні в `DeleteLessonCommandHandler`, ні в `ToggleLessonVisibilityCommandHandler` — мутації відбуваються поза агрегатом через `lessonRepository` напряму. Тому дірка в поведінці не проявлялась. Але вона є архітектурною міною: якщо хтось додасть виклик `EnsurePublishableInvariants()` чи перенесе мутацію в AR — зламає Published курси.

**Чому виправлено саме так (includeLessons: true):**
- Найменше втручання: один параметр в конструкторі base class.
- Завантаження уроків курсу в цих операціях — правильна вимога за семантикою (ми перевіряємо глобальний інваріант "≥1 visible lesson across all sections").

**Альтернативи:**
- Додати `Course.RemoveLesson()` метод що сам виконує `DeleteAsync` через domain service — ускладнює доменну модель залежністю від persistence. Відхилено: domain не знає про репозиторії.
- Перевіряти інваріант окремим SQL `EXISTS` запитом у хендлері — розмиває відповідальність. Інваріанти живуть в доменній моделі, а не в хендлерах.

**Наслідки:**
- `DeleteLesson`: завантажує всі секції з усіма уроками. Для великого курсу (~500 уроків) — трохи важче, але операція видалення уроку рідкісна і не є hot path.
- `ToggleLessonVisibility`: аналогічно.
- `EnsurePublishableInvariants()` відтепер гарантовано бачить коректний стан при цих двох операціях.
