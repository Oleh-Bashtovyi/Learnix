# Learnix — Local Development Setup Guide

This guide walks you through getting a fully working dev environment from a fresh clone.

---

## Contents

1. [Stack Overview](#stack-overview)
2. [Prerequisites](#prerequisites)
3. [Step 1 — Start Infrastructure](#step-1--start-infrastructure)
4. [Step 2 — Environment Setup](#step-2--environment-setup)
   - [Backend .env variables](#backend-env-variables)
   - [Frontend .env variables](#frontend-env-variables)
5. [Step 3 — Run Backend Migrations and Start](#step-3--run-backend-migrations-and-start)
6. [Step 4 — Start Frontend](#step-4--start-frontend)
7. [Seeded Accounts](#seeded-accounts)
8. [Service URLs](#service-urls)
9. [Inspecting Databases](#inspecting-databases)
10. [Pre-commit Auto-formatting](#pre-commit-auto-formatting)

---

## Stack Overview

| Layer | Technology | Notes |
|---|---|---|
| Backend | .NET 8 / ASP.NET Core | `http://localhost:5000`, Swagger at `/swagger` |
| Frontend | React 19 + Vite | `http://localhost:5173` |
| PostgreSQL | Docker (port 5432) | Primary database |
| MongoDB | Docker (port 27017) | Chat & reviews |
| Redis | Docker (port 6379) | Caching |
| Azurite | Docker (port 10000) | Local Azure Blob Storage emulator |
| Mailpit | Docker (port 1025 / 8025) | Local email catcher for dev |

---

## Prerequisites

Install all of these before starting:

- **Docker Desktop** — [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
- **.NET 8 SDK** — [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+** — [nodejs.org](https://nodejs.org)
- **EF Core CLI** — `dotnet tool install --global dotnet-ef`

---

## Step 1 — Start Infrastructure

From the repository root:

```bash
docker compose up -d
```

This starts PostgreSQL, MongoDB, Redis, Azurite (blob storage emulator), and Mailpit. All containers run with persistent volumes so data survives restarts.

> **Note:** If you want to run the entire platform via Docker (including the .NET backend and React frontend), run `docker compose --profile apps up -d` instead.

Verify everything is healthy:

```bash
docker compose ps
```

All services should show `healthy` or `running`.

---

## Step 2 — Environment Setup

Both the frontend and backend require an `.env` file to run correctly. 

### Backend .env variables

```bash
cd Learnix.Backend/Learnix.API
cp .env.example .env
```

**Database connections — no action needed**
These match the Docker Compose credentials exactly. Leave them as-is. MongoDB and Redis connection strings are already set in `appsettings.json` and `appsettings.Development.json` — you do not need to add them to `.env` unless you want to override them.

**JWT — no action needed in development**
`Jwt__Secret` is **not required in the `.env` file for development**. `appsettings.Development.json` already provides a dev-only secret. This secret is fine for local development, but in production it must be replaced with a random 64+ character string.

> **IMPORTANT:** Out of the box, the project will run with just these defaults, except for **Google Login** and **AI Chat** which require real API keys. 
> 
> 👉 **See [API_KEYS_GUIDE.md](API_KEYS_GUIDE.md)** for exact, step-by-step instructions on how to get Google, Anthropic, and Gemini API keys.

### Frontend .env variables

```bash
cd learnix-client
cp .env.example .env
```

**`VITE_API_URL` — no action needed**
Points to the backend HTTP endpoint (`http://localhost:5000/api`). Leave as-is.

**`VITE_GOOGLE_CLIENT_ID` — action required**
This is the same **Client ID** you create for the backend (see [API_KEYS_GUIDE.md](API_KEYS_GUIDE.md)). Unlike the Client Secret, the Client ID is public by design — it is safe to expose in frontend code.

---

## Step 3 — Run Backend Migrations and Start

The backend uses a standalone migrator project (`Learnix.DbMigrator`) that safely initializes databases, blob storage containers, and default system accounts.

```bash
cd Learnix.Backend

# 1. Apply database migrations, initialize storage, and seed system data
# Optional: Add --seed-demo to automatically generate fake courses and students
dotnet run --project Learnix.DbMigrator -- --seed-demo

# 2. Start the API (HTTP on port 5000, HTTPS on 5001)
dotnet run --project Learnix.API
```

Once running, Swagger UI is available at **[https://localhost:5001/swagger](https://localhost:5001/swagger)**.

---

## Step 4 — Start Frontend

In a new terminal:

```bash
cd learnix-client
npm install
npm run dev
```

Frontend runs at **[http://localhost:5173](http://localhost:5173)**.

---

## Seeded Accounts

When you run `Learnix.DbMigrator`, it automatically creates the following default accounts. You do **not** need to set `SeedAdmin__Email` in your `.env` file; the migrator uses defaults from `appsettings.Development.json`.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@learnix.dev` | `Admin123!` |
| Instructor | `instructor@learnix.dev` | `Instructor123!` |
| Student | `student@learnix.dev` | `Student123!` *(Only generated if `--seed-demo` is used)* |

You can also register a new account through the UI to get a fresh Student role.

---

## Service URLs

| Service | URL | Notes |
|---|---|---|
| Frontend | http://localhost:5173 | Vite dev server |
| Backend (HTTP) | http://localhost:5000 | API base |
| Backend (HTTPS) | https://localhost:5001 | API base (HTTPS) |
| Swagger | http://localhost:5000/swagger | API docs & testing |
| Mailpit | http://localhost:8025 | View emails sent by the app |
| Azurite Blob | http://localhost:10000 | Local blob storage emulator |
| PostgreSQL | localhost:5432 | DB: `learnix`, user: `learnix`, pass: `learnix` |
| MongoDB | localhost:27017 | user: `learnix`, pass: `learnix` |
| Redis | localhost:6379 | No auth in dev |

---

## Inspecting Databases

> **💡 Pro Tip:** If you want an all-in-one GUI client for both PostgreSQL and MongoDB, we highly recommend **[TablePlus](https://tableplus.com/)**. It allows you to connect to and manage both databases from a single beautiful interface.

### PostgreSQL

**Option A — psql inside the container (no extra tools needed)**

```bash
docker exec -it learnix-postgres psql -U learnix -d learnix
```

Useful commands once inside:
```sql
\dt                         -- list all tables
\d "Courses"                -- describe a table
SELECT * FROM "Courses" LIMIT 10;
\q                          -- quit
```

**Option B — GUI client (DBeaver, TablePlus, DataGrip, pgAdmin)**

Connection settings:

| Field | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `learnix` |
| Username | `learnix` |
| Password | `learnix` |

---

### MongoDB

**Option A — mongosh inside the container (no extra tools needed)**

```bash
docker exec -it learnix-mongo mongosh "mongodb://learnix:learnix@localhost:27017/learnix?authSource=admin"
```

Useful commands once inside:
```js
show collections          // list collections
db.chat_sessions.find().pretty()   // view all chat sessions
db.chat_sessions.find({ isActive: true })
db.chat_sessions.deleteMany({})    // clear all sessions
exit
```

**Option B — MongoDB Compass (official GUI)**

Connection string:
```
mongodb://learnix:learnix@localhost:27017/learnix?authSource=admin
```

Paste it into the connection dialog in MongoDB Compass and click **Connect**.

---

## Pre-commit Auto-formatting

The project uses [Husky](https://typicode.github.io/husky/) and [lint-staged](https://github.com/okonet/lint-staged) to automatically format code before every `git commit`. This ensures consistent formatting without relying on developers to remember manual formatting steps.

### What happens on each commit

The hook runs in two stages:

**Stage 1 — lint-staged** (only staged files, in parallel):

| Files changed | Tool | Effect |
|---|---|---|
| `Learnix.Backend/**/*.cs` | `dotnet format --include` | Fixes formatting per `.editorconfig` for staged files only |
| `learnix-client/src/**/*.{ts,tsx,js,jsx}` | ESLint + Prettier | Auto-fixes linting errors and formats staged JS/TS files |
| `learnix-client/src/**/*.{css,scss,md}` | Prettier | Formats staged styling/markdown files |

**Stage 2 — build validation** (always runs once, sequentially, after Stage 1):

| Project | Tool | Effect |
|---|---|---|
| Backend | `dotnet build` | Ensures the solution compiles with no errors |
| Frontend | `npm run type-check` | Checks TypeScript types across the entire frontend (`tsc -b`) |
| Global | `jscpd` | **Code Duplication check:** Ensures the codebase has < 5% duplicated code. Fails the commit if exceeded. |

> **Why are builds in Stage 2 and not inside lint-staged?**
> lint-staged splits large commits into parallel chunks and runs all returned commands simultaneously per chunk. Running `dotnet build` in parallel causes a race condition — multiple MSBuild processes attempt to write to the same `.dll` file at once (`CS2012`). By placing builds after `lint-staged` in `.husky/pre-commit`, they run exactly once, sequentially.

> **Why only changed files in Stage 1?**
> Formatting the entire solution on every commit would take 10–30 seconds. More importantly, it would include formatting changes in hundreds of files you never touched, ruining Git history and causing massive merge conflicts.

### Running checks manually

Use these commands to format, lint, and validate both projects without committing:

**Backend:**
```bash
# Format all C# files
dotnet format Learnix.Backend/Learnix.Backend.slnx

# Build and check for compilation errors
dotnet build Learnix.Backend/Learnix.Backend.slnx
```

**Frontend:**
```bash
# Format all frontend files
npm run format --prefix learnix-client

# Lint all frontend files
npm run lint --prefix learnix-client

# Check TypeScript types (no output = no errors)
npm run type-check --prefix learnix-client
```

**Code Duplication (Root):**
The project enforces a maximum of 5% code duplication across both the frontend and backend. Commits will be blocked if duplication exceeds this threshold. You can manually check the duplication status from the root directory:

```bash
# Check code duplication (outputs to console only)
npm run check:duplication

# Check code duplication AND generate an HTML report (outputs to report/ folder)
# Use this when you need to clearly see which exact lines are duplicated.
npm run check:duplication:report
```

### First-time setup

After cloning the repository, two separate `npm install` calls are required — they are independent `package.json` files and do not share dependencies:

```bash
# 1. Install Git hooks (Husky + lint-staged) — run from the repository root
npm install

# 2. Install frontend dependencies — required to run, build, and lint the frontend
cd learnix-client && npm install
```

> [!NOTE]
> Both installs are one-time per machine. After that, formatting runs silently on every `git commit`.

### Skipping the hook (emergency only)

If you need to commit without formatting (not recommended):

```bash
git commit --no-verify -m "your message"
```
