# Azure Container App (API)

## Environment Variables

By completing this guide, you will determine the following values. Configure them in your GitHub repository (**Settings → Secrets and variables → Actions**):

* **Variables:**
  - `CONTAINER_APP_NAME` (e.g., `learnix-api`)
  - `CONTAINER_APP_RG` (e.g., `learnix-rg`)
  - `VITE_API_URL` (The Application Url generated at the end of this guide)

---

## Create Container App

Search for **Container Apps** in the Azure Portal and click **Create**. 

### 1. Basics
* **Project details:**
  * **Subscription:** Select your active subscription (e.g., `Azure subscription 1`)
  * **Resource group:** `learnix-rg`
* **Container app name:** `learnix-api`
* **Optimize for Azure Functions:** Leave unchecked
* **Deployment source:** `Container image`
* **Container Apps environment:**
  * **Show environments in all regions:** Leave unchecked
  * **Region:** Select your region (e.g., `Poland Central`)
  * **Container Apps environment:** Select or create an environment (e.g., `(new) managedEnvironment-learnixrg-8e87 (learnix-rg)`)

Click **Next : Container >**

### 2. Container
* **Use quickstart image:** Leave unchecked
* **Container details:**
  * **Name:** `learnix-api`
* **Image source:** `Docker Hub or other registries`
* **Image type:** `Private`
* **Registry login server:** `docker.io`
* **Registry authentication:**
  * **Authentication type:** `Secret-based`
  * **Registry user name:** Enter your Docker Hub username
  * **Registry password:** Enter your Docker Hub Personal Access Token (or password)
* **Image and tag:** Enter your full image name (e.g., `yourusername/learnix-api:latest`)
* **Command override:** Leave empty
* **Arguments override:** Leave empty
* **Development stack-specific features:**
  * **Development stack:** `.NET`
* **Container resource allocation:**
  * **Workload profile:** `Consumption - Up to 4 vCPUs, 8 Gib memory`
  * **CPU and memory:** `0.5 CPU cores, 1 Gi memory`
* **Environment variables:**
  * **Name:** Leave empty
  * **Value:** Leave empty
  * *(Note: Leave these completely empty during manual creation. The GitHub Actions pipeline will automatically inject all required environment variables and secrets during deployment).*

Click **Next : Ingress >**

### 3. Ingress
* **Application ingress settings:**
  * **Ingress:** `Enabled` (Checked)
  * **Ingress traffic:** Select `Accepting traffic from anywhere` *(Required for your frontend Static Web App to communicate with this API over the internet).*
  * **Ingress type:** `HTTP`
  * **Transport:** `Auto`
  * **Insecure connections:** Leave unchecked
  * **Target port:** `8080`
  * **Session affinity:** Leave unchecked
  * **Additional TCP ports:** Leave empty / collapsed

Click **Review + create**, wait for validation, then click **Create**.

### 4. Post-Creation
Once the deployment is complete, go to the resource.

#### 1. Save Application URL
Copy the **Application Url** (e.g., `https://learnix-api.xxxx.azurecontainerapps.io`). 
Save this value in your GitHub Variables as `VITE_API_URL`.

#### 2. Configure Scaling (Cost Control)
By default, Azure Container Apps sets Max replicas to 10, which can cause unexpected costs if traffic spikes. Limit it to 1 for this deployment.
1. In the left-hand menu under **Application**, click **Scale**.
2. Under **Scale rule settings**, configure exactly as follows:
   - **Min replicas:** `0`
   - **Max replicas:** `1`
   - **Cooldown period:** `300`
   - **Polling interval:** `30`
3. Under **Scale rules**, verify there is a default rule:
   - **Name:** `http-scaler`
   - **Type:** `HTTP scaling`
4. Save your changes (this may create a new revision).
