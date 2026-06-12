# Learnix — Local Development Setup Guide

This guide walks you through getting a fully working dev environment from a fresh clone. It covers every `.env` variable: what it is, whether you need to change it, and exactly where to get the value.

---

## Contents

1. [Stack Overview](#stack-overview)
2. [Prerequisites](#prerequisites)
3. [Step 1 — Start Infrastructure](#step-1--start-infrastructure)
4. [Step 2 — Backend Environment](#step-2--backend-environment)
   - [2.1 Copy the template](#21-copy-the-template)
   - [2.2 Variable Reference](#22-variable-reference)
   - [2.3 Summary: what actually needs values](#23-summary-what-actually-needs-values)
5. [Step 3 — Run Backend Migrations and Start](#step-3--run-backend-migrations-and-start)
6. [Step 4 — Frontend Environment](#step-4--frontend-environment)
   - [4.1 Copy the template](#41-copy-the-template)
   - [4.2 Variable Reference](#42-variable-reference)
   - [4.3 Install dependencies and start](#43-install-dependencies-and-start)
7. [Seeded Accounts](#seeded-accounts)
8. [Service URLs](#service-urls)
9. [Inspecting Databases](#inspecting-databases)
   - [PostgreSQL](#postgresql)
   - [MongoDB](#mongodb)

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

## Step 2 — Backend Environment

### 2.1 Copy the template

```bash
cd Learnix.Backend/Learnix.API
cp .env.example .env
```

### 2.2 Variable Reference

Below is every variable in `.env`, whether it needs action, and how to obtain it.

---

#### Database connections — no action needed

```env
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=learnix;Username=learnix;Password=learnix
```

These match the Docker Compose credentials exactly. Leave them as-is.

MongoDB and Redis connection strings are already set in `appsettings.json` and `appsettings.Development.json` — you do not need to add them to `.env` unless you want to override them.

---

#### JWT — no action needed in development

```env
Jwt__Issuer=learnix
Jwt__Audience=learnix-api
```

`Jwt__Secret` is **not required in the `.env` file for development**. `appsettings.Development.json` already provides a dev-only secret:

```
dev-only-secret-please-override-in-production-at-least-thirty-two-bytes-long-x9k2
```

This secret is fine for local development. **In production it must be replaced** with a random 64+ character string. You can generate one with:

```bash
# macOS / Linux
openssl rand -base64 64

# Windows PowerShell
[Convert]::ToBase64String((1..64 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

---

#### Seed Admin — no action needed in development

```env
SeedAdmin__Email=admin@learnix.dev
SeedAdmin__Password=Admin123!
```

On first startup the app creates an Admin account with these credentials if no Admin exists. `appsettings.Development.json` already has these values — the `.env` entry is only needed if you want different credentials.

A seeded Instructor account is also created automatically:
- Email: `instructor@learnix.dev`
- Password: `Instructor123!`

---

#### Google OAuth — action required for Google Sign-In to work

```env
Google__ClientId=
Google__ClientSecret=
```

The app uses Google's **token-based flow** (not a redirect flow): the frontend shows the Google Sign-In button, Google returns an `id_token` in JavaScript, the frontend sends that token to `POST /api/auth/google`, and the backend validates it. No callback URL is involved.

**How to get the credentials:**

1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Click the project dropdown at the top → **New Project**
   - Project name: `learnix-dev` (or any name)
   - Click **Create**
3. In the left sidebar → **APIs & Services** → **OAuth consent screen**
   - Click **Get started** (if prompted, or go to **Clients** directly)
   - **App name**: `Learnix`
   - **User support email**: select your email from the dropdown
   - Click **Next**
   - **Audience**: select **External**
   - **Contact Information**: enter your email again
   - Click **Next** → **Create**
4. In the left sidebar → **Clients** → **+ Create client** (or **Create Credentials** → **OAuth 2.0 Client ID**)
   - **Application type**: Web application
   - **Name**: `Learnix Dev`
   - **Authorized JavaScript origins** — click **Add URI** and add:
     ```
     http://localhost:5173
     ```
   - **Authorized redirect URIs** — leave empty (not used in this flow)
   - Click **Create**
5. A modal appears with **Client ID** and **Client Secret** — copy both immediately.

```env
Google__ClientId=123456789-xxxxxxxxxxxx.apps.googleusercontent.com
Google__ClientSecret=GOCSPX-xxxxxxxxxxxxxxxxxxxx
```

> `Google__ClientId` is not secret — it is also used in the frontend `.env`. `Google__ClientSecret` is secret and must only be in the backend `.env`, never committed to the repository.

> **Skipping Google OAuth**: if you don't set these, the app starts normally but Google Sign-In buttons will fail. You can still use email/password registration and login.

---

#### Anthropic API Key — action required for AI Chat (Anthropic provider)

```env
Anthropic__ApiKey=
```

The AI Chat feature supports two providers: Anthropic (Claude) and Gemini. The active provider is set in `appsettings.json` under `AiChat.Provider` (`"Anthropic"` by default).

**How to get the key:**

1. Go to [console.anthropic.com](https://console.anthropic.com)
2. Sign up or log in
3. In the left sidebar → **API Keys** → **Create Key**
   - Give it a name: `learnix-dev`
   - Click **Create Key**
4. Copy the key — it starts with `sk-ant-...` and is shown **only once**

```env
Anthropic__ApiKey=sk-ant-api03-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

> **Skipping this**: if not set, AI Chat will return errors when the Anthropic provider is active. The rest of the app works normally.

---

#### Gemini API Key — action required for AI Chat (Gemini provider)

```env
Gemini__ApiKey=
```

**How to get the key:**

1. Go to [aistudio.google.com](https://aistudio.google.com)
2. Sign in with your Google account
3. Click **Get API key** in the top-left panel (or navigate to the API key section)
4. Click **Create API key**
   - Select an existing Google Cloud project or create a new one
   - Click **Create API key in existing project**
5. Copy the key — it starts with `AIza...`

```env
Gemini__ApiKey=AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

> You only need one of Anthropic or Gemini configured. To switch providers, change `AiChat.Provider` in `appsettings.Development.json` to `"Gemini"`.

> **Skipping this**: only matters if `AiChat.Provider` is set to `"Gemini"`.

---

#### Stripe — not used

The project uses a **mock payment flow** instead of real Stripe integration (see `docs/DECISIONS_INFRA.md` ADR-018). `Stripe__SecretKey` is removed from `.env.example`. No action needed.

---

#### Azure Blob Storage — no action needed in development

```env
AZURE_BLOB_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEEmK/H4JQ3I0r4DiwUcMu4XV8U9b4uMpHfVL7pXbbKw5T9o3yXzRkEqQ/SD5EQ==;BlobEndpoint=http://localhost:10000/devstoreaccount1;
```

This connects to the **Azurite** emulator running in Docker. The account key is the well-known public Azurite dev key — it is not a secret. Leave this value exactly as-is.

`appsettings.Development.json` already overrides `AzureBlobStorage` to `UseDevelopmentStorage=true` which achieves the same result. The `.env` value is a fallback explicit connection string.

---

#### Email (SMTP) — no action needed in development

```env
Smtp__Password=
```

In development, `appsettings.Development.json` points SMTP to **Mailpit** running in Docker (`localhost:1025`). No password is needed — Mailpit accepts all email without authentication.

To see emails sent by the app (confirmations, notifications), open **[http://localhost:8025](http://localhost:8025)** in your browser — that is the Mailpit web UI.

You do not need a real SMTP password or SendGrid account for local development.

---

#### Azure Service Bus — skip in development

```env
# AzureServiceBus__ConnectionString=
```

This is commented out in `.env.example` and is not used in development. Leave it commented out.

---

### 2.3 Summary: what actually needs values

| Variable | Required for dev? | Notes |
|---|---|---|
| `ConnectionStrings__Postgres` | Pre-filled | Matches Docker defaults |
| `Jwt__Issuer` / `Jwt__Audience` | Pre-filled | Fixed values |
| `Jwt__Secret` | Not needed | Dev default in `appsettings.Development.json` |
| `SeedAdmin__*` | Not needed | Dev defaults already set |
| `Google__ClientId` | **Yes** (for Google login) | From Google Cloud Console |
| `Google__ClientSecret` | **Yes** (for Google login) | From Google Cloud Console |
| `Anthropic__ApiKey` | **Yes** (for AI Chat) | From Anthropic Console |
| `Gemini__ApiKey` | Only if switching provider | From Google AI Studio |
| `Stripe__SecretKey` | Not used | Mock payment flow (ADR-018) |
| `AZURE_BLOB_CONNECTION_STRING` | Pre-filled | Azurite emulator |
| `Smtp__Password` | Not needed | Mailpit needs no auth |
| `AzureServiceBus__ConnectionString` | Not needed | Not used in dev |

---

## Step 3 — Run Backend Migrations and Start

```bash
cd Learnix.Backend

# Apply database migrations
dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API

# Start the API (HTTP on port 5000, HTTPS on 5001)
dotnet run --project Learnix.API
```

Once running, Swagger UI is available at **[https://localhost:5001/swagger](https://localhost:5001/swagger)** (or `http://localhost:5000/swagger`).

On first startup the app seeds:
- Admin account: `admin@learnix.dev` / `Admin123!`
- Instructor account: `instructor@learnix.dev` / `Instructor123!`

---

## Step 4 — Frontend Environment

### 4.1 Copy the template

```bash
cd learnix-client
cp .env.example .env
```

### 4.2 Variable Reference

---

#### `VITE_API_URL` — no action needed

```env
VITE_API_URL=http://localhost:5000/api
```

Points to the backend HTTP endpoint. Leave as-is.

---

#### `VITE_GOOGLE_CLIENT_ID` — same value as backend `Google__ClientId`

```env
VITE_GOOGLE_CLIENT_ID=
```

This is the same **Client ID** you created in Step 2 (Google OAuth). Copy it from the Google Cloud Console Clients page.

```env
VITE_GOOGLE_CLIENT_ID=123456789-xxxxxxxxxxxx.apps.googleusercontent.com
```

> Unlike `Google__ClientSecret`, the Client ID is public by design — it is safe to expose in frontend code and in version control `.env.example` files.

---

---

### 4.3 Install dependencies and start

```bash
cd learnix-client
npm install
npm run dev
```

Frontend runs at **[http://localhost:5173](http://localhost:5173)**.

---

## Seeded Accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@learnix.dev` | `Admin123!` |
| Instructor | `instructor@learnix.dev` | `Instructor123!` |

Register a new account through the UI to get a Student role.

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
