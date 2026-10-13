#!/usr/bin/env bash
# N1-8 (tuần 4): quét secret bị gửi nhầm vào repo.
# W5-10 (TV4 08/10): mở rộng — quét thêm `appsettings*.json` + `env:` trong `.github/workflows/*.yml`
# (BUG-W4-02: N1-8 chỉ quét render.yaml/compose + 7 pattern token, nên `Password=postgres`,
# `minioadmin`, JWT key trong appsettings/backend.yml lọt qua). Bỏ qua giá trị placeholder
# (`${{ secrets.* }}`, `${VAR}`, `$()`), giá trị rỗng, và credential CI chỉ dùng trong lúc chạy
# runner (tiền tố `ci-`) — xem hàm `is_literal_secret_value`.
#
# Vì sao cần: tuần 3 phát hiện Jwt__SigningKey nằm thẳng trong render.yaml — ai đọc repo cũng
# ký được token. Có script trong CI thì lỗi đó bị chặn NGAY khi commit, thay vì chỉ được phát
# hiện khi review.
#
# Nguyên tắc: script ưu tiên báo cáo rõ tên file + tên biến để người sửa biết đường đi nhanh.
# Chạy được ngoài CI để kiểm tra trước khi push:  bash deploy/scan-secrets.sh
# (máy thiếu bash thật — dùng Git Bash:  C:\Program Files\Git\bin\bash.exe deploy/scan-secrets.sh)
set -euo pipefail

failures=0
report_fail() {
  local file="$1" line="$2" reason="$3"
  printf 'SECRET FOUND  %s:%s\n    %s\n' "$file" "$line" "$reason" >&2
  failures=$((failures + 1))
}

# Giá trị có phải secret thật (cần flag) không? Trả về 0 = PHẢI flag; 1 = giá trị an toàn.
# An toàn khi: rỗng · placeholder của CI/config (`${{`, `${`, `$(`) · `changeme`/mẫu `your…` ·
# giá trị CI-only (`ci-…` — password ephemeral sinh riêng cho runner, không dùng ở prod).
is_literal_secret_value() {
  local value="$1"
  [ -n "$value" ] || return 1
  case "$value" in
    *'${{'*|*'${'*|*'$('*) return 1 ;;
    changeme*|*'your'*) return 1 ;;
    ci-*) return 1 ;;
  esac
  return 0
}

# 1) render.yaml / compose: mọi biến có tên "secret" KHÔNG được gán giá trị literal.
#    Cách gán hợp lệ: `sync: false`, `fromDatabase`, `fromService`.
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
      if [[ "$lower_key" == *password* || "$lower_key" == *secret* || "$lower_key" == *key* || \
            "$lower_key" == *token* || "$lower_key" == *connectionstring* ]]; then
        if is_literal_secret_value "$value"; then
          report_fail "$file" "$key_line" \
            "biến nhạy cảm '$key' được gán giá trị literal. Dùng 'sync: false' hoặc fromDatabase/fromService để Render quản lý."
        fi
      fi
    fi
  done < "$file"
}

# 2) `appsettings*.json` (và JSON config khác trong src/backend): khoá nhạy cảm có giá trị
#    literal, hoặc chuỗi kết nối chứa `Password=...` literal. (BUG-W4-02)
check_json_config_secrets() {
  local file="$1"
  [ -f "$file" ] || return 0
  local line_no=0
  while IFS= read -r line; do
    line_no=$((line_no + 1))
    # Trích mọi cặp "khoá": "giá trị" trên dòng (chấp nhận JSON format nhiều dòng lẫn 1 dòng).
    local pair key value lower_key lower_value pw
    while IFS= read -r pair; do
      [ -n "$pair" ] || continue
      [[ "$pair" =~ ^\"([^\"]*)\"[[:space:]]*:[[:space:]]*\"([^\"]*)\"[[:space:]]*$ ]] || continue
      key="${BASH_REMATCH[1]}"
      value="${BASH_REMATCH[2]}"
      lower_key="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]')"
      lower_value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
      # 2a) Khoá nhạy cảm + giá trị literal
      if [[ "$lower_key" == *password* || "$lower_key" == *secret* || "$lower_key" == *key* || \
            "$lower_key" == *token* || "$lower_key" == *connectionstring* ]]; then
        if is_literal_secret_value "$lower_value"; then
          report_fail "$file" "$line_no" "khoá '$key' có giá trị literal. Chuyển sang config override (.env) hoặc biến môi trường."
        fi
      fi
      # 2b) Chuỗi kết nối chứa Password=literal
      if [[ "$lower_value" == *'password='* ]]; then
        pw="$(printf '%s' "$lower_value" | sed -n 's/.*password=\([^;]*\).*/\1/p')"
        if is_literal_secret_value "$pw"; then
          report_fail "$file" "$line_no" "chuỗi kết nối trong '$key' chứa mật khẩu literal. Dùng placeholder \$VAR thay thế."
        fi
      fi
    done < <(grep -oE '"([^"]+)"[[:space:]]*:[[:space:]]*"([^"]*)"' <<< "$line" || true)
  done < "$file"
}

# 3) `env:` trong workflow GitHub Actions: biến là chuỗi literal thì bắt.
#    Hợp lệ khi dùng `${{ secrets.X }}` hoặc `${{ github.run_id }}` (CI-only). (BUG-W4-02)
check_workflow_env_secrets() {
  local file="$1"
  [ -f "$file" ] || return 0
  local line_no=0 in_env=0
  while IFS= read -r line; do
    line_no=$((line_no + 1))
    if [[ "$line" =~ ^[[:space:]]*env:[[:space:]]*$ ]]; then
      in_env=1
      continue
    fi
    [ "$in_env" -eq 1 ] || continue
    # Bên ngoài khối env (không phải `NAME: value`) thì thoát; ví dụ `steps:` (rỗng), `ports: [...]`.
    if [[ "$line" =~ ^[[:space:]]+([A-Za-z0-9_]+)[[:space:]]*:[[:space:]]*(.+)$ ]]; then
      local key="${BASH_REMATCH[1]}"
      local value="${BASH_REMATCH[2]}"
      local lower_key lower_value
      lower_key="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]')"
      lower_value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
      if [[ "$lower_key" == *password* || "$lower_key" == *secret* || "$lower_key" == *key* || \
            "$lower_key" == *token* ]]; then
        if is_literal_secret_value "$lower_value"; then
          report_fail "$file" "$line_no" "biến env '$key' trong workflow gán giá trị literal. Dùng \${{ secrets.X }}."
        fi
      fi
      # Chuỗi kết nối có Password=literal (v.d. TEST_DATABASE)
      if [[ "$lower_value" == *'password='* ]]; then
        local pw="$(printf '%s' "$lower_value" | sed -n 's/.*password=\([^;]*\).*/\1/p')"
        if is_literal_secret_value "$pw"; then
          report_fail "$file" "$line_no" "biến env '$key' chứa mật khẩu literal trong chuỗi kết nối."
        fi
      fi
    else
      in_env=0
    fi
  done < "$file"
}

# 4) Token/khoá riêng của dịch vụ phổ biến xuất hiện ở bất kỳ file nào.
scan_known_token_patterns() {
  local pattern description file
  while IFS='|' read -r pattern description; do
    [ -n "$pattern" ] || continue
    while IFS= read -r hit; do
      file="${hit%%:*}"
      local rest="${hit#*:}"
      report_fail "$file" "${rest%%:*}" "$description"
    done < <(git grep -nIE "$pattern" -- . \
      ':!docs' ':!.env.example' ':!**/*.example' ':!deploy/scan-secrets.sh' ':!deploy/scan-secrets.test.sh' 2>/dev/null || true)
  done <<'PATTERNS'
ghp_[A-Za-z0-9]{36,}|GitHub personal access token (ghp_)
github_pat_[A-Za-z0-9_]{50,}|GitHub fine-grained token
-----BEGIN (RSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|Khoá private bị commit
AKIA[0-9A-Z]{16}|AWS access key id
sk-[A-Za-z0-9]{32,}|OpenAI-style API key
xox[baprs]-[A-Za-z0-9-]{10,}|Slack token
PATTERNS
}

main() {
  echo "== N1-8/W5-10: quét secret trong repo =="

  check_yaml_secret_keys render.yaml
  check_yaml_secret_keys docker-compose.dev.yml

  # W5-10: quét mọi appsettings + workflow (không chỉ 2 file ở trên).
  local json_file workflow_file
  while IFS= read -r json_file; do check_json_config_secrets "$json_file"; done \
    < <(find src/backend -name 'appsettings*.json' -type f ! -path '*/bin/*' ! -path '*/obj/*')
  while IFS= read -r workflow_file; do check_workflow_env_secrets "$workflow_file"; done \
    < <(find .github/workflows -name '*.yml' -o -name '*.yaml' | sort)

  scan_known_token_patterns

  if [ "$failures" -gt 0 ]; then
    echo ""
    echo "Phát hiện $failures secret bị gửi vào repo." >&2
    echo "Cách xử lý: đổi sang placeholder \${VAR} / \${{ secrets.* }} / 'sync: false'/'fromService'" >&2
    echo "(Render tự hỏi khi deploy), và nếu secret ĐÃ bị push thì phải rotate giá trị đó" >&2
    echo "— xoá trong file không đủ." >&2
    exit 1
  fi

  echo "OK: không phát hiện secret bị gửi vào repo."
}

if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
  main
fi