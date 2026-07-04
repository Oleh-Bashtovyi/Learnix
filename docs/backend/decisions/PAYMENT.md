# Learnix — ADR: Платіжна система

## Підсумок: що реалізовано

| Endpoint | Хто | Що робить |
|---|---|---|
| `POST /api/payments` | Student (EmailConfirmed) | Купує платний курс: створює `Payment` + `Enrollment` |
| `GET /api/payments/mine` | Student | Власна історія платежів (paginated) |
| `GET /api/instructor/earnings` | Instructor, Admin | Earnings по курсах інструктора (grouped summary) |
| `GET /api/admin/payments` | Admin | Усі платежі платформи (paginated, search by email/course) |

**Що НЕ реалізовано (known gaps):**
- Refunds / failed payment simulation
- Real payment provider (Stripe/LiqPay) — задокументоване рішення в ADR-PAY-006

---

## ADR-PAY-001: Окрема сутність `Payment` замість атрибутів на `Enrollment`

**Рішення:** `Payment` — самостійна сутність (`BaseEntity`) з полями `UserId`, `CourseId`, `EnrollmentId`, `Amount`, `Currency`, `Status`, `PaymentProvider`, `CompletedAt`. Має FK до `Enrollment` (1:1).

**Чому:**
- `Enrollment` — факт участі студента в курсі. `Payment` — факт грошової транзакції. Це різні доменні концепції.
- `Enrollment` вже має `PaymentStatus` enum для швидкої перевірки "чи оплачено" без JOIN. `Payment` зберігає повну аудит-трасу транзакції.
- При переході на реальний провайдер (Stripe, LiqPay) `Payment` отримає `ExternalTransactionId`, `ProviderResponse` — поля що не стосуються enrollment.
- Адмін-панель і dashboard інструктора потребують агрегацій по платежах (сума зборів, динаміка по місяцях) — зручніше запитувати окрему таблицю.

**Альтернативи:**
- Зберігати все на `Enrollment` (додати `ProviderTransactionId`, `PaidAt`) — відкинуто: payment details забруднюють enrollment aggregate, ускладнюють майбутні звіти.
- Окремий `PaymentDetail` value object в `Enrollment` як JSONB — відкинуто: потребує складного JSONB-запиту для агрегацій.

**Наслідки:**
- `Payments` — окрема таблиця з unique index на `EnrollmentId`.
- `Enrollment.PaymentStatus` залишається як денормалізований флаг для швидкої перевірки без JOIN.

---

## ADR-PAY-002: `POST /api/payments` як окремий endpoint для оплати (замість розширення `/api/enrollments`)

**Рішення:** Для оплатних курсів студент викликає `POST /api/payments { courseId }`. Безкоштовні курси — `POST /api/enrollments { courseId }` (без змін). Два окремих endpoint, кожен зі своїм призначенням.

**Чому:**
- Семантика відрізняється: "enroll" = приєднатись до курсу, "pay" = здійснити транзакцію і отримати доступ. Змішувати в одному endpoint порушує принцип єдиної відповідальності.
- `POST /api/payments` обробляє бізнес-логіку оплати (перевіряє `Price > 0`, створює `Payment` record) і як side-effect активує enrollment. `POST /api/enrollments` перевіряє `Price == 0` — у майбутньому явно відхилятиме paid courses.
- Frontend checkout flow: окремий endpoint дозволяє показати "payment confirmation" сторінку між натисканням "Buy" і отриманням успішного enrollment — логічний UX-перехід.
- При заміні на реальний Stripe: змінюється тільки handler `InitiateMockPaymentCommandHandler`. Контролер, routing, frontend — без змін.

**Альтернативи:**
- Один `POST /api/enrollments` для всього — відкинуто: приховує різницю між free і paid flow, ускладнює додавання real payment у майбутньому.
- `POST /api/courses/{id}/purchase` — семантично правильне, але порушує RESTful resource-centric routing. Відкинуто.

**Наслідки:**
- Frontend для **безкоштовних** курсів: `POST /api/enrollments`.
- Frontend для **платних** курсів: `POST /api/payments`.
- Existing `/api/enrollments` не змінюється — backward compatible.

---

## ADR-PAY-003: Атомарне створення `Payment` + `Enrollment` в одному `SaveChangesAsync`

**Рішення:** `InitiateMockPaymentCommandHandler` додає і `Enrollment`, і `Payment` до відповідних репозиторіїв до виклику `unitOfWork.SaveChangesAsync(cancellationToken)`. Обидва записи зберігаються в одній транзакції.

**Чому:**
- Гарантує консистентність: неможлива ситуація "Payment є, Enrollment немає" або навпаки.
- Якщо `SaveChangesAsync` кидає виключення — rollback обох. Студент отримає помилку і зможе повторити дію.
- EF Core за замовчуванням обгортає `SaveChangesAsync` у транзакцію — нічого додаткового не потрібно.

**Альтернативи:**
- Два окремих `SaveChangesAsync` — відкинуто: вікно між першим і другим save = стан некоректної часткової записи.
- Domain event + `EnrollmentCreatedDomainEvent` handler для Payment — надмірно для синхронного мок-flow. Відкинуто.

---

## ADR-PAY-004: `PaymentProvider` як рядок замість enum

**Рішення:** `Payment.PaymentProvider` — `string` (max 50), не enum. Default значення для мока — `"Mock"`.

**Чому:**
- Майбутні провайдери (`"Stripe"`, `"LiqPay"`, `"PayPal"`) з'являються без зміни схеми БД і без міграції для нового enum значення.
- `"Mock"` — самодокументуючий рядок. В Swagger і відповіді API одразу зрозуміло що це мок-транзакція.
- Enum для payment provider — over-engineering на рівні бізнес-логіки, де порівняння по Provider рідко потрібне в коді.

**Альтернативи:**
- `PaymentProvider` enum — відкинуто: кожен новий провайдер потребує EF міграції.
- Зовсім без `PaymentProvider` поля — відкинуто: не можна відрізнити мок від реального платежу в аудит-лозі.

---

## ADR-PAY-005: Відсутність instructor-facing earnings endpoint (Phase 4)

**Рішення:** `GET /api/instructor/earnings` повертає зведену фінансову статистику інструктора: `TotalEarnings`, `TotalPayments`, та список `Courses` згрупованих по `CourseId` (PaymentsCount, TotalAmount, LastPaymentAt). Пагінація відсутня — повертаються всі курси інструктора в одному відповіді.

**Чому без пагінації:**
- Групування по курсу відбувається in-memory в handler після завантаження всіх платежів інструктора. Пагінація по згрупованих даних потребувала б складнішого DB-level GROUP BY або двох проходів.
- Кількість курсів одного інструктора — від одиниць до кількох десятків. In-memory grouping прийнятний.
- Analytics/stats endpoints традиційно повертають повний snapshot, не сторінки.

**Доступ:** `Authorize(Roles = "Instructor,Admin")` — адмін також може перевіряти earning stats (фільтр по `currentUser.UserId` в handler).

**Специфікація:** `InstructorPaymentsSpecification(instructorId)` — фільтрує по `p.Course.InstructorId == instructorId` і `p.Status == Completed`.

**Наслідки:**
- Earnings endpoint: `GET /api/instructor/earnings`.

---

## ADR-PAY-006: Мок-оплата замість реального Stripe

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
