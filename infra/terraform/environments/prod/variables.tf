variable "backend_server_name" {
  type    = string
  default = "norge360-backend-1"
}

variable "backend_firewall_name" {
  type    = string
  default = "norge360-private-firewall"
}

variable "backend_ssh_key_name" {
  type        = string
  description = "Name of the existing Hetzner SSH key used by the backend server."
  default     = "norge360-hetzner"
}

variable "frontend_server_name" {
  type        = string
  description = "Name of the existing Hetzner frontend server used as the public DNS target."
  default     = "norge360-frontend-1"
}

variable "cloudflare_zone_name" {
  type        = string
  description = "Cloudflare zone that will own the Norge360 DNS records."
  default     = "norge360.com"
}

variable "backend_ssh_private_key_path" {
  type        = string
  description = "Path to the local SSH private key used to bootstrap the backend server."
  default     = "C:/Users/berka/.ssh/id_ed25519"
}

variable "backend_ssh_user" {
  type        = string
  description = "SSH user for the backend server bootstrap connection."
  default     = "root"
}

variable "enable_k3s_bootstrap" {
  type        = bool
  description = "Whether Terraform should bootstrap k3s on the backend server over SSH."
  default     = false
}
