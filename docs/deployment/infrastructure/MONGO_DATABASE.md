# Environment Variables

**For Azure Cosmos DB for MongoDB:**
* Secrets: `PROD_MONGO_CONN` (Your MongoDB connection string format: `mongodb+srv://...`)

## Azure Cosmos DB for MongoDB (vCore) - Free
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
   Copy the **Global read-write connection string**. It follows the standard MongoDB URI format:
      ```text
      mongodb+srv://<admin-username>:<password>@...
      ```
      *(Replace the `<password>` placeholder with the auto-generated password you copied earlier. The username is usually already included in the string)*.
      This string goes into your Azure secret (`PROD_MONGO_CONN`) and app's `Mongo__ConnectionString` environment variable.

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
