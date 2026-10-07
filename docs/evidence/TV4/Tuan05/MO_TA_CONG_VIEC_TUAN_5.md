# MÔ TẢ CÔNG VIỆC TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D5-D7` (tạo từ `origin/main` = `262201b`)
- **Task**: **D5, D7** — tuần 5 của TV4: "Tự deploy/restore/test2 API; review; HTTPS/CORS/volumes, load/SEO/runbook"
- **Kế hoạch**: [`KE_HOACH_TUAN_5_TV4.md`](KE_HOACH_TUAN_5_TV4.md)

> Tài liệu này mô tả **chi tiết việc cần làm trong tuần 5** và **Definition of Done** từng việc.
> Việc đã có sẵn một phần từ tuần 4 được ghi rõ phần nào **còn thiếu** — không viết lại phần đã xong.

---

## 1. Ma trận việc → FR/NFR/Kỹ năng

| Việc | FR | NFR | Ô K | Điểm theo kế hoạch tổng |
|---|---|---|---|---|
| W5-1 Baseline + PR | — | `NFR-MAINT-003` | K24 | DoD 12.3 |
| W5-2 Deploy staging lặp lại được + restore + trace | — | `NFR-SCALE-003`, `NFR-REL-001`, `NFR-REL-003` | K23 | D5 (12đ) |
| W5-3 2 API instance trên staging + health/upstream | **`FR-OBS-001`** | `NFR-SCALE-001`, `NFR-SCALE-003` | K23 | D5 |
| W5-4 HTTPS/HSTS/CORS/volumes | — | **`NFR-SEC-005`** | K10, K23 | D5 ("HTTPS và no secrets") |
| W5-5 Runbook `docs/RUNBOOK.md` | `FR-JOB-003` | `NFR-REL-001`, `NFR-MAINT-003` | K23, K24 | D7 (13đ) |
| W5-6 D4-UI: progress + Unpublish/Archive/**Xóa** | **`FR-RCP-006`**, **`FR-RCP-007`** | **`NFR-USE-004`** | K05, K17 | D4 (tuần 2, dở dang) |
| W5-7 5 E2E flows (G6) | `FR-RCP-007` | `NFR-MAINT-002` | K21 | D7 |
| W5-8 Số đo load/SEO + EXPLAIN + cache-hit + metrics | **`FR-OBS-003`** | `NFR-PERF-002`, `NFR-SEO-003`, `NFR-REL-001` | K22, K19, K06, K11, K12 | D7 |
| W5-9 Lab còn thiếu + `npm audit` + `cqrs-behavior` (K04) | — | — | K05, K16, K17, K04 | D6 (nợ tuần 4) |
| **W5-10** Đóng bản vá lab + nợ nhỏ P1/P2 | — | `NFR-SEC-004`, `NFR-MAINT-003` | K10, K24, K13 | A1, A2, N3-4, BUG-W4-01/02/03/06/09 |

---

## 2. Chi tiết từng việc

### W5-1 — Chạy lại baseline trên `262201b` và mở PR

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Chạy lại toàn bộ kiểm định | `dotnet build -c Release` · `dotnet format --verify-no-changes` · `dotnet test -c Release` · `bash deploy/check-coverage.sh` · `npx tsc --noEmit` · `npm run lint` · `npm run build` | `Tuan05/logs/baseline_*.log`, số liệu ghi vào `TRANG_THAI_THUC_HIEN_TUAN_5.md` §2 |
| 2 | Xác nhận CI xanh cho `main` mới | Xem run `Backend week 1` + `Frontend CI` sau merge PR #29 | Link run + trạng thái |
| 3 | Mở PR cho nhánh tuần 5 | PR `2312739_NHTSon_D5-D7` → `main`, mô tả dẫn kế hoạch này | Link PR |

**DoD**: 7 lệnh chạy xanh (hoặc ghi rõ lệnh nào đỏ kèm lý do), CI có run xanh mới nhất được xác nhận, PR mở.

**⚠️ Bẫy đã vấp (tuần 4)**: `deploy/scan-secrets.sh` không chạy được trên máy thiếu bash/WSL — nếu máy
vẫn thiếu thì quét tay bằng `git grep` và **ghi rõ** trong báo cáo, không ghi "secret scan pass".

---

### W5-2 — Deploy staging lặp lại được (N4-B)

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Dựng staging từ checkout sạch | `docker compose -f docker-compose.staging.yml up -d` (yêu cầu `JWT_SIGNING_KEY` — thiếu thì compose phải **fail-fast**, exit 1) | `Tuan05/logs/staging_run1.log` |
| 2 | Kiểm tra sau khi lên | `/health/ready` 200 · FE `200` · `/sitemap.xml` 200 · đăng nhập được · upload 1 ảnh | Checklist trong log |
| 3 | **Lặp lại lần 2** | Dọn volume/container → chạy lại → cùng kết quả | `staging_run2.log` (không ghi đè lần 1) |
| 4 | **Drill backup→restore trên staging** | `deploy/backup-db.sh` → hạ stack → restore → `/health/ready` 200 + truy vấn 1 bảng (D5: "deploy/**restore**") | 2 file log backup + restore, cùng số bảng N1 (14) |
| 5 | **Trace HTTP→DB trên staging** | 1 request → tìm TraceId (mục A6) → xác nhận span HTTP→EFCore→Postgres | Log Seq theo TraceId |
| 6 | Ghi giới hạn hạ tầng | `render.yaml` vẫn `plan: free`; staging chạy trên máy dev → **không** tuyên bố `NFR-REL-001` 99,5% | Mục "Giới hạn" trong runbook |

**DoD**: 2 lần chạy sạch đều thành công, có 2 file log riêng, health/FE/sitemap đều đạt; backup→restore
trên staging có log; trace HTTP→DB xác nhận trên staging; giới hạn được ghi.

> Việc này do **TV1 dựng trước** (`docker-compose.staging.yml`, ADR 0004) — TV4 **kiểm chứng lại + làm
> idempotent + bổ sung 2 API (W5-3)**, không dựng lại từ đầu.

---

### W5-3 — 2 API instance trên staging (N4-B3, G6 "test 2 API")

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Thêm profile 2 API | `profiles:` trong `docker-compose.staging.yml`, service `culinary-api-2` (hiện compose chỉ có **1** `culinary-api`, `container_name` cứng) | `docker compose --profile multi config` exit 0 |
| 2 | Nginx upstream ≥ 2 server | `nginx/nginx.staging.conf` hiện `upstream culinary_backend { server culinary-api:8080; }` (1 server) → thêm server thứ 2 + `proxy_next_upstream` | `nginx -t` hợp lệ |
| 3 | Xác minh request trải đều | Gọi qua Nginx ≥10 lần, log 2 instance | `Tuan05/logs/staging_two_api.log` |
| 4 | Cache/queue dùng chung | Redis shared; khoá phân tán sitemap không sinh trùng (đã có `SitemapLockTests`) | Log/test |
| 5 | **`/health/ready` tích hợp Nginx upstream** | `proxy_next_upstream error timeout` + `max_fails` để Nginx **tạm ngừng gửi traffic** khi instance chưa ready (`FR-OBS-001` phần tích hợp upstream) | `nginx -t` + log failover |
| 6 | Số Hangfire server khi 2 API | Xác nhận số server + khoá phân tán (lab tuần 4 chưa xác nhận — thiếu connection string đọc DB) | Log Hangfire |

**DoD**: 2 API chạy trên staging, `nginx -t` xanh, có log chứng minh trải đều + cache dùng chung,
Nginx ngừng gửi traffic khi instance không ready, số Hangfire server được ghi nhận.

> ⚠️ **Hạn chế đã biết**: rate limit in-memory **nhân theo số instance** và thiếu `UseForwardedHeaders`
> (G1 — việc TV1). Nếu G1 chưa sửa trước khi đo, **phải ghi rõ** trong runbook/báo cáo số đo.

---

### W5-4 — HTTPS / HSTS / CORS / volumes (N4-B4 · `NFR-SEC-005`)

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | TLS cho Nginx staging | Chứng thư **tự ký** cho staging (hoặc ghi rõ "chưa có chứng thư") | Cấu hình + `curl -k -I https://...` |
| 2 | Redirect HTTP → HTTPS | `return 301 https://$host$request_uri;` | Header 301 khi đo |
| 3 | HSTS | `Strict-Transport-Security: max-age=...; includeSubDomains` | Header trả về |
| 4 | CORS explicit origins | `AllowedOrigins` từ env, không `*`; `UseForwardedHeaders` + `KnownProxies` | Test/log |
| 5 | Volumes persistent | DB/S3/Redis có volume named, không dùng anonymous volume cho dữ liệu | `docker volume ls` + compose |

**DoD**: có cấu hình + đo được header; **phải ghi rõ** chứng thư tự ký ≠ TLS production. `NFR-SEC-005`
chỉ chuyển "đạt" khi nhóm chốt hạ tầng TLS thật (câu hỏi Q3 của tuần 4 — chưa có trả lời).

> **Bản vá sẵn chưa merge**: `BUG-W4-03` (thiếu `UseForwardedHeaders` + rate limit chung sau Nginx) **đã có
> patch trên nhánh `lab/TV4-audit-tuan4`** nhưng chưa merge; `Program.cs` là của TV1 → ghi mục 6, chờ Tâm giao
> (xem kế hoạch §6). Nếu chưa merge thì mục 4 ở trên phải ghi rõ hạn chế khi đo k6.

---

### W5-5 — Runbook `docs/RUNBOOK.md` (N4-A)

| # | Mục | Yêu cầu | Nguồn số liệu |
|---|---|---|---|
| A1 | Dựng stack từ checkout sạch | Lệnh thật, đã chạy được | W5-2 log |
| A2 | Backup / restore | Số liệu drill 14 bảng | `Tuan04/HANDOFF_TV4_TUAN4_N1.md` |
| A3 | Failover từng dependency | Số outage drill (Redis 0.2s · S3 0.5s · API 3.5s) | `Tuan04/logs/c1_outage_drill.log` |
| A4 | 2 API instance | Lệnh + log | W5-3 log |
| A5 | Lệnh k6 | Trỏ đúng `tests/performance/read-load.js` | `tests/performance/README.md` |
| A6 | Tìm log/trace một request theo TraceId | Lệnh thật | `Tuan04/logs/seq_trace_recipes.log` |
| A7 | Tìm log Seq | Lệnh/screenshot | Seq UI |

**DoD**: đủ 7 mục, **mỗi mục có số liệu thật hoặc ghi "chưa đo" + lý do**; không có mục "nên làm";
mục "Giới hạn" nêu rõ điều **chưa đạt** (backup 7 ngày, chưa TLS thật, chưa đo SLA).

---

### W5-6 — D4-UI: progress upload + nút Unpublish/Archive/Xóa

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Thanh **progress %** khi upload | FE hiện **0 chỗ** có `onUploadProgress`/`xhr.upload` → dùng `XMLHttpRequest` hoặc fetch stream, hiển thị % + trạng thái lỗi | Screenshot hoặc log % tăng dần; test |
| 2 | Nút **Unpublish** | API đã có `Program.cs:620` — thêm nút ở dashboard theo trạng thái | Test click → `FR-RCP-006` đổi trạng thái |
| 3 | Nút **Archive** | API đã có `Program.cs:627` | Test click → `FR-RCP-007` |
| 4 | Nút **Xóa** (soft delete) | API delete đã có → thêm nút + xác nhận 2 bước (báo cáo 3 tuần: FR-RCP-005/006/007 "còn thiếu nút Unpublish/Archive/**Xóa**") | Test click → item biến mất, record vẫn còn; `CategoryTests` soft-delete còn xanh |
| 5 | Không hồi quy | Chạy lại `recipe-publish.spec.ts`, `wizard-week4.spec.ts` | `npx playwright test` xanh |

**DoD**: có nút + thanh progress hoạt động thật với ảnh thật (kể cả **Xóa**); E2E luồng gỡ xuất bản viết được;
các spec cũ không đỏ.

> Đây là **nợ từ tuần 3**, được plan tuần 4 xếp "không được cắt" nhưng vẫn dở dang (`N2-D1/D2`).
> `NFR-USE-004` đang **15%** — thấp nhất trong 10 NFR của TV4.

---

### W5-7 — 5 E2E flows (cổng G6)

| Luồng | Spec hiện có | Việc tuần 5 |
|---|---|---|
| register | `create-recipe.spec.ts` | Xác nhận phủ đủ, ghi rõ tên test |
| login | `create-recipe.spec.ts` | như trên |
| category | chưa thấy spec của TV4 | Hỏi TV2 — **người viết test là người có phần đó** |
| create-recipe / draft | `create-recipe.spec.ts`, `wizard-week4.spec.ts` | Chạy trên staging |
| publish | `recipe-publish.spec.ts` (4/4) | Chạy trên staging |

**DoD**: `npx playwright test` trên staging chạy toàn bộ 5 luồng → **pass**; bảng "luồng nào do ai viết"
ghi trong `SO_EVIDENCE_TUAN_5.md`. Không nhận phần của thành viên khác là của mình.

---

### W5-8 — Số đo load / SEO + số đo bị cắt tuần 4

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | k6 ≥100 VU trên **staging** | `tests/performance/read-load.js`, chạy ≥1 lần, ghi p50/p95/p99 + `http_req_failed` | `Tuan05/logs/k6_staging_*.log` |
| 2 | SEO trên staging | `sitemap.xml` (Published-only), `robots.txt`, metadata/canonical/JSON-LD; kiểm **301 redirect** + URL danh mục (`NFR-SEO-003` chưa kiểm chứng) | File/link + kết quả crawl |
| 3 | **EXPLAIN re-run** | `N2-6` bị cắt ở tuần 4 (`KE_HOACH_V2` §4.4) — đo lại plan query chính; K11 (FTS) phụ thuộc TV2 sửa `to_tsquery` → ghi phần chưa đo được | Plan + log (K06/K11/K22) |
| 4 | **Cache-hit ratio** | Đọc hit/miss từ Redis thật — K12 gap còn lại của `N2-5` | Số liệu Redis |
| 5 | **Metrics scrape thật** | `FR-OBS-003` 90% — tuần 4 mới có trace, **chưa có bằng chứng số liệu chạy thật**: request count / duration / error từ `/metrics` | Log/`curl` endpoint metrics |
| 6 | Ghi giới hạn | Hạ tầng đo, có qua proxy không, rate limit G1 đã sửa chưa | Mục "Giới hạn" |

**DoD**: có số đo mới **trên staging** (không dùng lại số dev tuần 4) + EXPLAIN/cache-hit/metrics có số
(hoặc ghi rõ chưa đo được kèm lý do) + ghi rõ giới hạn.

---

### W5-9 — Lab còn thiếu + nợ kỹ thuật (ưu tiên thấp nhất)

| # | Việc | Trạng thái đầu tuần |
|---|---|---|
| 1 | `N3-A3` Zod/RHF phía FE | `zod` trong `package.json` nhưng `src/` chưa import |
| 2 | `N3-A5` Google OAuth/PKCE | ⛔ Chờ credentials — ghi "còn chờ", không tính đạt |
| 3 | Sửa **4** phase Lab L5 chưa PASS | `isr-detail` (ISR không chạy), `image-opt` (9 file `<img>` thô), `search-ssr` (`no-store`), **`query-rollback`** (RowVersion — cần tài khoản E2E **sở hữu** công thức, xem kế hoạch §6) |
| 4 | Chạy phase **`cqrs-behavior`** (K04) | Sổ K04: "phần tự viết behavior trong lab L5 → tuần 5"; L5 tuần 4 chỉ chạy 7 phase, thiếu phase này |
| 5 | `npm audit` 10 vulnerability (9 high, 1 critical) | Rà + ghi kết quả; nâng major cần nhóm chốt → ghi đề xuất |

**DoD**: mỗi việc có kết quả ghi nhận (xong / chưa xong + lý do). Đây là nhóm việc **cắt trước** khi trượt.

---

### W5-10 — Đóng bản vá lab + nợ nhỏ P1/P2 (không cắt phần A1 + PR L4)

| # | Việc | Trạng thái đầu tuần (đo 07/10) | Bằng chứng DoD |
|---|---|---|---|
| 1 | Đối chiếu + chốt `BUG-W4-01` | `main` **đã có** `RecipeImageConfiguration.cs:21` `ValueGeneratedNever()` + migration `20261001112029_RecipeImageIdValueGeneratedNever` (commit `c624b9f`) — khác với báo cáo 05/10 ghi "chưa merge"; cần chạy test hồi quy + báo Tâm chọn C1/C2 (đụng schema TV3) | Log test + reply Tâm |
| 2 | Dọn secret còn lại (A1 phần cuối) | `appsettings.json:3` + `appsettings.Development.json:3` còn `Password=postgres`; `appsettings.Development.json` + `.github/workflows/backend.yml:47,48,67,68` còn `minioadmin`; `Jwt:SigningKey` đã rỗng ✅ | `git grep` 0 khi chạy lại + CI xanh |
| 3 | Mở rộng `deploy/scan-secrets.sh` + test | Script (commit cuối `0c692d3`) **chỉ** quét `render.yaml`/compose — chưa quét `appsettings*.json` và `env:` workflow | Script chạy pass/đúng regex + có test |
| 4 | `BUG-W4-03` — ghi chờ nhóm | Bản vá `UseForwardedHeaders` + bucket theo IP đã có trên `lab/TV4-audit-tuan4`, **chưa merge**; `Program.cs` của TV1 | Mục 6 của kế hoạch + log khi được giao |
| 5 | **Mở PR `practice/TV4/L4`** | Nhánh có sẵn, `gh pr list` **không có PR nào cho L4** (P1 "nâng lên bắt buộc" — `KE_HOACH_V2` §4.2; cut-list §4.4: "để tuần 5") | Link PR (K24) |
| 6 | ADR soft-delete + đổi tên test | `Recipe` soft delete, `Category` không → viết ADR; đổi tên test cho khớp (P2 `BUG-W4-06`) | File ADR + test đổi tên, CI xanh |
| 7 | Header `X-Sitemap-Generated` | `BUG-W4-09` — trả `false` khi lock + 1 dòng runbook | Header trả về + dòng runbook |
| 8 | Test path traversal `../` | `NFR-SEC-004`: "chưa có test path traversal cụ thể (`../` trong tên file)" | Test mới pass |

**DoD**: mục 2/3 có `git grep` sạch + test; mục 5 có link PR; các mục còn lại xong hoặc ghi rõ lý do
chưa xong (chờ Tâm/chờ nhóm). **Không được để A1/PR L4 thành "cắt nếu trượt"** — tuần 4 đã ghi "không cắt".

---

## 3. Quy ước làm việc trong tuần

1. Mỗi việc nhỏ **một commit**, tên commit ghi rõ mã việc (`W5-2`, `N2-D1`…) + FR/NFR/K liên quan.
2. Lỗi phát sinh ghi vào báo cáo lỗi riêng của tuần, **không** sửa ngầm trong kế hoạch.
3. Việc phát sinh ngoài phạm vi (của TV1/TV2/TV3) → ghi mục 6 của kế hoạch, **báo trước, không chờ trả lời**.
4. Không push thẳng `main`; mọi thay đổi qua PR được **Tâm** review.
5. Không commit secret; file `*.dump` và khoá JWT không được vào repo (đã có `JwtSigningKeyNotCommittedTests` chặn).

## 4. Thứ tự ưu tiên khi thiếu thời gian

```
W5-1 ──▶ W5-2 ──▶ W5-3 ──▶ W5-4 ──▶ W5-7 ──▶ W5-8
                    │
                    ├──▶ W5-6 (D4-UI) ──▶ W5-5 (runbook — lấy số liệu của các việc trên)
                    │
                    ├──▶ W5-10 (song song từ 07/10: PR L4 + dọn secret A1)
                    │
                    └──▶ W5-9 (cắt đầu tiên)
```

- **Cắt thứ tự**: W5-9 (`npm audit` → `cqrs-behavior` → sửa 4 phase Lab L5) → W5-8 nâng cao
  (EXPLAIN/cache-hit/metrics — giữ k6 + SEO) → W5-6 nút Xóa.
- **Không cắt**: W5-1, W5-2, W5-4, W5-5, W5-7 và **W5-10 phần A1 (P0) + PR `practice/TV4/L4` (P1)**
  — là điều kiện G6 và nợ tuần 4 đã ghi "chuyển tuần 5"/"không cắt".
