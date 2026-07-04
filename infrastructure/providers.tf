terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.100"
    }
  }

  # State must be stored remotely for GitHub Actions
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "tfstatelearnix"
    container_name       = "tfstate"
    key                  = "storage.tfstate"
  }
}

provider "azurerm" {
  features {}
}
