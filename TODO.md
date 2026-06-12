# Learnix — TODO

> Order = implementation priority (top to bottom).
> Statuses: `not started` · `in progress` · `done`
> Updated at the end of each chat session.

---

## Backend

### Phase 1 — Foundation (without this nothing works)

| # | Task | Status | Notes |
|---|---|---|---|
| B-01 | Project scaffolding: create solution + 4 projects (Domain, Application, Infrastructure, API) with correct dependencies | done | |
| B-02 | Domain layer: BaseEntity, IDomainEvent, Enums | done | |
| B-03 | Application layer: Constants, Result<T> (FluentResults), Specification<T> base, pipeline behaviors (Validation, Logging) | done | |
| B-04 | Infrastructure: ApplicationDbContext, UnitOfWork, SpecificationEvaluator | done | |
| B-05 | Infrastructure: first EF migration (User, RefreshToken) | done | |
| B-06 | API: Program.cs (DI, middleware pipeline), ExceptionHandlingMiddleware, SecurityHeadersMiddleware | done | |
| B-07 | Docker Compose: PostgreSQL + MongoDB + Redis for local development | done | |

### Phase 2 — Auth (gate for everything else)

| # | Task | Status | Notes |
|---|---|---|---|
| B-08 | ASP.NET Core Identity setup (User entity, role seeding) | done | |
| B-09 | Register command (+ validator + handler + email verification event) | done | |
| B-10 | Login command (JWT generation + refresh token creation) | done | |
| B-10.5 | Document Authentication pipeline in ARCHITECTURE.md | done | |
| B-11 | Refresh token endpoint (rotation + revocation logic) | done | |
| B-11.5 | Refresh token cleanup background service (deletes tokens older than expiry + 7 days) | done | |
| B-12 | Email confirmation flow (confirm endpoint + resend) | done | Real SMTP via MailKit + RazorLight .cshtml templates; ConsoleEmailSender removed (ADR-INFRA-016); email localization (EN/UK) via IStringLocalizer + .resx (ADR-INFRA-017) |
| B-13 | Password reset flow (forgot + reset endpoints) | done | |
| B-14 | Google OAuth integration | done | |
| B-15 | Rate limiting middleware (auth endpoints) | done | |
| B-16 | Auth controllers (thin, delegate to MediatR) | done | |

### Phase 3 — Core Domain (Courses + Lessons)

| # | Task | Status | Notes |
|---|---|---|---|
| B-17 | Domain entities: Category, Course, Section, Lesson (TPH), Question, QuestionOption, TextAnswerConfig | done (part: Category, Course, Section, Lesson TPH — Question-related deferred to Test-subsystem chat) | |
| B-17.1 | Domain: Question, QuestionOption, TextAnswerConfig, TestAttempt, TestAttemptAnswer entities | done | Feeds B-29 |
| B-18 | EF Configurations + migration for course-related entities | done | |
| B-19 | Category CRUD (seed initial categories) | done | |
| B-20 | Course CRUD (create/edit/delete/publish/archive) — Instructor only | done | |
| B-21 | Course queries: list (with filters, pagination, sorting), get by id | done | |
| B-22 | Section CRUD + reordering | done | |
| B-23 | Lesson CRUD (Video/Post) + reordering | done | |
| B-24 | File upload service (Azure Blob): video + cover image | done | |
| B-25 | Instructor application flow (submit, admin approve/reject) | done | |
| B-25.1 | Admin seeding: `AdminSeederHostedService` — creates first admin from `SeedAdmin:Email`/`SeedAdmin:Password` config on startup if no Admin exists. Dev defaults in `appsettings.Development.json` (`admin@learnix.dev` / `Admin123!`). Admin can promote others via existing `POST /api/admin/users/{id}/roles/{role}`. | done | |

### Phase 4 — Student Features

| # | Task | Status | Notes |
|---|---|---|---|
| B-26 | Enrollment (free courses — instant) | done | Mock payment auto-confirms paid courses |
| B-27 | Lesson progress (mark complete, track per user) | done | |
| B-28 | Lesson likes | CANCELED | |
| B-29 | Test system: submit attempt, score calculation, attempt limits + cooldown | done | Order-based answer matching (not GUID) — EF8 JSON collections require ordinal keys |
| B-30 | Course completion detection + Certificate generation (PDF) | done | Async PDF gen via BackgroundService (PeriodicTimer 30s); QuestPDF; Azure Blob upload; SAS URL for download |
| B-31 | Student profile (edit name, avatar, bio, category preferences) | done | |

### Phase 5 — Payments

| # | Task | Status | Notes |
|---|---|---|---|
| B-32 | Stripe integration (test mode): create checkout session | CANCELED | Mock payment remains |
| B-33 | Stripe webhook handler (payment completed → activate enrollment) | CANCELED | Mock payment remains |
| B-34 | Payment history queries | done | GetMyPayments, GetInstructorEarnings, GetAdminPayments |
| B-34.5 | Outbox pattern: OutboxMessage entity + EF config + background publisher worker (replace direct domain events publishing in ApplicationDbContext) | done | OutboxProcessorService + OutboxDbContextHolder |
| B-34.6 | Split `UserRegisteredDomainEvent` into two events: `UserRegistered` (raised in Register flow) and `EmailConfirmationRequested` (raised in Resend flow). Current implementation uses `RaiseUserRegistered` in both places — semantic smell. | not started | |

### Phase 6 — Async Processing ~~(MassTransit)~~

| # | Task | Status | Notes |
|---|---|---|---|
| B-35 | MassTransit + Azure Service Bus setup | CANCELED | Overkill for a pet project. Email and achievements are already handled via Outbox + BackgroundService |
| B-36 | Email consumers (verification, enrollment, approval) | CANCELED | Replaced by Outbox (OutboxMessageTypes + OutboxProcessorService) |
| B-37 | Certificate generation consumer | CANCELED | Replaced by CertificatePdfGenerationService (BackgroundService + PeriodicTimer) |
| B-38 | Achievement checking consumer | CANCELED | Replaced by Outbox (EvaluateLessonCompleted, EvaluateEnrollmentCompleted, etc.) |

### Phase 7 — Achievements & Notifications

| # | Task | Status | Notes |
|---|---|---|---|
| B-39 | Achievement entities + seed data | done | Achievement, UserAchievement, UserAchievementProgress entities |
| B-40 | Achievement checking logic (lesson/course/test/social conditions) | done | AchievementEvaluator service |
| B-41 | Notification system (create, list, mark read) | done | |
| B-42 | Domain events → notifications (achievement earned, enrollment confirmed, certificate ready) | done | Certificate ready: NotificationsHub SignalR push after PDF generation. Achievement: already exists. The rest (enrollment confirmed) — not started |
| B-42.5 | Merge AchievementsHub + ChatHub → NotificationsHub (single WS connection) | done | INotificationsHubClient; /hubs/notifications; ICertificateNotifier + SignalRCertificateNotifier |

### Phase 8 — Chat & AI

| # | Task | Status | Notes |
|---|---|---|---|
| B-43 | Student ↔ Instructor messaging (per course, SignalR) | done | PostgreSQL (CourseConversation + CourseMessage); ChatHub; unread count denormalization |
| B-44 | MongoDB setup: MongoDbContext, repositories | Done | |
| B-45 | AI chat: Multi-provider integration (Anthropic + Gemini, streaming SSE, tool use) | Done | |
| B-46 | Chat session persistence (MongoDB) | Done | |
| B-46.5 | Background job: cleanup closed AI chat sessions older than 30 days | Done | |

### Phase 9 — Reviews & Recommendations

| # | Task | Status | Notes |
|---|---|---|---|
| B-47 | Course reviews (PostgreSQL): create, list, average rating | done | Decision: PostgreSQL (not MongoDB) — structured data, FK joins, unique constraint. |
| B-48 | Denormalized rating on Course entity | done | AverageRating (numeric 4,2) + ReviewsCount on Course. Inline arithmetic (AddRating/UpdateRating/RemoveRating). |
| B-49 | Recommendations engine (based on enrollments, likes, preferences) | CANCELED | Abandoned Preferences feature, showing the same courses to everyone |

### Phase 10 — Admin & Caching

| # | Task | Status | Notes |
|---|---|---|---|
| B-50 | Admin: user management (list, search, ban/unban, role change) | Done | |
| B-51 | Admin: course management (view all, unpublish, delete) | Done | |
| B-52 | Redis caching: CachingBehavior + ICacheable queries | done | ICacheable<TValue> + CachingBehavior<TRequest,TValue> : IPipelineBehavior<TRequest,Result<TValue>>; categories (24h), featured courses (30m), course detail (10m), public catalog (5m) |
| B-53 | Cache invalidation in relevant commands | done | Course commands (Publish/Unpublish/Archive/Unarchive/Update/Delete × instructor+admin) + Review commands (Create/Update/Delete) |

---

## Frontend

### Phase 1 — Setup & Auth UI

| # | Task | Status | Notes |
|---|---|---|---|
| F-01 | Vite + React 19 + TypeScript scaffolding | done | |
| F-02 | TailwindCSS + shadcn/ui setup | done | |
| F-03 | Routing (React Router v6): public/private route guards | done | |
| F-04 | Axios instance (interceptors: attach JWT, handle 401 → refresh) | done | |
| F-05 | Auth store (Zustand): token management, user state | done | |
| F-06 | Login / Register / Forgot Password pages | done | |
| F-07 | Google OAuth button | done | |
| F-08 | Email verification page | done | |

### Phase 2 — Course Browsing (Student)

| # | Task | Status | Notes |
|---|---|---|---|
| F-09 | Course catalog page (grid, filters, search, pagination) | done | Server-side search/sort/filter (sortBy, isFree, minRating added to backend). URL-synced filters via useSearchParams. PublicCourseCardDto enriched with averageRating, reviewsCount, categoryName, instructorFullName. |
| F-10 | Course detail page (description, sections, lessons list, enroll button) | done | |
| F-11 | Enrollment flow (free + paid) | done | Added PaymentPage for mock premium course enrollment checkout. Uses react-hook-form + zod for card input. |
| F-11.5 | Wishlist page (view, remove) | done | Implemented WishlistPage with WishlistCard utilizing backend Wishlist API |

### Phase 3 — Learning Experience

| # | Task | Status | Notes |
|---|---|---|---|
| F-12 | Lesson viewer: video player | done | |
| F-13 | Lesson viewer: post (markdown rendering) | done | |
| F-14 | Lesson viewer: test (quiz UI, submit, results) | done | |
| F-15 | Lesson progress tracking (mark complete, sidebar state) | done | |
| F-16 | Lesson likes | CANCELED | |

### Phase 4 — Instructor Dashboard

| # | Task | Status | Notes |
|---|---|---|---|
| F-17 | Course creation/edit form (React Hook Form + Zod) | done | |
| F-18 | Section & lesson management (drag-and-drop reorder) | done | |
| F-19 | Lesson editors: video upload, post markdown editor, test builder | done | |
| F-20 | Instructor dashboard: course list, enrollment stats | done | Dashboard (take:5, overview) + MyCourses page (/instructor/courses, paginated take:20, search, all actions) |
| F-21 | Instructor application form | done | |

### Phase 5 — Profile & Social

| # | Task | Status | Notes |
|---|---|---|---|
| F-22 | Student profile page (edit, avatar upload) | done | Preferences section canceled and hidden |
| F-23 | Achievements display | done | Static meta map for 10 backend codes; mark-seen on mount |
| F-24 | Certificates list + download | done | Added GET /api/certificates/mine backend endpoint |
| F-25 | Course reviews: leave review, view reviews | done | CourseDetailPage at /courses/:courseId with reviews section |

### Phase 6 — Chat & Notifications

| # | Task | Status | Notes |
|---|---|---|---|
| F-26 | Student ↔ Instructor chat UI | done | Two-column layout; ConversationList; ConversationView; ChatMessage; MessageInput; useChatHub. Instructor: InstructorMessagesPage at /instructor/messages, enabled nav link, h-full layout via InstructorLayout h-screen fix |
| F-27 | AI chat widget (streaming response) | done | Floating FAB + slide-up panel; SSE via fetch+ReadableStream; session history on open; tool-use indicator; clear session |
| F-28 | Notification bell + dropdown | done | NotificationBell in Header; polls unread-count every 30s; SignalR push via UnreadCountChanged |

### Phase 7 — Admin Panel

| # | Task | Status | Notes |
|---|---|---|---|
| F-29 | Admin: user management table | done | Search + ban/unban/delete/recover + role management dialog |
| F-30 | Admin: course moderation table | done | Search + includeDeleted toggle + unpublish/delete/recover |
| F-31 | Admin: instructor applications review | done | Card layout + approve + reject with optional reason dialog |
| F-32 | Admin: payment history | done | Mock data (12 records) + status filter; no real Stripe backend |

### Phase 8 — Polish

| # | Task | Status | Notes |
|---|---|---|---|
| F-33 | Homepage: hero, popular courses, recommendations | done | |
| F-34 | Responsive design pass (mobile) | done | |
| F-35 | Loading states, error boundaries, empty states | done | QueryError component; error+empty states on Landing (categories+featured), CourseCatalog (skeleton+error), CourseDetail (network error+empty curriculum); removed mock fallbacks from useCategories/useFeaturedCourses |
| F-36 | Dark mode (optional) | done | Low priority |
| F-37 | Refactor unread-count to pure-reactive (remove polling) | done | Removed `refetchInterval: 30_000` and set `staleTime: Infinity` in NotificationBell; SignalR `UnreadCountChanged` already called `setQueryData` directly — no HTTP request on push |

---

## Deploy & Infrastructure

### Phase 1 — Containerization

| # | Task | Status | Notes |
|---|---|---|---|
| D-01 | Dockerfile for API (.NET 8, multi-stage build) | done | Learnix.Backend/Dockerfile; non-root appuser; publishes Learnix.API on port 8080 |
| D-02 | Dockerfile for frontend (Vite build → nginx) | done | learnix-client/Dockerfile + nginx.conf; SPA fallback; gzip; immutable asset cache |
| D-03 | Docker Compose: full stack (API + client + postgres + mongo + redis) | done | api + client services added; api depends on postgres/mongo/redis healthcheck |

### Phase 2 — CI/CD

| # | Task | Status | Notes |
|---|---|---|---|
| D-04 | GitHub Actions: build + test on PR | not started | |
| D-05 | GitHub Actions: build Docker images + push to ACR | not started | |

### Phase 3 — Azure Deployment

| # | Task | Status | Notes |
|---|---|---|---|
| D-06 | Azure Container Apps (or App Service) for API | not started | |
| D-06.5 | Configure ForwardedHeaders for rate limiting partition-by-real-IP behind reverse proxy | done | Prerequisite for rate limiter to work correctly in Azure |
| D-07 | Azure Static Web Apps (or Container App) for frontend | not started | |
| D-08 | Azure Database for PostgreSQL (Flexible Server) | not started | |
| D-09 | Azure Cosmos DB for MongoDB API | not started | |
| D-10 | Azure Cache for Redis | not started | |
| D-11 | Azure Blob Storage account + containers | not started | |
| D-12 | Azure Service Bus namespace + queues | CANCELED | MassTransit is not used |
| D-13 | Azure Key Vault for secrets | not started | |
| D-14 | Custom domain + SSL | not started | |
| D-15 | Application Insights (Serilog sink) | not started | |

---

## Summary

> CANCELED tasks excluded from count.

| Section | Total | Done | Remaining |
|---|---|---|---|
| Backend | 54 | 50 | 4 |
| Frontend | 37 | 37 | 0 |
| Deploy | 14 | 3 | 11 |
| **Total** | **105** | **90** | **15** |
