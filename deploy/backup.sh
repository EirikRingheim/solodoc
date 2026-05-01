#!/bin/bash
# Daily PostgreSQL backup script for Solodoc
# Run via cron: 0 2 * * * /root/solodoc/deploy/backup.sh

set -e

BACKUP_DIR="/root/solodoc/backups"
RETENTION_DAYS=30
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/solodoc_$DATE.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "$(date) — Starting database backup..."

# Dump and compress
docker compose -f /root/solodoc/docker-compose.production.yml exec -T postgres \
    pg_dump -U solodoc solodoc | gzip > "$BACKUP_FILE"

SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
echo "$(date) — Backup complete: $BACKUP_FILE ($SIZE)"

# Clean up old backups (keep last 30 days)
find "$BACKUP_DIR" -name "solodoc_*.sql.gz" -mtime +$RETENTION_DAYS -delete
echo "$(date) — Cleaned backups older than $RETENTION_DAYS days"

# Count remaining backups
COUNT=$(ls -1 "$BACKUP_DIR"/solodoc_*.sql.gz 2>/dev/null | wc -l)
echo "$(date) — $COUNT backup files retained"
