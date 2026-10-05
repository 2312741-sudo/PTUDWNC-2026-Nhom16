#!/usr/bin/env bash
# ==============================================================================
# Script: backup-storage.sh
# Tác giả: Nguyễn Thanh Tâm (TV1 - 2312741) - Tuần 5
# Mục đích: Sao lưu hình ảnh công thức và media tĩnh (public/images/recipes)
# ==============================================================================
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-./backups/storage}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
SOURCE_DIR="./src/frontend/public/images/recipes"

mkdir -p "$BACKUP_DIR"
BACKUP_FILE="${BACKUP_DIR}/recipes_images_${TIMESTAMP}.tar.gz"

echo "================================================================="
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Bắt đầu sao lưu kho media hình ảnh..."
echo "Thư mục nguồn: ${SOURCE_DIR}"
echo "================================================================="

if [ -d "$SOURCE_DIR" ]; then
    tar -czf "$BACKUP_FILE" -C "$(dirname "$SOURCE_DIR")" "$(basename "$SOURCE_DIR")"
    FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] ✅ Sao lưu media thành công: ${BACKUP_FILE} (${FILE_SIZE})"
else
    echo "⚠️ Thư mục nguồn ${SOURCE_DIR} không tồn tại."
fi
echo "================================================================="
