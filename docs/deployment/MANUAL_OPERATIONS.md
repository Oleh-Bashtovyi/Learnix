# Manual Deployment Operations

Although the CI/CD pipeline (`deploy.yml`) handles database migrations and Docker image builds automatically, you may occasionally need to perform these steps manually for testing, debugging, or initial setup verification.

---

## 1. Run Database Migrations Manually

If you need to fix a database state or manually verify migrations against your production database, you can run the `DbMigrator` tool directly from your local machine.

> [!NOTE]
> If you used **Supabase (Alternative)**, paste your Supabase connection URI here instead of the Azure one. Ensure connection pooling is off (port 5432) for migrations to succeed.

```powershell
# Open a terminal in the Learnix.Backend directory
cd d:\projects\Learnix\Learnix.Backend

# Set your connection string temporarily (Azure or Supabase):
$env:ConnectionStrings__Postgres = "postgresql://postgres.yourprojectref:YourStrongPassword123!@aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require&Trust Server Certificate=true"

# Ensure the migrator targets the production environment configuration
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Run the migrator project with data seeding enabled
dotnet run --project Learnix.DbMigrator --no-launch-profile -- --seed-demo
```

---

## 2. Build & Push API Docker Image Manually

You can manually build and push your API image to your Container Registry to verify Dockerfile correctness or to bypass the CI/CD pipeline temporarily.

> [!NOTE]
> If you are using **Azure Container Registry (ACR)** instead of Docker Hub, replace `yourusername` with your ACR login server and use `az acr login` instead of `docker login`.

```powershell
# Open a terminal in the solution root
cd d:\projects\Learnix

# Login to Docker Hub (it will prompt for your password/access token)
docker login -u yourusername

# Build the image from the solution root
docker build -t yourusername/learnix-api:latest -f Learnix.Backend/Dockerfile ./Learnix.Backend

# Push the image to the registry
docker push yourusername/learnix-api:latest
```

---

## 3. Handle a Squashed Migration History

When the migration history is collapsed into a new single migration (ADR-BACK-MIGR-004), existing
databases stop being migratable: `__EFMigrationsHistory` names ids the assembly no longer contains, so EF
treats the schema as empty and fails on the first `CREATE TABLE`.

### Default: recreate

```bash
docker compose down -v
docker compose up -d
docker compose --profile init up migrator
```

For a deployed database, drop and recreate the schema, then run the migrator against it (section 1).

### If the data must survive

Only valid when the schema is **already at the last migration the squash collapsed** — check
`SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";` first. If anything is missing,
apply that DDL by hand from git history before continuing, or the rewrite will claim a schema that is not
there.

```sql
BEGIN;
DELETE FROM "__EFMigrationsHistory";
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260731221014_InitialCreate', '8.0.26');
COMMIT;
```

> [!NOTE]
> Both values must match the squashed migration: the id is its file name, and `ProductVersion` is the
> `.HasAnnotation("ProductVersion", …)` at the top of its `.Designer.cs`.
