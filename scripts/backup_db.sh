#!/usr/bin/env bash
set -e

backup_dir="$(dirname "$0")/../db_backup"
mkdir -p "$backup_dir"

timestamp=$(date +%Y%m%d_%H%M%S)
backup_file="$backup_dir/clipviewer_${timestamp}.sql.gz"

docker compose exec -T db pg_dump -U "clipviewer" -d "clipviewer" | gzip > "$backup_file"

echo "Backup written to $backup_file"
