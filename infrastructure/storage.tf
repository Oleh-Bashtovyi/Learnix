# Fetch existing resource group instead of creating a new one
data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

# Storage Account names must be globally unique and lowercase (no hyphens)
# Generate a random suffix to ensure uniqueness
resource "random_string" "storage_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_account" "storage" {
  name                     = "${var.prefix}storage${random_string.storage_suffix.result}"
  resource_group_name      = data.azurerm_resource_group.rg.name
  location                 = data.azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
}

# Public Containers
resource "azurerm_storage_container" "avatars" {
  name                  = "avatars"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "blob"
}

resource "azurerm_storage_container" "course_covers" {
  name                  = "course-covers"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "blob"
}

resource "azurerm_storage_container" "course_videos" {
  name                  = "course-videos"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "blob"
}

resource "azurerm_storage_container" "category_images" {
  name                  = "category-images"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "blob"
}

# Private Containers
resource "azurerm_storage_container" "certificates" {
  name                  = "certificates"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "temp_uploads" {
  name                  = "temp-uploads"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "private"
}

# Lifecycle Management for temp-uploads
resource "azurerm_storage_management_policy" "cleanup_policy" {
  storage_account_id = azurerm_storage_account.storage.id

  rule {
    name    = "cleanup-temp-uploads"
    enabled = true
    filters {
      blob_types   = ["blockBlob"]
      prefix_match = ["temp-uploads/"]
    }
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = 1
      }
    }
  }
}

# Output the connection string for easy access after deployment
output "storage_connection_string" {
  value     = azurerm_storage_account.storage.primary_connection_string
  sensitive = true
}
