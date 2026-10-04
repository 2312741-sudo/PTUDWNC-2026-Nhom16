#!/usr/bin/env bash
# N2-C6 — chặn regression coverage của tầng Application.
#
# Coverlet xuất Cobertura; script đọc line-rate của package `CulinaryBlog.Application` và
# fail nếu thấp hơn ngưỡng (mặc định 80%). Chỉ chạy ở CI (runner ubuntu có sẵn awk);
# máy TV4 không có bash nên kiểm số liệu tương đương bằng cách đọc trực tiếp
# TestResults/**/coverage.cobertura.xml (xem SO_EVIDENCE_TUAN_4.md).
#
# Dùng:  bash deploy/check-coverage.sh [nguong-phan-tram] [thu-muc-ket-qua]
set -euo pipefail

THRESHOLD="${1:-80}"
RESULTS_DIR="${2:-TestResults}"
PACKAGE="CulinaryBlog.Application"

report="$(find "$RESULTS_DIR" -name 'coverage.cobertura.xml' -print -quit 2>/dev/null || true)"
if [ -z "$report" ]; then
  echo "::error::Không tìm thấy coverage.cobertura.xml trong $RESULTS_DIR — test chưa chạy coverage?"
  exit 1
fi

# <package name="CulinaryBlog.Application" line-rate="0.8434" ...> nằm chung một dòng.
rate="$(awk -v pkg="$PACKAGE" '
  index($0, "<package ") == 0 { next }
  index($0, "name=\"" pkg "\"") == 0 { next }
  match($0, /line-rate="[0-9.]+"/) {
    print substr($0, RSTART + 11, RLENGTH - 12)
    exit
  }
' "$report")"

if [ -z "$rate" ]; then
  echo "::error::Không đọc được line-rate của $PACKAGE trong $report"
  exit 1
fi

pct="$(awk -v r="$rate" 'BEGIN { printf "%.2f", r * 100 }')"
echo "$PACKAGE line coverage = ${pct}% (ngưỡng ${THRESHOLD}%)"

if ! awk -v r="$rate" -v t="$THRESHOLD" 'BEGIN { exit !((r * 100) >= t) }'; then
  echo "::error::Line coverage ${pct}% thấp hơn ngưỡng ${THRESHOLD}% — nghĩa là đã mất test hoặc thêm code chưa phủ."
  exit 1
fi

echo "✔ Đạt ngưỡng coverage $PACKAGE."
