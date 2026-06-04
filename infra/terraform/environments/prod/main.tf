data "hcloud_server" "backend" {
  name = var.backend_server_name
}

data "hcloud_firewall" "backend" {
  name = var.backend_firewall_name
}

data "hcloud_ssh_key" "backend_admin" {
  name = var.backend_ssh_key_name
}

data "hcloud_server" "frontend" {
  name = var.frontend_server_name
}

data "cloudflare_zone" "norge360" {
  filter = {
    name = var.cloudflare_zone_name
  }
}

resource "hcloud_firewall_attachment" "backend" {
  firewall_id = data.hcloud_firewall.backend.id
  server_ids  = [data.hcloud_server.backend.id]
}

resource "cloudflare_dns_record" "root" {
  zone_id = data.cloudflare_zone.norge360.id
  name    = "@"
  type    = "A"
  content = data.hcloud_server.frontend.ipv4_address
  proxied = true
  ttl     = 1
}

resource "cloudflare_dns_record" "auth" {
  zone_id = data.cloudflare_zone.norge360.id
  name    = "auth"
  type    = "A"
  content = data.hcloud_server.frontend.ipv4_address
  proxied = true
  ttl     = 1
}

resource "null_resource" "backend_k3s_bootstrap" {
  count = var.enable_k3s_bootstrap ? 1 : 0

  triggers = {
    server_id = data.hcloud_server.backend.id
    version   = "2026-06-04-1"
  }

  connection {
    host        = data.hcloud_server.backend.ipv4_address
    user        = var.backend_ssh_user
    private_key = file(var.backend_ssh_private_key_path)
    timeout     = "10m"
  }

  provisioner "remote-exec" {
    inline = [
      "set -eu",
      "if ! command -v k3s >/dev/null 2>&1; then curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC='server --write-kubeconfig-mode 644' sh -; fi",
      "systemctl enable --now k3s",
      "k3s kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/download/v0.8.1/components.yaml",
      "k3s kubectl -n kube-system rollout status deploy/metrics-server --timeout=5m",
      "k3s kubectl get nodes",
    ]
  }
}
