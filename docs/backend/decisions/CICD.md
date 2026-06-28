# Learnix — ADR: CI/CD (GitHub Actions)

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where CI/CD architectural decisions were made.

Related files: [DECISIONS_INFRA.md](DECISIONS_INFRA.md) · [DECISIONS_MIGRATIONS.md](DECISIONS_MIGRATIONS.md) · [DECISIONS_ARCHITECTURE.md](DECISIONS_ARCHITECTURE.md)

## Status Convention

ADRs are not deleted. If a decision is reviewed — the old ADR is marked `Superseded by ADR-XXX`, the new one — `Supersedes ADR-YYY`. This preserves the history of thought and shows how the architecture evolved.

---

## GitHub Actions

GitHub Actions is GitHub's built-in CI/CD platform. Workflows are YAML files stored in `.github/workflows/`. They are triggered by events (push, pull_request, workflow_dispatch, etc.) and execute a sequence of **jobs**. Each **job** runs on a fresh virtual machine (runner) and consists of **steps**. Steps can run shell commands (`run:`) or reuse pre-built community actions (`uses:`).

**Key concepts:**

| Concept | Meaning |
|---|---|
| `on:` | Trigger definition — what event fires the workflow |
| `jobs:` | Parallel or sequential units of work, each on its own runner |
| `steps:` | Ordered list of actions/commands within a job |
| `needs:` | Declares a job dependency — forces sequential execution |
| `outputs:` | Values a job exposes to downstream jobs via `needs.<job>.outputs.<key>` |
| `secrets.*` | Encrypted key-value pairs stored in GitHub Settings → Secrets. Never logged. |
| `env:` | Environment variables available to all steps in a job (or the whole workflow) |
| `uses:` | Reuses a published Action (e.g., `actions/checkout@v4`) |
| `with:` | Input parameters for a `uses:` action |
| `runs-on:` | The runner OS — `ubuntu-latest` means GitHub-hosted Ubuntu VM |

**Why GitHub Actions and not Jenkins/TeamCity/CircleCI:**
- Native integration with the GitHub repository — no external service to maintain.
- Free for public repos; generous free tier for private (2,000 min/month for free accounts).
- The Actions Marketplace has ready-made actions for Azure, Docker, npm, dotnet, etc.
- Secrets management is built-in (GitHub Settings → Secrets and variables → Actions).
- YAML workflow files live in the repo itself — version-controlled, code-reviewed like any other file.

---

## Workflows in This Project

The project has **three workflow files** in `.github/workflows/`:

```
.github/workflows/
├── backend-ci.yml    # Validates backend on every push/PR to main or dev
├── frontend-ci.yml   # Validates frontend on every push/PR to main or dev
└── deploy.yml        # Full deployment pipeline — triggered only on push to main
```

---

## ADR-CICD-001: Separate CI workflows for backend and frontend

**Decision:** The backend and frontend each have their own CI workflow file (`backend-ci.yml` and `frontend-ci.yml`) that trigger independently on every push or pull request to `main` or `dev`.

**Why:**
- **Fast feedback per team:** A backend change does not wait for the frontend lint/build to finish, and vice versa. Both jobs run in parallel on GitHub's runners.
- **Separate failure domains:** If the frontend TypeScript has a compile error, the backend CI still goes green. This makes it obvious which side is broken.
- **Simpler files:** Each workflow file is ~35–42 lines and easy to read/modify without touching the other.

**Alternatives:**
- One monorepo CI workflow with both jobs — technically equivalent, but harder to read and the failure messages are less clear.
- No CI at all, rely solely on pre-commit hooks — hooks are local, can be skipped with `--no-verify`. CI is the authoritative safety net that runs on every push regardless.

**Consequences:**
- Every PR to `main` or `dev` must pass both CI workflows before merging (configured via branch protection rules).
- The deploy workflow (`deploy.yml`) implicitly assumes both CIs pass, since it only triggers on push to `main` (which requires PR review + passing checks).

---

## ADR-CICD-002: Backend CI — dotnet restore → build → test → format check

**Decision:** The backend CI job (`build-and-test`) runs four sequential steps on `ubuntu-latest`:
1. `dotnet restore` — restores NuGet packages.
2. `dotnet build --no-restore --configuration Release` — compiles in Release mode.
3. `dotnet test --no-build --configuration Release` — runs all xUnit tests.
4. `dotnet format --verify-no-changes` — fails if any file would be reformatted.

The working directory defaults to `./Learnix.Backend` so all commands target the solution file `Learnix.Backend.slnx` without full path prefixes.

**Why:**
- `--no-restore` on build and `--no-build` on test avoid redundant restore/compile steps, making the pipeline faster.
- `Release` configuration matches production — catches issues that only appear with optimizations enabled (e.g., inlining, trimming).
- `dotnet format --verify-no-changes` is the CI counterpart of the local pre-commit hook. It fails the build if a developer bypassed the hook with `--no-verify`, ensuring the repo always has consistently formatted code.

**Why `dotnet format` in CI even though we have a pre-commit hook:**
- Pre-commit hooks are local and optional — any developer can skip them with `git commit --no-verify`.
- CI is mandatory and cannot be bypassed. It acts as the final enforcement gate.
- The dual-layer approach (hook for fast local feedback, CI for enforcement) is a standard industry pattern.

**Alternatives:**
- Build in Debug mode — faster, but does not reflect production behavior.
- Skip format check in CI — shifts responsibility entirely to the developer; the codebase style will diverge over time.

---

## ADR-CICD-003: Frontend CI — install → lint → type-check → build

**Decision:** The frontend CI job (`build-and-lint`) runs on `ubuntu-latest` with `./learnix-client` as the working directory. Steps:
1. `npm ci` — clean install from `package-lock.json` (deterministic, ignores `node_modules`).
2. `npm run lint` — ESLint check.
3. `npm run type-check` — TypeScript compiler in `--noEmit` mode (no output files, checks only types).
4. `npm run build` — Vite production build with placeholder environment variables.

**Why `npm ci` instead of `npm install`:**
- `npm ci` deletes `node_modules` and installs exactly what `package-lock.json` says. No risk of a slightly-different-version sneaking in. `npm install` can update `package-lock.json` silently.
- On CI runners, `node_modules` doesn't exist anyway, so the performance difference is minimal.
- `npm ci` fails if `package-lock.json` is out of sync with `package.json` — catches developer mistakes.

**Why run a production build in CI (not just lint + type-check):**
- TypeScript in strict mode can pass `type-check` (`tsc --noEmit`) but still fail during Vite's build (e.g., Vite plugins applying additional transforms, Zod schema validation on `import.meta.env`). A build step catches those hidden failures.
- Environment variables (`VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID`) are injected as placeholders — enough to pass Zod/Vite validation. The real secrets are used only in `deploy.yml`.

**Why the Node.js cache uses `cache-dependency-path: learnix-client/package-lock.json`:**
- The `actions/setup-node@v4` action caches `node_modules` based on a hash of the lock file. If `package-lock.json` doesn't change, the next run restores cached modules in seconds instead of downloading them.
- The path must point to the lock file relative to the repo root (not the working directory), hence `learnix-client/package-lock.json`.

**Alternatives:**
- Skip the build step, only lint + type-check — misses Vite-specific build errors.
- Use `yarn` or `pnpm` — the project standardized on `npm`; switching would require regenerating the lock file and updating all scripts.

---

## ADR-CICD-004: Deploy pipeline — four sequential jobs with `needs:` dependencies

**Decision:** The deploy workflow (`deploy.yml`) triggers only on push to `main` (or manual `workflow_dispatch`) and executes four jobs in strict order:

```
build-api → migrate-db → deploy-api → deploy-frontend
                ↑                          ↑
         (needs: build-api)      (needs: deploy-api)
```

Jobs:
1. **`build-api`** — Builds a Docker image of the .NET backend and pushes it to Azure Container Registry (ACR).
2. **`migrate-db`** — Runs `dotnet run --project Learnix.DbMigrator -- --seed-demo` to apply EF Core migrations and seed demo data against the production PostgreSQL database.
3. **`deploy-api`** — Deploys the new Docker image to Azure Container Apps, injecting all production secrets as environment variables.
4. **`deploy-frontend`** — Builds the React app with production env vars and deploys the static output to Azure Static Web Apps.

**Why this specific order:**
- Migrations **must** run before the new API version starts, because the new code may expect schema changes that don't exist yet (e.g., a new column). Running migrations first eliminates the window where the new API crashes against the old schema.
- The API **must** be deployed before the frontend, because the frontend references the API URL. While the frontend is mostly static (SPA), deploying the API first ensures that the production URL is live before users start hitting it.
- `build-api` must complete first so that `deploy-api` knows the exact image tag to deploy (communicated via `outputs.image-tag`).

**Alternatives:**
- Run migrations inside the API on startup (`Database.MigrateAsync()`) — rejected (see ADR-INFRA-006). Race conditions on scale-out, schema errors crash the startup, no human review gate.
- Run frontend and API deploys in parallel — safe only if there are no breaking API changes. Rejected for simplicity: sequential deploys with a 2-3 minute total window are acceptable.

---

## ADR-CICD-005: Docker image tagging — SHA + `latest`

**Decision:** The `build-api` job uses `docker/metadata-action@v5` to generate two tags for the ACR image:
- `type=sha,prefix=,format=short` → e.g., `abc1234` (the short Git commit SHA)
- `type=raw,value=latest` → always `latest`

The deploy job then deploys the **SHA-tagged** image: `learnix-api:${{ needs.build-api.outputs.image-tag }}`.

**Why SHA tag (not `latest`) for deploying:**
- **Reproducibility:** Each deploy is pinned to an exact commit. Rolling back means deploying a previous SHA tag — no ambiguity about what code is running.
- **`latest` as a convenience alias** — useful for local development and testing (`docker pull learnix-api:latest` always pulls the newest image), but should never be used in a deploy script because it is mutable.
- If `deploy-api` used `latest` and the deploy failed halfway, retrying would pull whatever image is currently tagged `latest` (could be different), making rollback unreliable.

**How the SHA is passed between jobs:**
- `build-api` declares `outputs.image-tag: ${{ steps.meta.outputs.version }}`.
- `deploy-api` uses `needs.build-api.outputs.image-tag` to reference it.
- This is GitHub Actions' inter-job data-passing mechanism — values are strings serialized into the workflow's context.

**Alternatives:**
- Semantic versioning tags (`v1.2.3`) — requires bumping a version file on every commit. Adds friction without clear benefit for a continuously-deployed web app.
- Always `latest` — simpler, but non-reproducible. Rejected.

---

## ADR-CICD-006: Migrations via a dedicated `Learnix.DbMigrator` project (not `dotnet ef database update`)

**Decision:** The `migrate-db` CI job runs `dotnet run --project Learnix.DbMigrator -- --seed-demo` instead of `dotnet ef database update`.

**Why:**
- `dotnet ef database update` requires the EF Core CLI tools to be installed on the runner and a valid project context. It is slower (compiles the entire solution) and less configurable.
- `Learnix.DbMigrator` is a dedicated console project that: (1) applies EF Core migrations programmatically via `DbContext.Database.MigrateAsync()`, and (2) runs the data seeder when `--seed-demo` is passed. This collapses two concerns (migrate + seed) into a single CI step.
- The migrator is a self-contained executable: it reads `ConnectionStrings__Postgres` from the environment, creates a `WebApplication` host (or minimal host), runs migrations, optionally seeds, and exits. This matches the Docker/container model perfectly.

**Alternatives:**
- `dotnet ef database update` — requires EF tools installation, no seeding capability, less portable.
- Running migrations inside the API on startup — rejected (see ADR-INFRA-006).
- SQL script (`dotnet ef migrations script --idempotent`) applied via `psql` — valid approach, but requires `psql` on the runner and management of the script artifact. More moving parts.

---

## ADR-CICD-007: All production secrets passed as Container App environment variables (not `appsettings.Production.json`)

**Decision:** The `deploy-api` job passes every production secret as an environment variable to the Azure Container App via the `environmentVariables:` block of `azure/container-apps-deploy-action@v1`. There is no `appsettings.Production.json` committed to the repository.

**Why:**
- **Security:** Secrets never touch the filesystem or the Docker image. The image built by `build-api` is environment-agnostic — the same image could be deployed to staging or production by changing only the environment variables.
- **ASP.NET Core configuration hierarchy:** Environment variables override `appsettings.json` values automatically. The double-underscore `__` separator maps to nested JSON keys: `ConnectionStrings__Postgres` → `ConnectionStrings.Postgres` in code. This is the official .NET convention.
- **No secrets in the image:** The Docker image contains only the compiled code. Anyone with access to ACR cannot extract production credentials from the image.

**How secrets flow:**
```
GitHub Secrets (encrypted) → workflow ${{ secrets.PROD_POSTGRES_CONN }}
    → Container App environment variable ConnectionStrings__Postgres
        → ASP.NET Core Configuration system
            → injected into services via IOptions<T> or ConnectionStrings
```

**Required GitHub Secrets (configured in Settings → Secrets and variables → Actions):**

| Secret | Purpose |
|---|---|
| `AZURE_CREDENTIALS` | JSON from `az ad sp create-for-rbac --sdk-auth` — authenticates the workflow with Azure |
| `ACR_LOGIN_SERVER` | ACR hostname, e.g. `learnixacr.azurecr.io` |
| `ACR_USERNAME` / `ACR_PASSWORD` | ACR admin credentials for `docker login` |
| `CONTAINER_APP_NAME` / `CONTAINER_APP_RG` | Azure Container App name and resource group |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token from Azure Portal → Static Web App |
| `PROD_POSTGRES_CONN` | Azure PostgreSQL connection string |
| `PROD_REDIS_CONN` | Azure Redis connection string |
| `PROD_MONGO_CONN` | Azure Cosmos DB (MongoDB API) connection string |
| `PROD_BLOB_CONN` | Azure Blob Storage connection string |
| `PROD_JWT_SECRET` | 64+ char random string for JWT signing |
| `PROD_SMTP_PASSWORD` | SendGrid API key |
| `PROD_GOOGLE_CLIENT_ID` / `PROD_GOOGLE_CLIENT_SECRET` | Google OAuth credentials |
| `PROD_ANTHROPIC_KEY` / `PROD_GEMINI_KEY` | AI provider API keys |
| `PROD_ALLOWED_ORIGINS` | Frontend URL for CORS (e.g. `https://learnix.azurestaticapps.net`) |
| `VITE_API_URL` / `VITE_GOOGLE_CLIENT_ID` | Injected into the React build at compile time |

**Alternatives:**
- `appsettings.Production.json` in the repo — leaks secrets in git history. Rejected.
- Azure Key Vault managed identity — the most secure approach in production at scale. Not implemented yet due to added complexity; the current secrets approach is sufficient for the current team size.

---

## ADR-CICD-008: Frontend deployed to Azure Static Web Apps (not Container Apps or Azure Blob)

**Decision:** The React frontend is deployed via `Azure/static-web-apps-deploy@v1` to Azure Static Web Apps (SWA). The Vite build output (`dist/`) is uploaded directly; `skip_app_build: true` is set because the build already ran in the previous step.

**Why Static Web Apps over Container Apps or Azure Blob Storage + CDN:**
- **SWA handles SPA routing natively:** A single-page application (React Router) requires a fallback rule — any 404 should serve `index.html` so client-side routing works. SWA does this out of the box. Blob Storage requires custom CDN rules for this.
- **Built-in global CDN:** SWA distributes static assets via a global CDN automatically. No additional Azure CDN resource needed.
- **Free SSL and custom domains:** SWA provides HTTPS certificates automatically.
- **Cost:** SWA Free tier is sufficient for the current load.

**Why `skip_app_build: true`:**
- The `install and build` step already ran `npm ci && npm run build` with the correct secrets. Passing `skip_app_build: true` tells the SWA action to upload the pre-built `dist/` folder directly, without re-running the build inside the action. This avoids building twice and ensures the secrets are used exactly once.

**Alternatives:**
- Deploy React as a Docker container to Container Apps — possible but wasteful: a React SPA is 100% static after `npm run build`. Running a Node.js or Nginx container adds cost and complexity for no benefit.
- Azure Blob Storage + CDN — more control, but requires manual SPA routing configuration, CDN setup, and SSL management. SWA is a managed abstraction that handles all of this.

---

## ADR-CICD-009: Pre-commit hooks (Husky + lint-staged) as a local complement to CI

**Decision:** Husky is configured at the monorepo root with a `pre-commit` hook that runs `lint-staged`. `lint-staged` applies `dotnet format` to staged `.cs` files and `prettier --write` to staged frontend files. This is a **local developer tool**, not a CI pipeline component.

**Why both hooks and CI checks:**
- Hooks provide **immediate feedback** — the developer sees formatting issues before the commit is created, with zero network latency.
- CI provides **enforcement** — it cannot be bypassed (without `--no-verify` on the commit itself and then CI still catches it).
- The combination eliminates most formatting CI failures in practice: developers rarely see the CI fail for formatting because the hook already fixed it locally.

**Scope of lint-staged:**
- Only staged files are processed — not the entire codebase. This makes the hook fast even in a large monorepo.
- `.cs` files → `dotnet format` with `--include` pointing to the exact file list.
- `*.ts`, `*.tsx`, `*.js`, `*.jsx`, `*.json`, `*.css`, `*.md` → `prettier --write`.

**Alternatives:**
- Husky without lint-staged (format everything) — too slow; reformatting unchanged files adds seconds/minutes.
- Formatting only in CI, no local hooks — developers get feedback only after push; slower iteration loop.

---

## Summary — CI/CD Pipeline at a Glance

```
PR / push to dev or main
├── Backend CI (backend-ci.yml)
│   └── restore → build (Release) → test → format check
└── Frontend CI (frontend-ci.yml)
    └── npm ci → lint → type-check → build (with placeholders)

push to main (after PR merged)
└── Deploy (deploy.yml)
    ├── 1. build-api: Docker build → push to ACR (tagged :sha + :latest)
    ├── 2. migrate-db: dotnet run Learnix.DbMigrator -- --seed-demo
    ├── 3. deploy-api: Azure Container Apps ← :sha image + all prod secrets
    └── 4. deploy-frontend: npm build → Azure Static Web Apps
```

**Total deploy time (approximate):** ~5–8 minutes end-to-end on GitHub's free runners.
