# Learnix

<div align="center">

![.NET 8](https://img.shields.io/badge/-%2E%4E%45%54%208.0-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind%20CSS-38B2AC?logo=tailwind-css&logoColor=white)

![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-4EA94B?logo=mongodb&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

**A production-grade, full-stack Learning Management System (LMS) demonstrating modern architectural patterns and clean code practices.**

🚀 **Live Demo:** <!-- LIVE_DEMO_START -->*(Deployment pending...)*<!-- LIVE_DEMO_END -->

</div>

## Overview

Learnix is a comprehensive platform where students can browse and purchase courses, instructors can create and manage content, and administrators can moderate the ecosystem. It serves as a technical showcase of building scalable, maintainable monoliths using Clean Architecture and domain-driven principles.

### Video Demonstrations
*(Videos coming soon)*
- **Student Experience** — Browsing courses, learning, taking tests, earning certificates.
- **Instructor Experience** — Creating courses, uploading videos, tracking earnings.
- **Admin Experience** — Moderating platform, approving instructors, managing categories.

---

## Tech Stack

### Backend — `Learnix.Backend/`
- **.NET 8** — C# 12, ASP.NET Core 8
- **Clean Architecture + CQRS** via MediatR
- **PostgreSQL** (primary data) + **MongoDB** (chat sessions) + **Redis** (caching)
- **Entity Framework Core** — ORM with Specification Pattern
- **SignalR** — Real-time WebSockets for chat and notifications
- **Transactional Outbox Pattern** — Async messaging via PostgreSQL LISTEN/NOTIFY
- **ASP.NET Identity + JWT** — Auth with refresh token rotation
- **FluentValidation & FluentResults** — Pipeline behavior without exceptions for business logic
- **Serilog + Seq (local) + Azure Application Insights** — Structured JSON logging with CorrelationId tracing
- **AI Integrations** — Anthropic Claude API & Google Gemini API
- **Document & Email Generation** — QuestPDF (certificates) and MailKit (SMTP)
- **Azure Blob Storage** — File uploads (videos, images) via Azure SDK

### Frontend — `learnix-client/`
- **React 19 + Vite + TypeScript**
- **TanStack Query** — Server state (caching, mutations, optimistic updates)
- **Zustand** — Client-only state (auth, UI)
- **React Hook Form + Zod** — Type-safe form validation
- **React Router v6** — Nested layouts, role-based route guards, lazy loading
- **Tailwind CSS + shadcn/ui** — Styling and accessible primitives
- **i18next** — Multi-language localization support
- **Axios** — HTTP client with interceptor-based token refresh

---

## Core Features

- **Authentication & Roles:** JWT-based Auth (Email/Password + Google OAuth). Three distinct roles: Student, Instructor, Admin.
- **Course Ecosystem:** Instructors create/edit courses and modules. Students search, filter, enroll (mock payments via Stripe API), track progress, and leave **1-5 star reviews**.
- **Interactive Lessons & Quizzes:** Support for Video (blob streaming), Rich Text (Markdown), and Tests (Multiple choice, Text input with fuzzy match).
- **Real-Time Communication:** 1-on-1 Student ↔ Instructor messaging and global in-app notifications powered by **SignalR**.
- **AI Assistant:** Context-aware chat widget leveraging **Anthropic Claude** or **Google Gemini** with streaming responses.
- **Achievements & Certificates:** Auto-awarded badges via domain events, and auto-generated PDF certificates (QuestPDF) upon course completion.
- **Admin Panel:** Comprehensive moderation tools (manage users, review instructor applications, oversee mock payments and courses).
- **Localization:** Fully translated UI with language toggles (i18n).

*Full specification → [`docs/FEATURES.md`](./docs/FEATURES.md)*

---

## System Architecture & Patterns

This project is deliberately built as a **modular monolith** with clean boundaries, ensuring that evolution toward microservices remains possible without rewriting the core domain.

**Backend Architecture:**
- **CQRS via MediatR:** All operations go through dedicated Command/Query handlers; controllers are completely devoid of business logic.
- **Specification Pattern:** Query logic is fully decoupled from repositories, keeping data access clean and testable.
- **Event-Driven Side Effects:** Domain events trigger in-process MediatR integration events. The **Outbox pattern** handles async side effects (sending emails, generating PDFs, checking achievements) reliably.
- **Result Pattern:** `FluentResults` provides explicit error handling. Exceptions are strictly reserved for infrastructure failures, never for control flow.
- **ProblemDetails (RFC 7807):** Standardized, uniform API error responses.
- **Soft Delete:** Implemented for `User` and `Course` aggregates with a 30-day retention background worker.

**Frontend Architecture:**
- **Layer-Based & Feature-Sliced:** Code is organized by domain features within structural layers.
- **Page Co-location:** Page-specific components live strictly alongside their page routes. Reusable UI components are abstracted to `components/common/`.
- **Zod & DTO Separation:** Strict separation between Zod form schemas and typed DTOs. Transformations happen explicitly in `onSubmit` to prevent frontend/backend data shape bleeding.
- **Robust Auth Flow:** Access tokens are kept in memory, while HttpOnly cookies handle refresh tokens. Axios interceptors manage silent token refreshes and queue failed requests during the refresh window.

**Code Quality & Tooling:**
- **Code Duplication Protection:** The project uses **`jscpd`** to strictly enforce a maximum of **5% code duplication** across the entire repository (both C# and TS/TSX). This is validated globally on every commit via Husky hooks, as well as in GitHub Actions CI pipelines.
- **Strict Formatting:** Managed automatically via `lint-staged` (Prettier for frontend, `dotnet format` for backend).

---

## Repository Structure

```
learnix/
├── Learnix.Backend/
│   ├── Learnix.API              # Controllers, middleware, DI setup
│   ├── Learnix.Application      # CQRS handlers, validators, specifications
│   ├── Learnix.DbMigrator       # Standalone EF Core migrations and data seeding
│   ├── Learnix.Domain           # Entities, domain events, enums, exceptions
│   └── Learnix.Infrastructure   # EF Core, MongoDB, Redis, SignalR, external services
│
├── learnix-client/
│   └── src/                     # React frontend
│
├── docker-compose.yml           # Local infrastructure (postgres, mongo, redis, azurite)
└── docs/                        
    ├── backend/                 # Backend documentation & ADRs
    ├── frontend/                # Frontend documentation & ADRs
    ├── API_KEYS_GUIDE.md        # API configuration guide
    ├── CONTRIBUTING.md          # Commit conventions and contribution guidelines
    └── DEV_SETUP.md             # Local setup checklist
```

---

## Running Locally

Detailed setup instructions, including how to configure external API keys (Google, Anthropic, Gemini), can be found in the documentation:

👉 **[Local Development Setup Guide (`docs/DEV_SETUP.md`)](./docs/DEV_SETUP.md)**

### Option 1: Run everything in Docker (Recommended for quick start)

This approach runs the infrastructure, backend API, and frontend entirely within Docker containers.

> [!IMPORTANT]
> You must copy the `.env.example` files to `.env` in both `Learnix.Backend/Learnix.API` and `learnix-client` BEFORE running these commands. See `docs/DEV_SETUP.md` for details.

```bash
# 1. Start infrastructure (PostgreSQL, MongoDB, Redis, Azurite, Mailpit, Seq)
docker compose up -d

# 2. Initialize database and blob storage (runs the migrator container)
docker compose --profile init up migrator

# 3. Start API and Frontend containers
docker compose --profile apps up -d
```

**Available Endpoints (Docker Setup):**
- **Frontend Client:** [http://localhost:80](http://localhost:80)
- **Backend API:** [http://localhost:8080](http://localhost:8080)
- **Mailpit (Email UI):** [http://localhost:8025](http://localhost:8025)
- **Seq (Logs UI):** [http://localhost:5341](http://localhost:5341)

### Option 2: Run infrastructure in Docker, Apps locally (Recommended for development)

```bash
# 1. Start infrastructure
docker compose up -d

# 2. Initialize database and blob storage
docker compose --profile init up migrator

# 3. Start API locally
cd Learnix.Backend
dotnet run --project Learnix.API

# 4. Start frontend locally
cd ../learnix-client
npm install
npm run dev
```

> **Note:** For a detailed step-by-step guide on how to configure environment variables, API keys, and start the project, see **[`docs/DEV_SETUP.md`](./docs/DEV_SETUP.md)**.

---

## Documentation & Decisions

All architectural choices, trade-offs, and technical debt are documented using Architecture Decision Records (ADRs).

- **[`docs/backend/decisions/README.md`](./docs/backend/decisions/README.md)** — Backend ADRs (Domain modeling, Auth flow, DB choices)
- **[`docs/frontend/decisions/README.md`](./docs/frontend/decisions/README.md)** — Frontend ADRs (State management, UI patterns, i18n)
- **[`docs/TODO.md`](./docs/TODO.md)** — Feature tracking and project roadmap
- **[`docs/CONTRIBUTING.md`](./docs/CONTRIBUTING.md)** — Repository commit conventions

---

## Status

**This project serves as a comprehensive showcase of my full-stack engineering capabilities.** Actively maintained and continuously updated with new features and architectural refinements.
