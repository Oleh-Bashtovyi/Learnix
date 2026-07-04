# Azure Blob Storage (Manual Provisioning)

## Environment Variables

By completing this guide, you will determine the following values. Configure them in your GitHub repository (**Settings → Secrets and variables → Actions**):

* **Secrets:**
  - `PROD_BLOB_CONNECTION_STRING` (The connection string obtained at the end of this guide. *Note: If you use the automated Terraform pipeline, you do not need to set this manually, as Terraform handles it automatically*).

---

## Create Storage Account

Search for **Storage accounts** in the Azure Portal and click **Create**. Follow the tabs below to configure the resource correctly.

### 1. Basics
* **Project details:**
  * **Subscription:** Select your active subscription (e.g., `Azure subscription 1`)
  * **Resource group:** `learnix-rg`
* **Instance details:**
  * **Storage account name:** Enter a globally unique name (lowercase, no spaces, e.g., `learnixstorage123`)
  * **Region:** `Poland Central` (or your preferred region)
  * **Primary service:** `Azure Blob Storage or Azure Data Lake Storage` *(Note: This is a single, combined option in the Azure dropdown. Select this exact text)*
  * **Performance:** `Standard: Recommended for most scenarios` *(Why? Standard is highly cost-effective and perfectly suited for serving web assets like images. Premium is significantly more expensive and only needed for ultra-low latency disk I/O).*
  * **Redundancy:** `Locally redundant storage (LRS)` *(Why? LRS is the most affordable option. It replicates data three times within a single data center, providing sufficient safety without the high cost of geo-replication).*

Click **Next : Advanced >**

### 2. Advanced
* **Azure Blob Storage:**
  * **Access tier:** `Hot` *(Why? The Hot tier is optimized for data that is accessed frequently, such as course thumbnails and user avatars that load every time a user opens the app).*
  * *(Leave all other checkboxes like Hierarchical namespace unchecked)*

Click **Next : Networking >**

### 3. Networking
* **Public access:**
  * **Public network access:** `Enable from all networks` *(Why? Your frontend application and end-users need to download images directly via their public URLs over the internet).*
* **Network routing:**
  * **Routing preference:** `Microsoft network routing`

Click **Next : Data protection >**

### 4. Data protection
* **Recovery:**
  * **Enable soft delete for blobs:** Checked (7 days)
  * **Enable soft delete for containers:** Checked (7 days)
  *(Note: This is Azure's default recommendation to prevent accidental data loss).*

Click **Next : Security >**

### 5. Security
* **Security:**
  * **Require secure transfer for REST API operations:** Checked
  * **Allow enabling anonymous access on individual containers:** Checked
    > [!CAUTION]
    > **Important Security Setting:** Azure now disables this option by default for new accounts. However, for Learnix to work, you **MUST CHECK** this box. If you leave it unchecked, the frontend will not be able to load public images (like avatars and course covers) and will return a `403 Forbidden` error.
  * **Enable storage account key access:** Checked
  * **Minimum TLS version:** `Version 1.2`

Click **Next : Encryption >**

### 6. Encryption
* **Encryption type:** `Microsoft-managed keys (MMK)`
* **Enable support for customer-managed keys:** `Blobs and files only`
* **Enable infrastructure encryption:** Unchecked

Click **Review + create**, wait for validation, then click **Create**.

---

## Post-Creation Steps

Once created, go to the resource.

### 1. Create Containers
1. Under **Data storage** in the left menu, click **Containers**.
2. For each container below, click **+ Container** and apply these base settings:
   - Expand the **Advanced** section:
     - **Encryption scope:** `Select from existing account scopes`
     - **Use this encryption scope for all blobs in the container:** Unchecked
     - **Enable version-level immutability support:** Unchecked
3. Create the following containers, paying strict attention to the **Anonymous access level**:
   - `temp-uploads` (Access level: **Private (no anonymous access)**)
   - `avatars` (Access level: **Blob (anonymous read access for blobs only)**)
   - `course-covers` (Access level: **Blob (anonymous read access for blobs only)**)
   - `course-videos` (Access level: **Blob (anonymous read access for blobs only)**)
   - `category-images` (Access level: **Blob (anonymous read access for blobs only)**)
   - `certificates` (Access level: **Private (no anonymous access)**)

### 2. Set Up Lifecycle Policy for Temporary Uploads
To ensure unconfirmed uploads (e.g., interrupted file transfers) do not consume storage indefinitely:
1. In the left menu, scroll down to **Data management** and select **Lifecycle management**.
2. Click **Add a rule**.
3. **Rule name:** `CleanupTempUploads`.
4. **Rule scope:** Select **Limit blobs with filters**.
5. **Blob type:** Select **Block blobs**.
6. **Blob subtype:** Select **Base blobs**.
7. Click **Next** to go to the **Base blobs** tab.
8. Set the condition: **If base blobs were Last modified more than 1 days ago**, then **Delete the blob**.
9. Click **Next** to go to the **Filter set** tab.
10. Under **Blob prefix**, type: `temp-uploads/`
11. Click **Add** to save the rule.

### 3. Get Connection String
1. Go to **Security + networking** -> **Access keys** on the left menu.
2. Click **Show** next to `key1` Connection string.
3. Copy it and save it as your `PROD_BLOB_CONNECTION_STRING` secret (only if you are doing manual deployment without Terraform).
