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
| Seq | Docker (port 5341) | UI for structured logs (Tracing/Debugging) |
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

## Step 1 — Environment Setup

Both the frontend and backend require an `.env` file to run correctly. 

> [!IMPORTANT]
> You MUST copy the `.env` files **BEFORE** running Docker Compose, as the frontend build process uses `learnix-client/.env` as a BuildKit secret.

### Backend .env variables

```bash
cd Learnix.Backend/Learnix.API
cp .env.example .env
```

**Database connections — no action needed**
These match the Docker Compose credentials exactly. Leave them as-is. MongoDB and Redis connection strings are already set in `appsettings.json` and `appsettings.Development.json` — you do not need to add them to `.env` unless you want to override them.

**JWT — no action needed in development**
`Jwt__Secret` and `Jwt__RefreshTokenSecret` are **not required in the `.env` file for development**. `appsettings.Development.json` already provides dev-only secrets. These secrets are fine for local development, but in production they must be replaced with random 64+ character strings.

> **IMPORTANT:** Out of the box, the project will run with just these defaults, except for **Google Login** and **AI Chat** which require real API keys. 
> 
> 👉 **See [API_KEYS_GUIDE.md](API_KEYS_GUIDE.md)** for exact, step-by-step instructions on how to get Google, Anthropic, and Gemini API keys.

### Frontend .env variables

```bash
cd ../../learnix-client
cp .env.example .env
```

**`VITE_API_URL` — no action needed**
Points to the backend HTTP endpoint (`http://localhost:5000/api`). Leave as-is.

**`VITE_GOOGLE_CLIENT_ID` — optional but recommended**
This is the same **Client ID** you create for the backend (see [API_KEYS_GUIDE.md](API_KEYS_GUIDE.md)). If you leave the dummy value in place, the application will still run, but Google Login will not function.

---

## Step 2 — Start Infrastructure

From the repository root, you have two options:

### Option A: Run everything in Docker (Recommended for quick start)
This approach runs the infrastructure, backend API, and frontend entirely within Docker containers. It automatically applies migrations and seeds demo data.
```bash
docker compose --profile apps up -d
```

### Option B: Run infrastructure only in Docker (Recommended for development)
This starts PostgreSQL, MongoDB, Redis, Azurite (blob storage emulator), and Mailpit. All containers run with persistent volumes so data survives restarts. You will run the backend and frontend locally in subsequent steps.
```bash
docker compose up -d
```

Verify everything is healthy:

```bash
docker compose ps
```

All services should show `healthy` or `running`.

---

## Step 3 — Run Backend Migrations and Start

The backend uses a standalone migrator project (`Learnix.DbMigrator`) that safely initializes databases, blob storage containers, and default system accounts.

```bash
# 1. Apply database migrations and seed system data
# The easiest way is to run the migrator via Docker Compose:
docker compose --profile init up migrator

# Alternatively, you can run it locally using the .NET CLI:
# cd Learnix.Backend
# dotnet run --project Learnix.DbMigrator --launch-profile Development -- --create-blob --seed-demo

# 2. Start the API (HTTP on port 5000, HTTPS on 5001)
cd Learnix.Backend
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

| Service | URL (Local Run) | URL (Docker Run) | Notes |
|---|---|---|---|
| Frontend | http://localhost:5173 | http://localhost:80 | React client |
| Backend (HTTP) | http://localhost:5000 | http://localhost:8080 | API base |
| Backend (HTTPS) | https://localhost:5001 | N/A | API base (HTTPS) |
| Swagger | http://localhost:5000/swagger | http://localhost:8080/swagger | API docs & testing |
| Seq (Logs) | http://localhost:5341 | http://localhost:5341 | View structured logs & traces |
| Mailpit | http://localhost:8025 | http://localhost:8025 | View emails sent by the app |
| Azurite Blob | http://localhost:10000 | http://localhost:10000 | Local blob storage emulator |
| PostgreSQL | localhost:5432 | localhost:5432 | DB: `learnix`, user/pass: `learnix` |
| MongoDB | localhost:27017 | localhost:27017 | user/pass: `learnix` |
| Redis | localhost:6379 | localhost:6379 | No auth in dev |

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

### Azurite (Blob Storage)

**GUI client (Azure Storage Explorer)**

Azurite runs locally and acts as a fully compatible emulator for Azure Blob Storage. To inspect the containers and files (images, videos, etc.) uploaded during development:

1. Download and install **[Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)**.
2. Open the app and open the **Connect** dialog (plug icon on the left sidebar).
3. Select **Local storage emulator** and click Next.
4. Give it a Display name (e.g. `Azurite Local`) and make sure the **Blobs port** is set to `10000`. Leave the rest as defaults and click Next -> Connect.
5. In the left panel, expand **Local & Attached** -> **Storage Accounts** -> **(Emulator - Default Ports)** -> **Blob Containers**.
6. You will see all containers created by the seeder (e.g., `avatars`, `course-covers`, etc.). You can double-click them to view, upload, or delete files.

> **Note:** The blob containers are only created locally during the seeder execution if you pass the `--create-blob` flag. In production, containers are managed by Terraform.

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
