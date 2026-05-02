# Learnix — TODO

> Порядок = пріоритет імплементації (зверху вниз).
> Статуси: `not started` · `in progress` · `done`
> Оновлюється в кінці кожного чату.

---

## Backend

### Phase 1 — Foundation (без цього нічого не працює)

| # | Task | Status | Notes |
|---|---|---|---|
| B-01 | Project scaffolding: створити solution + 4 проєкти (Domain, Application, Infrastructure, API) з правильними залежностями | done | |
| B-02 | Domain layer: BaseEntity, IDomainEvent, Enums | done | |
| B-03 | Application layer: Constants, Result<T> (FluentResults), Specification<T> base, pipeline behaviors (Validation, Logging) | done | |
| B-04 | Infrastructure: ApplicationDbContext, UnitOfWork, SpecificationEvaluator | done | |
| B-05 | Infrastructure: перша EF міграція (User, RefreshToken) | done | |
| B-06 | API: Program.cs (DI, middleware pipeline), ExceptionHandlingMiddleware, SecurityHeadersMiddleware | done | |
| B-07 | Docker Compose: PostgreSQL + MongoDB + Redis для локальної розробки | done | |

### Phase 2 — Auth (gate для всього іншого)

| # | Task | Status | Notes |
|---|---|---|---|
| B-08 | ASP.NET Core Identity setup (User entity, role seeding) | done | |
| B-09 | Register command (+ validator + handler + email verification event) | done | |
| B-10 | Login command (JWT generation + refresh token creation) | done | |
| B-10.5 | документувати Authentication pipeline в ARCHITECTURE.md | done | |
| B-11 | Refresh token endpoint (rotation + revocation logic) | done | |
| B-11.5 | Refresh token cleanup background service (видаляє токени старші expiry + 7 днів) | done | |
| B-12 | Email confirmation flow (confirm endpoint + resend) | done (mock confirmation with console) | |
| B-13 | Password reset flow (forgot + reset endpoints) | done | |
| B-14 | Google OAuth integration | done | |
| B-15 | Rate limiting middleware (auth endpoints) | done | |
| B-16 | Auth controllers (thin, delegate to MediatR) | done | |

### Phase 3 — Core Domain (Courses + Lessons)

| # | Task | Status | Notes |
|---|---|---|---|
| B-17 | Domain entities: Category, Course, Section, Lesson (TPH), Question, QuestionOption, TextAnswerConfig | done (part: Category, Course, Section, Lesson TPH — Question-related deferred to Test-subsystem chat) | |
| B-17.1 | Domain: Question, QuestionOption, TextAnswerConfig, TestAttempt, TestAttemptAnswer entities | not started | Feeds B-29 |
| B-18 | EF Configurations + міграція для course-related entities | done | |
| B-19 | Category CRUD (seed initial categories) | done | |
| B-20 | Course CRUD (create/edit/delete/publish/archive) — Instructor only | done | |
| B-21 | Course queries: list (with filters, pagination, sorting), get by id | done | |
| B-22 | Section CRUD + reordering | done | |
| B-23 | Lesson CRUD (Video/Post) + reordering | done | |
| B-24 | File upload service (Azure Blob): video + cover image | done | |
| B-25 | Instructor application flow (submit, admin approve/reject) | done | |

### Phase 4 — Student Features

| # | Task | Status | Notes |
|---|---|---|---|
| B-26 | Enrollment (free courses — instant) | done | Mock payment auto-confirms paid courses |
| B-27 | Lesson progress (mark complete, track per user) | done | |
| B-28 | Lesson likes | CANCELED | |
| B-29 | Test system: submit attempt, score calculation, attempt limits + cooldown | done | Order-based answer matching (not GUID) — EF8 JSON collections require ordinal keys |
| B-30 | Course completion detection + Certificate generation (PDF) | done | Async PDF gen via BackgroundService (PeriodicTimer 30s); QuestPDF; Azure Blob upload; SAS URL for download |
| B-31 | Student profile (edit name, avatar, bio, category preferences) | not started | |

### Phase 5 — Payments

| # | Task | Status | Notes |
|---|---|---|---|
| B-32 | Stripe integration (test mode): create checkout session | not started | |
| B-33 | Stripe webhook handler (payment completed → activate enrollment) | not started | |
| B-34 | Payment history queries | not started | |
| B-34.5 | Outbox pattern: OutboxMessage entity + EF config + background publisher worker (замінити пряму публікацію domain events в ApplicationDbContext) | not started | Передумова для надійної асинхронної обробки в Phase 6 |
| B-34.6 | Розділити `UserRegisteredDomainEvent` на два events: `UserRegistered` (raised в Register flow) і `EmailConfirmationRequested` (raised в Resend flow). Поточна реалізація використовує `RaiseUserRegistered` в обох місцях — семантичний запах. Рефакторити одночасно з міграцією email на integration events через MassTransit (B-36). | not started | Залежить від B-35 |

### Phase 6 — Async Processing (MassTransit)

| # | Task | Status | Notes |
|---|---|---|---|
| B-35 | MassTransit + Azure Service Bus setup | not started | |
| B-36 | Email consumers (verification, enrollment, approval) | not started | |
| B-37 | Certificate generation consumer | not started | |
| B-38 | Achievement checking consumer | not started | |

### Phase 7 — Achievements & Notifications

| # | Task | Status | Notes |
|---|---|---|---|
| B-39 | Achievement entities + seed data | not started | |
| B-40 | Achievement checking logic (lesson/course/test/social conditions) | not started | |
| B-41 | Notification system (create, list, mark read) | not started | |
| B-42 | Domain events → notifications (achievement earned, enrollment confirmed, certificate ready) | not started | |

### Phase 8 — Chat & AI

| # | Task | Status | Notes |
|---|---|---|---|
| B-43 | Student ↔ Instructor messaging (per course, polling) | NOT IN SCOPE | |
| B-44 | MongoDB setup: MongoDbContext, repositories | Done | |
| B-45 | AI chat: Multi-provider integration (Anthropic + Gemini, streaming SSE, tool use) | Done | |
| B-46 | Chat session persistence (MongoDB) | not started | |
| B-46.5 | Background job: cleanup closed AI chat sessions older than 30 days | Done | |

### Phase 9 — Reviews & Recommendations

| # | Task | Status | Notes |
|---|---|---|---|
| B-47 | Course reviews (MongoDB): create, list, average rating | not started | |
| B-48 | Denormalized rating on Course entity | not started | |
| B-49 | Recommendations engine (based on enrollments, likes, preferences) | not started | Low priority |

### Phase 10 — Admin & Caching

| # | Task | Status | Notes |
|---|---|---|---|
| B-50 | Admin: user management (list, search, ban/unban, role change) | Done | |
| B-51 | Admin: course management (view all, unpublish, delete) | Done | |
| B-52 | Redis caching: CachingBehavior + ICacheable queries | not started | |
| B-53 | Cache invalidation in relevant commands | not started | |

---

## Frontend

### Phase 1 — Setup & Auth UI

| # | Task | Status | Notes |
|---|---|---|---|
| F-01 | Vite + React 19 + TypeScript scaffolding | not started | |
| F-02 | TailwindCSS + shadcn/ui setup | not started | |
| F-03 | Routing (React Router v6): public/private route guards | not started | |
| F-04 | Axios instance (interceptors: attach JWT, handle 401 → refresh) | not started | |
| F-05 | Auth store (Zustand): token management, user state | not started | |
| F-06 | Login / Register / Forgot Password pages | not started | |
| F-07 | Google OAuth button | not started | |
| F-08 | Email verification page | not started | |

### Phase 2 — Course Browsing (Student)

| # | Task | Status | Notes |
|---|---|---|---|
| F-09 | Course catalog page (grid, filters, search, pagination) | not started | |
| F-10 | Course detail page (description, sections, lessons list, enroll button) | not started | |
| F-11 | Enrollment flow (free + paid) | not started | |

### Phase 3 — Learning Experience

| # | Task | Status | Notes |
|---|---|---|---|
| F-12 | Lesson viewer: video player | not started | |
| F-13 | Lesson viewer: post (markdown rendering) | not started | |
| F-14 | Lesson viewer: test (quiz UI, submit, results) | not started | |
| F-15 | Lesson progress tracking (mark complete, sidebar state) | not started | |
| F-16 | Lesson likes | not started | |

### Phase 4 — Instructor Dashboard

| # | Task | Status | Notes |
|---|---|---|---|
| F-17 | Course creation/edit form (React Hook Form + Zod) | not started | |
| F-18 | Section & lesson management (drag-and-drop reorder) | not started | |
| F-19 | Lesson editors: video upload, post markdown editor, test builder | not started | |
| F-20 | Instructor dashboard: course list, enrollment stats | not started | |
| F-21 | Instructor application form | not started | |

### Phase 5 — Profile & Social

| # | Task | Status | Notes |
|---|---|---|---|
| F-22 | Student profile page (edit, avatar upload, preferences) | not started | |
| F-23 | Achievements display | not started | |
| F-24 | Certificates list + download | not started | |
| F-25 | Course reviews: leave review, view reviews | not started | |

### Phase 6 — Chat & Notifications

| # | Task | Status | Notes |
|---|---|---|---|
| F-26 | Student ↔ Instructor chat UI | not started | |
| F-27 | AI chat widget (streaming response) | not started | |
| F-28 | Notification bell + dropdown | not started | |

### Phase 7 — Admin Panel

| # | Task | Status | Notes |
|---|---|---|---|
| F-29 | Admin: user management table | not started | |
| F-30 | Admin: course moderation table | not started | |
| F-31 | Admin: instructor applications review | not started | |
| F-32 | Admin: payment history | not started | |

### Phase 8 — Polish

| # | Task | Status | Notes |
|---|---|---|---|
| F-33 | Homepage: hero, popular courses, recommendations | not started | |
| F-34 | Responsive design pass (mobile) | not started | |
| F-35 | Loading states, error boundaries, empty states | not started | |
| F-36 | Dark mode (optional) | not started | Low priority |

---

## Deploy & Infrastructure

### Phase 1 — Containerization

| # | Task | Status | Notes |
|---|---|---|---|
| D-01 | Dockerfile for API (.NET 8, multi-stage build) | not started | |
| D-02 | Dockerfile for frontend (Vite build → nginx) | not started | |
| D-03 | Docker Compose: full stack (API + client + postgres + mongo + redis) | not started | |

### Phase 2 — CI/CD

| # | Task | Status | Notes |
|---|---|---|---|
| D-04 | GitHub Actions: build + test on PR | not started | |
| D-05 | GitHub Actions: build Docker images + push to ACR | not started | |

### Phase 3 — Azure Deployment

| # | Task | Status | Notes |
|---|---|---|---|
| D-06 | Azure Container Apps (або App Service) для API | not started | |
| D-06.5 | Configure ForwardedHeaders for rate limiting partition-by-real-IP behind reverse proxy | not started | Prerequisite for rate limiter to work correctly in Azure |
| D-07 | Azure Static Web Apps (або Container App) для frontend | not started | |
| D-08 | Azure Database for PostgreSQL (Flexible Server) | not started | |
| D-09 | Azure Cosmos DB for MongoDB API | not started | |
| D-10 | Azure Cache for Redis | not started | |
| D-11 | Azure Blob Storage account + containers | not started | |
| D-12 | Azure Service Bus namespace + queues | not started | |
| D-13 | Azure Key Vault для секретів | not started | |
| D-14 | Custom domain + SSL | not started | |
| D-15 | Application Insights (Serilog sink) | not started | |

---

## Summary

| Section | Total | Done | Remaining |
|---|---|---|---|
| Backend | 53 | 21 | 32 |
| Frontend | 36 | 0 | 36 |
| Deploy | 15 | 0 | 15 |
| **Total** | **104** | **21** | **83** |
