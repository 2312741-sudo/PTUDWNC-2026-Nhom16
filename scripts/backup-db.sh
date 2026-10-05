#!/usr/bin/env bash
# ==============================================================================
# Script: backup-db.sh
# Tác giả: Nguyễn Thanh Tâm (TV1 - 2312741) - Tuần 5
# Mục đích: Tự động sao lưu cơ sở dữ liệu PostgreSQL (CulinaryBlog)
# Output: Thư mục backups/database/ với định dạng .sql.gz kèm mã băm SHA-256
# ==============================================================================
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-./backups/database}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
DB_HOST="${DB_HOST:-}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-culinary_blog}"
DB_USER="${DB_USER:-postgres}"

mkdir -p "$BACKUP_DIR"
BACKUP_FILE="${BACKUP_DIR}/${DB_NAME}_${TIMESTAMP}.sql.gz"
CHECKSUM_FILE="${BACKUP_FILE}.sha256"

echo "================================================================="
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Bắt đầu quá trình sao lưu CSDL: ${DB_NAME}..."
echo "Database: ${DB_NAME} | User: ${DB_USER}"
echo "================================================================="

# Thực hiện pg_dump và nén trực tiếp qua gzip
HOST_ARGS=""
if [ -n "$DB_HOST" ]; then
  HOST_ARGS="-h $DB_HOST -p $DB_PORT"
fi

pg_dump $HOST_ARGS -U "$DB_USER" -d "$DB_NAME" \
  --format=plain \
  --no-owner \
  --no-acl \
  --clean \
  --if-exists | gzip > "$BACKUP_FILE"

# Tạo mã kiểm tra tính toàn vẹn SHA-256
if command -v sha256sum &> /dev/null; then
    sha256sum "$BACKUP_FILE" > "$CHECKSUM_FILE"
elif command -v shasum &> /dev/null; then
    shasum -a 256 "$BACKUP_FILE" > "$CHECKSUM_FILE"
fi

FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
echo "[$(date '+%Y-%m-%d %H:%M:%S')] ✅ Sao lưu CSDL thành công!"
echo "File lưu trữ: ${BACKUP_FILE} (Dung lượng: ${FILE_SIZE})"
echo "Checksum: $(cat "$CHECKSUM_FILE")"
echo "================================================================="
