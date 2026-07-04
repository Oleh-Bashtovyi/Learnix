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
