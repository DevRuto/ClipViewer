#!/usr/bin/env bash
docker compose exec -T db psql -U "clipviewer" -d "clipviewer" \
-v username="$1" <<'SQL'
UPDATE "Users"
SET "ApiKey" = gen_random_uuid()
WHERE "Username" = :'username'
RETURNING *;
SQL