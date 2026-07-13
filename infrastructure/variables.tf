variable "prefix" {
  description = "A prefix used for all resources in this example"
  default     = "learnix"
}

variable "resource_group_name" {
  type = string
}

# The browser uploads straight to Blob Storage with a SAS URL, so the *storage account* — not just the
# API — has to accept the frontend's origin, or the preflight fails and no file ever leaves the page.
variable "allowed_origins" {
  description = "Origins allowed to call Blob Storage from a browser (the frontend site)."
  type        = list(string)
  default     = []
}
