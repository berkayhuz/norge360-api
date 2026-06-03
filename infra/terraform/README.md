# Terraform

This folder is the infrastructure root for Norge360.

## Current scope

- Hetzner cloud resources
- Firewall rules
- Private networking
- DNS records
- Persistent storage
- Environment-specific stacks

## Layout

- `backend.tf`: remote state configuration
- `providers.tf`: provider configuration
- `versions.tf`: Terraform and provider version constraints
- `modules/`: reusable infrastructure building blocks
- `environments/`: per-environment root modules

## Next step

Wire the Hetzner resources into the `prod` environment first, then add `dev` if needed.
