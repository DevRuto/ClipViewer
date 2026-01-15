#!/usr/bin/env bash
docker compose exec -T db psql -U "clipviewer" -d "clipviewer" \
-v username="$1" <<'SQL'
INSERT INTO "Users" ("Username", "ApiKey", "CreatedAt")
VALUES (:'username', gen_random_uuid(), NOW())
RETURNING *;
SQL
