# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Learnix** is a full-stack Learning Management System (LMS). Students browse, purchase and complete courses; instructors create content; admins moderate the platform.

- **Backend:** .NET 8 / C# 12, Clean Architecture + light DDD + CQRS (MediatR), PostgreSQL primary DB, MongoDB for AI chat sessions, Redis for distributed cache, Azure Blob Storage for media
- **Frontend:** React 19 + Vite 8, TypeScript, Tailwind CSS v3 + shadcn/ui, TanStack Query + Zustand, react-i18next
- **Deployment:** Azure Container Apps (API) + Azure Static Web Apps (client), provisioned via Terraform

## Repository Layout

```
Learnix.Backend/          .NET solution (Learnix.Backend.slnx)
  Learnix.Domain/         entities, value objects, domain events, enums — zero NuGet deps
  Learnix.Application/    CQRS handlers, validators, specifications, interfaces, behaviors
  Learnix.Infrastructure/ EF Core, MongoDB, Redis, Azure Blob, email, AI providers, Outbox
  Learnix.API/            controllers, middleware, SignalR hubs, rate limiting, DI wiring
  Learnix.DbMigrator/     standalone migration + seeding runner (see ADR MIGRATIONS.md)
  Learnix.Domain.UnitTests/
  Learnix.Application.UnitTests/
learnix-client/           React SPA
docs/                     all project documentation (English)
infrastructure/           Terraform (Azure)
mockups/                  UI mockups
```

## Running Locally

**Prerequisites:** Docker, .NET 8 SDK, Node 20+, `dotnet tool install --global dotnet-ef`

Copy `.env` files **before** starting Docker (the client build consumes `learnix-client/.env` as a BuildKit secret):

```bash
cp Learnix.Backend/Learnix.API/.env.example Learnix.Backend/Learnix.API/.env
cp learnix-client/.env.example learnix-client/.env
```

**Option A — everything in Docker:**
```bash
docker compose --profile apps up -d      # infra + migrator + api + client
```

**Option B — infra in Docker, apps locally (preferred for development):**
```bash
docker compose up -d                     # postgres, mongo, redis, azurite, mailpit, seq
docker compose --profile init up migrator # apply migrations + seed

cd Learnix.Backend && dotnet run --project Learnix.API   # http://localhost:5000, https://localhost:5001
cd learnix-client && npm install && npm run dev          # http://localhost:5173
```

Swagger is served **only in Development** at `/swagger`.

Full walkthrough incl. seeded accounts, service URLs and DB inspection: `docs/DEV_SETUP.md`.
API keys (Google OAuth, Anthropic, Gemini): `docs/API_KEYS_GUIDE.md`.

### Local service URLs

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API / Swagger | http://localhost:5000/swagger |
| Seq (structured logs) | http://localhost:5341 |
| Mailpit (email catcher) | http://localhost:8025 |
| Azurite (blob emulator) | http://localhost:10000 |

## Commands

**Backend** (from `Learnix.Backend/`):
```bash
dotnet build Learnix.Backend.slnx
dotnet test  Learnix.Backend.slnx
dotnet format Learnix.Backend.slnx --verify-no-changes   # CI enforces this

# Migrations — output into the Infrastructure project
dotnet ef migrations add {Name} \
    --project Learnix.Infrastructure \
    --startup-project Learnix.API \
    --output-dir Persistence/EntityFramework/Migrations
# Apply via the migrator, NOT `dotnet ef database update` (ADR MIGRATIONS.md)
dotnet run --project Learnix.DbMigrator --launch-profile Development -- --create-blob --seed-demo
```

**Frontend** (from `learnix-client/`):
```bash
npm run dev
npm run build          # tsc -b && vite build
npm run type-check     # tsc -b
npm run lint           # ESLint
npm run format         # Prettier + Tailwind class sort
npm run format:check   # CI enforces this
```

**Repo root:**
```bash
npm run check:duplication   # jscpd — runs in pre-commit and CI
npm run check:secrets       # gitleaks via Docker
```

> `TreatWarningsAsErrors` is on for every backend project, and `EnforceCodeStyleInBuild` makes IDE analyzers (unused usings, unused members) fail the build. A warning **is** a build failure.

### Pre-commit hook (`.husky/pre-commit`)

Runs `lint-staged` (ESLint --fix + Prettier on staged frontend files, `dotnet format` on staged C# files), then sequentially: frontend `type-check`, backend `dotnet build`, and `check:duplication`. Don't bypass it.

## Backend Architecture

```
HTTP Request
  → Controller (thin — `ISender` + `result.ToActionResult()`)
  → MediatR pipeline: LoggingBehavior → ValidationBehavior → DomainExceptionBehavior → CachingBehavior
  → Command/Query handler → returns Result / Result<T> (FluentResults)
  → Repository (Ardalis.Specification `IRepositoryBase<T>`) via a Specification
  → EF Core (PostgreSQL) / MongoDB / Redis / Azure Blob
  → Interceptors on SaveChanges: Auditable, SoftDelete, DomainEvents (dispatched pre-commit, same transaction)
  → Domain event handlers enqueue Outbox messages; a background worker drains them
```

**Dependency rule:** `API → Application → Domain`, `Infrastructure → Application`. `Application → Infrastructure` is forbidden — only via interfaces.

**Feature folder structure** (inside Application):
```
Payments/
  Abstractions/IPaymentRepository.cs      ← feature-scoped interfaces
  Commands/InitiateMockPayment/
    InitiateMockPaymentCommand.cs
    InitiateMockPaymentCommandHandler.cs
    InitiateMockPaymentValidator.cs
    InitiateMockPaymentResponse.cs
  Queries/GetMyPayments/                  ← DTOs co-located, no separate DTOs folder
  Specifications/
  Constants/
```
Cross-cutting interfaces live in `Application/Common/Abstractions/{Category}/`.

**Key patterns:**
- **Result<T>:** handlers return `Result` / `Result<T>` — never throw for business errors. Typed errors in `Application/Common/Errors/` (`NotFoundError` → 404, `ConflictError` → 409, `ForbiddenError` → 403, `AuthenticationError` → 401, `ValidationError` → 400) map to RFC 7807 `ProblemDetails`.
- **Authorization lives in handlers**, not controllers.
- **Specifications:** repositories are `Ardalis.Specification` `RepositoryBase<T>`; all queries go through a `Specification<T>`. No raw LINQ in handlers.
- **Soft delete:** `ISoftDeletable` auto-filtered by EF query filter; `.IgnoreQueryFilters()` when needed.
- **Auditing:** `IAuditable` populated by `AuditableInterceptor`.
- **Domain events:** raised in entity methods, dispatched in-process via MediatR from `DomainEventsInterceptor.SavingChangesAsync` — **before** the INSERT/UPDATE runs, inside the same transaction, so the Outbox rows their handlers write commit atomically with the entity. A handler therefore cannot query for the change that raised it: the row is not there yet.
- **Outbox:** durable side-effects (emails, achievement evaluation, notifications, `DeleteBlob`) enqueued by domain-event handlers, drained by `OutboxProcessorService` (PostgreSQL `LISTEN/NOTIFY` + `FOR UPDATE SKIP LOCKED`).
- **Caching:** queries implementing `ICacheable<TValue>` are cached by `CachingBehavior` (Redis).
- **Rate limiting:** policies in `API/RateLimiting/RateLimitPolicies.cs`, applied per-endpoint with `[EnableRateLimiting]`.
- **DI:** each layer exposes a `DependencyInjection.cs`. MediatR and FluentValidation use assembly scanning.

**Notable integrations:** Azure Blob — uploads go client → SAS → `temp-uploads`, then a handler calls `CommitUploadAsync` **synchronously** to validate (magic bytes) and promote the blob to its final container; entities store the relative `{container}/{blobName}` path — the container prefix is mandatory, everything downstream parses it out (ADR-BLOB-003). SignalR `NotificationsHub` at `/hubs/notifications`, AI chat via swappable `Anthropic` / `Gemini` providers (`AiChat:Provider`) with sessions in MongoDB, certificates via QuestPDF + QRCoder, email via MailKit + RazorLight templates + PreMailer.

**Payments are mocked** (`InitiateMockPayment`) — there is no real payment gateway.

## Authentication

- **JWT:** 15-minute access tokens (Bearer header)
- **Refresh token:** 7-day HttpOnly cookie, hashed with a pepper in DB, rotated on every refresh
- **Roles:** Student (default), Instructor (via application + admin approval), Admin
- **Identity:** `User : IdentityUser<Guid>`, roles via AspNetRoles tables
- **Google OAuth** via ID-token verification; **email confirmation** via 6-digit OTP
- `EmailConfirmed` authorization policy gates sensitive endpoints
- Full endpoint table: `docs/backend/decisions/AUTH.md`

## Frontend Architecture

- **Styling:** 100% Tailwind CSS — no CSS modules, no SCSS. Design tokens (HSL CSS custom properties) live in `src/styles/index.css`
- **Theme:** `.dark` class on `<html>`, persisted via Zustand
- **State split:** Zustand for client-only state (`auth`, `theme`, `locale`, `ui`, `player`); TanStack Query for **all** server state
- **Forms:** React Hook Form + Zod (schema is the source of truth for types)
- **HTTP:** Axios with interceptor-based token refresh; queued 401 handling to avoid refresh storms
- **Routing:** React Router **v7** — nested layouts, role-based guards, lazy loading
- **Realtime:** SignalR (`@microsoft/signalr`) for notifications
- **Localization:** react-i18next — JSON namespaces in `src/i18n/locales/{en,uk}/*.json`, Zod messages via `zod-i18n-map`. **Never hardcode UI strings.**
- **Components:** shadcn/ui primitives in `components/ui/` (added via CLI — never hand-written); shared in `components/common/`; page-specific co-located with the page
- **Role-based pages:** `pages/public/`, `pages/student/`, `pages/instructor/`, `pages/admin/`

## Testing

xUnit + FluentAssertions + NSubstitute. Coverage is collected in CI and reported to SonarCloud.

```bash
dotnet test Learnix.Backend.slnx --settings coverage.runsettings
```

There are no frontend tests.

## CI/CD

- `.github/workflows/checks.yml` — backend build/test/SonarCloud, frontend format/lint/type-check/build, jscpd duplication, gitleaks secret scanning. Runs on PRs to any branch and pushes to `main`.
- `.github/workflows/deploy.yml` — on push to `main`: Docker image → ACR/Docker Hub → Azure Container Apps; frontend → Azure Static Web Apps. Jobs skip unchanged packages.

## Commit Convention

Conventional Commits: `type(optional-scope): message`

Types: `feat`, `fix`, `refactor`, `docs`, `chore`, `test`, `perf`, `style`, `ci`

```
feat(auth): add refresh token rotation
fix: resolve 401 refresh loop in axios interceptor
refactor(courses): extract pagination to shared hook
```

## Documentation Map

Docs live under `docs/` and are predominantly **English**. Exception: `docs/backend/decisions/DOMAIN.md` is written in Ukrainian and still uses the legacy un-scoped `ADR-NNN` numbering — match the local convention when editing that file.

| Doc | Purpose |
|---|---|
| `docs/DEV_SETUP.md` | Full local setup, seeded accounts, DB inspection |
| `docs/API_KEYS_GUIDE.md` | Obtaining Google / Anthropic / Gemini keys |
| `docs/FEATURES.md` | Functional spec per role |
| `docs/DATA_MODEL.md` | PostgreSQL entities, MongoDB documents, relationships |
| `docs/TODO.md` | Implementation tracking by phase |
| `docs/TECH_DEBT.md` | Known suboptimal implementations + fix plans |
| `docs/CONTRIBUTING.md` | Contribution workflow |
| `docs/backend/` | `ARCHITECTURE.md`, `PROJECT_STRUCTURE.md`, `decisions/` (ADRs) |
| `docs/frontend/` | `ARCHITECTURE.md`, `PROJECT_STRUCTURE.md`, `CODING_STYLE.md`, `DEPLOYMENT.md`, `decisions/` (ADRs) |
| `docs/deployment/` | `TERRAFORM_GUIDE.md`, `MANUAL_OPERATIONS.md` |

ADRs are grouped by topic, one file per scope (e.g. `docs/backend/decisions/AUTH.md`, `docs/frontend/decisions/UI.md`). Use `decisions/TEMPLATE.md` when adding one, and register it in `decisions/README.md`.

---

> **For Claude:** Backend task standards and pre/post-task checklists are in `.claude/skills/backend-standards/SKILL.md`. Frontend task standards are in `.claude/skills/frontend-standards/SKILL.md`. Security/code audits: `/audit-backend`, `/audit-frontend`.
