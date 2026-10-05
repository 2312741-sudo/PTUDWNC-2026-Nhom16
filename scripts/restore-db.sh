#!/usr/bin/env bash
# ==============================================================================
# Script: restore-db.sh
# Tác giả: Nguyễn Thanh Tâm (TV1 - 2312741) - Tuần 5
# Mục đích: Tự động khôi phục cơ sở dữ liệu PostgreSQL từ bản sao lưu .sql.gz
# Kiểm tra: Xác thực checksum SHA-256 trước khi nạp vào CSDL
# ==============================================================================
set -euo pipefail

BACKUP_FILE="${1:-}"
DB_HOST="${DB_HOST:-}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-culinary_blog}"
DB_USER="${DB_USER:-postgres}"

if [ -z "$BACKUP_FILE" ]; then
    echo "❌ Lỗi: Vui lòng chỉ định đường dẫn file backup cần khôi phục!"
    echo "Cách dùng: $0 <path_to_backup_file.sql.gz>"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Lỗi: File sao lưu không tồn tại: $BACKUP_FILE"
    exit 1
fi

echo "================================================================="
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Bắt đầu quá trình khôi phục CSDL..."
echo "Target DB: ${DB_NAME} (User: ${DB_USER})"
echo "Source File: ${BACKUP_FILE}"
echo "================================================================="

# 1. Kiểm tra mã SHA-256 nếu có file checksum
CHECKSUM_FILE="${BACKUP_FILE}.sha256"
if [ -f "$CHECKSUM_FILE" ]; then
    echo "Đang kiểm tra tính toàn vẹn file với SHA-256..."
    if command -v sha256sum &> /dev/null; then
        sha256sum -c "$CHECKSUM_FILE"
    elif command -v shasum &> /dev/null; then
        shasum -a 256 -c "$CHECKSUM_FILE"
    fi
    echo "✅ Checksum khớp! File sao lưu hoàn toàn nguyên vẹn."
fi

# 2. Thực hiện giải nén và nạp vào database
HOST_ARGS=""
if [ -n "$DB_HOST" ]; then
  HOST_ARGS="-h $DB_HOST -p $DB_PORT"
fi

echo "Đang phục hồi dữ liệu vào database '${DB_NAME}'..."
gunzip -c "$BACKUP_FILE" | psql $HOST_ARGS -U "$DB_USER" -d "$DB_NAME" --quiet

# 3. Kiểm tra số lượng bản ghi sau phục hồi
RECIPE_COUNT=$(psql $HOST_ARGS -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT count(*) FROM \"Recipes\";" | xargs)
CATEGORY_COUNT=$(psql $HOST_ARGS -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT count(*) FROM \"Categories\";" | xargs)

echo "[$(date '+%Y-%m-%d %H:%M:%S')] ✅ Khôi phục CSDL thành công!"
echo "Xác nhận dữ liệu: ${CATEGORY_COUNT} Categories, ${RECIPE_COUNT} Recipes sẵn sàng."
echo "================================================================="
