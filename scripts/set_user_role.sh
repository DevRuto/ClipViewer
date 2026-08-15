#!/usr/bin/env bash
set -e

if [ "$2" != "Admin" ] && [ "$2" != "User" ]; then
  echo "Usage: $0 <username> <Admin|User>"
  exit 1
fi

role=$(docker compose exec -T db psql -U "clipviewer" -d "clipviewer" -tA \
-v username="$1" -v role="$2" <<'SQL'
UPDATE "Users"
SET "Role" = :'role'
WHERE "Username" = :'username'
RETURNING "Role";
SQL
)

if [ -z "$role" ]; then
  echo "No user found with username '$1'"
  exit 1
fi

echo "Role for '$1': $role"
