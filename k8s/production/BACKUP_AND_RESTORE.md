# Backup and Restore

This cluster is production-oriented but still needs external backup discipline.

## What to back up

- PostgreSQL databases used by `auth`, `accounts`, `community`, `discovery`, and `notification`
- `norge360-redis` PVC because auth Data Protection keys and hot cache entries depend on it
- `norge360-meilisearch-data` PVC
- `norge360-rabbitmq-data` PVC if you want durable queue state
- Kubernetes secrets from `k8s/production/secrets.yaml` after replacing placeholders with real values

## What does not need a local PVC backup anymore

- `auth` Data Protection keys now live in Redis, so the auth pod no longer depends on a local key-ring volume
- Redis is stateful now, so losing its PVC will clear auth key-ring material and any cached hot data.

## Restore order

1. Restore PostgreSQL.
2. Restore Redis.
3. Restore Meilisearch and RabbitMQ PVCs if needed.
4. Re-apply `kubectl apply -k k8s/production`.
5. Re-deploy the app workloads.

## Notes

- Keep database snapshots outside the Kubernetes cluster.
- Test restore procedures before relying on them in a real incident.
