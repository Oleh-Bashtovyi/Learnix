# Learnix

<div align="center">

[![Backend CI](https://github.com/USERNAME/Learnix/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/USERNAME/Learnix/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/USERNAME/Learnix/actions/workflows/ci.yml/badge.svg)](https://github.com/USERNAME/Learnix/actions/workflows/ci.yml)

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-4EA94B?logo=mongodb&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)

</div>

Full-featured online learning platform (LMS) built as a portfolio project.

Students browse and purchase courses, Instructors create and manage content, Admins moderate the platform.

### Video Demonstrations
*Placeholders for portfolio videos:*
- **Student Experience** — Browsing courses, learning, taking tests, earning certificates. *(Video link here)*
- **Instructor Experience** — Creating courses, uploading videos, tracking earnings. *(Video link here)*
- **Admin Experience** — Moderating platform, approving instructors, managing categories. *(Video link here)*

---

## Tech Stack

### Backend — `Learnix.Backend/`
- **.NET 8** — C# 12, ASP.NET Core 8
- **Clean Architecture + CQRS** via MediatR
- **PostgreSQL** (primary data) + **MongoDB** (chat sessions, reviews) + **Redis** (caching)
- **Entity Framework Core** — ORM
- **Transactional Outbox Pattern** — async messaging via PostgreSQL LISTEN/NOTIFY
- **ASP.NET Identity + JWT** — auth with refresh token rotation
- **FluentValidation** — pipeline behavior, returns `Result.Fail()` (no exceptions for business errors)
- **FluentResults** — Result pattern
- **Serilog + Azure Application Insights** — structured logging
- **Stripe** (test mode) — payments
- **Anthropic Claude API** — AI chat assistant (streaming via SSE)
- **Azure Blob Storage** — file uploads (videos, images)

### Frontend — `learnix-client/`
- **React 19 + Vite + TypeScript**
- **React Router v6** — nested layouts, role-based route guards, lazy loading
- **TanStack Query** — server state (cache, mutations, optimistic updates)
- **Zustand** — client-only state (auth, theme, UI)
- **React Hook Form + Zod** — form validation
- **Axios** — HTTP client with interceptor-based token refresh
- **Tailwind CSS + shadcn/ui** — styling and accessible primitives
- **npm** + **Node 20 LTS**

---

## Repository Structure

```
learnix/
├── Learnix.Backend/
│   ├── Learnix.API              # Controllers, middleware, DI
│   ├── Learnix.Application      # CQRS handlers, validators, specifications
│   ├── Learnix.Domain           # Entities, domain events, enums
│   └── Learnix.Infrastructure   # EF Core, MongoDB, Redis, MassTransit, external services
│
├── learnix-client/
│   └── src/                     # React frontend
│
├── docker-compose.yml           # Local infrastructure (postgres, mongo, redis)
├── docs/                        # Architecture, setup, and decision logs
│   ├── ARCHITECTURE.md          # Backend architecture specification
│   ├── ARCHITECTURE_FRONTEND.md # Frontend architecture specification
│   ├── DECISIONS.md             # Backend ADRs
│   ├── DECISIONS_FRONTEND.md    # Frontend ADRs
│   ├── DECISIONS_INFRA.md       # Infrastructure ADRs
│   ├── DECISIONS_ACHIEVEMENTS.md# Domain-specific ADRs
│   ├── DATA_MODEL.md            # Entity schemas and relationships
│   ├── DEV_SETUP.md             # Detailed local setup and API key guide
│   └── FEATURES.md              # Feature specification
├── TODO.md                      # Implementation tracking
└── README.md
```

---

## Core Features

- **Authentication** — email/password + Google OAuth, email verification, password reset
- **Three roles** — Student (default), Instructor (via application + admin approval), Admin
- **Courses** — CRUD by Instructors, browse/filter/search by Students, free or paid
- **Lessons** — three types: Video, Post (markdown), Test (quiz with SingleChoice / MultipleChoice / TextInput questions)
- **Enrollments & Progress** — track lesson completion, course completion triggers certificate
- **Payments** — Stripe test mode, mock payments create enrollments on success
- **Achievements** — auto-awarded on milestones (first lesson, 5 courses, perfect test score, etc.)
- **Certificates** — auto-generated PDF on course completion, shareable via unique URL
- **AI Assistant** — Claude-powered chat, streaming responses, conversation history per user
- **Student ↔ Instructor messaging** — in-course 1-on-1 chat
- **Course reviews** — 1-5 star rating + text after completion
- **Notifications** — in-app bell for messages, achievements, certificates, enrollment updates
- **Admin panel** — user management, course moderation, instructor application review, payment history

Full specification → [`docs/FEATURES.md`](./docs/FEATURES.md)

---

## Running Locally

### Prerequisites

- **.NET 8 SDK**
- **Node 20 LTS** (use `nvm use` in `learnix-client/`)
- **npm 10+** (ships with Node 20)
- **Docker** (for local PostgreSQL + MongoDB + Redis)

### Start infrastructure

To run only the infrastructure (PostgreSQL, MongoDB, Redis, Azurite, Mailpit):

```bash
docker compose up -d
```

To run the **entire platform** via Docker (Infrastructure + Backend API + React Client):

```bash
docker compose --profile apps up -d
```

### Backend

```bash
cd Learnix.Backend
dotnet restore
dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
dotnet run --project Learnix.API
```

API available at `https://localhost:5001` (or port configured in `launchSettings.json`).

### Frontend

```bash
cd learnix-client
npm install
npm run dev
```

App available at `http://localhost:5173`.

### Environment Variables

Each side has its own `.env.example`:

**Backend** (`Learnix.Backend/Learnix.API/.env.example`):
```
DATABASE_URL
MONGO_URI
REDIS_URL
JWT_SECRET
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
STRIPE_SECRET_KEY
ANTHROPIC_API_KEY
AZURE_BLOB_CONNECTION_STRING
AZURE_SERVICE_BUS_CONNECTION_STRING
```

**Frontend** (`learnix-client/.env.example`):
```
VITE_API_URL
VITE_GOOGLE_CLIENT_ID
VITE_STRIPE_PUBLISHABLE_KEY
```

Copy to `.env` and fill in values. Never commit `.env` files.

> **Note:** For a detailed step-by-step guide on how to get these API keys (Google, Stripe, Anthropic) and start the project, see **[`docs/DEV_SETUP.md`](./docs/DEV_SETUP.md)**.

---

## Architecture Highlights

This project is deliberately built as a **monolith with clean separation** rather than microservices. The separation is structural (Clean Architecture layers, bounded contexts, event-driven async flows) — so evolution toward microservices remains possible without rewriting the core.

**Backend:**
- **CQRS via MediatR** — all operations go through Command/Query handlers; no logic in controllers
- **Specification Pattern** — query logic decoupled from repositories
- **Domain events → Integration events** — MediatR in-process for intra-module reactions, Outbox pattern with PostgreSQL LISTEN/NOTIFY for async side effects (email, PDF generation, achievement checking)
- **FluentResults** — explicit error returns, exceptions reserved for infrastructure failures
- **ProblemDetails (RFC 7807)** — standardized error responses
- **Soft delete** for `User` and `Course` (30-day retention via background job)

**Frontend:**
- **Layer-based structure** with feature-split inside layers
- **Page co-location** — page-specific components live with the page; shared components in `components/common/`
- **Role-based page split** — `public/`, `student/`, `instructor/`, `admin/`
- **Access token in memory + HttpOnly refresh cookie** — silent refresh on app start, queued 401 handling in Axios interceptor
- **Typed DTOs separate from Zod form schemas** — explicit transformation in `onSubmit`

Full details:
- [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) — backend
- [`docs/ARCHITECTURE_FRONTEND.md`](./docs/ARCHITECTURE_FRONTEND.md) — frontend
- [`docs/DECISIONS.md`](./docs/DECISIONS.md) — backend ADRs
- [`docs/DECISIONS_FRONTEND.md`](./docs/DECISIONS_FRONTEND.md) — frontend ADRs

---

## Commit Convention

This repo follows [Conventional Commits](https://www.conventionalcommits.org/).

Format:
```
<type>: <short summary>

[optional body]
```

Types:

| Type | When to use |
|---|---|
| `feat` | New feature (user-facing or internal capability) |
| `fix` | Bug fix |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only (README, ADRs, code comments) |
| `chore` | Tooling, dependencies, build config, no production code change |
| `test` | Adding or fixing tests |
| `perf` | Performance improvement |
| `style` | Formatting, whitespace, no logic change |

Examples:
```
feat: add course enrollment command
fix: resolve 401 refresh loop in axios interceptor
refactor: extract pagination logic to shared hook
docs: add FADR-011 tooling decisions
chore: bump .NET SDK to 8.0.400
```

Scope in parentheses is optional for disambiguation:
```
feat(auth): add Google OAuth callback endpoint
fix(frontend): correct CourseCard responsive layout
```

---

## Project Tracking

- **[`TODO.md`](./TODO.md)** — task breakdown by phase (Backend / Frontend / Deploy), status per task
- **[`docs/DECISIONS.md`](./docs/DECISIONS.md)** and **[`docs/DECISIONS_FRONTEND.md`](./docs/DECISIONS_FRONTEND.md)** — ADRs (what was decided, why, alternatives considered)

---

## Status

Portfolio project — actively in development. Not intended for production use.
