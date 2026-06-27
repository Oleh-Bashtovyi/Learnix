# Learnix — Azure Provisioning Guide (Azure Portal UI)

> Manual first-time provisioning walkthrough using the Azure Portal UI.  
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

A resource group is a logical container for all Learnix Azure resources.
It lets you manage, monitor, and delete everything (ACR, Container Apps, PostgreSQL, etc.)
as a single unit.

1. In the Azure Portal, search for **Resource groups** and click **Create**.

2. **Subscription:** choose your active subscription.

3. **Resource group name:** `learnix-rg`
   — one group for all Learnix resources keeps billing and cleanup simple.

4. **Region:** `Poland Central` (or `West Europe`)
   — determines where the group's metadata is stored. Does not restrict where
   individual resources inside it can be deployed, but keeping everything
   in one region avoids cross-region egress costs.

5. Click **Review + create**, then **Create**.

---

## Step 1.1 — Set Up Cost Budget (Recommended)

To protect yourself from unexpected charges, it's highly recommended to set up a budget and billing alerts for your new resource group.

1. Once the resource group is created, go to the **`learnix-rg`** resource group page in the Azure Portal.
2. In the left-hand menu, scroll down to the **Cost Management** section and click **Budgets**.
3. Click **+ Add** to open the "Create a budget" screen.
4. Fill in the **Budget Details**:
   - **Name:** Enter a unique name (e.g., `learnix-budget`).
   - **Reset period:** `Monthly`.
   - **Creation / Expiration date:** Leave the default values.
5. Under **Budget Amount**, enter your desired monthly threshold (e.g., `5` or `10`).
6. Click **Next** to proceed to the **Set alerts** tab.
7. Under **Alert conditions**, configure when you want to be notified:
   - **Type:** Select `Actual cost` (or `Forecasted cost` to be warned before you hit the limit).
   - **% of budget:** Enter a percentage like `50`, `90`, or `100`.
   - **Action group:** Leave as `None`.
8. Under **Alert recipients (email)**, enter your email address to receive notifications.
9. Leave **Language preference** as `Default` and click **Create**.

> [!NOTE]
> Hitting the budget limit will *not* automatically turn off your services. It will simply send you an email alert so you can log in and manually delete or scale down resources if necessary.

---

## Step 2 — Azure Container Registry (ACR)

> [!WARNING]
> **SKIPPED / DEPRECATED:** ACR costs ~$5/month even if empty. For pet projects and 100% free deployments, we highly recommend using **Docker Hub** instead. Proceed directly to **Step 2 (Alternative)** below.

<details>
<summary>Click here if you still want to deploy Azure Container Registry (Original Instructions)</summary>

ACR is a private Docker image registry. GitHub Actions will push built images here,
and Container Apps will pull from here to run your backend.

1. Search for **Container registries** and click **Create**.

2. **Resource group:** `learnix-rg`
   — keeps all Learnix resources grouped together.

3. **Registry name:** `learnixacr`
   — must be globally unique; becomes your login server: `learnixacr.azurecr.io`.
   If taken, try `learnixacrdev` or similar.

4. **Location:** `Poland Central` (or `West Europe`)
   — keep the same region as your other resources to avoid cross-region egress costs.

5. **Domain name label scope:** `Unsecure`
   — affects registry domain name format only; irrelevant for portfolio use.

6. **Registry domain name:** leave empty
   — auto-filled based on registry name.

7. **Use availability zones:** leave unchecked
   — only available on Premium plan; not needed for portfolio.

8. **Pricing plan:** `Basic`
   — ~$5/month flat. Sufficient storage and throughput for portfolio CI/CD.

9. **Role assignment permissions mode:** `RBAC Registry Permissions`
   — standard mode; controls who can push/pull images via Azure roles.

10. Click **Review + create**, then **Create**.

11. Once created, go to the resource → **Settings → Access keys**.
    Enable **Admin user** and save:
    - **Login server** (e.g. `learnixacr.azurecr.io`)
    - **Username**
    - **Password**
    
    These will be stored as GitHub Actions secrets for the CI/CD pipeline.
</details>

---

## Step 2 (Alternative) — Docker Hub (Free)

Docker Hub provides 1 free private repository and unlimited public repositories.

1. Go to [Docker Hub](https://hub.docker.com/) and create a free account if you don't have one.
2. Log in and click **Create repository**.
3. **Name:** `learnix-api`
4. **Visibility:** `Public` (or `Private` if you prefer). Public is easier because Azure Container Apps can pull it without configuring authentication.
5. Click **Create**.
6. Note down your Docker Hub username (e.g., `yourusername`) and the repository name. Your full image name will be `yourusername/learnix-api`.

---

## Step 3 — Azure Database for PostgreSQL (Flexible Server)

> [!WARNING]
> **SKIPPED / DEPRECATED:** Due to the minimum cost of ~$20/month even for the lowest `B1ms` tier, this step is skipped for pet projects. We highly recommend using **Supabase** (Free Tier) instead. Proceed directly to **Step 3 (Alternative)** below.

<details>
<summary>Click here if you still want to deploy Azure PostgreSQL (Original Instructions)</summary>

PostgreSQL Flexible Server is the primary relational database for the Learnix backend.

1. Search for **Azure Database for PostgreSQL flexible servers** and click **Create**.

2. **Resource group:** `learnix-rg`
   — keeps the database logically grouped with the rest of the project.

3. **Server name:** `learnix-postgres`
   — must be globally unique across Azure. Try adding a suffix if it's taken (e.g., `learnix-postgres-dev`).

4. **Region:** `Poland Central` (or `West Europe`)
   — select the exact same region as your Resource Group to ensure low latency and avoid cross-region data transfer costs.

5. **PostgreSQL version:** `16`
   — the latest stable version supported by the application.

6. **Workload type:** `Development`
   — pre-selects cost-effective defaults.

7. **Compute + storage:** Click *Configure server* to open the detailed configuration pane:
   - **Cluster options:** Select `Server` (Elastic cluster is not needed).
   - **Compute tier:** Select `Burstable (1-20 vCores)` — intended for development use outside of a production environment.
   - **Compute size:** Select `Standard_B1ms (1 vCore, 2 GiB memory, 640 max iops)` — this is the most cost-effective option for development and pet projects. (Azure might default to B2s, but B1ms is perfectly sufficient).
   - **Storage type:** Select `Premium SSD`.
   - **Storage size:** `32 GiB`.
   - **Performance tier:** `P4 (120 iops)`.
   - **Storage autogrow:** Uncheck the box (leave it disabled).
   - **Zonal resiliency:** Select `Disabled (99.9% SLA)`.
   - **Backup retention period (in days):** Set the slider to `7`.
   - Click **Save** at the bottom of the pane.

8. **Authentication:** 
   - **Authentication method:** Select `PostgreSQL authentication only` (or `PostgreSQL and Microsoft Entra authentication`).
   - Create an admin user `learnixadmin` and a strong password `YourStrongPassword123!`. Keep these safe!

9. Click **Next: Networking**.

10. **Firewall rules:** 
    - Check **Allow public access from any Azure service within Azure to this server**
      — *Crucial:* this allows your Azure Container App (backend) to communicate with this database. Without it, the backend will fail to connect.
    - Click **Add current client IP address**
      — *Crucial:* this allows your home/work PC to connect to the database to run Entity Framework migrations.

11. Click **Review + create**, then **Create** (provisioning takes ~3-5 minutes).

12. Once created, go to the resource, click **Databases** on the left menu, and click **Add**. Name it `learnix` and save.

**Connection string format for later:**
```
Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true
```
</details>

---

## Step 3 (Alternative) — Supabase PostgreSQL (Free)

1. Go to [Supabase.com](https://supabase.com/) and create a free account.
2. Create a new organization (required for first-time users):
   - **Name:** Enter your name (e.g., `My-Organisation's Org`).
   - **Type:** Select `Personal`.
   - **Plan:** Select `Free - $0/month`.
   - Click **Create organization**.
3. Click **New Project** and select your newly created organization.
4. **Name:** `learnix`
5. **Database Password:** Generate a strong password (e.g., `YourStrongPassword123!`) and save it securely.
6. **Region:** Select `Central EU (Frankfurt)` for the lowest latency to your Azure resources in West Europe / Poland Central.
7. **Security** (Leave defaults as shown in the UI):
   - **Enable Data API:** Checked.
   - **Automatically expose new tables:** Checked.
   - **Enable automatic RLS:** Unchecked.
     *(Note: Since Learnix uses its own custom .NET backend API to access the database directly, these Supabase-specific Data API features aren't used, so the defaults are perfectly fine).*
8. Click **Create new project** (takes a few minutes).
9. Once the dashboard loads, click the green **Connect** button at the top of the screen.
10. In the modal that opens, select the **Direct (Connection string)** tab.
11. Under **Connection Method**, select **Session pooler**.
    *(Important: Do NOT select "Direct connection". Supabase direct connections use IPv6 by default, which may block your home PC or Azure Container Apps from connecting. "Session pooler" provides a compatible IPv4 connection on port `5432` which is required for Entity Framework migrations).*
12. Under **Type**, ensure **URI** is selected.
13. Scroll down and copy your **Connection string**. It will look something like this:

**Supabase connection string format for later:**
```
postgresql://postgres.yourprojectref:[YOUR-PASSWORD]@aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require&Trust Server Certificate=true
```
*(Remember to manually replace `[YOUR-PASSWORD]` in the copied string with the actual password you created earlier).*

> [!TIP]
> **SSL Encryption:** We appended `?sslmode=require&Trust Server Certificate=true` to the end of the connection string. This ensures your data is encrypted in transit (`sslmode=require`), while bypassing the need to manually install Supabase's CA certificates on your server or local machine (`Trust Server Certificate=true`). This provides the ideal balance of security and simplicity.

---

## Step 4 — Azure Cosmos DB for MongoDB (vCore)

Azure Cosmos DB for MongoDB (vCore) acts as a fully managed MongoDB cluster.
Learnix uses it for flexible-schema data: AI chat sessions.

Azure DocumentDB stores Learnix's flexible-schema data: AI chat sessions and course reviews.
Your existing MongoDB.Driver code connects to it without any changes.

1. Search for **Azure Cosmos DB** and click **Create**.

2. On the **Recommended APIs** tab, click **Create** under
   **Azure DocumentDB (with MongoDB compatibility)**.

3. On the **Choose Architecture** screen, click **Create** under **Azure DocumentDB**
   (the recommended option on the left).
   — Do NOT select "Request unit (RU)" — it has limited analytics support
   and a more complex billing model. Azure DocumentDB is MongoDB-compatible
   and uses straightforward vCore + storage pricing.

4. **Subscription:** choose your active subscription.

5. **Resource group:** `learnix-rg`.

6. **Cluster name:** `learnix-cosmos`
   — must be globally unique across Azure as it forms part of your cluster's DNS name. Try adding a suffix if it's already taken (e.g., `learnix-cosmos-dev`).

7. **Free tier:** check the box to enable the Free tier (Limit: one Free Tier cluster per Azure subscription).

8. **Location:** select an available European region such as `(Europe) Norway East` or the nearest to your primary resources.

9. **Cluster tier:** will automatically be set to `Shards: 1, no high availability (HA)` with 32 GiB storage.

10. **MongoDB version:** leave as default (e.g., `8.0`).

11. **Administrator account:** The username and password are auto-generated. **Important:** Copy these credentials and save them securely, as you won't be able to view them after creation. You will need them for your connection string.

12. Click **Next: Networking**.
    - **Connectivity method:** Select `Public access (allowed IP addresses)`.
    - **Public access:** Ensure `Allow public access to this resource through the internet using a public IP address` is checked. This is required because our simpler deployment architecture relies on public endpoints rather than complex, expensive private Virtual Networks (VNets).
    - **Firewall rules:** Check `Allow public access from Azure services and resources within Azure to this cluster` so your backend Container App can communicate with the database. You can also click `+ Add current client IP address` if you need to access it from your local PC.

13. The remaining tabs (**Global distribution**, **Encryption**, **Tags**) can be left with their default settings (e.g., `Service-managed key` for Encryption, and empty Tags).

14. Click **Review + create**, then **Create** (takes ~5–10 minutes).

15. Once created, go to the resource → **Connection strings**.
    Copy the **Primary connection string** — it follows standard MongoDB URI format:
    ```
    mongodb+srv://<admin-username>:<password>@...
    ```
    *(Remember to replace `<admin-username>` and `<password>` placeholders with the auto-generated credentials you copied earlier)*.
    This goes into your app's `Mongo__ConnectionString` environment variable.

---

## Step 4 (Alternative) — MongoDB Atlas (Free)

If you have already used your Cosmos DB Free Tier quota, or simply prefer an alternative, you can use a MongoDB Atlas M0 Free Cluster. It provides 512 MB of storage, which is perfectly sufficient for the Learnix application (chat sessions and reviews).

1. Go to [MongoDB Atlas](https://www.mongodb.com/cloud/atlas/register) and sign up for a free account.
2. Create a new Organization and Project if prompted.
3. Click **Create** to deploy a new database cluster.
4. **Cluster Type:** Select **M0 Free**.
5. **Provider & Region:** 
   - Choose **Azure** as the cloud provider (or AWS/GCP if Azure isn't available for M0 in your area).
   - Select a region close to your other Azure resources, such as `Netherlands (europe-west4)` or `Frankfurt`.
6. **Cluster Name:** `learnix-cluster` (or leave as default `Cluster0`).
7. Click **Create Deployment**.
8. **Security Quickstart:**
   - **How would you like to authenticate your connection?** Select `Username and Password`.
   - Create a database user (e.g., `learnixadmin`) and a strong password (or use the auto-generated one). **Save this password securely**. Click **Create Database User**.
   - **Where would you like to connect from?** Select `My Local Environment`.
   - Under IP Access List, click **Allow Access from Anywhere** (which adds `0.0.0.0/0`). This is necessary because Azure Container Apps use dynamic outbound IP addresses, and you'll need access from your local machine as well. Click **Add IP Address**.
   - Click **Finish and Close**.
9. Once your cluster is ready, click **Connect** на сторінці огляду кластера.
10. Select **Drivers**.
11. Under **Driver**, select `C# / .NET` and ensure the latest version is selected.
12. Copy the **Connection String**. It will look something like this:
    ```
    mongodb+srv://learnixadmin:<password>@learnix-cluster.xxxxx.mongodb.net/?retryWrites=true&w=majority&appName=learnix-cluster
    ```
    *(Remember to replace `<password>` with the actual password you created in step 8).*
    This string goes into your app's `Mongo__ConnectionString` environment variable.


---

## Step 5 — Azure Cache for Redis

> [!WARNING]
> **NOT FREE:** Azure Cache for Redis does not have a free tier. The `Basic C0` tier costs approximately ~$16/month. For a 100% free portfolio deployment, skip this step and use **Upstash Redis** (see Step 5 Alternative below).

<details>
<summary>Click here if you still want to deploy Azure Cache for Redis</summary>

1. Search for **Azure Cache for Redis** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **DNS name:** `learnix-redis`.
4. **Location:** `West Europe`.
5. **Pricing tier:** `Basic C0`.
6. Click **Review + create**, then **Create**.
7. Once created, go to **Access keys** on the left menu to find your Primary connection string.

</details>

---

## Step 5 (Alternative) — Upstash Redis (Free)

Upstash provides a fully managed, serverless Redis database with a generous free tier (10,000 requests per day), perfect for pet projects and portfolios.

1. Go to [Upstash](https://upstash.com/) and create a free account (no credit card required).
2. In the console, click **Create Database** under the Redis section.
3. **Name:** `learnix-redis`.
4. **Type:** `Regional`.
5. **Region:** Select a region close to your Azure resources (e.g., `eu-central-1` / Frankfurt).
6. **Enable TLS (SSL):** Check this box for secure communication.
7. Click **Create**.
8. Once created, scroll down to the **Connect to your database** section on the database details page.
9. Look for your **Endpoint** and **Password**.
10. Construct your connection string. It will look like this:
    ```
    <endpoint>:<port>,password=<password>,ssl=True,abortConnect=False
    ```
    *(Example: `enjoyable-dog-12345.upstash.io:32541,password=YourSuperSecretPassword,ssl=True,abortConnect=False`)*.
    This string goes into your app's `ConnectionStrings__Redis` environment variable.

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
   - `temp-uploads` (Set public access level to **Private**)
   - `avatars` (Set public access level to **Blob**)
   - `course-covers` (Set public access level to **Blob**)
   - `course-videos` (Set public access level to **Blob**)
   - `certificates` (Set public access level to **Private**)
9. Go to **Access keys** on the left menu and copy your **Connection string**.

### Step 6.1 — Set Up Lifecycle Policy for Temporary Uploads

To ensure unconfirmed uploads (e.g., interrupted file transfers) do not consume storage indefinitely, configure an automatic cleanup policy:

1. In your Storage Account menu, scroll down to **Data management** and select **Lifecycle management**.
2. Click **Add a rule**.
3. **Rule name:** `CleanupTempUploads`.
4. **Rule scope:** Select **Limit blobs with filters**.
5. **Blob type:** Select **Block blobs**.
6. **Blob subtype:** Select **Base blobs**.
7. Click **Next** to go to the **Base blobs** tab.
8. Set the condition: **If base blobs were Last modified more than 1 days ago**, then **Delete the blob**.
9. Click **Next** to go to the **Filter set** tab.
10. Under **Blob prefix**, type: `temp-uploads/`
11. Click **Add** to save the rule. This will automatically delete any orphaned blobs older than 24 hours in the temporary container.
