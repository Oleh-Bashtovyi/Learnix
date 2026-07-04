# Docker Container Registry

## Environment Variables

Depending on your choice of container registry, configure the following in your GitHub repository (**Settings → Secrets and variables → Actions**):

**For Azure Container Registry (ACR):**
* **Variables:**
  - `REGISTRY_TYPE` = `ACR`
  - `ACR_LOGIN_SERVER` (e.g., `learnixacr.azurecr.io`)
  - `ACR_USERNAME`
* **Secrets:**
  - `ACR_PASSWORD` (from **Access keys** in ACR resource)

**For Docker Hub (Alternative):**
* **Variables:**
  - `REGISTRY_TYPE` = `DOCKERHUB`
  - `DOCKERHUB_USERNAME`
* **Secrets:**
  - `DOCKERHUB_TOKEN` (Personal Access Token from Docker Hub)

## Azure Container Registry (ACR)

> [!WARNING]
> **SKIPPED:** ACR costs ~$5/month even if empty. For pet projects and 100% free deployments, we highly recommend using **Docker Hub** instead. Proceed directly to **Step 2 (Alternative)** below.

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

### ACR_LOGIN_SERVER
This is the unique URL address of your container registry. The pipeline uses it to understand which Azure server to send the built Docker image of your API to.

**How to get the value:**
1. Open the Azure Portal and navigate to your **Container Registry** resource.
2. On the **Overview** page, look for the **Login server** field (usually on the right side).
3. Copy this value. It will always be in the format `yourregistryname.azurecr.io` (e.g., `learnixacr.azurecr.io`).

### ACR_PASSWORD
This is the admin password for your Azure Container Registry (ACR), allowing the pipeline to push the backend Docker image.

**How to get the password:**
1. Go to the [Azure Portal](https://portal.azure.com/).
2. Navigate to your **Container Registry** resource.
3. In the left sidebar, under **Settings**, click **Access keys**.
4. Check the box to enable the **Admin user**.
5. Copy the value of **password** (or password2). Save it in GitHub Secrets as **ACR_PASSWORD**.

### ACR_USERNAME
This is the login for the administrator to authorize in this registry.

**How to get the value:**
1. You will find it in the same place where you got ACR_PASSWORD.
2. In the **Container Registry** resource, go to the left menu: **Settings → Access keys**.
3. Make sure the **Admin user** toggle is enabled.
4. Find the **Username** field and copy it.

Technical note: In Azure, the username for ACR by default always matches exactly the name of the registry itself. That is, if your server is named learnixacr.azurecr.io, then the login will simply be learnixacr.

---

## Alternative — Docker Hub (Free)

Docker Hub provides 1 free private repository and unlimited public repositories.

1. Go to [Docker Hub](https://hub.docker.com/) and create a free account if you don't have one.
2. Log in and click **Create repository**.
3. **Name:** `learnix-api`
4. **Visibility:** `Public` (or `Private` if you prefer). Public is easier because Azure Container Apps can pull it without configuring authentication.
5. Click **Create**.
6. Note down your Docker Hub username (e.g., `yourusername`) and the repository name. Your full image name will be `yourusername/learnix-api`.

---

### DOCKERHUB_USERNAME & DOCKERHUB_TOKEN

1. Go to [Docker Hub](https://hub.docker.com/) and log in.
2. Your **DOCKERHUB_USERNAME** is your Docker ID (displayed in the top left corner under the personal account dropdown, e.g., my-organisation). Save this value in GitHub Variables.
3. To generate the token, click your profile picture (top right) → **Account settings**.
4. In the left sidebar, navigate to **Security** → **Personal Access Tokens**.
5. Click **New Access Token**.
6. **Description:** `GitHub Actions Learnix`
7. **Access permissions:** `Read & Write`
8. Click **Generate**. Copy the token immediately (it is only shown once) and save it in GitHub **Secrets** as **`DOCKERHUB_TOKEN`**.

---

## How to Verify Locally

Before triggering the pipeline, confirm your credentials work from your machine.

```bash
docker login learnixacr.azurecr.io --username --password 
```

or for docker hub:

```bash
docker login login docker.io --username --password 
```


Expected output: `Login Succeeded`

---

After a successful pipeline run, verify the image was pushed:

### ACR

```bash
az acr repository list --name learnixacr --output table
az acr repository show-tags --name learnixacr --repository learnix-api --output table
```

### Docker Hub

Open `https://hub.docker.com/r/<DOCKERHUB_USERNAME>/learnix-api/tags` in your browser
and confirm the `latest` and short SHA tags are present.
