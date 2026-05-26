# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Learnix** is a full-stack Learning Management System (LMS). Students browse and complete courses; instructors create content; admins moderate the platform.

- **Backend:** .NET 8 / C# 12, Clean Architecture + CQRS (MediatR), PostgreSQL primary DB, MongoDB for chat/reviews, Redis for caching
- **Frontend:** React 19 + Vite, TypeScript, Tailwind CSS, TanStack Query + Zustand

## Running Locally

**Prerequisites:** Docker, .NET 8 SDK, Node 20

```bash
# 1. Start infrastructure (PostgreSQL:5432, MongoDB:27017, Redis:6379)
docker compose up -d

# 2. Backend — copy .env.example → .env, then:
cd Learnix.Backend
dotnet restore
dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
dotnet run --project Learnix.API          # https://localhost:5001, Swagger at /swagger

# 3. Frontend
cd learnix-client
npm install
npm run dev                                # http://localhost:5173
```

**Other frontend commands:**
```bash
npm run build    # production build
npm run lint     # ESLint
npm run preview  # preview production build
```

No tests exist yet (Phase 2 of development). When added: `dotnet test` for backend.

## Backend Architecture

```
HTTP Request
  → Controller (thin — delegates to IMediator)
  → MediatR Pipeline: LoggingBehavior → ValidationBehavior (FluentValidation → Result.Fail)
  → Command/Query Handler → returns Result<T> (FluentResults)
  → Repository via Specification<T> pattern
  → EF Core (PostgreSQL) / MongoDB / Redis
  → [Domain events published post-SaveChanges via MediatR in-process]
```

**Layer rules:**
- `Learnix.Domain` — entities, enums, domain events, no external dependencies
- `Learnix.Application` — CQRS handlers, validators, interfaces; depends only on Domain
- `Learnix.Infrastructure` — implements Application interfaces; EF Core, MongoDB, Redis, external services
- `Learnix.API` — controllers, middleware, DI wiring

**Feature folder structure** (inside Application):
```
Auth/
  Commands/Register/
    RegisterCommand.cs
    RegisterCommandHandler.cs
    RegisterValidator.cs
  Queries/GetProfile/
    GetProfileQuery.cs
    GetProfileQueryHandler.cs
    GetProfileResponse.cs    ← DTOs live here, no separate DTOs folder
  EventHandlers/
```

**Key patterns:**
- **Result<T>:** Handlers return `Result<T>` / `Result` — never throw for business errors. Controllers check `.IsFailed` and return `ProblemDetails` (RFC 7807).
- **Specifications:** All repository queries built via `Specification<T>` + `SpecificationEvaluator`. No raw LINQ in handlers.
- **Soft delete:** `ISoftDeletable` entities auto-filtered by EF query filter. Use `.IgnoreQueryFilters()` when needed.
- **Auditing:** `IAuditable` (CreatedAt, UpdatedAt) populated by `AuditableInterceptor`.
- **Domain events:** Raised in entity methods, dispatched via MediatR after `SaveChangesAsync`.
- **DI:** Each layer has a `DependencyInjection.cs` with extension methods. MediatR and FluentValidation use assembly scanning.

## Authentication

- **JWT:** 15-minute access tokens (Bearer header)
- **Refresh token:** 7-day HttpOnly cookie, hashed in DB, rotated on every refresh
- **Roles:** Student (default), Instructor (via application + admin approval), Admin
- **Identity:** `User : IdentityUser<Guid>`, roles via AspNetRoles tables
- **Email confirmation:** Token-based; currently `ConsoleEmailSender` (mock). Will move to MassTransit consumers.

## Frontend Architecture

- **Styling:** 100% Tailwind CSS — no CSS modules, no SCSS
- **Design tokens:** CSS custom properties (HSL) in `src/index.css` for shadcn/ui light/dark theme
- **Theme:** `.dark` class on `<html>`, persisted in localStorage via Zustand
- **State split:** Zustand for client-only state (auth, theme, UI); TanStack Query for all server state
- **Forms:** React Hook Form + Zod (schema is source of truth for types)
- **HTTP:** Axios with interceptor-based token refresh; queued 401 handling to avoid refresh storms
- **Routing:** React Router v6 with planned nested layouts, role-based route guards, lazy loading
- **Components:** shadcn/ui primitives (added via CLI, become project code in `components/ui/`)
- **Co-location:** Page-specific components live with their page; shared components in `components/common/`
- **Role-based pages:** `pages/public/`, `pages/student/`, `pages/instructor/`, `pages/admin/`

## Commit Convention

Conventional Commits format: `type(optional-scope): message`

Types: `feat`, `fix`, `refactor`, `docs`, `chore`, `test`, `perf`, `style`

Examples:
```
feat(auth): add refresh token rotation
fix: resolve 401 refresh loop in axios interceptor
refactor(courses): extract pagination to shared hook
```

## Key Docs

- `ARCHITECTURE.md` — detailed backend layer rules and request flow
- `ARCHITECTURE_FRONTEND.md` — frontend folder structure and state decisions
- `DATA_MODEL.md` — PostgreSQL entities, MongoDB documents, relationships
- `DECISIONS.md` / `DECISIONS_FRONTEND.md` — ADRs explaining major technical choices
- `FEATURES.md` — full feature specification per role
- `TODO.md` — implementation tracking by phase (Phases 1–9)

> Most docs are written in Ukrainian.

---

> **For Claude:** Backend task standards and pre/post-task checklists are in `.claude/skills/backend-standards/SKILL.md`. Frontend task standards are in `.claude/skills/frontend-standards/SKILL.md`.
