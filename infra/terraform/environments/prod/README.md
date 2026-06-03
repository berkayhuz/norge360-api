# Prod Environment

This environment manages the current production backend server only.

## Current scope

- `norge360-backend-1`
- `norge360-private-firewall`
- Hetzner SSH key registration for the backend admin key
- Cloudflare DNS records for `norge360.com` and `auth.norge360.com`
- Optional k3s bootstrap on the backend server

## Not in scope yet

- `norge360-db-1`
- `norge360-frontend-1`
- Private network resources

## Required inputs

- `HCLOUD_TOKEN`
- `backend_ssh_key_name`
- `CLOUDFLARE_API_TOKEN`

The default SSH key name is:

- `norge360-hetzner`

## Workflow

1. Set `HCLOUD_TOKEN` in your shell.
2. Set `CLOUDFLARE_API_TOKEN` in your shell.
3. Run `terraform init` inside this directory.
4. Run `terraform plan`.
5. Run `terraform apply` after reviewing the plan.

## Optional bootstrap

To bootstrap k3s on the backend server, set:

- `enable_k3s_bootstrap=true`

The bootstrap connects to the backend server over SSH using:

- [`C:/Users/berka/.ssh/id_ed25519`](C:/Users/berka/.ssh/id_ed25519)

The bootstrap currently installs:

- k3s server
- metrics-server
