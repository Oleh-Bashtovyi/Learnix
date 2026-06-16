# Learnix — Azure Deployment Guide (Azure Portal UI)

> Manual first-time deployment walkthrough using the Azure Portal UI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  
> **Estimated time:** 2–3 hours for first-time setup

For deployment via Azure CLI, see [AZURE_DEPLOY_CLI.md](./AZURE_DEPLOY_CLI.md).

---

## Prerequisites

1. Login to the [Azure Portal](https://portal.azure.com/).
2. You still need Docker installed locally to build and push the API image, and `.NET Core CLI` to run EF Core database migrations.

---

## Resource naming convention

| Resource | Name used in this guide |
|---|---|
| Resource Group | `learnix-rg` |
| Location | `West Europe` (or `North Europe`) |
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

1. In the Azure Portal, search for **Resource groups** and click **Create**.
2. **Subscription:** Choose your subscription.
3. **Resource group:** Enter `learnix-rg`.
4. **Region:** Select `West Europe` or `Poland Central`.
5. Click **Review + create**, then **Create**.

---

## Step 2 — Azure Container Registry (ACR)

1. Search for **Container registries** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Registry name:** `learnixacr`.
4. **Location:** `West Europe`.
5. **Pricing plan:** `Basic`.
6. Click **Review + create**, then **Create**.
7. Once created, go to the resource. Under **Settings**, click **Access keys**.
8. **Enable Admin user**. Note down the **Login server**, **Username**, and **password**.

---

## Step 3 — Azure Database for PostgreSQL (Flexible Server)

1. Search for **Azure Database for PostgreSQL flexible servers** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Server name:** `learnix-postgres`.
4. **Region:** `West Europe`.
5. **PostgreSQL version:** `16`.
6. **Workload type:** Development.
7. **Compute + storage:** Click *Configure server*. Choose **Burstable**, **B1ms**, 32 GB storage. Save.
8. **Authentication:** Create an admin user `learnixadmin` and a strong password `YourStrongPassword123!`.
9. Click **Next: Networking**.
10. **Firewall rules:** 
    - Check **Allow public access from any Azure service within Azure to this server**.
    - Click **Add current client IP address** (this allows you to run migrations from your PC).
11. Click **Review + create**, then **Create** (takes ~3-5 mins).
12. Once created, go to the server, click **Databases** on the left menu, and add a database named `learnix`.

**Connection string format for later:**
```
Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true
```

---

## Step 4 — Azure Cosmos DB (MongoDB API)

1. Search for **Azure Cosmos DB** and click **Create**.
2. Select **Azure Cosmos DB for MongoDB** and click **Create**.
3. Select **Request Unit (RU)** architecture (or vCore if you prefer).
4. **Resource group:** `learnix-rg`.
5. **Account name:** `learnix-cosmos`.
6. **Location:** `West Europe`.
7. **Capacity mode:** Serverless (cheaper for Dev) or Provisioned.
8. Click **Review + create**, then **Create** (takes ~5-10 mins).
9. Once created, click **Data Explorer** and click **New Database**. Name it `learnix`.
10. Click **Connection strings** on the left menu. Copy the **PRIMARY CONNECTION STRING**.

---

## Step 5 — Azure Cache for Redis

1. Search for **Azure Cache for Redis** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **DNS name:** `learnix-redis`.
4. **Location:** `West Europe`.
5. **Pricing tier:** `Basic C0`.
6. Click **Review + create**, then **Create**.
7. Once created, go to **Access keys** on the left menu to find your Primary connection string.

---

## Step 6 — Azure Blob Storage

1. Search for **Storage accounts** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Storage account name:** `learnixstorage`.
4. **Region:** `West Europe`.
5. **Performance:** Standard. **Redundancy:** LRS.
6. Click **Review + create**, then **Create**.
7. Once created, click **Containers** under Data storage.
8. Create the following containers:
   - `avatars` (Set public access level to **Blob**)
   - `course-covers` (Set public access level to **Blob**)
   - `course-videos` (Set public access level to **Blob**)
   - `certificates` (Set public access level to **Private**)
9. Go to **Access keys** on the left menu and copy your **Connection string**.

---

## Step 7 — Run Database Migrations

Before deploying the API, apply EF migrations from your local machine using the CLI.

```powershell
# Open a terminal in the Learnix.Backend directory.
# Set the Azure connection string temporarily:
$env:ConnectionStrings__Postgres = "Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true"
$env:ASPNETCORE_ENVIRONMENT = "Production"

dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
```

---

## Step 8 — Build & Push API Docker Image

You need to push your API image to the ACR created in Step 2.

```powershell
# Login using the Admin username and password you copied from ACR Access Keys
docker login learnixacr.azurecr.io -u learnixacr -p <YOUR_ACR_PASSWORD>

# Build the image from the solution root
docker build -t learnixacr.azurecr.io/learnix-api:latest -f Learnix.Backend/Dockerfile ./Learnix.Backend

# Push the image
docker push learnixacr.azurecr.io/learnix-api:latest
```

---

## Step 9 — Create Container App (API)

1. Search for **Container Apps** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Container App name:** `learnix-api`.
4. **Region:** `West Europe`.
5. Under Container Apps Environment, click **Create new**. Name it `learnix-env` and save.
6. Click **Next: Container**.
7. **Use image from:** Azure Container Registry.
8. Select your registry (`learnixacr`), image (`learnix-api`), and tag (`latest`).
9. **CPU and Memory:** 0.5 CPU, 1.0 Gi memory.
10. **Environment variables:** Add all necessary backend variables:
    - `ASPNETCORE_ENVIRONMENT` = `Production`
    - `ASPNETCORE_URLS` = `http://+:8080`
    - `ConnectionStrings__Postgres` = `<YOUR_POSTGRES_CONN_STRING>`
    - `ConnectionStrings__Redis` = `<YOUR_REDIS_CONN_STRING>`
    - `ConnectionStrings__AzureBlobStorage` = `<YOUR_BLOB_CONN_STRING>`
    - `Mongo__ConnectionString` = `<YOUR_COSMOS_CONN_STRING>`
    - `Mongo__DatabaseName` = `learnix`
    - `Jwt__Secret` = `<YOUR_64_CHAR_SECRET>`
    - `Jwt__Issuer` = `Learnix`
    - `Jwt__Audience` = `LearnixClient`
    - `Smtp__Host`, `Smtp__Username`, `Smtp__Password` (from SendGrid)
    - etc. (Refer to the CLI guide for the full list of variables).
11. Click **Next: Ingress**.
12. **Ingress:** Enabled.
13. **Ingress traffic:** Accepting traffic from anywhere.
14. **Target port:** `8080`.
15. Click **Review + create**, then **Create**.
16. Once created, copy the **Application Url** (e.g., `https://learnix-api.xxxx.azurecontainerapps.io`). This is your `VITE_API_URL` for the frontend.

---

## Step 10 — Deploy Frontend to Azure Static Web Apps

1. Before starting, update `.env.production` locally to include your API URL: `VITE_API_URL=https://learnix-api.xxxx.azurecontainerapps.io/api`.
2. Push your code to GitHub.
3. In the Azure Portal, search for **Static Web Apps** and click **Create**.
4. **Resource group:** `learnix-rg`.
5. **Name:** `learnix-frontend`.
6. **Hosting plan:** Free.
7. **Region:** `West Europe`.
8. **Deployment details:** Select **GitHub**. Authenticate and choose your organization, repository, and branch (`main`).
9. **Build Details:** 
    - **Build Presets:** Custom
    - **App location:** `/learnix-client`
    - **Api location:** (leave blank)
    - **Output location:** `dist`
10. Click **Review + create**, then **Create**.
11. Azure will automatically create a GitHub Action in your repo and start building the frontend.
12. Once deployed, copy the **URL** (e.g., `https://proud-pond-xxx.azurestaticapps.net`).

---

## Step 11 — Update CORS and ClientBaseUrl

1. Go back to your Container App (`learnix-api`).
2. Click **Containers** -> **Edit and deploy**.
3. Click on your container image.
4. Add two new environment variables:
   - `Cors__AllowedOrigins__0` = `https://proud-pond-xxx.azurestaticapps.net`
   - `App__ClientBaseUrl` = `https://proud-pond-xxx.azurestaticapps.net`
5. Save and deploy.

Also add the Static Web Apps URL to **Google Cloud Console → OAuth → Authorized JavaScript origins**.

---

## Step 12 — Update API image (subsequent deploys)

When you make changes to the backend:
1. Build and push the new Docker image locally (see Step 8).
2. Go to your Container App (`learnix-api`) in the Portal.
3. Click **Revision management**.
4. Click **Create new revision**.
5. Keep the settings identical, but make sure the image pulls the latest tag, then save to deploy the new revision.
