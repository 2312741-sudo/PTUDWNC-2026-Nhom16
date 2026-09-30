#!/usr/bin/env bash
# N1-4 (tuần 4): sao lưu PostgreSQL lúc 03:00 Asia/Ho_Chi_Minh, giữ 30 ngày.
#
# Vì sao 03:00 ICT: nghị quyết tuần 4 chốt giờ này. Lưu ý khi đọc cron: cron luôn dùng giờ UTC,
# nên 03:00 Asia/Ho_Chi_Minh (UTC+7) = 20:00 UTC HÔM TRƯỚC. Sai một ngày là sao lưu lệch 24h.
#   cron:  0 20 * * *  /opt/culinary/deploy/backup.sh >> /var/log/culinary-backup.log 2>&1
#   GitHub Actions (xem .github/workflows/backup.yml): schedule '0 20 * * *'
#
# Cần biến môi trường (Render đặt sẵn dạng DATABASE_URL):
#   DATABASE_URL  - chuỗi kết nối PostgreSQL (bắt buộc)
#   BACKUP_DIR    - nơi lưu file .dump (mặc định ./backups)
#   BACKUP_KEEP_DAYS - số ngày giữ lại (mặc định 30)
#
# Dùng pg_dump định dạng custom (-Fc) vì: nén sẵn, và pg_restore có thể chọn lọc bảng khi
# khôi phụng. Chạy được ngoài Docker (postgresql-client) hoặc bên trong container postgres.
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-./backups}"
KEEP_DAYS="${BACKUP_KEEP_DAYS:-30}"
# -Fc: custom format. --no-owner/--no-acl: bản khôi phục phải chạy được trên user khác.
# --clean --if-exists: dump có lệnh DROP để khôi phục lên DB cũ không vướng dữ liệu cũ.
PG_DUMP_FLAGS=(--format=custom --no-owner --no-acl --clean --if-exists)

log() { printf '[backup] %s\n' "$*"; }
die() { printf '[backup] LỖI: %s\n' "$*" >&2; exit 1; }

command -v pg_dump >/dev/null 2>&1 || die "không tìm thấy pg_dump (cài postgresql-client)"

[ -n "${DATABASE_URL:-}" ] || die "thiếu biến môi trường DATABASE_URL"

mkdir -p "$BACKUP_DIR"

# Giờ UTC có dấu chấm phẩy sẽ hỏng tên file trên Windows/scp, nên dùng định dạng gọn.
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
target="$BACKUP_DIR/culinary_${timestamp}.dump"
tmp="${target}.partial"

log "bắt đầu sao lưu -> $target"
# Ghi ra file .partial rồi mới đổi tên: nếu pg_dump chết giữa chừng, file cuối cùng không bao giờ
# là file nửa vời mà bị restore nhầm.
if ! pg_dump "${PG_DUMP_FLAGS[@]}" --file "$tmp" "$DATABASE_URL"; then
  rm -f "$tmp"
  die "pg_dump thất bại (kiểm tra DATABASE_URL và mạng tới DB)"
fi

mv "$tmp" "$target"

size_bytes="$(wc -c < "$target" | tr -d ' ')"
# File rỗng nghĩa là sao lưu hỏng mà không báo lỗi — nếu không kiểm tra, ta sẽ tưởng có backup
# cho tới khi cần khôi phụng.
[ "$size_bytes" -gt 0 ] || { rm -f "$target"; die "file sao lưu rỗng"; }
log "xong: $target ($(awk -v b="$size_bytes" 'BEGIN{printf "%.1f MB", b/1048576}'))"

# Dọn file cũ hơn KEEP_DAYS ngày. Chỉ xoá đúng file backup tên chuẩn của script này.
removed=0
while IFS= read -r old; do
  [ -n "$old" ] || continue
  rm -f "$old"
  removed=$((removed + 1))
  log "đã xoá bản sao lưu cũ: $(basename "$old")"
done < <(find "$BACKUP_DIR" -maxdepth 1 -type f -name 'culinary_*.dump' -mtime "+$KEEP_DAYS")
log "giữ ${KEEP_DAYS} ngày; đã dọn $removed file"

# Danh sách bản sao lưu còn lại để đối chiếu nhanh.
find "$BACKUP_DIR" -maxdepth 1 -type f -name 'culinary_*.dump' -printf '%TY-%Tm-%Td %TH:%TM  %10s bytes  %p\n' | sort
