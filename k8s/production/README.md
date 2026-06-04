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

- Secret values are synced from GitHub repository secrets by the deploy workflow into the cluster before manifests are applied.
- `secrets.yaml` is now a template/reference file for manual or local cluster setup; it is not part of the automated overlay.
- The deploy workflow pushes commit-SHA tagged images to GHCR and updates live workloads to that immutable tag during rollout.
- The deploy workflow also creates/refreshes the `ghcr-pull-secret` secret in the target namespace from the long-lived `GHCR_READ_USER` and `GHCR_READ_TOKEN` repository secrets.
- The deploy workflow creates the `norge360-rabbitmq-tls` secret on first deploy and keeps it stable afterwards. RabbitMQ listens on `amqps://norge360-rabbitmq:5671`, and repository secret `Messaging__RabbitMq__Uri` must use that `amqps` endpoint.
- Required GitHub repository secrets include application connection strings, RabbitMQ, Cloudflare R2, SMTP, Turnstile, and the auth signing private key PEM. Redis is deployed in-cluster and the connection string is derived from the internal service name.
- Keep the Hetzner firewall restricted to the frontend server only. The gateway is exposed through the cluster ingress controller and should only be reachable from the frontend server network path.
- Auth now uses Redis-backed shared Data Protection keys, so it can run with multiple replicas without a local key-ring PVC.
- NetworkPolicy rules deny all ingress by default, allow same-namespace service-to-service traffic, and allow the cluster ingress controller namespace to reach the edge pods.
- This overlay is tuned for a single backend node with 8 GB RAM, so rolling updates favor lower peak usage over zero-downtime surge capacity.

## Notes

- The auth and notification workers disable AWS Parameter Store in-cluster.
- Redis, Meilisearch, and RabbitMQ are stateful and have persistent volume claims. RabbitMQ uses an internal self-signed CA stored in `norge360-rabbitmq-tls`; delete and recreate that secret only when you intentionally rotate the broker certificate.
- This is not a public edge deployment; the gateway is intended to be reachable only from the frontend server and trusted internal traffic.
- The gateway ingress is hostless on purpose, so there is no `gateway.norge360.com` or similar public subdomain involved.
