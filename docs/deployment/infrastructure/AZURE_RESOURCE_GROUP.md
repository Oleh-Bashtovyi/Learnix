# Learnix — Azure Provisioning Guide (Azure Portal UI)

> Manual first-time provisioning walkthrough using the Azure Portal UI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  
> **Estimated time:** 2–3 hours for first-time setup

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
| Storage Account (App Data) | `learnixstorage` |
| Static Web App | `learnix-frontend` |
| Terraform State Resource Group | `terraform-state-rg` |
| Terraform State Storage Account | `tfstatelearnix` |

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

## Step 1.2 — Terraform State Storage (CI/CD Prerequisite)

If you plan to automate your Azure infrastructure deployment using GitHub Actions and Terraform, you must create a remote storage location for the Terraform state file (`terraform.tfstate`). This allows GitHub Actions to track existing resources across multiple workflow runs.

> [!WARNING]
> **CRITICAL:** Do not place these resources in the main `learnix-rg` resource group. The state storage must exist in complete isolation to prevent accidental deletion if you ever destroy or recreate the main application infrastructure.

1. **Create the State Resource Group:**
   - Search for **Resource groups** and click **Create**.
   - **Resource group name:** `terraform-state-rg`
   - **Region:** `West Europe` (or match your primary region).
   - Click **Review + create**, then **Create**.

2. **Create the State Storage Account:**
   Search for **Storage accounts** and click **Create**. Follow these tabs carefully (do **NOT** use the app blob storage guide for this, as Terraform state requires maximum security and no public access):
   
   - **Basics Tab:**
     - **Resource group:** Select the newly created `terraform-state-rg`.
     - **Storage account name:** `tfstatelearnix` (Must be globally unique, 3-24 characters, lowercase only. Add numbers if taken).
     - **Region:** Match your resource group region.
     - **Primary service:** `Azure Blob Storage or Azure Data Lake Storage`
     - **Performance:** `Standard`
     - **Redundancy:** `Locally-redundant storage (LRS)` (Sufficient for state files).
   
   - **Advanced Tab:**
     - **Enable hierarchical namespace:** Unchecked
     - **Enable SFTP:** Unchecked
     - **Allow cross-tenant replication:** Unchecked
     - **Access tier:** `Hot`
     - **Enable network file system v3:** Unchecked
     - **Enable Managed Identity for SMB:** Unchecked
     - **Require Encryption in Transit for SMB:** Checked *(Note: This is checked by default in Azure)*
     - **Require Encryption in Transit for NFS:** Unchecked
   
   - **Networking Tab:**
     - **Public network access:** `Enable`
     - **Public network access scope:** `Enable from all networks` (Required for GitHub Actions to reach it over the internet).
     - **Routing preference:** `Microsoft network routing`
   
   - **Data protection Tab:**
     - **Enable point-in-time restore for containers:** Unchecked
     - **Enable soft delete for blobs:** Checked (7 days) - *Crucial to prevent accidental loss of your Terraform state.*
     - **Enable soft delete for containers:** Checked (7 days)
     - **Enable versioning for blobs:** Unchecked
     - **Enable blob change feed:** Unchecked
     - **Enable version-level immutability support:** Unchecked
     - **Enable soft delete for classic file shares:** Checked (7 days)
   
   - **Security Tab:**
     - **Require secure transfer for REST API operations:** Checked
     - **Allow enabling anonymous access on individual containers:** **UNCHECKED** 
       > [!CAUTION]
       > **SECURITY WARNING:** Unlike the main application storage, this MUST remain UNCHECKED. The Terraform state file contains sensitive infrastructure secrets (like DB passwords) and must NEVER be publicly accessible.
     - **Enable storage account key access:** Checked
     - **Default to Microsoft Entra authorization in the Azure portal:** Unchecked
     - **Minimum TLS version:** `Version 1.2`
     - **Permitted scope for copy operations (Preview):** `From any storage account`
     - **Enable Defender for Storage:** Unchecked
   
   - **Encryption Tab:**
     - **Encryption type:** `Microsoft-managed keys (MMK)`
     - **Enable support for customer-managed keys:** `Blobs and files only`
     - **Enable infrastructure encryption:** Unchecked
   
   Click **Review + create**, wait for validation, then click **Create**. Wait for the deployment to finish.

3. **Create the Blob Container:**
   - Go to your newly created storage account.
   - In the left-hand menu under **Data storage**, click **Containers**.
   - Click **+ Container**.
   - **Name:** `tfstate`
   - **Anonymous access level:** `Private (no anonymous access)` *(Note: This is automatically enforced because we disabled anonymous access in the Security tab).*
   - Expand the **Advanced** section:
     - **Encryption scope:** `Select from existing account scopes`
     - **Use this encryption scope for all blobs in the container:** Unchecked
     - **Enable version-level immutability support:** Unchecked
   - Click **Create**.

Once this is done, your GitHub Actions pipeline will automatically drop the `storage.tfstate` file into this container during its first run.
