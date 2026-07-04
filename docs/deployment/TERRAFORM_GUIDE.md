# Guide: Running Terraform for Learnix Infrastructure

This document describes how to deploy the Azure Blob Storage infrastructure (with automatic cleanup rules and specific access tiers) manually from your local computer using Terraform.

> [!NOTE] 
> **Automated Deployment via CI/CD**
> The Learnix GitHub Actions pipeline (`deploy.yml`) is already configured to automatically run these Terraform scripts every time you push to the `main` branch. 
> You **only** need this manual guide if you want to deploy a separate testing environment from your own machine or need to troubleshoot infrastructure changes locally.

> [!IMPORTANT] 
> **Local Development vs Cloud**
> Do not use Terraform to configure your local development environment (Azurite emulator). Azurite does not support advanced Azure features like Lifecycle Policies.
> For local development, Blob containers are automatically provisioned via the `Learnix.DbMigrator` project on startup. Terraform should ONLY be used to deploy real cloud infrastructure.

## 1. Prerequisites

1. Install [Terraform](https://developer.hashicorp.com/terraform/downloads) (e.g., via `winget install Hashicorp.Terraform`).
2. Ensure you have the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed (`winget install Microsoft.AzureCLI`).
3. You must already have an Azure Resource Group created (e.g., `learnix-rg`).

## 2. Authentication

Open a terminal (PowerShell) in the `infrastructure/` folder and run the following command:
```powershell
az login
```
Your browser will open. Log in to your Azure account. Terraform will automatically use this active session to access Azure, so **no passwords or secrets need to be hardcoded**.

## 3. Running Terraform

Navigate to the `infrastructure` folder (if you aren't already there):
```powershell
cd d:\projects\Learnix\infrastructure
```

### Step 1: Initialize
```powershell
terraform init
```
This command downloads the necessary provider plugins (AzureRM) required to interact with the cloud.

### Step 2: Plan
Because the Terraform configuration requires the name of your resource group, you must pass it as a variable.
```powershell
terraform plan -var="resource_group_name=learnix-rg"
```
Terraform will analyze your configuration files (`storage.tf`) and display exactly what it intends to create (Storage Account, public containers, private containers, and lifecycle policies). It won't create anything yet; this is just a preview.

### Step 3: Apply
If the plan looks correct, apply it to provision the resources in Azure:
```powershell
terraform apply -var="resource_group_name=learnix-rg"
```
You will be prompted to confirm the action (type `yes` and press Enter). Terraform will begin creating the Blob Storage infrastructure.

## 4. Results

After Terraform completes successfully, it will output a variable to the console:
- `storage_connection_string` (The connection string for your new Azure Blob Storage).

Because this output is marked as `sensitive`, Terraform hides it by default. You must run the following command to view its actual value:
```powershell
terraform output -raw storage_connection_string
```
Copy this string. You can use it in your `appsettings.json` (if testing the cloud storage locally) or configure it in Azure.

---

> [!NOTE] 
> **About Lifecycle Management**
> The `temp-uploads` container is automatically created with a lifecycle cleanup policy (blobs older than 1 day will be automatically deleted). You no longer need to manage this manually or via C# code.
