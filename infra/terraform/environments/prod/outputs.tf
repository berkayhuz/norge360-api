output "backend_server_id" {
  value = data.hcloud_server.backend.id
}

output "backend_server_ipv4" {
  value = data.hcloud_server.backend.ipv4_address
}

output "backend_firewall_id" {
  value = data.hcloud_firewall.backend.id
}

output "backend_ssh_key_id" {
  value = data.hcloud_ssh_key.backend_admin.id
}
