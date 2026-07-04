# Postgress database

## Environment Variables

**For Azure PostgreSQL:**
* Secrets: `PROD_POSTGRES_CONN` (Your Azure or Supabase connection string format)

## Azure Database for PostgreSQL (Flexible Server)

> [!WARNING]
> **SKIPPED:** Due to the minimum cost of ~$20/month even for the lowest `B1ms` tier, this step is skipped for pet projects. We highly recommend using **Supabase** (Free Tier) instead. Proceed directly to **Step 3 (Alternative)** below.

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

## Alternative — Supabase PostgreSQL (Free)

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
