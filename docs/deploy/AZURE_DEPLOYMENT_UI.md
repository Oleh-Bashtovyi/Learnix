# Learnix — Azure Deployment Guide (Azure Portal UI)

> Application deployment walkthrough using the Azure Portal UI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  

---

## Step 7 — Run Database Migrations

Before deploying the API, apply EF migrations from your local machine using the CLI.

> [!NOTE]
> If you used **Supabase (Step 3 Alternative)**, paste your Supabase connection URI here instead of the Azure one. Ensure connection pooling is off (port 5432) for migrations to succeed.

```powershell
# Open a terminal in the Learnix.Backend directory.
# Set your connection string temporarily (Azure or Supabase):
$env:ConnectionStrings__Postgres = "postgresql://postgres.yourprojectref:YourStrongPassword123!@aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require&Trust Server Certificate=true"
$env:ASPNETCORE_ENVIRONMENT = "Production"

dotnet run --project Learnix.DbMigrator -- --seed-demo
```

---

## Step 8 — Build & Push API Docker Image

You need to push your API image to the registry created in Step 2.

> [!NOTE]
> If you used **Docker Hub (Step 2 Alternative)**, use the commands below. If you used ACR, replace `yourusername` with your ACR login server.

```powershell
# Login to Docker Hub (it will prompt for your password/access token)
docker login -u yourusername

# Build the image from the solution root
docker build -t yourusername/learnix-api:latest -f Learnix.Backend/Dockerfile ./Learnix.Backend

# Push the image
docker push yourusername/learnix-api:latest
```

---

## Step 9 — Create Container App (API)

1. Search for **Container Apps** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Container App name:** `learnix-api`.
4. **Region:** `West Europe`.
5. Under Container Apps Environment, click **Create new**. Name it `learnix-env` and save.
6. Click **Next: Container**.
7. **Use image from:** Select `Docker Hub or other registries` (if using Docker Hub) or `Azure Container Registry` (if using ACR).
8. **Image details:** 
   - **Image type:** `Public` (or `Private` if your repo is private).
   - *If private*, enter **Registry login server**: `docker.io`, and provide your Docker Hub username and password.
   - **Image and tag:** Enter your full image name, e.g., `yourusername/learnix-api:latest`.
9. **CPU and Memory:** 0.5 CPU, 1.0 Gi memory.
10. **Environment variables:** Add all necessary backend variables:
    - `ASPNETCORE_ENVIRONMENT` = `Production`
    - `ASPNETCORE_URLS` = `http://+:8080`
    - `ConnectionStrings__Postgres` = `<YOUR_POSTGRES_CONN_STRING>` (Use the Supabase URI if you followed Step 3 Alternative)
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
