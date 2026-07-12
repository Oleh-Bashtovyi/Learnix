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
  access_tier              = "Hot"
  min_tls_version          = "TLS1_2"

  lifecycle {
    prevent_destroy = true
  }

  # Advanced (Matching UI screenshot)
  is_hns_enabled                   = false
  sftp_enabled                     = false
  cross_tenant_replication_enabled = false
  nfsv3_enabled                    = false
  infrastructure_encryption_enabled = false

  # Explicit Security & Networking (Ensuring parity with UI screenshots)
  public_network_access_enabled   = true
  allow_nested_items_to_be_public = true # Critical: allows 'blob' access level for public containers
  https_traffic_only_enabled      = true
  shared_access_key_enabled       = true
  default_to_oauth_authentication = false

  routing {
    choice = "MicrosoftRouting"
  }

  # Data Protection (Matching UI screenshot for 7-day soft delete)
  blob_properties {
    delete_retention_policy {
      days = 7
    }
    container_delete_retention_policy {
      days = 7
    }
  }

  share_properties {
    retention_policy {
      days = 7
    }
  }
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

resource "azurerm_storage_container" "category_images" {
  name                  = "category-images"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "blob"
}

# Private Containers
#
# Lesson videos are paid content behind an enrollment check: GetLessonContent verifies the enrollment and
# hands back a 2-hour SAS (BlobUrlTtlConstants.VideoLessonReadUrl). That SAS is only worth anything if the
# container refuses anonymous reads — with container_access_type = "blob" the plain URL works forever, for
# anyone it is ever shared with, and the expiry is decoration. StorageSeeder has always created this
# container private locally; this is Terraform catching up with the model the code assumes.
resource "azurerm_storage_container" "course_videos" {
  name                  = "course-videos"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "private"
}

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
