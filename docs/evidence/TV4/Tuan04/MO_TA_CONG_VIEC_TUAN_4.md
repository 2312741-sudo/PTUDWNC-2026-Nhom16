# MÔ TẢ CÔNG VIỆC TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> Tài liệu mô tả **chi tiết việc cần làm trong tuần 4** dựa trên SRS v1.1.1, task **D5, D6, D7**
> (`docs/KE_HOACH_DU_AN.md` mục 8) và giao việc tuần 4 của TV4 (`docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4).
> Kế hoạch tổng quan: [`KE_HOACH_TUAN_4_TV4.md`](KE_HOACH_TUAN_4_TV4.md) ·
> Sổ minh chứng: [`SO_EVIDENCE_TUAN_4.md`](SO_EVIDENCE_TUAN_4.md) ·
> Trạng thái: [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](TRANG_THAI_THUC_HIEN_TUAN_4.md)

> [!NOTE]
> Mọi hạng mục dưới đây là **việc chưa làm**. Phần đã xong ở tuần 1–3 nằm trong `Tuan01/`–`Tuan03/`.
> Ba đề xuất **B1, B2, B6** cần nhóm trưởng duyệt trước khi làm; **B4** thì TV4 có thể tự làm
> vì đã có đề xuất sẵn trong `docs/proposal/DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md`.

---

## 1. Tổng quan tuần 4

TV4 gánh 3 task cuối của mình, tất cả đều là **phần đo và bảo đảm** chứ không phải tính năng mới:

| Nhóm | Task | Mã | Nội dung | Điểm |
|---|---|---|---|---|
| Vận hành thật | D5 | D5 | Health xác thực credential, observability có bằng chứng, backup/restore, 2 instance dùng chung state | 12đ |
| Kỹ năng cá nhân | D6 | D6 | Lab L5 + bù mục 2 của L4 (Identity/Google/refresh/forms/FTS) | 25đ |
| Bàn giao & phát hành | D7 | D7 | E2E, resilience, kịch bản tấn công file, cổng CI frontend, runbook, release | 13đ |

> Điểm số giữ nguyên như kế hoạch tổng thể; không phải thang điểm của giảng viên.

---

## 2. Chi tiết công việc

### 2.1. D5 — Vận hành: health, observability, backup, multi-instance

#### 2.1.1. Health check xác thực credential object storage (block B4)

**Vấn đề hiện tại** — `src/backend/CulinaryBlog.API/Health.cs:36-43`:

```
MinIOHealthCheck → TcpHealthCheckHelper.CheckTcpAsync(host, port)
```

Chỉ mở TCP. Cổng 9000 mở nhưng `AccessKey`/`SecretKey` sai ⇒ `/health` vẫn báo `Healthy`,
trong khi `POST /recipes/{id}/images` trả `500`. Đó chính xác là nguyên nhân gốc lỗi upload 500
đã ghi ở `docs/report/BAO_CAO_LOI_UPLOAD_ANH_500.md`.

**Cần làm**

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Thêm `ObjectStorageHealthCheck` dùng `IObjectStorageReader` (đã có từ D27) để kiểm tra bucket tồn tại / đọc được object mẫu | Không gọi `MakeBucket` trong health check (không được tạo tài nguyên khi chỉ đo) |
| 2 | Phân biệt 3 kết quả: `Healthy` (đọc được) · `Degraded` (TCP OK nhưng auth lỗi) · `Unhealthy` (không kết nối) | `Degraded` là thông tin quan trọng nhất — đó là trạng thái gây lỗi 500 |
| 3 | Giữ `MinIOHealthCheck` (TCP) làm check phụ để phân biệt "container chết" với "sai credential" | Tag: `all` (không đưa vào `ready` để không chặn traffic vì storage lỗi, theo D22) |
| 4 | Timeout 3 s, không log credential | NFR-SEC-007 |
| 5 | Test: storage thật (RustFS local) ⇒ `Healthy`; trỏ sai endpoint ⇒ `Degraded`/`Unhealthy` | Test phải **fail được** khi hành vi sai |

#### 2.1.2. `/health/ready` trả 503 khi dependency chết

**Hiện tại**: `tests/CulinaryBlog.Tests/HealthTests.cs:41` chấp nhận **cả 200 lẫn 503** ⇒ test
luôn xanh, không chứng minh được gì.

**Cần làm**

| # | Việc | Kỳ vọng |
|---|---|---|
| 1 | Thay kỳ vọng mơ hồ bằng kỳ vọng dứt khoát theo ADR **D22** | Redis không reachable ⇒ `/health/ready` **503**, body `status: Unhealthy`, entry `redis.status = Unhealthy` |
| 2 | `/health/live` **vẫn 200** khi DB/Redis chết (liveness = tiến trình còn sống) | Không được dùng liveness để đánh giá dependency |
| 3 | Kịch bản thật: dừng container `redis` → gọi probe → khởi động lại → probe xanh | Lưu log vào `Tuan04/logs/health_ready_503.log` |
| 4 | Đặt `ResultStatusCodes` tường minh khi map health check | Không phụ thuộc hành vi mặc định của framework |

#### 2.1.3. Observability có nơi nhận và có bằng chứng

**Hiện tại**: OTEL đã cấu hình nhưng `AddOtlpExporter()` **không có endpoint**; Serilog chỉ ghi
console; container `datalust/seq` có trong compose nhưng **không service nào gửi log/traces tới**.

**Cần làm**

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Thêm service `otel-collector` vào `docker-compose.dev.yml` + file cấu hình nhận OTLP, xuất ra console/Seq | Không thêm service mà không ghép cấu hình — tránh lặp lại kiểu "có container, không ai dùng" |
| 2 | `AddOtlpExporter()` đọc endpoint từ cấu hình/env (`OTEL_EXPORTER_OTLP_ENDPOINT`) | Production đặt qua env |
| 3 | Thêm `Serilog.Sinks.Seq` → log có `CorrelationId` | Cần regenerate `packages.lock.json` (**không** dùng `--locked-mode` khi thêm package) |
| 4 | **Chụp trace thật**: 1 request `GET /api/v1/recipes` → thấy span HTTP → span `Npgsql`/EF → SQL | Đây là tiêu chí nghiệm thu của D5 (FR-OBS-003, "trace nối HTTP→DB") |
| 5 | Lưu bằng chứng: `Tuan04/logs/seq_trace_recipes.log` + ảnh chụp dashboard | Chỉ dẫn request vừa thao tác (NFR-MAINT-003) |

#### 2.1.4. Backup / restore (hiện chưa tồn tại)

**Hiện tại**: không có script, không có lịch, không có drill. `pg_dump` chỉ xuất hiện trong văn bản SRS.

**Cần làm**

| # | Việc | Ghi chú |
|---|---|---|
| 1 | `deploy/backup.sh`: `pg_dump` (format custom) + nén + xoay vòng **giữ 30 ngày** | Lịch **03:00**; **timezone là quyết định của nhóm** — không tự suy ra UTC chỉ vì sitemap dùng UTC |
| 2 | `deploy/restore.sh`: khôi phục lên DB sạch, có xác nhận trước khi ghi đè | Không chạy restore lên DB đang dùng mà không hỏi |
| 3 | Backup **volume file** ảnh (không chỉ DB) | D08 giữ ảnh khi soft delete ⇒ file là dữ liệu cần khôi phục |
| 4 | **Một drill thật**: backup → xoá dữ liệu → restore → đối chiếu số bảng/số row/số object | Log `Tuan04/logs/backup_restore_drill.log` |
| 5 | Ghi rõ giới hạn: chưa có giải pháp lưu trữ ngoài (nfs/S3) | NFR-REL-003 ghi "chưa nghiệm thu triển khai" nếu chỉ có thiết kế |

#### 2.1.5. Hai API instance dùng chung state

**Hiện tại**: `RecipeCacheService` là `ConcurrentDictionary` **trong tiến trình**; toàn repo có
**0** dòng dùng `StackExchange.Redis`; `render.yaml` để `plan: free`.

**Cần làm** — chọn **một** trong hai hướng và **ghi ADR**:

| Hướng | Việc | Khi nào chọn |
|---|---|---|
| **A — Chia sẻ qua Redis** | Redis 7 đã có sẵn (AOF) → thay cache in-process bằng `IDistributedCache`; mọi instance thấy cùng dữ liệu | Nếu muốn số đo p95/p99 trên 2 instance là đáng tin |
| **B — Ghi giới hạn rõ ràng** | ADR nêu rõ cache hiện là per-instance, số đo chỉ áp dụng 1 instance, tuần 5 đo lại trên staging | Nếu thiếu thời gian — **không được** đo rồi báo như đang chạy 2 instance |

Bắt buộc chung cho cả hai hướng: 2 API instance dùng **chung PostgreSQL** và **chung Hangfire
queue**; refresh token phải sống qua restart; sitemap **không được sinh trùng** (xem 2.1.6).

#### 2.1.6. Sitemap theo lịch 02:00 UTC + distributed lock (CR-7)

**Hiện tại**: chỉ có endpoint on-demand `GET /recipes/sitemap`; không có `RecurringJob`/`Cron`.

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Đăng ký `RecurringJob` cron `0 2 * * *` (UTC) sinh sitemap từ `GetPublishedForSitemapAsync` | Dữ liệu đã Published-only sẵn |
| 2 | **Distributed lock**: `[DisableConcurrentExecution]` trên job **hoặc** lock Redis | `WorkerCount = 4` ⇒ không có lock thì 4 worker chạy cùng lúc |
| 3 | Test: 2 worker, đếm số lần job chạy trong 1 chu kỳ | Bằng chứng "không sinh trùng" |
| 4 | Retry 2 lần theo mô tả riêng của job sitemap | Không dùng chung lịch retry với resize |
| 5 | Ghi rõ trong runbook: sitemap **có độ trễ tối đa 24h**, hoặc thêm trigger refresh sau publish/unpublish bằng ADR | Không hứa "cập nhật tức thì" khi chỉ có cron |

#### 2.1.7. No-secrets

| # | Việc | Ghi chú |
|---|---|---|
| 1 | `render.yaml:18-19` đang **hardcode JWT signing key** → chuyển sang biến môi trường/secret store | NFR-SEC-007 |
| 2 | Thêm bước **secret scan** vào CI (fail nếu phát hiện) | K10/K24 |
| 3 | Không tự rotate key đang dùng trên môi trường đang chạy | Cần nhóm trưởng quyết định |

---

### 2.2. D7 — E2E, tấn công file, CI, số đo

#### 2.2.1. Hạ tầng Playwright (điều kiện tiên quyết cho 4 ô kỹ năng)

| # | Việc | Ghi chú |
|---|---|---|
| 1 | `npm i -D @playwright/test` trong `src/frontend` + script `test:e2e` | Hiện `@playwright/test` **không** có trong `package.json` |
| 2 | `playwright.config.ts`: `baseURL` từ env, `webServer` tự bật `next start` | Không hardcode cổng |
| 3 | `tests/e2e/` trong `src/frontend` (hoặc `tests/e2e` ở gốc) — chọn một và ghi rõ | Phải nằm trong `package.json` scripts để tái lập được |
| 4 | Dùng **tài khoản seed**, không tạo dữ liệu thật | NFR-SEC-007 |

#### 2.2.2. Năm luồng E2E bắt buộc

| Luồng | Chủ sở hữu | Ghi chú cho TV4 |
|---|---|---|
| Đăng ký | TV1 | TV4 chỉ cần biết nó có để không viết trùng |
| Đăng nhập | TV1 | — |
| Tạo công thức | TV3 | — |
| **Xuất bản** | **TV4** | Tạo Draft → thêm ≥1 nguyên liệu + ≥1 bước → `PATCH /publish` → xuất hiện ngoài trang public; ngược lại 422 `RECIPE_PUBLISH_INCOMPLETE` |
| **Tìm kiếm** | **TV4 (chung TV2)** | **Tự động hoá TC1–TC12** của `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` → chặn tái phát đúng lỗi đã gặp |

#### 2.2.3. Cổng CI cho frontend (nguyên nhân gốc lỗi 500)

`.github/workflows/backend.yml` hiện **chỉ** chạy 4 bước .NET. Lỗi `500` ở mọi request của
`/search` lọt vào `main` vì không có bước nào build/test frontend.

| # | Việc | Kỳ vọng |
|---|---|---|
| 1 | Workflow (mới hoặc bước mới): `actions/setup-node` → `npm ci` → `tsc --noEmit` → `next build` → `next lint` | Xanh trên `main` |
| 2 | Xác minh: `next build` có bắt được lỗi RSC không, hay chỉ E2E mới bắt | Quyết định bước CI có đủ hay cần cả smoke test HTTP |
| 3 | Chạy E2E search trên CI (tối thiểu luồng search) | Trùng lỗi ⇒ CI đỏ trước khi merge |

#### 2.2.4. Kịch bản tấn công file (E2E, không chỉ unit)

Hiện chỉ có unit test validator (`ImageUploadValidatorTests`). Cần thêm kịch bản ở mức HTTP thật:

| # | Kịch bản | Kỳ vọng |
|---|---|---|
| 1 | File **đúng 5 MiB** | 201 (biên trên) |
| 2 | File **5 MiB + 1 byte** | 400 `FILE_SIZE_EXCEEDED`, **không** phải 500 |
| 3 | Đổi tên file nhị phân thành `.jpg` (MIME giả) | 400 `FILE_MIME_INVALID` |
| 4 | File rỗng 0 byte | 400, không 500 |
| 5 | File JPEG cắt cụt (header đúng, dữ liệu hỏng) | 201 (resize fallback original) hoặc 400 — **ghi rõ hành vi đã chọn** |
| 6 | Nhiều request upload liên tiêp | Có rate limit 5/phút/IP ⇒ 429 + `Retry-After` (NFR-SEC-003) |
| 7 | Upload lên recipe của người khác | 403, **không ghi object** (rò object kiểu D1) |

#### 2.2.5. Resilience có số liệu

| # | Kịch bản | Cần đo |
|---|---|---|
| 1 | Dừng PostgreSQL | `/health/ready` 503; request lỗi trả gì; phục hồi sau bao lâu |
| 2 | Dừng Redis | API fallback (D22) **và** readiness 503 — hai kết quả khác nhau, phải tách bằng chứng |
| 3 | Dừng object storage | Upload trả lỗi rõ ràng (B1 nếu được duyệt), health `Degraded` |
| 4 | Dừng Hangfire worker | Job chờ trong queue, chạy lại khi bật, không mất |
| 5 | 2 API instance, tắt 1 giữa chừng | Request không rơi hoàn toàn; Nginx upstream có `proxy_next_upstream` |

Ghi bảng số liệu vào `SO_EVIDENCE_TUAN_4.md`, log vào `Tuan04/logs/`.

#### 2.2.6. Số đo k6 + EXPLAIN tái lập được

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Commit script k6 vào `tests/performance/` | Tuần 3 chạy bằng heredoc (`script: /k6.js`) ⇒ **không tái lập được** |
| 2 | Chạy lại: smoke → load; ghi p50/p95/p99, req/s, % lỗi | Ghi rõ máy/vm/workload/cache state |
| 3 | `EXPLAIN ANALYZE` lại query publish/list/sitemap | Không thêm index thiếu căn cứ |
| 4 | Ghi lệnh tái lập vào runbook | Tuần 5 người khác phải chạy lại được |

#### 2.2.7. Coverage có ngưỡng

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Đọc `coverage.cobertura.xml` trong CI | Hiện chỉ upload artifact, **không ai đọc** |
| 2 | Đặt ngưỡng **line ≥ 80% cho `CulinaryBlog.Application`** | `NFR-MAINT-002` |
| 3 | Báo cáo số đo thật kèm giới hạn | Không nâng/thụt ngưỡng để "đạt" mà không ghi rõ |

#### 2.2.8. D4-UI còn treo từ tuần 3 + checklist WCAG

| # | Việc | Kỳ vọng |
|---|---|---|
| 1 | Thanh **progress % upload** (hiện thiếu) | Xem tiến trình thật; lỗi ⇒ rollback |
| 2 | Nút **Unpublish/Archive** (API đã có, UI chưa có) | Đổi trạng thái end-to-end |
| 3 | Checklist 320/768/1200 px, focus ring, `aria-*`, trạng thái loading/empty/error | K18 |

---

### 2.3. D6 — Lab cá nhân

#### 2.3.1. Bù mục 2 của Lab L4

`PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 và `KE_HOACH_DU_AN.md` mục 4.2 đều xếp lab
**Identity/Google/refresh/forms/FTS** vào **tuần 3**; sổ tuần 3 tự ghi "để tuần 4".
⇒ **Tuần 4 phải bù phần này**, không ghi là "optional".

| # | Nội dung lab | Quy tắc |
|---|---|---|
| 1 | Refresh hash/rotation/reuse trên dataset riêng | Dùng đường code thật của sản phẩm, không viết lại hệ thống thứ hai |
| 2 | Form RHF/Zod | Có case biên |
| 3 | FTS tiếng Việt trên dataset thật | Có EXPLAIN |
| 4 | Google Code+PKCE + verify | **Thiếu credentials ⇒ ghi "integration ngoài còn chờ"; mock KHÔNG tính là hoàn thành** |

#### 2.3.2. Lab L5 (`practice/TV4/L5`)

| # | Phase | Nội dung | Kiểm chứng bằng |
|---|---|---|---|
| 1 | `search-ssr` | SSR `/search`, FTS, filter/sort/page | HTTP thật + EXPLAIN |
| 2 | `isr-detail` | ISR trang Published + CSR editor | Header `x-nextjs-cache`, HTML render |
| 3 | `query-rollback` | TanStack Query optimistic + rollback | Test tự viết trong lab |
| 4 | `image-opt` | `next/image` + kích thước phái sinh 300×300/800×600 | Kích thước file thật |
| 5 | `seo` | metadata/OG/Twitter/canonical/robots/JSON-LD/301 | Parse lại output |
| 6 | `obs` | Serilog/Seq/OTEL/metrics/health | Trace + metric thật |
| 7 | `ops` | Docker/Nginx, **2 API instance + shared cache/jobs** | Health 2 instance + 1 job duy nhất |
| 8 | `resilience` | Dừng dependency, đo phục hồi | Số liệu |

Cách chấm giống L4: **đếm check** (`media 25/25`, `jobs 8/8` …), exit code khác 0 khi fail,
ghi sổ `Tuan04/SOK_LAB_L5.md` nêu **giới hạn** (không nói bằng chứng bằng không).

#### 2.3.3. Mở PR cho nhánh lab

`PHAN_CHIA_CONG_VIEC_6_TUAN.md` yêu cầu *"có PR lab ngoài phần chính"*. Hiện `practice/TV4/L4`
mới chỉ có branch đã push, **chưa có PR** ⇒ mở PR cho L4 (và L5 sau khi xong) để reviewer có
đường vào. Không merge code lab vào `main` (quy ước LAB).

---

### 2.4. Runbook và release (D7)

`docs/RUNBOOK.md` — mỗi mục phải có **số liệu thật**, không viết "nên làm":

| # | Mục | Nội dung bắt buộc |
|---|---|---|
| 1 | Dựng từ checkout sạch | Lệnh + thời gian thực tế (mục tiêu < 5 phút) |
| 2 | Backup / restore | Lệnh, lịch, giữ bao lâu, kết quả drill |
| 3 | Failover từng dependency | Bảng: dừng cái gì → triệu chứng → phục hồi sau bao lâu |
| 4 | 2 API instance | Cách scale, cách kiểm tra shared cache/queue |
| 5 | Tìm log/trace của một request vừa thao tác | Dùng `CorrelationId` |
| 6 | Đo tải & truy vấn | Lệnh k6 + EXPLAIN |
| 7 | Secrets & xoay khoá | Biến môi trường cần có, **không** ghi giá trị |
| 8 | Giới hạn đã biết | Liệt kê rõ, không tuyên bố đạt SLA bằng test ngắn (D21) |

---

## 3. Quy tắc bắt buộc trong tuần 4

| # | Quy tắc |
|---|---|
| 1 | **Mọi mọc bắt đầu ở trạng thái Chưa làm**; chỉ đánh dấu xong khi có lệnh chạy + kết quả + reviewer Tâm xác nhận |
| 2 | Mỗi task nhỏ **một commit**, tên commit ghi rõ task (`D5`, `D7`…) và FR/NFR/K liên quan |
| 3 | Trước khi mở PR: `dotnet build -c Release` **0 warning** → `dotnet format --verify-no-changes` → `dotnet test` → `npm ci && npm run build` |
| 4 | **Không commit secret**: credential storage, JWT key, mật khẩu DB chỉ nằm ở `.env`/biến môi trường |
| 5 | Log bằng chứng phải **redact** token/password trước khi lưu vào `Tuan04/logs/` |
| 6 | Không sửa trực tiếp code thuộc phần của thành viên khác (ví dụ `/search` của TV2) — báo cáo rồi chờ |
| 7 | Thêm package ⇒ regenerate `packages.lock.json` (**không** `--locked-mode`); CI restore vẫn phải xanh |
| 8 | **Không sửa/xoá** 4 file `Lab 04` trong `docs/evidence/TV4/` do TV1 tạo (commit `80b2c0e`) — chỉ ghi chú nguồn gốc |
