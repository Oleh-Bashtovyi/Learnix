# External Services & API Keys Guide

This document provides exact instructions for obtaining and configuring third-party API credentials required to run Learnix locally.

Once you have these values, place them in the corresponding `.env` files (see [DEV_SETUP.md](DEV_SETUP.md) for environment setup).

---

## 1. Google OAuth — action required for Google Sign-In to work

The app uses Google's **token-based flow** (not a redirect flow): the frontend shows the Google Sign-In button, Google returns an `id_token` in JavaScript, the frontend sends that token to `POST /api/auth/google`, and the backend validates it. No callback URL is involved.

**How to get the credentials:**

1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Click the project dropdown at the top → **New Project**
   - Project name: `learnix-dev` (or any name)
   - Click **Create**
3. In the left sidebar → **APIs & Services** → **OAuth consent screen**
   - Click **Get started** (if prompted, or go to **Clients** directly)
   - **App name**: `Learnix`
   - **User support email**: select your email from the dropdown
   - Click **Next**
   - **Audience**: select **External**
   - **Contact Information**: enter your email again
   - Click **Next** → **Create**
4. In the left sidebar → **Clients** → **+ Create client** (or **Create Credentials** → **OAuth 2.0 Client ID**)
   - **Application type**: Web application
   - **Name**: `Learnix Dev`
   - **Authorized JavaScript origins** — click **Add URI** and add:
     ```
     http://localhost:5173
     ```
   - **Authorized redirect URIs** — leave empty (not used in this flow)
   - Click **Create**
5. A modal appears with **Client ID** and **Client Secret** — copy both immediately.

```env
Google__ClientId=123456789-xxxxxxxxxxxx.apps.googleusercontent.com
Google__ClientSecret=GOCSPX-xxxxxxxxxxxxxxxxxxxx
```

> `Google__ClientId` is not secret — it is also used in the frontend `.env`. `Google__ClientSecret` is secret and must only be in the backend `.env`, never committed to the repository.

> **Skipping Google OAuth**: if you don't set these, the app starts normally but Google Sign-In buttons will fail. You can still use email/password registration and login.

---

## 2. Anthropic API Key — action required for AI Chat (Anthropic provider)

The AI Chat feature supports two providers: Anthropic (Claude) and Gemini. The active provider is set in `appsettings.json` under `AiChat.Provider` (`"Anthropic"` by default).

**How to get the key:**

1. Go to [console.anthropic.com](https://console.anthropic.com)
2. Sign up or log in
3. In the left sidebar → **API Keys** → **Create Key**
   - Give it a name: `learnix-dev`
   - Click **Create Key**
4. Copy the key — it starts with `sk-ant-...` and is shown **only once**

```env
Anthropic__ApiKey=sk-ant-api03-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

> **Skipping this**: if not set, AI Chat will return errors when the Anthropic provider is active. The rest of the app works normally.

---

## 3. Gemini API Key — action required for AI Chat (Gemini provider)

**How to get the key:**

1. Go to [aistudio.google.com](https://aistudio.google.com)
2. Sign in with your Google account
3. Click **Get API key** in the top-left panel (or navigate to the API key section)
4. Click **Create API key**
   - Select an existing Google Cloud project or create a new one
   - Click **Create API key in existing project**
5. Copy the key — it starts with `AIza...`

```env
Gemini__ApiKey=AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

> You only need one of Anthropic or Gemini configured. To switch providers, change `AiChat.Provider` in `appsettings.Development.json` to `"Gemini"`.

> **Skipping this**: only matters if `AiChat.Provider` is set to `"Gemini"`.

---

## 4. Stripe — not used

The project uses a **mock payment flow** instead of real Stripe integration (see `docs/backend/decisions/INFRA.md` ADR-018). `Stripe__SecretKey` is removed from `.env.example`. No action needed.

---

## 5. Azure Blob Storage — no action needed in development

```env
AZURE_BLOB_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEEmK/H4JQ3I0r4DiwUcMu4XV8U9b4uMpHfVL7pXbbKw5T9o3yXzRkEqQ/SD5EQ==;BlobEndpoint=http://localhost:10000/devstoreaccount1;
```

This connects to the **Azurite** emulator running in Docker. The account key is the well-known public Azurite dev key — it is not a secret. Leave this value exactly as-is.

`appsettings.Development.json` already overrides `AzureBlobStorage` to `UseDevelopmentStorage=true` which achieves the same result. The `.env` value is a fallback explicit connection string.

> **Note on Infrastructure Provisioning:** For local development, Blob containers are created automatically by the `Learnix.DbMigrator` project on startup. You do not need to run Terraform locally against Azurite. Terraform is used exclusively for deploying the Production/Staging environments in Azure cloud.

---

## 6. Email (SMTP)і

**Development (Local): No action needed.**
```env
Smtp__Password=
```
In development, `appsettings.Development.json` points SMTP to **Mailpit** running in Docker (`localhost:1025`). No password is needed — Mailpit accepts all email without authentication. To see emails sent by the app (confirmations, notifications), open **[http://localhost:8025](http://localhost:8025)** in your browser.

**Production (Real Email Sending via Gmail):**
To send real emails (e.g. for user registration or password reset) using a standard Gmail account:

1. Go to your [Google Account Manage page](https://myaccount.google.com/).
2. Navigate to **Security** on the left menu.
3. Under "How you sign in to Google", enable **2-Step Verification** (required for App Passwords).
4. After enabling 2-Step Verification, search for **App passwords** in the top search bar of your Google Account.
5. Create a new App Password (you can name it "Learnix"). Google will generate a 16-character password (e.g., `abcd efgh ijkl mnop`).
6. Remove the spaces from this password and use it in your configuration.

Configure PROD_SMTP_PASSWORD in GitHub Secrets, and the rest of the values (PROD_SMTP_HOST, PROD_SMTP_PORT, etc.) in GitHub Variables (Settings → Secrets and variables → Actions). 
*(Примітка: оскільки у вас налаштовано CI/CD через `deploy.yml`, GitHub Actions при кожному деплої буде перезаписувати змінні середовища в Azure Container App значеннями з ваших GitHub Secrets. Тому додавати їх напряму в порталі Azure немає сенсу — вони затруться при наступному релізі).*

```env
PROD_SMTP_HOST=smtp.gmail.com
PROD_SMTP_PORT=587
PROD_SMTP_USERNAME=your-email@gmail.com
PROD_SMTP_PASSWORD=abcdefghijklmnop
PROD_SMTP_SENDER_EMAIL=your-email@gmail.com
PROD_SMTP_SENDER_NAME=Learnix
```

---

## 7. Azure Deployment Credentials — action required for GitHub Actions CI/CD

These credentials are NOT used in your local `.env` file. They are required by the `.github/workflows/deploy.yml` pipeline to authenticate with Azure and deploy the application. 

You must add these values to **GitHub Repository Settings → Secrets and variables → Actions → Secrets**.

### AZURE_CREDENTIALS
This is a JSON object representing a Service Principal that gives GitHub Actions permission to manage your Azure resources.

**How to get the credentials:**
1. Open the [Azure Portal](https://portal.azure.com/) and launch the **Cloud Shell** (the terminal icon in the top right header).
2. Ensure you are using **Bash**.
3. Run the following command to get your Subscription ID:
   ```bash
   az account show --query id -o tsv
   ```

   Run the following command to create the Service Principal (replace {your-subscription-id} and {your-resource-group-name} with your actual values):
   ```bash
   az ad sp create-for-rbac --name "GitHubActionsDeploy" --role contributor --scopes /subscriptions/{your-subscription-id}/resourceGroups/{your-resource-group-name} --json-auth
   ```

   The command will output a JSON object. Copy the entire JSON block (from { to }) and save it in GitHub Secrets as AZURE_CREDENTIALS.

