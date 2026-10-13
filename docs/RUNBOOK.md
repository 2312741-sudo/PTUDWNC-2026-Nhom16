# RUNBOOK — Vận hành CulinaryBlog (TV4)

> Mục đích: quy trình vận hành **đã chạy thật** cho hệ thống CulinaryBlog — dựng stack từ đầu,
> backup/restore, đối phó khi một dependency sập, chạy 2 API instance, đo tải, và truy vết
> log/theo dõi khi có sự cố. Mỗi mục đều có **số liệu hoặc log thật**; mục nào chưa đo được ở
> môi trường này thì ghi rõ "chưa đo" + lý do — không có mục "nên làm".
>
> Tác giả: TV4 (Nguyễn Hữu Trung Sơn) — W5-5 (N4-A). Ngày: 10/10/2026.
> Số liệu kéo từ `docs/evidence/TV4/Tuan04/…`, `docs/evidence/TV4/Tuan05/logs/*.log`,
> `docs/evidence/TV4/Tuan04/logs/c1_outage_drill.log`.

---

## A1 — Dựng stack từ đầu (clean checkout → healthy)

**Thời điểm cần dùng:** máy mới, repo mới clone, hoặc stack bị dọn sạch.

```bash
git clone <repo> && cd PTUDWNC-2026-Nhom16
# Bắt buộc: khai biến môi trường (không commit). Thiếu JWT_SIGNING_KEY thì compose fail-fast exit 1.
$env:POSTGRES_PASSWORD="..."      # cùng giá trị với `.env` dùng cho hạ tầng
$env:JWT_SIGNING_KEY="$(openssl rand -base64 48)"   # >= 64 byte UTF-8, đặt vào .env
docker compose -f docker-compose.staging.yml up -d --build
docker compose -f docker-compose.staging.yml ps   # tất cả healthy
```

**Kiểm tra lên thật:**
```bash
curl http://localhost/health/ready     # 200 (qua nginx TLS bắt buộc dùng https + -k)
curl -I http://localhost/              # 301 -> https://localhost/  (HSTS)
curl -k https://localhost/             # 200
curl -k https://localhost/sitemap.xml  # 200 (Published-only)
```

**Số liệu đã đo:**
- Lần chạy lại với image đã cache (run 2, 07/10): `up` exit 0, **thời gian = 24s** đến khi
  `/health/ready` HTTP 200 (`Tuan05/logs/staging_run2.log`).
- Lần đầu có build image (`--build`, run 1): build backend + frontend xong rồi mới lên
  container; `/health/ready` 200 (đã qua 1 lần sửa `Redis__Host` — xem A6). Toàn bộ log:
  `Tuan05/logs/staging_run1.log`.
- **Chưa đo:** thời gian dựng trên một máy thật sự sạch (không có image cache) — chưa có máy
  mới. Ước đủ theo run 1 + nhánh CI (build ~ 3–4 phút) nên vẫn thoả dưới ngưỡng 5 phút, nhưng
  không tuyên bố đã chứng minh `<5 phút` trên máy sạch.

**Lưu ý đã vấp (bẫy):**
- Thiếu `JWT_SIGNING_KEY` → API exit ngay với `InvalidOperationException` và crash-loop; compose
  đã fail-fast ngay từ `config` (thoát 1). Nhánh `docker compose ... up` bị "mất env"
  đến từ việc chạy từ một shell không có biến → **luôn chạy từ shell có `.env` đầy đủ**.
- `/images` upload ảnh: nếu container API được tạo với env thiếu thì register/upload lỗi — kiểm
  bằng `docker compose config` trước khi `up`.

---

## A2 — Backup / restore PostgreSQL

Script: `deploy/backup-db.sh` (dùng `pg_dump` bản PG18, kết nối sang DB PG16 trong Docker).

**Sao lưu (backup):**
```bash
bash deploy/backup-db.sh                     # sinh /tmp/backups/culinary_<UTC>.dump, giữ 30 ngày
```
Đã đo 07/10 trên staging: file `culinary_20261007T145532Z.dump` = **287.387 bytes (0.3 MB)**,
dọn 0 file cũ (`logs/staging_backup.log`).

**Khôi phục (restore):**
```bash
# 1) backup file ở host -> 2) hạ hết API để không ghi đè DB -> 3) restore -> 4) lên lại API
docker compose -f docker-compose.staging.yml stop culinary-api     # + culinary-api-2
docker exec -i staging-culinary-db psql -U postgres -d culinary_blog < /tmp/backups/culinary_<UTC>.dump
docker compose -f docker-compose.staging.yml start culinary-api     # /health/ready 200
psql ... -c "\dt public.*" | wc -l
```

**Kết quả drill thật (đã chạy, không phải giả định):**
- Số bảng public sau restore = **14/14**, khớp trước backup (postgresql: N1 03/10 = 14 bảng,
  staging 07/10 = 14 bảng) — `logs/staging_restore.log` + `Tuan04/HANDOFF_TV4_TUAN4_N1.md`.
- Truy vấn lại 3 danh mục đầu (`Món khai vị | mon-khai-vi`, …) chạy đúng sau restore.

**Giới hạn:** backup ghi cục bộ 1 tệp, giữ 30 ngày — **chưa** backup offsite hoặc 7 ngày theo
lịch tự động trên server (chưa có server thật).

---

## A3 — Failover khi một dependency sập

Drill full: `docs/evidence/TV4/Tuan04/logs/c1_outage_drill.log`. Mô phỏng theo **từng**
dependency (lab 30/09, runner chạy `docker stop` lần lượt):

| Dependency bị dừng | Nhận biết | Hồi phục thật |
|---|---|---|
| Redis | `/health/ready` → 503 (mất Redis); API vẫn trả **200** cho danh sách nhờ fallback in-process cache | `RECOVER redis.list **0.2s** → 200`, `redis.ready 0.0s → 200` |
| S3 (object storage) | ảnh → **503**, `/health/ready` → 503 | _measured during drill (S3) ~ 0.5s_ |
| API/node duy nhất | client thấy **connection refused** — node là SPOF, **không** có failover | sau khi `up` lại, ready 200 |

Trên staging (W5-3, 08/10) — vận hành **2 instance** nên không còn SPOF:
- Dừng `staging-culinary-api-2`: **8/8 request 200** (api-1 gánh hết), nginx `proxy_next_upstream`
  chuyển upstream tự động (`logs/staging_two_api.log`, mục 4).
- Bật lại api-2: 8/8 request 200, quay lại trải đều.

**Quy tắc:** khi một API instance không ready, nginx ngừng gửi traffic tới nó (`max_fails=3`,
`fail_timeout=10s`, `proxy_next_upstream error timeout invalid_header http_5xx`,
`proxy_connect_timeout 2s`).

---

## A4 — Vận hành 2 API instance (scale ngang)

- Định nghĩa: `docker-compose.staging.yml` — `culinary-api` (mặc định, host 5080) +
  `culinary-api-2` (profile `multi`, host 5081). Nginx upstream nêu ở A3.
- Bật thêm instance: `docker compose -f docker-compose.staging.yml --profile multi up -d`
- **Kiểm chứng đã chạy thật (08/10)** — `logs/staging_two_api.log`:
  - 12 request tới nginx trải **50/50** (6 api-1, 6 api-2), 0 non-200.
  - Failover: stop api-2 → 0/8 non-200; start lại → phục hồi.
  - **Hangfire có 2 server** (bảng `hangfire."Server"` trong DB Postgres — dùng chung storage +
    khoá phân tán nên số lịch sinh trùng = 0).
  - Cache/queue Redis dùng chung (`Redis__Host: culinary-redis`), sitemap không chạy trùng.

---

## A5 — Đo tải bằng k6

Kịch bản: `tests/performance/read-load.js` (đã đo ở dev tuần 4 — K22; **sẽ đo trên staging ở
W5-8**). Hướng dẫn đầy đủ: `tests/performance/README.md`.

```bash
# cần 3 thứ ghi kèm số liệu: máy đo / trạng thái cache / quy mô dữ liệu
docker run --rm --network host -v "$PWD/tests/performance:/scripts:ro" \
  -e BASE_URL=http://localhost:5080 grafana/k6 run /scripts/read-load.js
# muốn đo đúng đường "xuống DB" thì dọn cache trước:
docker exec culinaryblog-redis redis-cli FLUSHDB
# tham số: RATE, DURATION, PRE_VUS, MAX_VUS, THRESHOLD_CACHED_MS / UNCACHED / MIXED
```

NGƯỠNG LÀ HỢP ĐỒNG: chạy lại mà p95 vượt `200ms cache` / `800ms uncached` / `500ms mixed`
là có hồi quy cần điều tra, không phải "chạy lại cho đỏ". Muốn chỉ đo không chặn:
thêm `--no-thresholds`.

**Trạng thái hiện tại:** số k6 hợp lệ mới có ở môi trường **dev** (tuần 4). Trên staging đang
chưa đo (W5-8 cũng đang chờ). Vì vậy mục này ở staging ghi **chưa đo** + nêu rõ điều kiện phải
ghi kèm khi đo (máy, cache ấm/lạnh, số recipe/category, số lần chạy).

---

## A6 — Truy vết một request theo TraceId (HTTP → EFCore → Postgres)

**Đã chứng minh thật:**
- Dev (N1, 30/09): cùng một **TraceId**, có span HTTP cha `GET /api/v1/recipes/` và span con
  `db.system=postgresql` + `peer.service=127.0.0.1` — log gốc `Tuan04/logs/seq_trace_recipes.log`.
- Staging (W5-2.5, 07/10): `logs/staging_trace.log` — `GET /api/v1/recipes?sort=...` (key mới
  chưa cache) sinh span `RequestLoggingMiddleware` + `Microsoft.EntityFrameworkCore.Database.Command.
  CommandExecuted`, **cùng CorrelationId**, log vào Seq qua `host.docker.internal:5341`.

**Cách tra:** gửi 1 request, lấy TraceId từ header response hoặc từ log Serilog
(`"TraceId": "…"`), rồi tìm trong Seq:
```bash
# API log ra Seq (biến Seq__Url) hoặc log console:
docker logs staging-culinary-api --since 5m | Select-String "TraceId|DB update|/api/v1/recipes"
# Trong Seq UI (A7): tìm "TraceId == '<id>'" -> xem span HTTP + span EF trong cùng trace.
# Điều kiện: request đó phải THỰC SỰ xuống DB (không trúng cache Redis).
```

**Bẫy đã gặp:** `/api/v1/categories` trúng cache nên lần sau không còn span EF — phải dùng
request có key mới (ví dụ `recipes?sortBy=createdAt`). Lúc đầu span EF không hiện vì default
filter `Microsoft.*` = Warning → cấu hình `Logging__LogLevel__Microsoft.EntityFrameworkCore:
Information` (đã set trong `x-api-env`).

---

## A7 — Tìm log tập trung (Seq)

- Seq là **instance dev** (`docker-compose.dev.yml`, container `culinaryblog-seq`, image
  `datalust/seq:2026.1`, web UI + intake **http://localhost:5341**, auth tắt cho dev).
- Staging API gửi log sang Seq **của host** qua `Seq__Url: http://host.docker.internal:5341`
  (docker-compose.staging.yml) — trên server thật thay bằng URL Seq của môi trường đó.
- Thao tác cơ bản:
  1. Mở `http://localhost:5341`.
  2. Lọc nhanh: `TraceId == '<id>'` (xem A6) hoặc `@MessageTemplate like '%EFCore%'`.
  3. Tìm lỗi 5xx: `Level in ['Error','Fatal']` + xem bối cảnh bằng CorrelationId/TraceId.
  4. Xem span EFCore: nhập `Microsoft.EntityFrameworkCore.Database.Command`.
  5. Điểm nhận log của API qua OTLP: `docker compose -f docker-compose.dev.yml logs otel-collector`.

**Giới hạn:** Seq staging dùng chung instance dev ở host — không phải hạ tầng production; chưa
có retention/chỉ mục tối ưu theo ngày trên staging.

---

## Giới hạn chung (cần biết trước khi tin số liệu)

1. **TLS chỉ là self-signed** (CN=localhost, sinh vào volume `staging_certs`, 10 năm) — đúng cho
   dev/staging chạy trên máy dev, **không** phải chứng thư public: không được tuyên bố đạt
   TLS production / `NFR-SEC-005` 100%.
2. **SLA 99,5% chưa đo** (`NFR-REL-001`): staging chạy trên máy dev, `render.yaml` vẫn `plan: free`.
3. **Backup**: cục bộ, 1 tệp, 30 ngày — chưa có backup ngoài máy server + chưa có lịch tự động
   trên server thật (mới drill bằng tay).
4. **Số đo load/SEO/cache-hit/metrics scraping trên staging đang chưa có** (dự kiến W5-8) —
   hiện chỉ có số đo dev (k6 tuần 4, EXPLAIN tuần 4).
5. **Rate limit login theo IP**; khi qua nginx, mọi request từ host nhìn chung một bucket
   (gateway) — số "2 IP riêng bucket" mới đúng khi gọi thẳng API với header XFF hợp lệ
   (`BUG-W4-03` đã sửa).
6. **Chưa đo thời gian khởi động trên máy sạch 100%** (chỉ có số với image cache) — xem A1.