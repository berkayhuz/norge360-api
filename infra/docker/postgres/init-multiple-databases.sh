#!/usr/bin/env sh
set -eu

if [ -z "${POSTGRES_MULTIPLE_DATABASES:-}" ]; then
  exit 0
fi

echo "$POSTGRES_MULTIPLE_DATABASES" | tr ',' '\n' | while read -r database; do
  database="$(echo "$database" | xargs)"
  if [ -z "$database" ]; then
    continue
  fi

  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
SELECT 'CREATE DATABASE "$database"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$database')\gexec
EOSQL
done
