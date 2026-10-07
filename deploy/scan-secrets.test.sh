#!/usr/bin/env bash
# W5-10 (TV4 08/10): test cho deploy/scan-secrets.sh.
# Chạy:  bash deploy/scan-secrets.test.sh   (hoặc Git Bash)
# Ý tưởng: source script để dùng riêng từng hàm quét lên file fixture (tạo tạm, xoá sau),
# assert số lỗi tìm được với kỳ vọng. Cuối cùng chạy full scan trên repo thật (phải exit 0).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=deploy/scan-secrets.sh
source "$SCRIPT_DIR/scan-secrets.sh"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

pass=0
fail=0
check_case() { # name expected_failures file fn...
  local name="$1" expected="$2" file="$3" fn="$4"
  shift 4
  failures=0
  "$fn" "$file"
  if [ "$failures" -eq "$expected" ]; then
    pass=$((pass + 1))
    printf '  PASS  %s (tim %s loi)\n' "$name" "$expected"
  else
    fail=$((fail + 1))
    printf '  FAIL  %s (ky vong %s loi, tim duoc %s)\n' "$name" "$expected" "$failures"
  fi
}

echo "== W5-10: test scan-secrets =="

# --- JSON ---
printf '%s\n' '{"Jwt":{"SigningKey":""},"ConnectionStrings":{"Database":"Host=x;Port=1;Password=${DB_PASSWORD}"}}' > "$TMP/pos.json"
check_case "JSON: SigningKey rong + Password=ductrinh placeholder -> 0 loi" 0 "$TMP/pos.json" check_json_config_secrets

printf '%s\n' '{"Minio":{"AccessKey":"minioadmin","SecretKey":"minioadmin"}}' > "$TMP/neg_minio.json"
check_case "JSON: AccessKey/SecretKey literal -> 2 loi" 2 "$TMP/neg_minio.json" check_json_config_secrets

printf '%s\n' '{"ConnectionStrings":{"Database":"Host=h;Password=postgres"}}' > "$TMP/neg_cs.json"
check_case "JSON: chuoi ket noi Password=literal -> 1 loi" 1 "$TMP/neg_cs.json" check_json_config_secrets

printf '%s\n' '{"Jwt":{"SigningKey":"changeme"}}' > "$TMP/ok_placeholder.json"
check_case "JSON: gia tri changeme -> 0 loi" 0 "$TMP/ok_placeholder.json" check_json_config_secrets

# --- Workflow env ---
cat > "$TMP/pos_wf.yml" <<'YML'
jobs:
  test:
    env:
      TOKEN: ${{ secrets.MY_TOKEN }}
      ACCESS: ci-${{ github.run_id }}
      EMPTY_VAL: ""
YML
check_case "Workflow: secrets.* + ci-* + empty -> 0 loi" 0 "$TMP/pos_wf.yml" check_workflow_env_secrets

cat > "$TMP/neg_wf.yml" <<'YML'
jobs:
  test:
    env:
      TOKEN: hardcoded-secret-value
YML
check_case "Workflow: TOKEN literal -> 1 loi" 1 "$TMP/neg_wf.yml" check_workflow_env_secrets

cat > "$TMP/neg_wf_cs.yml" <<'YML'
jobs:
  test:
    env:
      DATABASE_URL: Host=h;Password=supersecret
YML
check_case "Workflow: chuoi ket noi Password=literal -> 1 loi" 1 "$TMP/neg_wf_cs.yml" check_workflow_env_secrets

# --- render.yaml dạng envVar ---
cat > "$TMP/pos_render.yaml" <<'YML'
envVars:
  - key: JWT__SigningKey
    sync: false
YML
check_case "render: sync: false -> 0 loi" 0 "$TMP/pos_render.yaml" check_yaml_secret_keys

cat > "$TMP/neg_render.yaml" <<'YML'
envVars:
  - key: Jwt__SigningKey
    value: abcdef
YML
check_case "render: value literal -> 1 loi" 1 "$TMP/neg_render.yaml" check_yaml_secret_keys

# --- Token pattern dương tính (file fixture nằm ngoài git grep nên bỏ qua; kiểm mẫu bằng repo thật ở bước cuối) ---

echo ""
if [ "$fail" -gt 0 ]; then
  echo "KET QUA: $pass pass, $fail FAIL -> KHONG DAT"
  exit 1
fi

# --- Tích hợp: full scan repo thật phải exit 0 (repo đang sạch secret thật) ---
if bash "$SCRIPT_DIR/scan-secrets.sh" >/dev/null 2>&1; then
  echo "KET QUA: $pass pass, $fail fail + full-scan repo exit 0 -> DAT"
else
  echo "KET QUA: $pass pass, $fail fail nhung full-scan repo FAIL (exit != 0)"
  exit 1
fi