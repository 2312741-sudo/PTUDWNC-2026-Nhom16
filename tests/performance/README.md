# tests/performance — kịch bản k6

Kịch bản: `read-load.js` (N2-C3). Đo tải các endpoint **đọc công khai** và tách riêng
đường đọc cache với đường xuống Postgres, vì gộp lại sẽ che mất việc đường DB chậm hơn bao nhiêu.

## Chạy

Máy này **không có binary `k6`**, nên dùng Docker:

```bash
# mặc định: 20 req/s mỗi scenario, 30s, chạy đủ 3 nhánh
docker run --rm --network host -v "$PWD/tests/performance:/scripts:ro" \
  -e BASE_URL=http://localhost:5080 grafana/k6 run /scripts/read-load.js

# sạch cache trước khi muốn đo đường DB (bắt buộc nếu ghi "uncached")
docker exec culinaryblog-redis redis-cli FLUSHDB
```

Biến môi trường (không bắt buộc):

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `BASE_URL` | `http://localhost:5080` | gốc API |
| `RATE` | `20` | request/giây **cho mỗi** scenario |
| `DURATION` | `30s` | thời lượng mỗi scenario |
| `PRE_VUS` / `MAX_VUS` | `50` / `100` | VU dự trữ / tối đa |
| `THRESHOLD_CACHED_MS` | `200` | ngưỡng p95 nhánh cache |
| `THRESHOLD_UNCACHED_MS` | `800` | ngưỡng p95 nhánh xuống DB |
| `THRESHOLD_MIXED_MS` | `500` | ngưỡng p95 nhóm workload thật |

Ngưỡng là **hợp đồng**: máy khác chạy lại mà đỏ nghĩa là có hồi quy, cần điều tra chứ không phải
"chạy lại cho đỏ". Muốn chỉ đo, không chặn:

```bash
docker run --rm --network host -v "$PWD/tests/performance:/scripts:ro" \
  grafana/k6 run --no-thresholds /scripts/read-load.js
```

## Điều kiện bắt buộc phải ghi kèm số liệu

Báo cáo p50/p95/p99 mà không ghi kèm mấy thứ sau thì vô nghĩa, vì cùng một con số có thể từ
tải rất khác nhau:

1. **Máy đo**: CPU, RAM, OS.
2. **Trạng thái cache**: cache ấm hay vừa `FLUSHDB`. Chạy lại ngay sau lần trước thì phần lớn
   request đã trong Redis và p95 sẽ đẹp hơn thực tế.
3. **Quy mô dữ liệu**: số recipe/category trong DB.
4. **Chạy bao nhiêu lần**: lấy số của lần giữa, đừng lấy lần đẹp nhất.

Kết quả đã đo: `docs/evidence/TV4/Tuan04/SO_EVIDENCE_TUAN_4.md`, mục TV4-K22.
Log gốc: `docs/evidence/TV4/Tuan04/logs/`.
