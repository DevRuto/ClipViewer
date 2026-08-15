#!/usr/bin/env bash
set -e

api_key=$(docker compose exec -T db psql -U "clipviewer" -d "clipviewer" -tA \
-v username="$1" <<'SQL'
UPDATE "Users"
SET "ApiKey" = gen_random_uuid()
WHERE "Username" = :'username'
RETURNING "ApiKey";
SQL
)

echo "API key for '$1': $api_key"