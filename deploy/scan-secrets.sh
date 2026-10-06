#!/usr/bin/env bash
# N1-8 (tuần 4): quét secret bị gửi nhầm vào repo.
#
# Vì sao cần: tuần 3 phát hiện Jwt__SigningKey nằm thẳng trong render.yaml — ai đọc repo cũng
# ký được token. Có script trong CI thì lỗi đó bị chặn NGAY khi commit, thay vì chỉ được phát
# hiện khi review.
#
# Nguyên tắc: script ưu tiên báo cáo rõ tên file + tên biến để người sửa biết đường đi nhanh.
# Chạy được ngoài CI để kiểm tra trước khi push:  bash deploy/scan-secrets.sh
set -euo pipefail

failures=0
report_fail() {
  local file="$1" line="$2" reason="$3"
  printf 'SECRET FOUND  %s:%s\n    %s\n' "$file" "$line" "$reason" >&2
  failures=$((failures + 1))
}

# 1) render.yaml / docker-compose / workflow: mọi biến có tên "secret" KHÔNG được gán giá trị
#    literal. Cách gán hợp lệ: `sync: false`, `fromDatabase`, `fromService`.
check_yaml_secret_keys() {
  local file="$1"
  [ -f "$file" ] || return 0
  local key="" key_line=0
  while IFS= read -r line; do
    key_line=$((key_line + 1))
    # Bắt đầu một mục envVar mới: dòng có "key: TÊN_BIẾN"
    if [[ "$line" =~ ^[[:space:]]*(-[[:space:]]*)?key:[[:space:]]*([A-Za-z0-9_]+)[[:space:]]*$ ]]; then
      key="${BASH_REMATCH[2]}"
      continue
    fi
    [ -n "$key" ] || continue
    # Gán giá trị literal cho biến đang xét?
    if [[ "$line" =~ ^[[:space:]]*value:[[:space:]]*(.+)$ ]]; then
      local value="${BASH_REMATCH[1]}"
      local lower_key lower_value
      lower_key="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]')"
      lower_value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
      if [[ "$lower_key" == *password* || "$lower_key" == *secret* || "$lower_key" == *key* || \
            "$lower_key" == *token* || "$lower_key" == *connectionstring* ]]; then
        # Ngoại lệ duy nhất: giá trị rỗng hoặc dạng biến/placeholder không phải secret thật.
        if [[ "$lower_value" != *'$('* && "$lower_value" != *'${'* && "$lower_value" != "changeme"* && "$lower_value" != *your* ]]; then
          report_fail "$file" "$key_line" \
            "biến nhạy cảm '$key' được gán giá trị literal. Dùng 'sync: false' hoặc fromDatabase/fromService để Render quản lý."
        fi
      fi
    fi
  done < "$file"
}

# 2) Token/khoá riêng của dịch vụ phổ biến xuất hiện ở bất kỳ file nào.
scan_known_token_patterns() {
  local pattern description file
  while IFS='|' read -r pattern description; do
    [ -n "$pattern" ] || continue
    while IFS= read -r hit; do
      file="${hit%%:*}"
      local rest="${hit#*:}"
      report_fail "$file" "${rest%%:*}" "$description"
    done < <(git grep -nIE "$pattern" -- . \
      ':!docs' ':!.env.example' ':!**/*.example' ':!deploy/scan-secrets.sh' 2>/dev/null || true)
  done <<'PATTERNS'
ghp_[A-Za-z0-9]{36,}|GitHub personal access token (ghp_)
github_pat_[A-Za-z0-9_]{50,}|GitHub fine-grained token
-----BEGIN (RSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|Khoá private bị commit
AKIA[0-9A-Z]{16}|AWS access key id
sk-[A-Za-z0-9]{32,}|OpenAI-style API key
xox[baprs]-[A-Za-z0-9-]{10,}|Slack token
PATTERNS
}

echo "== N1-8: quét secret trong repo =="

check_yaml_secret_keys render.yaml
check_yaml_secret_keys docker-compose.dev.yml

scan_known_token_patterns

if [ "$failures" -gt 0 ]; then
  echo ""
  echo "Phát hiện $failures secret bị gửi vào repo." >&2
  echo "Cách xử lý: đổi sang 'sync: false'/'fromService' (Render tự hỏi khi deploy)," >&2
  echo "và nếu secret ĐÃ bị push thì phải rotate giá trị đó — xoá trong file không đủ." >&2
  exit 1
fi

echo "OK: không phát hiện secret bị gửi vào repo."
