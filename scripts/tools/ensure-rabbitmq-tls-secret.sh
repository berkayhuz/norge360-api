#!/usr/bin/env bash
set -euo pipefail

namespace="${K8S_NAMESPACE:-norge360-production}"
secret_name="${RABBITMQ_TLS_SECRET:-norge360-rabbitmq-tls}"

if kubectl -n "$namespace" get secret "$secret_name" >/dev/null 2>&1; then
  echo "RabbitMQ TLS secret already exists: $namespace/$secret_name"
  exit 0
fi

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT

cat > "$workdir/server.cnf" <<'EOF'
[req]
prompt = no
distinguished_name = req_distinguished_name
req_extensions = req_ext

[req_distinguished_name]
CN = norge360-rabbitmq

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = norge360-rabbitmq
DNS.2 = norge360-rabbitmq.norge360-production
DNS.3 = norge360-rabbitmq.norge360-production.svc
DNS.4 = norge360-rabbitmq.norge360-production.svc.cluster.local
EOF

openssl genrsa -out "$workdir/ca.key" 4096
openssl req \
  -x509 \
  -new \
  -nodes \
  -key "$workdir/ca.key" \
  -sha256 \
  -days 3650 \
  -out "$workdir/ca.crt" \
  -subj "/CN=Norge360 RabbitMQ Internal CA"

openssl genrsa -out "$workdir/tls.key" 2048
openssl req \
  -new \
  -key "$workdir/tls.key" \
  -out "$workdir/server.csr" \
  -config "$workdir/server.cnf"

openssl x509 \
  -req \
  -in "$workdir/server.csr" \
  -CA "$workdir/ca.crt" \
  -CAkey "$workdir/ca.key" \
  -CAcreateserial \
  -out "$workdir/tls.crt" \
  -days 825 \
  -sha256 \
  -extensions req_ext \
  -extfile "$workdir/server.cnf"

kubectl -n "$namespace" create secret generic "$secret_name" \
  --from-file=ca.crt="$workdir/ca.crt" \
  --from-file=tls.crt="$workdir/tls.crt" \
  --from-file=tls.key="$workdir/tls.key"

echo "Created RabbitMQ TLS secret: $namespace/$secret_name"
