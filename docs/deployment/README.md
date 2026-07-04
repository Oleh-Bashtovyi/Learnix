# Learnix — Azure Deployment Guide

This directory contains all the instructions needed to deploy the Learnix application to Azure. The infrastructure deployment process is separated into individual modular guides, allowing you to set up each component step-by-step using the Azure Portal UI.

---

## Order of Execution

To successfully deploy the entire system from scratch without encountering dependency errors, you must provision the infrastructure in a specific order. Follow the guides in the `infrastructure/` folder exactly in this sequence:

### Phase 1: Core Foundation
Before creating any services, you need a resource group to hold them.
1. [**Azure Resource Group**](./infrastructure/AZURE_RESOURCE_GROUP.md) — Create the logical container for all your resources.

### Phase 2: Databases & Storage
Set up the persistence layer so the backend has databases and blob storage to connect to.
2. [**PostgreSQL Database**](./infrastructure/POSTGRESS_DATABASE.md) — The primary relational database (Azure Database for PostgreSQL Flexible Server).
3. [**Redis Cache**](./infrastructure/REDIS_CACHE.md) — The distributed cache for sessions and temporary data (Azure Cache for Redis).
4. [**MongoDB**](./infrastructure/MONGO_DATABASE.md) — The document database for course content and chat history (Azure Cosmos DB for MongoDB).
5. [**Azure Blob Storage**](./infrastructure/AZURE_BLOB_STORAGE.md) — Alternatively, use the [**Terraform Guide**](./TERRAFORM_GUIDE.md) to automatically deploy storage and lifecycle policies via code.

### Phase 3: Application Hosting
Create the compute and registry resources. Do not run the GitHub Actions CI/CD pipeline during this phase.
6. [**Container Registry**](./infrastructure/CONTAINER_REGISTRY.md) — Create the registry (Docker Hub or ACR) to store the backend Docker image.
7. [**Azure Container App (API)**](./infrastructure/AZURE_CONTAINER_APP.md) — Create the backend hosting environment.
8. [**Azure Static Web Apps (Frontend)**](./infrastructure/AZURE_STATIC_WEB_APPS.md) — Create the frontend hosting environment.

---

## Resolving the "Chicken and Egg" Problem

When deploying the full stack, you will encounter a cyclic dependency:
- The **Frontend** needs the Backend URL during the build (`VITE_API_URL`) to know where to send API requests.
- The **Backend** needs the Frontend URL during deployment (`PROD_ALLOWED_ORIGINS`) to configure CORS security.

To resolve this safely, we separate **infrastructure creation** from **code deployment**:

1. **Provision Empty Resources (Phases 1-3):** Follow all the guides above to create the resources in Azure manually. Azure will immediately assign permanent public URLs to your Container App and Static Web App, even though they don't have your actual code running on them yet.
2. **Collect Variables:** At the top of every guide, there is an `## Environment Variables` section. Collect all these variables as you complete each guide.
3. **Configure GitHub:** Go to your repository on GitHub (**Settings → Secrets and variables → Actions**). Populate all the `Variables` (like URLs and hostnames) and `Secrets` (like connection strings and tokens) that you collected.
4. **Deploy the Code:** Once all Secrets and Variables are safely saved in GitHub, trigger the CI/CD pipeline (`deploy.yml`) by pushing to the `main` branch or running it manually from the Actions tab. The pipeline will automatically fetch the code, build it, run migrations, and inject the correct URLs into both the frontend and backend deployments.

---

## Additional Guides

- [**Terraform Local Guide**](./TERRAFORM_GUIDE.md) — Instructions for manually provisioning the Blob Storage infrastructure via command line instead of the portal.
- [**Manual Operations**](./MANUAL_OPERATIONS.md) — Reference commands for manually running database migrations and pushing Docker images for testing or debugging.

---

## Post-Deployment Checklist

After your CI/CD pipeline completes successfully, don't forget to configure external integrations with your new production URLs:
1. **Google Cloud Console:** Go to your OAuth credentials and add your Static Web App URL (e.g., `https://proud-pond-xxx.azurestaticapps.net`) to the **Authorized JavaScript origins** and **Authorized redirect URIs**.
