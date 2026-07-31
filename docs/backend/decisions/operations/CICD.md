# Learnix — ADR: CI/CD (GitHub Actions)

> Format: what was decided → why → what alternatives were rejected.
> Updated after each chat where CI/CD architectural decisions were made.

Related files: [INFRA.md](../platform/INFRA.md) · [MIGRATIONS.md](../platform/MIGRATIONS.md) · [ARCHITECTURE.md](../platform/ARCHITECTURE.md)

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

The project has **two workflow files** in `.github/workflows/`:

```
.github/workflows/
├── checks.yml   # "Checks & Validation" — on every push to main and every PR to any branch
└── deploy.yml   # "Deploy to Azure" — on push to main, or manual workflow_dispatch
```

---

## ADR-BACK-CICD-001: One validation workflow, four independent jobs — not one workflow per concern

**Decision:** `checks.yml` is a single workflow file that fans out into four jobs, all running in parallel on their own runner: `backend` (build, test, SonarCloud), `frontend` (format, lint, type-check, build), `duplication` (jscpd plus the doc-drift checks below), and `gitleaks` (secret scanning). It triggers on push to `main` and on pull requests targeting **any** branch — not only `main`/`dev`.

**Why one file:**
- The four jobs share nothing that would justify separate trigger/permission blocks — same `on:`, same runner image, no job depending on another's output. A second file would duplicate that header for no isolation gained.
- A single workflow gives one check-list per PR in the GitHub UI, rather than the results of two unrelated workflow runs a reviewer has to correlate by commit.
- `duplication` and `gitleaks` are cross-cutting — jscpd scans both `learnix-client/src` and `Learnix.Backend`, and secret scanning does not belong to either side. Splitting the file along backend/frontend lines would leave nowhere natural for either job to live without arbitrarily attaching it to one side.

**Why four jobs and not one:**
- **Fast, isolated feedback.** A frontend lint failure does not wait on the backend's SonarCloud analysis, and a reviewer sees exactly which job is red without reading a combined log.
- **Different toolchains.** `backend` needs .NET, Java (for SonarScanner) and Node (SonarJS needs the client's `node_modules` to resolve types); `frontend` needs only Node; `duplication` and `gitleaks` need neither .NET runtime nor a full `npm ci`.

**Alternatives:**
- Separate `backend-ci.yml` / `frontend-ci.yml` files — the project's own earlier structure. Rejected on consolidation: no isolation benefit remained once duplication and secret scanning needed a home that was neither backend nor frontend, and two files meant two copies of the same trigger and branch-protection wiring to keep in sync.
- One workflow, one job — simplest, but a lint failure would block seeing the SonarCloud result and vice versa, and jobs that need different toolchains would all pay for all of them.
- No CI at all, rely solely on pre-commit hooks — hooks are local, can be skipped with `--no-verify`. CI is the authoritative safety net that runs on every push regardless.

**Consequences:**
- Every PR must pass all four jobs before merging (branch protection rules).
- The deploy workflow does not itself depend on `checks.yml` — it triggers on push to `main`, which branch protection already gates on `checks.yml` passing via the PR that landed there.

---

## ADR-BACK-CICD-002: Backend job — format check → SonarScanner-wrapped build, test and coverage

**Decision:** The `backend` job runs from the **repository root**, not `Learnix.Backend/`, and its steps are:
1. `dotnet format Learnix.Backend/Learnix.Backend.slnx --verify-no-changes` — fails first, before anything expensive runs.
2. `dotnet sonarscanner begin` — starts the SonarCloud analysis session, with `sonar.exclusions`, `sonar.coverage.exclusions` and a per-rule `sonar.issue.ignore.multicriteria` list (each with its reason, in the workflow file's own comments) as arguments.
3. `dotnet build Learnix.Backend/Learnix.Backend.slnx --no-restore --configuration Release`.
4. `dotnet test Learnix.Backend/Learnix.Backend.slnx --no-build --configuration Release --collect:"XPlat Code Coverage" --settings Learnix.Backend/coverage.runsettings`.
5. `dotnet sonarscanner end` — uploads the build's findings and the test coverage to SonarCloud.

**Why the repository root and not `Learnix.Backend/`:** SonarScanner's `begin`/`build`/`end` sequence shares one working directory, and that directory is what gets indexed. Running from `Learnix.Backend/` would mean `learnix-client/` is never analysed at all — which is also why the job sets up Node and runs `npm ci` in `learnix-client/` before `begin`: SonarJS needs the client's `node_modules` to resolve types from `tsconfig`, or its type-aware rules go quiet.

**Why format check runs before the build, not after:** a formatting failure is cheap to detect and does not need SonarScanner started, .NET built, or tests run first — failing fast here means a developer who bypassed the pre-commit hook finds out in seconds, not minutes.

**Why `dotnet format` in CI even though there is a pre-commit hook:**
- Pre-commit hooks are local and optional — any developer can skip them with `git commit --no-verify`.
- CI is mandatory and cannot be bypassed. It acts as the final enforcement gate.
- The dual-layer approach (hook for fast local feedback, CI for enforcement) is a standard industry pattern.

**Alternatives:**
- Build in Debug mode — faster, but does not reflect production behavior.
- Skip format check in CI — shifts responsibility entirely to the developer; the codebase style will diverge over time.
- Run SonarScanner from `Learnix.Backend/` — rejected, it would silently exclude the frontend from analysis.

---

## ADR-BACK-CICD-003: Frontend job — install → format check → lint → type-check → build

**Decision:** The `frontend` job runs on `ubuntu-latest` with `./learnix-client` as the working directory. Steps:
1. `npm ci` — clean install from `package-lock.json` (deterministic, ignores `node_modules`).
2. `npm run format:check` — Prettier, read-only.
3. `npm run lint` — ESLint check.
4. `npm run type-check` — TypeScript compiler in `--noEmit` mode (no output files, checks only types).
5. `npm run build` — Vite production build with placeholder environment variables (`VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID`, and `VITE_SITE_URL` — the build fails without it, since canonical URLs, `og:image` and the sitemap all read it).

**Why `npm ci` instead of `npm install`:**
- `npm ci` deletes `node_modules` and installs exactly what `package-lock.json` says. No risk of a slightly-different-version sneaking in. `npm install` can update `package-lock.json` silently.
- On CI runners, `node_modules` doesn't exist anyway, so the performance difference is minimal.
- `npm ci` fails if `package-lock.json` is out of sync with `package.json` — catches developer mistakes.

**Why run a production build in CI (not just lint + type-check):**
- TypeScript in strict mode can pass `type-check` (`tsc --noEmit`) but still fail during Vite's build (e.g., Vite plugins applying additional transforms, Zod schema validation on `import.meta.env`). A build step catches those hidden failures.
- Environment variables (`VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID`, `VITE_SITE_URL`) are injected as placeholders — enough to pass Zod/Vite validation. The real secrets are used only in `deploy.yml`.

**Why the Node.js cache uses `cache-dependency-path: learnix-client/package-lock.json`:**
- The `actions/setup-node@v4` action caches `node_modules` based on a hash of the lock file. If `package-lock.json` doesn't change, the next run restores cached modules in seconds instead of downloading them.
- The path must point to the lock file relative to the repo root (not the working directory), hence `learnix-client/package-lock.json`.

**Alternatives:**
- Skip the build step, only lint + type-check — misses Vite-specific build errors.
- Use `yarn` or `pnpm` — the project standardized on `npm`; switching would require regenerating the lock file and updating all scripts.

**The other two `checks.yml` jobs, briefly, since they belong to neither side:**
- **`duplication`** runs `npm run check:duplication` (jscpd over `learnix-client/src` and `Learnix.Backend`), plus three doc/config-drift guards that are cheap to run alongside it: `check:endpoints` (`docs/backend/ENDPOINTS.md` against the controllers), `check:adr` (every cited `ADR-BACK-*` id resolves to a real heading), and `check:containers` (Terraform's blob container definitions against the code's container names).
- **`gitleaks`** checks out full history (`fetch-depth: 0`) and scans it for committed secrets, config in `.gitleaks.toml`.

---

## ADR-BACK-CICD-004: Deploy pipeline — a change-detection gate, then four conditional jobs

**Decision:** `deploy.yml` triggers on push to `main` or manual `workflow_dispatch`, and runs:

```
changes ─┬─► build-api ─► deploy-backend ─► deploy-frontend ─► update-readme
         └───────────────────────────────────────┘
   (deploy-frontend and update-readme also gate on needs.changes.outputs.frontend)
```

1. **`changes`** — `dorny/paths-filter` reports whether `Learnix.Backend/**`/`infrastructure/**` or `learnix-client/**` changed since the last deploy. Every later job is conditioned on the half it cares about (`workflow_dispatch` always runs everything, bypassing the filter).
2. **`build-api`** — builds the API's Docker image and pushes it to whichever registry `vars.REGISTRY_TYPE` names (Azure Container Registry or Docker Hub — ADR-BACK-CICD-005 covers why both are supported).
3. **`deploy-backend`** — `terraform apply`s the infrastructure, reads the storage connection string back out of the Terraform state, runs `Learnix.DbMigrator` against production, then deploys the image built in step 2 to the Container App.
4. **`deploy-frontend`** — builds the React app against production env vars and uploads it to Azure Static Web Apps.
5. **`update-readme`** — rewrites the live-demo link in `README.md` and pushes the commit, only after a successful frontend deploy.

**Why `changes` and conditional jobs, not four unconditional ones:** a backend-only PR merged to `main` has no reason to rebuild and redeploy a frontend that did not change, and vice versa — this is the "jobs skip unchanged packages" behaviour. `deploy-frontend`'s condition is deliberately not a plain `needs.deploy-backend.result == 'success'`: it must also fire when the backend was skipped outright (no backend changes), which is why the expression checks `always()` and reads `needs.changes.outputs.backend` directly rather than trusting `deploy-backend`'s own skip to propagate.

**Why migrations run inside `deploy-backend` and not a separate job:** the migrator needs the storage connection string Terraform just produced (`ConnectionStrings__AzureBlobStorage`), and `deploy-backend` is also where Terraform runs — splitting migration into its own job would mean passing that value through `needs.*.outputs` for no isolation gained, since both steps already share the same Azure login and the same "backend changed" condition.

**Why Terraform runs here and not as a separate `provision` job:** the Container App the API is deployed to, and the storage account the migrator needs a connection string for, are themselves Terraform-managed. Provisioning has to happen before both, and nothing downstream of it needs to run independently of the deploy it enables.

**Why `update-readme` is its own job, last:** it needs the live URL confirmed reachable (a successful frontend deploy), and it pushes a commit to `main` with a bot identity (`ADMIN_PAT`) — a concern with nothing in common with building or deploying, and one that should not re-run if either deploy step is retried alone via `workflow_dispatch`.

**Alternatives:**
- Run migrations inside the API on startup (`Database.MigrateAsync()`) — rejected (see [ADR-BACK-MIGR-001](../platform/MIGRATIONS.md)). Race conditions on scale-out, schema errors crash the startup, no human review gate.
- Run frontend and API deploys in parallel — safe only if there are no breaking API changes. Rejected for simplicity: a short sequential window is an acceptable trade against the risk of the frontend briefly outrunning an API it depends on.

---

## ADR-BACK-CICD-005: Docker image tagging — SHA + `latest`

**Decision:** The `build-api` job uses `docker/metadata-action` (pinned to a commit SHA, ADR-BACK-CICD-011) to generate two tags for the image, regardless of which registry it is pushed to:
- `type=sha,prefix=,format=short` → e.g., `abc1234` (the short Git commit SHA)
- `type=raw,value=latest` → always `latest`

The deploy job then deploys the **SHA-tagged** image, `learnix-api:$IMAGE_TAG` on whichever registry `vars.REGISTRY_TYPE` selected (ADR-BACK-CICD-007).

**Why SHA tag (not `latest`) for deploying:**
- **Reproducibility:** Each deploy is pinned to an exact commit. Rolling back means deploying a previous SHA tag — no ambiguity about what code is running.
- **`latest` as a convenience alias** — useful for local development and testing (`docker pull learnix-api:latest` always pulls the newest image), but should never be used in a deploy script because it is mutable.
- If `deploy-backend` used `latest` and the deploy failed halfway, retrying would pull whatever image is currently tagged `latest` (could be different), making rollback unreliable.

**How the SHA is passed between jobs:**
- `build-api` declares `outputs.image-tag: ${{ steps.meta.outputs.version }}`.
- `deploy-backend` reads it as `needs.build-api.outputs.image-tag`.
- This is GitHub Actions' inter-job data-passing mechanism — values are strings serialized into the workflow's context.

**Alternatives:**
- Semantic versioning tags (`v1.2.3`) — requires bumping a version file on every commit. Adds friction without clear benefit for a continuously-deployed web app.
- Always `latest` — simpler, but non-reproducible. Rejected.

---

## ADR-BACK-CICD-006: Migrations via a dedicated `Learnix.DbMigrator` project (not `dotnet ef database update`)

**Decision:** The "Run migrations and seeding" step of `deploy-backend` runs `dotnet run --project Learnix.DbMigrator -- --seed-demo` instead of `dotnet ef database update`.

**Why:**
- `dotnet ef database update` requires the EF Core CLI tools to be installed on the runner and a valid project context. It is slower (compiles the entire solution) and less configurable.
- `Learnix.DbMigrator` is a dedicated console project that: (1) applies EF Core migrations programmatically via `DbContext.Database.MigrateAsync()`, and (2) runs the data seeder when `--seed-demo` is passed. This collapses two concerns (migrate + seed) into a single CI step.
- The migrator is a self-contained executable: it reads `ConnectionStrings__Postgres` from the environment, creates a `WebApplication` host (or minimal host), runs migrations, optionally seeds, and exits. This matches the Docker/container model perfectly.

**Alternatives:**
- `dotnet ef database update` — requires EF tools installation, no seeding capability, less portable.
- Running migrations inside the API on startup — rejected (see [ADR-BACK-MIGR-001](../platform/MIGRATIONS.md)).
- SQL script (`dotnet ef migrations script --idempotent`) applied via `psql` — valid approach, but requires `psql` on the runner and management of the script artifact. More moving parts.

---

## ADR-BACK-CICD-007: Production secrets reach the Container App via `az containerapp update`, not the `container-apps-deploy` action

**Decision:** `deploy-backend` authenticates once with `azure/login`, then deploys with `az containerapp update --image ... --replace-env-vars ...` called directly through the Azure CLI, passing every secret and config value as a `KEY="$ENV_VAR"` pair. It does **not** use `azure/container-apps-deploy-action`. There is no `appsettings.Production.json` committed to the repository. The registry itself is selectable: `vars.REGISTRY_TYPE` is `ACR` or `DOCKERHUB`, and `build-api`/`deploy-backend` branch on it to log in to and push toward the matching registry.

**Why the raw CLI instead of the deploy action:** the workflow file's own comment records the reason — `azure/container-apps-deploy-action@v1` has bugs handling environment variable values that contain spaces. `az containerapp update` does not have that failure mode.

**Why:**
- **Security:** secrets never touch the filesystem or the Docker image. The image `build-api` produces is environment-agnostic — the same image could be deployed to staging or production by changing only the environment variables passed at deploy time.
- **ASP.NET Core configuration hierarchy:** environment variables override `appsettings.json` values automatically. The double-underscore `__` separator maps to nested JSON keys: `ConnectionStrings__Postgres` → `ConnectionStrings.Postgres` in code. This is the official .NET convention.
- **No secrets in the image:** the Docker image contains only the compiled code. Anyone with pull access to the registry cannot extract production credentials from it.
- **Secrets never appear in the command line:** every value is bound in the step's `env:` block first, then referenced as `"$VAR"` inside the `run:` script — the injection risk and the on-disk-copy risk this avoids are covered in full in ADR-BACK-CICD-010.

**How secrets flow:**
```
GitHub Secrets/Variables → workflow ${{ secrets.PROD_POSTGRES_CONN }} / ${{ vars.* }}
    → step env: ConnectionStrings__Postgres
        → az containerapp update --replace-env-vars ConnectionStrings__Postgres="$ConnectionStrings__Postgres"
            → ASP.NET Core Configuration system
                → injected into services via IOptions<T> or ConnectionStrings
```

**Which GitHub Secrets and Variables exist today, and what each is for, is not repeated here.** `deploy.yml`'s own header comment is the exhaustive, current list — every secret and variable it reads, one line each, with its purpose. A table in this ADR would be a second copy of that list, and the two would drift the first time either changes; the workflow file cannot silently stop matching its own header.

**Alternatives:**
- `appsettings.Production.json` in the repo — leaks secrets in git history. Rejected.
- `azure/container-apps-deploy-action@v1` — the more "standard" path, and the one this project started with. Rejected after it broke on env var values containing spaces; the raw CLI has no such bug and is no less readable.
- Azure Key Vault managed identity — the most secure approach in production at scale. Not implemented yet due to added complexity; the current secrets approach is sufficient for the current team size.

---

## ADR-BACK-CICD-008: Frontend deployed to Azure Static Web Apps (not Container Apps or Azure Blob)

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

## ADR-BACK-CICD-009: Pre-commit hooks (Husky + lint-staged) as a local complement to CI

**Decision:** Husky is configured at the monorepo root with a `pre-commit` hook that runs `lint-staged`, then — sequentially, outside `lint-staged` — a frontend type-check, a backend `dotnet build`, and `npm run check:duplication`. This is a **local developer tool**, not a CI pipeline component.

**Why the sequential steps run outside `lint-staged`:** `lint-staged` splits a large commit into parallel chunks and runs every chunk's commands at once. A whole-project command like `dotnet build` run that way collides with itself — parallel MSBuild processes fighting over the same output DLL (`CS2012: cannot open file ... for writing`) — and `type-check`/`check:duplication` are whole-project by nature (a `tsconfig` build or a duplication scan cannot be scoped to "just the staged files"). They run once, after `lint-staged` finishes, instead.

**Why both hooks and CI checks:**
- Hooks provide **immediate feedback** — the developer sees formatting issues before the commit is created, with zero network latency.
- CI provides **enforcement** — it cannot be bypassed (without `--no-verify` on the commit itself and then CI still catches it).
- The combination eliminates most formatting CI failures in practice: developers rarely see the CI fail for formatting because the hook already fixed it locally.

**Scope of `lint-staged` itself** (`lint-staged.config.js`, only staged files, not the whole codebase):
- `learnix-client/src/**/*.{ts,tsx,js,jsx}` → ESLint `--fix`, then Prettier — in that order, so a fix ESLint applies is still reformatted if it disagrees with Prettier's style.
- `learnix-client/src/**/*.{css,scss,md}` → Prettier only; nothing to lint.
- `Learnix.Backend/**/*.{cs,csproj}` → `dotnet format --include` scoped to the exact staged file list. `dotnet build` is deliberately not here — see above.

**Alternatives:**
- Husky without lint-staged (format everything) — too slow; reformatting unchanged files adds seconds/minutes.
- Formatting only in CI, no local hooks — developers get feedback only after push; slower iteration loop.
- Running `dotnet build`/`type-check`/`check:duplication` inside `lint-staged` — rejected for the race condition above.

---

## ADR-BACK-CICD-010: Secrets reach a `run:` block through `env:`, never through `${{ }}`

**Decision:** A secret is never interpolated inside a `run:` script. It is bound to an environment variable in the step's `env:` block and the script reads the shell variable, quoted:

```yaml
# Wrong — the secret is pasted into the script text
run: az containerapp registry set --password ${{ secrets.DOCKERHUB_TOKEN }}

# Right — the secret arrives as data, through the environment
env:
  DOCKERHUB_TOKEN: ${{ secrets.DOCKERHUB_TOKEN }}
run: az containerapp registry set --password "$DOCKERHUB_TOKEN"
```

**Why:** `${{ }}` is expanded by the Actions runner **before** the shell exists. The runner takes the `run:` block, substitutes the secret **as text**, writes the resulting file to disk, and only then executes it. Three consequences follow:

- **Injection.** The substitution is textual, so the value is parsed as shell code. A quote, a backtick, a `$(…)` or a newline inside the secret would not corrupt a password — it would *execute*. This is the actual point of the rule: any `${{ }}` inside `run:` is a template splice into a program, and it is safe only for as long as the value happens to contain no metacharacters.
- **A copy on disk.** The expanded script sits on the runner's filesystem for the life of the job, readable by every later step in that job.
- **Leaks through diagnostics.** `set -x`, a shell error trace, anything that echoes the command — and the secret is in the command line. GitHub masks secrets in logs, but masking matches the exact value: base64-encode it, slice it, or otherwise transform it, and the mask no longer applies.

Passing through `env:` has none of these properties. The script contains only `$DOCKERHUB_TOKEN` — the characters `$`, `D`, `O`… — and the shell substitutes it at runtime as ordinary data, so metacharacters inside the value stay data. The secret never becomes part of the script text.

**Scope:** the rule applies to every *interpreted* context, not only `run:` — `script:` in `actions/github-script` and any other input that is evaluated as code. Expanding `${{ }}` into a **value**, including inside `env:` itself, is safe:

```yaml
env:
  ConnectionStrings__Postgres: ${{ secrets.PROD_POSTGRES_CONN }}   # fine — a value, not code
```

**What this does not buy:**
- The variable is visible to every process in the step. `env:` prevents *injection and the on-disk copy*; it does not hide the secret inside the job. There is no way to pass it in and hide it.
- Quote it in the shell — `"$VAR"`, never bare `$VAR` — or a value with spaces splits into several arguments.
- `az containerapp update --set-env-vars …` still passes values as **command-line arguments**, which are visible in the process list. On an ephemeral single-tenant runner this is an accepted risk; removing it entirely would mean a Key Vault reference (see ADR-BACK-CICD-007, alternatives).

**Alternatives:**
- Inline `${{ secrets.* }}` in `run:` — the original form. Works until a secret is rotated to one containing a shell metacharacter, at which point it silently becomes remote code execution on the runner. Rejected.

---

## ADR-BACK-CICD-011: Third-party actions pinned to a commit SHA, GitHub-owned ones to a tag

**Decision:** Every action published by someone other than GitHub is referenced by a full commit SHA, with the human-readable tag kept as a trailing comment:

```yaml
uses: gitleaks/gitleaks-action@ff98106e4c7b2bc287b24eaf42907196329070c7 # v2
```

`actions/checkout`, `actions/setup-node` and `actions/setup-dotnet` stay on tags — they are GitHub-owned and share the platform's trust boundary.

**Why:** a tag is a mutable pointer. Whoever controls the action's repository can move `v2` to a different commit at any time, and every workflow that referenced `@v2` runs the new code on the next build — with the job's secrets in its environment. A SHA is immutable: the code that runs is the code that was reviewed. This is the standard mitigation for the supply-chain attacks that have repeatedly hit the Actions marketplace.

**Cost:** updates are no longer automatic. A pinned action stays pinned until someone bumps it deliberately — which is the point, but it does mean security patches to those actions do not arrive on their own. The trailing `# v2` comment exists so a reader can tell what version a SHA corresponds to without querying the API.

**A note on resolving the SHA:** a release tag is usually an *annotated* tag object, so `git/ref/tags/v2` returns the SHA of the tag, not of the commit it points at. It has to be dereferenced (`git/tags/<sha>` → `.object.sha`) or the pin is meaningless. Do not copy a SHA from a comment or an old README — resolve it: while pinning this repo's actions, the SHA that had been sitting commented next to `Azure/static-web-apps-deploy@v1` turned out to be stale.

**Alternatives:**
- Tags everywhere — convenient, and the default in most examples. It means trusting every future maintainer of every action with the deploy credentials. Rejected.
- Dependabot on top of SHA pins — the right long-term answer: it raises PRs that bump the SHA and the comment together, restoring automatic updates without giving up immutability. Not configured yet.

---

## Summary — CI/CD Pipeline at a Glance

```
push to main / PR to any branch
└── Checks & Validation (checks.yml) — four parallel jobs
    ├── backend:     format check → SonarScanner begin → build (Release) → test + coverage → SonarScanner end
    ├── frontend:    npm ci → format check → lint → type-check → build (with placeholders)
    ├── duplication: jscpd → check:endpoints → check:adr → check:containers
    └── gitleaks:    full-history secret scan

push to main (after PR merged) / workflow_dispatch
└── Deploy to Azure (deploy.yml) — conditional on what changed
    ├── 1. changes:         path-filter — did Learnix.Backend/infrastructure or learnix-client change?
    ├── 2. build-api:       Docker build → push to ACR or Docker Hub (tagged :sha + :latest)
    ├── 3. deploy-backend:  terraform apply → run Learnix.DbMigrator -- --seed-demo → az containerapp update
    ├── 4. deploy-frontend: npm build → Azure Static Web Apps
    └── 5. update-readme:   rewrite the live-demo link, push to main
```
