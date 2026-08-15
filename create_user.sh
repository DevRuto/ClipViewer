#!/usr/bin/env bash
set -e

api_key=$(docker compose exec -T db psql -U "clipviewer" -d "clipviewer" -tA \
-v username="$1" <<'SQL'
INSERT INTO "Users" ("Username", "ApiKey", "CreatedAt")
VALUES (:'username', gen_random_uuid(), NOW())
RETURNING "ApiKey";
SQL
)

echo "API key for '$1': $api_key"
