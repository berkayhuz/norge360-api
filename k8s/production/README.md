# Norge360 Kubernetes

This directory contains a production-oriented Kustomize overlay for the Norge360 stack.

## Apply

```bash
kubectl apply -k k8s/production
```

## What it includes

- `norge360-auth-api`
- `norge360-gateway`
- `norge360-accounts-api`
- `norge360-community-api`
- `norge360-discovery-api`
- `norge360-search-api`
- `norge360-accounts-worker`
- `norge360-community-worker`
- `norge360-discovery-worker`
- `norge360-search-worker`
- `norge360-notification-worker`
- `norge360-meilisearch`
- `norge360-rabbitmq`

## Before applying

- Replace placeholder values in `secrets.yaml`.
- The deploy workflow pushes commit-SHA tagged images to GHCR and updates live workloads to that immutable tag during rollout.
- The deploy workflow also creates/refreshes the `ghcr-pull-secret` secret in the target namespace from the long-lived `GHCR_READ_USER` and `GHCR_READ_TOKEN` repository secrets.
- Keep the Hetzner firewall restricted to the frontend server only. This overlay also whitelists `10.0.0.4/32` at the ingress layer so the gateway stays private.
- Auth now uses Redis-backed shared Data Protection keys, so it can run with multiple replicas without a local key-ring PVC.
- NetworkPolicy rules deny all ingress by default, allow same-namespace service-to-service traffic, and allow the ingress controller namespace to reach the edge pods.

## Notes

- The auth and notification workers disable AWS Parameter Store in-cluster.
- Meilisearch and RabbitMQ are stateful and have persistent volume claims.
- This is not a public edge deployment; the gateway is intended to be reachable only from the frontend server and trusted internal traffic.
- The gateway ingress is hostless on purpose, so there is no `gateway.norge360.com` or similar public subdomain involved.
