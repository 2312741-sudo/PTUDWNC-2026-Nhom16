#!/usr/bin/env bash
# N1-4 (tuần 4): khôi phục PostgreSQL từ file .dump của backup.sh.
#
# An toàn: KHÔNG ghi đè DB đang chạy thật. Mặc định script này sẽ từ chối nếu database đích
# đã tồn tại và không có --allow-existing, để không bao giờ xoá sạch dữ liệu thật do gõ nhầm.
#
# Dùng:
#   bash deploy/restore.sh backups/culinary_20260930T200000Z.dump \
#        --host 127.0.0.1 --port 5432 --user postgres --db culinary_restore_drill
#
#   --db KHÔNG được là database đang dùng bởi ứng dụng.
set -euo pipefail

log() { printf '[restore] %s\n' "$*"; }
die() { printf '[restore] LỖI: %s\n' "$*" >&2; exit 1; }

command -v pg_restore >/dev/null 2>&1 || die "không tìm thấy pg_restore (cài postgresql-client)"

dump_file=""
host="${PGHOST:-127.0.0.1}"
port="${PGPORT:-5432}"
user="${PGUSER:-postgres}"
db=""
password="${PGPASSWORD:-}"
allow_existing=0

while [ $# -gt 0 ]; do
  case "$1" in
    --host) host="$2"; shift 2 ;;
    --port) port="$2"; shift 2 ;;
    --user) user="$2"; shift 2 ;;
    --db) db="$2"; shift 2 ;;
    --password) password="$2"; shift 2 ;;
    # Chỉ dùng khi CỐ TÌNH khôi phục đè lên một DB rác đã biết là không còn giá trị.
    --allow-existing) allow_existing=1; shift ;;
    -h|--help) sed -n '2,20p' "$0"; exit 0 ;;
    -*) die "tuỳ chọn lạ: $1" ;;
    *) [ -z "$dump_file" ] || die "chỉ nhận một file dump"; dump_file="$1"; shift ;;
  esac
done

[ -n "$dump_file" ] || die "thiếu đường dẫn file .dump"
[ -f "$dump_file" ] || die "không thấy file: $dump_file"
[ -n "$db" ] || die "thiếu --db (database đích)"
[ -n "$password" ] || die "thiếu --password hoặc PGPASSWORD"

export PGPASSWORD="$password"

# Từ chối an toàn: chặn khôi phục nhầm lên DB thật.
if psql -h "$host" -p "$port" -U "$user" -d postgres -tAc \
     "SELECT 1 FROM pg_database WHERE datname = '$db'" | grep -q 1; then
  if [ "$allow_existing" -ne 1 ]; then
    die "database '$db' đã tồn tại. Xác nhận đây là DB rác thì thêm --allow-existing (script sẽ XOÁ dữ liệu trong đó)."
  fi
  log "database '$db' đã tồn tại và --allow-existing được truyền: sẽ ghi đè"
else
  log "tạo database '$db'"
  createdb -h "$host" -p "$port" -U "$user" "$db"
fi

log "khôi phục $dump_file -> $db"
# --exit-on-error: dừng ngay khi gặp lỗi, nếu không ta có thể "khôi phục xong" với DB nửa vời.
if ! pg_restore -h "$host" -p "$port" -U "$user" -d "$db" --exit-on-error --no-owner --no-acl "$dump_file"; then
  die "pg_restore thất bại — database đích có thể đang dở dang, cần kiểm tra thủ công"
fi

# Kiểm chứng khôi phục: chỉ "exit 0" chưa chứng minh có dữ liệu dùng được.
log "kiểm chứng sau khôi phục:"
tables="$(psql -h "$host" -p "$port" -U "$user" -d "$db" -tAc \
  "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'")"
log "  số bảng trong schema public: $tables"
[ "${tables:-0}" -gt 0 ] || die "khôi phục xong nhưng schema public rỗng — coi như thất bại"

psql -h "$host" -p "$port" -U "$user" -d "$db" -tAc "
  SELECT format('  %-30s %s', table_name, row_estimate)
  FROM (
    SELECT c.relname AS table_name,
           c.reltuples::bigint AS row_estimate
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relkind = 'r'
    ORDER BY c.reltuples DESC
    LIMIT 10
  ) s" || true

log "khôi phục thành công vào database '$db'"
