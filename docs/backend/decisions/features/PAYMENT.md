# Learnix — ADR: Payment System

> **Endpoints:** see [`docs/backend/ENDPOINTS.md`](../../ENDPOINTS.md) — one generated table for
> the whole API, verified against the controllers in CI. An ADR records a decision; it is not the
> place to keep a copy of the API surface.

**Not implemented (known gaps):**
- Refunds / failed payment simulation
- A real payment provider (Stripe/LiqPay) — the decision is recorded in ADR-BACK-PAY-006

---

## ADR-BACK-PAY-001: Separate `Payment` entity instead of attributes on `Enrollment`

**Context:** joining a course and paying for it are two different facts, and cramming payment fields onto
`Enrollment` pollutes an aggregate that also has to support free courses with no payment at all.

**Decision:** `Payment` is an independent entity (`BaseEntity`) with fields `UserId`, `CourseId`, `EnrollmentId`, `Amount`, `Currency`, `Status`, `PaymentProvider`, `CompletedAt`. It has a FK to `Enrollment` (1:1).

**Why:**
- `Enrollment` is the fact of a student participating in a course. `Payment` is the fact of a financial transaction. These are different domain concepts.
- `Enrollment` already has a `PaymentStatus` enum for quick "is paid" checks without a JOIN. `Payment` stores the full audit trail of the transaction.
- When migrating to a real provider (Stripe, LiqPay), `Payment` will get `ExternalTransactionId`, `ProviderResponse` — fields that do not concern the enrollment.
- Admin panel and instructor dashboard need payment aggregations (total fees collected, monthly dynamics) — it is more convenient to query a separate table.

**Alternatives:**
- Store everything on `Enrollment` (add `ProviderTransactionId`, `PaidAt`) — rejected: payment details pollute the enrollment aggregate and complicate future reports.
- Separate `PaymentDetail` value object in `Enrollment` as JSONB — rejected: requires complex JSONB queries for aggregations.

**Consequences:**
- `Payments` — a separate table with a unique index on `EnrollmentId`.
- `Enrollment.PaymentStatus` remains as a denormalized flag for quick checks without a JOIN.

---

## ADR-BACK-PAY-002: `POST /api/payments` as a separate endpoint for payment (instead of extending `/api/enrollments`)

**Context:** "enroll" and "pay" are different actions with different consequences, and a paid course must
never become accessible through the same endpoint that grants free ones.

**Decision:** For paid courses, the student calls `POST /api/payments { courseId }`. Free courses use `POST /api/enrollments { courseId }` (unchanged). Two separate endpoints, each with its own purpose.

**Why:**
- Semantics differ: "enroll" = join a course, "pay" = make a transaction and get access. Mixing them in one endpoint violates the Single Responsibility Principle.
- `POST /api/payments` handles payment business logic (checks `Price > 0`, creates a `Payment` record) and activates enrollment as a side-effect. `POST /api/enrollments` rejects a paid course outright (`ConflictError`) rather than silently enrolling it for free — a student who skips checkout gets an error, not access.
- Frontend checkout flow: a separate endpoint allows showing a "payment confirmation" page between clicking "Buy" and receiving a successful enrollment — a logical UX transition.
- When replacing with real Stripe: only the `InitiateMockPaymentCommandHandler` changes. Controller, routing, frontend remain unchanged.

**Alternatives:**
- One `POST /api/enrollments` for everything — rejected: hides the difference between free and paid flows, complicates adding real payment in the future.
- `POST /api/courses/{id}/purchase` — semantically correct, but violates RESTful resource-centric routing. Rejected.

**Consequences:**
- Frontend for **free** courses: `POST /api/enrollments`.
- Frontend for **paid** courses: `POST /api/payments`.
- Existing `/api/enrollments` is not changed — backward compatible.

---

## ADR-BACK-PAY-003: Atomic creation of `Payment` + `Enrollment` in one `SaveChangesAsync`

**Context:** a payment and the enrollment it grants must never exist one without the other — a partial
write would leave a student who paid without access, or access with no paid record behind it.

**Decision:** `InitiateMockPaymentCommandHandler` adds both `Enrollment` and `Payment` to their respective repositories before calling `unitOfWork.SaveChangesAsync(cancellationToken)`. Both records are saved in one transaction.

**Why:**
- Guarantees consistency: a situation where "Payment exists, Enrollment doesn't" or vice versa is impossible.
- If `SaveChangesAsync` throws an exception — both are rolled back. The student will receive an error and can retry the action.
- EF Core wraps `SaveChangesAsync` in a transaction by default — nothing extra is needed.

**Alternatives:**
- Two separate `SaveChangesAsync` — rejected: the window between the first and second save creates an incorrect partial write state.
- Domain event + `EnrollmentCreatedDomainEvent` handler for Payment — excessive for a synchronous mock flow. Rejected.

---

## ADR-BACK-PAY-004: `PaymentProvider` as a string instead of an enum

**Context:** the platform expects to add real payment providers later, and an enum value for each one
would mean a migration every time.

**Decision:** `Payment.PaymentProvider` — `string` (max 50), not enum. Default value for the mock is `"Mock"`.

**Why:**
- Future providers (`"Stripe"`, `"LiqPay"`, `"PayPal"`) can be added without DB schema changes and without migrations for a new enum value.
- `"Mock"` is a self-documenting string. In Swagger and API responses it is immediately clear that this is a mock transaction.
- Enum for payment provider is over-engineering at the business logic level, where comparing by Provider is rarely needed in code.

**Alternatives:**
- `PaymentProvider` enum — rejected: every new provider requires an EF migration.
- No `PaymentProvider` field at all — rejected: impossible to distinguish a mock from a real payment in the audit log.

---

## ADR-BACK-PAY-005: Absence of instructor-facing earnings endpoint (Phase 4)

**Context:** an instructor needs to see how much their courses have earned, without a dedicated reporting
pipeline built for a dataset that, per instructor, tops out at a few dozen rows.

**Decision:** `GET /api/instructor/earnings` returns summarized financial statistics for the instructor: `TotalEarnings`, `TotalPayments`, and a list of `Courses` grouped by `CourseId` (PaymentsCount, TotalAmount, LastPaymentAt). Pagination is absent — all instructor courses are returned in a single response.

**Why no pagination:**
- Grouping by course happens in-memory in the handler after loading all instructor payments. Pagination over grouped data would require a more complex DB-level GROUP BY or two passes.
- The number of courses for one instructor ranges from units to a few dozens. In-memory grouping is acceptable.
- Analytics/stats endpoints traditionally return a full snapshot, not pages.

**Access:** `Authorize(Roles = "Instructor,Admin")` — an admin can also check earning stats (filter by `currentUser.UserId` in the handler).

**Specification:** `InstructorPaymentsSpecification(instructorId)` — filters by `p.Course.InstructorId == instructorId` and `p.Status == Completed`.

**Consequences:**
- Earnings endpoint: `GET /api/instructor/earnings`.

---

## ADR-BACK-PAY-006: Mock payment instead of real Stripe

**Context:** the platform needs a checkout flow to demonstrate, and a real payment provider brings
business verification, fees, webhooks and PCI scope that a portfolio project has no use for.

**Decision:** The payment system is implemented as a mock: the "Pay" button immediately writes a `Payment` with status `Completed` and `PaymentProvider = "Mock"` and activates enrollment without any external service. `Stripe__SecretKey` was removed from `.env.example`. The Stripe SDK is not installed.

**Why:**
- This is a pet project. Real Stripe requires business verification, adds a 2.9% + $0.30 fee, and significant complexity: webhooks, declined cards, refunds, PCI compliance.
- For a portfolio, demonstrating the **flow and architecture** (PurchaseCourse command, domain event, enrollment) is important, not actual money collection.
- The mock retains the full domain model: `Payment` entity with `Amount`, `Status`, `PaymentProvider` — the field `PaymentProvider = "Mock"` clearly signals that this is not production.
- Connecting a real provider in the future is a change in one place (handler + DI), without rebuilding the architecture.

**Alternatives:**
- Stripe test mode — still requires an account, webhook endpoint, Stripe SDK. Test keys don't charge cards, but add ~200 lines of code with no practical value for a pet project.
- Stripe fully — overkill for a demo project, legal requirements for production.

**Consequences:**
- `Stripe__SecretKey` removed from `Learnix.API/.env.example` and `learnix-client/.env.example`.
- `VITE_STRIPE_PUBLISHABLE_KEY` removed from frontend `.env.example`.
- `Payment.PaymentProvider` stores `"Mock"` — in the future `"Stripe"`, `"LiqPay"`, etc. can be added.
- When transitioning to a real provider: replace logic in `PurchaseCourseCommandHandler`, add a webhook endpoint, update `.env.example`.
