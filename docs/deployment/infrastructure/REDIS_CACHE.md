# Redis

## Environment Variables

* Secrets: `PROD_REDIS_CONN` (Your Redis connection string format: `<host>:<port>,password=<password>,ssl=True,abortConnect=False`)

## Azure Cache for Redis

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

## Alternative — Upstash Redis (Free)

Upstash provides a fully managed, serverless Redis database with a generous free tier (10,000 requests per day), perfect for pet projects and portfolios.

1. Go to [Upstash](https://upstash.com/) and create a free account (no credit card required).
2. In the console, click **Create Database** under the Redis section.
3. **Name:** `learnix-redis`.
4. **Primary Region:** Select a region closest to your Azure deployment from the dropdown list, such as Frankfurt, Germany (eu-central-1) or Ireland (eu-west-1).
5. Read Regions: Leave this option empty, as read replicas are only available for paid subscription models.
6. **Eviction:** Leave this toggle disabled by default.
(Note: Turning this on allows Redis to automatically remove old data when the storage limit is reached, which is good for pure caching but can lead to data loss if you store active sessions).
7. Click Next to proceed to the Select a Plan step, and choose the Free Tier.
8. Once the database is successfully created, scroll down to the Connect to your database section on the details page.
9. Locate your host Endpoint and Password credentials.
10. Construct your final .NET connection string using the following format:
    ```
    <endpoint>:<port>,password=<password>,ssl=True,abortConnect=False
    ```
    *(Example: `enjoyable-dog-12345.upstash.io:32541,password=YourSuperSecretPassword,ssl=True,abortConnect=False`)*.
    This string goes into your app's `ConnectionStrings__Redis` environment variable.

---

(Example: enjoyable-dog-12345.upstash.io:32541,password=YourSuperSecretPassword,ssl=True,abortConnect=False).
This string goes into your app's ConnectionStrings__Redis environment variable.
