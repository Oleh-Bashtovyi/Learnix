# Learnix — Azure Deployment Guide (CLI)

> Manual first-time deployment walkthrough using Azure CLI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  
> **Estimated time:** 2–3 hours for first-time setup

For deployment via Azure Portal (UI), see [AZURE_DEPLOY_UI.md](./AZURE_DEPLOY_UI.md).

---

## Prerequisites

Install these tools locally before starting:

```powershell
# Azure CLI
winget install Microsoft.AzureCLI

# Verify
az --version

# Login to your Azure account
az login
```

---

## Resource naming convention

| Resource | Name used in this guide |
|---|---|
| Resource Group | `learnix-rg` |
| Location | `westeurope` (or `northeurope`) |
| Container Registry | `learnixacr` |
| Container Apps Environment | `learnix-env` |
| Container App (API) | `learnix-api` |
| PostgreSQL Server | `learnix-postgres` |
| Cosmos DB (Mongo API) | `learnix-cosmos` |
| Redis Cache | `learnix-redis` |
| Storage Account | `learnixstorage` |
| Static Web App | `learnix-frontend` |

> **Note:** Storage account names must be globally unique, all lowercase, 3–24 chars, no hyphens.  
> Replace `learnixstorage` with something unique if it's taken.

---

## Step 1 — Resource Group

```powershell
az group create `
  --name learnix-rg `
  --location westeurope
```

---

## Step 2 — Azure Container Registry (ACR)

```powershell
az acr create `
  --resource-group learnix-rg `
  --name learnixacr `
  --sku Basic `
  --admin-enabled true

# Save the login server URL (you'll need it later)
az acr show --name learnixacr --query loginServer --output tsv
# → learnixacr.azurecr.io
```

---

## Step 3 — Azure Database for PostgreSQL (Flexible Server)

```powershell
# Create server (~3-5 minutes)
az postgres flexible-server create `
  --resource-group learnix-rg `
  --name learnix-postgres `
  --location westeurope `
  --admin-user learnixadmin `
  --admin-password "YourStrongPassword123!" `
  --sku-name Standard_B1ms `
  --tier Burstable `
  --storage-size 32 `
  --version 16 `
  --yes

# Create the database
az postgres flexible-server db create `
  --resource-group learnix-rg `
  --server-name learnix-postgres `
  --database-name learnix

# Allow Azure services to connect (required for Container Apps)
az postgres flexible-server firewall-rule create `
  --resource-group learnix-rg `
  --name learnix-postgres `
  --rule-name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# (Optional) Allow YOUR current IP for running migrations locally
az postgres flexible-server firewall-rule create `
  --resource-group learnix-rg `
  --name learnix-postgres `
  --rule-name AllowMyIP `
  --start-ip-address (Invoke-RestMethod -Uri 'https://api.ipify.org').Trim() `
  --end-ip-address (Invoke-RestMethod -Uri 'https://api.ipify.org').Trim()
```

**Connection string format:**
```
Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true
```

---

## Step 4 — Azure Cosmos DB (MongoDB API)

```powershell
az cosmosdb create `
  --resource-group learnix-rg `
  --name learnix-cosmos `
  --kind MongoDB `
  --server-version 7.0 `
  --default-consistency-level Session `
  --locations regionName=westeurope failoverPriority=0 isZoneRedundant=false

# Create the database
az cosmosdb mongodb database create `
  --resource-group learnix-rg `
  --account-name learnix-cosmos `
  --name learnix

# Get the connection string
az cosmosdb keys list `
  --resource-group learnix-rg `
  --name learnix-cosmos `
  --type connection-strings `
  --query "connectionStrings[0].connectionString" `
  --output tsv
```

> **Note:** Cosmos DB creation takes ~5-10 minutes.

---

## Step 5 — Azure Cache for Redis

```powershell
az redis create `
  --resource-group learnix-rg `
  --name learnix-redis `
  --location westeurope `
  --sku Basic `
  --vm-size C0

# Get connection string (~5 minutes to provision, then):
$redisHost = az redis show `
  --resource-group learnix-rg `
  --name learnix-redis `
  --query hostName --output tsv

$redisKey = az redis list-keys `
  --resource-group learnix-rg `
  --name learnix-redis `
  --query primaryKey --output tsv

Write-Host "Redis connection string: ${redisHost}:6380,password=${redisKey},ssl=True,abortConnect=False"
```

---

## Step 6 — Azure Blob Storage

```powershell
az storage account create `
  --resource-group learnix-rg `
  --name learnixstorage `
  --location westeurope `
  --sku Standard_LRS `
  --kind StorageV2 `
  --min-tls-version TLS1_2

# Get connection string
az storage account show-connection-string `
  --resource-group learnix-rg `
  --name learnixstorage `
  --output tsv

# Create blob containers (must match appsettings.json BlobStorage section)
$connStr = az storage account show-connection-string `
  --resource-group learnix-rg `
  --name learnixstorage `
  --output tsv

az storage container create --name avatars         --connection-string $connStr --public-access blob
az storage container create --name course-covers   --connection-string $connStr --public-access blob
az storage container create --name course-videos   --connection-string $connStr --public-access blob
az storage container create --name certificates    --connection-string $connStr --public-access none
```

> **Important:** `avatars`, `course-covers`, and `course-videos` use public blob access so images/videos load directly in the browser. `certificates` is private (SAS URL is used for download).

---

## Step 7 — Run Database Migrations

Before deploying the API, apply EF migrations from your local machine.

```powershell
# In Learnix.Backend directory, set the Azure connection string temporarily
$env:ConnectionStrings__Postgres = "Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true"
$env:ASPNETCORE_ENVIRONMENT = "Production"

cd d:\projects\Learnix\Learnix.Backend

dotnet ef database update `
  --project Learnix.Infrastructure `
  --startup-project Learnix.API

# Verify
Write-Host "Migrations applied successfully"
```

> **Note:** Make sure Step 3 firewall rule for your IP is set, otherwise the connection will be refused.

---

## Step 8 — Build & Push API Docker Image

```powershell
cd d:\projects\Learnix

# Log in to ACR
az acr login --name learnixacr

# Build and tag
docker build `
  -t learnixacr.azurecr.io/learnix-api:latest `
  -f Learnix.Backend/Dockerfile `
  ./Learnix.Backend

# Push
docker push learnixacr.azurecr.io/learnix-api:latest
```

---

## Step 9 — Create Container Apps Environment & Deploy API

```powershell
# Install Container Apps extension (first time only)
az extension add --name containerapp --upgrade

# Create the environment
az containerapp env create `
  --resource-group learnix-rg `
  --name learnix-env `
  --location westeurope

# Deploy the API Container App
# Replace all <YOUR_...> placeholders with real values from previous steps.
az containerapp create `
  --resource-group learnix-rg `
  --name learnix-api `
  --environment learnix-env `
  --image learnixacr.azurecr.io/learnix-api:latest `
  --registry-server learnixacr.azurecr.io `
  --registry-username learnixacr `
  --registry-password <ACR_ADMIN_PASSWORD> `
  --target-port 8080 `
  --ingress external `
  --min-replicas 0 `
  --max-replicas 3 `
  --cpu 0.5 `
  --memory 1.0Gi `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production `
    ASPNETCORE_URLS="http://+:8080" `
    "ConnectionStrings__Postgres=<POSTGRES_CONN_STRING>" `
    "ConnectionStrings__Redis=<REDIS_CONN_STRING>" `
    "ConnectionStrings__AzureBlobStorage=<BLOB_CONN_STRING>" `
    "Mongo__ConnectionString=<COSMOS_CONN_STRING>" `
    "Mongo__DatabaseName=learnix" `
    "Jwt__Secret=<YOUR_64_CHAR_SECRET>" `
    "Jwt__Issuer=Learnix" `
    "Jwt__Audience=LearnixClient" `
    "Smtp__Host=smtp.sendgrid.net" `
    "Smtp__Port=587" `
    "Smtp__Username=apikey" `
    "Smtp__Password=<SENDGRID_API_KEY>" `
    "Smtp__SenderEmail=noreply@learnix.dev" `
    "Smtp__SenderName=Learnix" `
    "Smtp__EnableSsl=true" `
    "Google__ClientId=<GOOGLE_CLIENT_ID>" `
    "Google__ClientSecret=<GOOGLE_CLIENT_SECRET>" `
    "AiChat__Provider=Gemini" `
    "Gemini__ApiKey=<GEMINI_API_KEY>" `
    "Anthropic__ApiKey=<ANTHROPIC_API_KEY>" `
    "SeedAdmin__Email=<ADMIN_EMAIL>" `
    "SeedAdmin__Password=<ADMIN_PASSWORD>" `
    "Cors__AllowedOrigins__0=<FRONTEND_URL>" `
    "App__ClientBaseUrl=<FRONTEND_URL>"

# Get the API URL
az containerapp show `
  --resource-group learnix-rg `
  --name learnix-api `
  --query properties.configuration.ingress.fqdn `
  --output tsv
# → learnix-api.XXXX.westeurope.azurecontainerapps.io
```

> **Save this URL** — it's your `VITE_API_URL` for the frontend build.

---

## Step 10 — Deploy Frontend to Azure Static Web Apps

### Option A: Via Azure CLI (manual upload from local build)

```powershell
# 1. Create the Static Web App resource
az staticwebapp create `
  --resource-group learnix-rg `
  --name learnix-frontend `
  --location westeurope `
  --source https://github.com/YOUR_GITHUB_USERNAME/Learnix `
  --branch main `
  --app-location "learnix-client" `
  --output-location "dist" `
  --login-with-github
```

> **Note:** Azure Static Web Apps is tightly integrated with GitHub — it creates a workflow automatically. If you want to deploy manually without GitHub, use the SWA CLI (Option B).

### Option B: Via SWA CLI (manual, no GitHub integration needed)

```powershell
# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# Update learnix-client/.env.production with the real API URL
# VITE_API_URL=https://learnix-api.XXXX.westeurope.azurecontainerapps.io/api

# Build the frontend
cd d:\projects\Learnix\learnix-client
npm ci
npm run build

# Get the deployment token from Azure Portal:
# Static Web Apps → learnix-frontend → Settings → Deployment token
# OR via CLI:
$deployToken = az staticwebapp secrets list `
  --resource-group learnix-rg `
  --name learnix-frontend `
  --query "properties.apiKey" `
  --output tsv

# Deploy
swa deploy ./dist `
  --deployment-token $deployToken `
  --env production
```

---

## Step 11 — Update CORS and ClientBaseUrl

After getting the Static Web Apps URL (format: `https://learnix-frontend.azurestaticapps.net`):

```powershell
az containerapp update `
  --resource-group learnix-rg `
  --name learnix-api `
  --set-env-vars `
    "Cors__AllowedOrigins__0=https://learnix-frontend.azurestaticapps.net" `
    "App__ClientBaseUrl=https://learnix-frontend.azurestaticapps.net"
```

Also add the Static Web Apps URL to **Google Cloud Console → OAuth → Authorized JavaScript origins**.

---

## Step 12 — Update API image (subsequent deploys)

```powershell
# Rebuild and push
docker build -t learnixacr.azurecr.io/learnix-api:latest -f Learnix.Backend/Dockerfile ./Learnix.Backend
docker push learnixacr.azurecr.io/learnix-api:latest

# Trigger Container App to pull the new image
az containerapp update `
  --resource-group learnix-rg `
  --name learnix-api `
  --image learnixacr.azurecr.io/learnix-api:latest
```

---

## Secrets / Config Summary

Generate a JWT secret:

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

| Config key | Where to get |
|---|---|
| `ConnectionStrings__Postgres` | Step 3 output |
| `ConnectionStrings__Redis` | Step 5 output |
| `ConnectionStrings__AzureBlobStorage` | Step 6 — `az storage account show-connection-string` |
| `Mongo__ConnectionString` | Step 4 — `az cosmosdb keys list` |
| `Jwt__Secret` | Generate with PowerShell above |
| `Smtp__Password` | SendGrid → Settings → API Keys → Create |
| `Google__ClientId/Secret` | Google Cloud Console → Credentials |
| `Gemini__ApiKey` | aistudio.google.com |
| `Anthropic__ApiKey` | console.anthropic.com |

---

## GitHub Actions (future)

Three workflow files are prepared and commented out in `.github/workflows/`:

| File | Purpose |
|---|---|
| `backend-ci.yml` | Build + test + format check on every PR |
| `frontend-ci.yml` | Lint + type-check + build on every PR |
| `deploy.yml` | Full deploy pipeline on push to `main` |

To activate: remove the `#` comment prefix from all lines in the desired file and configure the GitHub Secrets listed at the top of `deploy.yml`.

---

## Troubleshooting

### Container App won't start
```powershell
az containerapp logs show --resource-group learnix-rg --name learnix-api --follow
```

### Check Container App revision status
```powershell
az containerapp revision list --resource-group learnix-rg --name learnix-api --output table
```

### SignalR / WebSocket issues
Container Apps support WebSockets by default on the `external` ingress. No extra config needed.

### Rate limiter not getting real IPs
Add the Container Apps ingress IP to `Proxy:TrustedProxies` in the env vars. You can find the IP range in the Azure Portal under the Container Apps environment → Networking.
