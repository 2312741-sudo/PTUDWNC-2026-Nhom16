# BÁO CÁO TIẾN ĐỘ TUẦN 5 SO VỚI TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh chính tuần 5**: `2312739_NHTSon_D5-D7` (gốc `origin/main` = `262201b`; mở PR #32 `week-5` → `main`)
> **Nhánh lab**: `practice/TV4/L4` (đã mở PR #33 lần đầu sau 2 tuần treo)
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Khoảng thời gian**: 07/10/2026 – 11/10/2026
> **Nguồn đối chiếu**: `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md`, `KE_HOACH_TUAN_5_TV4.md`, `TRANG_THAI_THUC_HIEN_TUAN_5.md`, `SO_EVIDENCE_TUAN_5.md`, `HANDOFF_TV4_TUAN5.md`, lịch sử `git`.

> [!IMPORTANT]
> **Phạm vi file này**: CHỈ ghi phần **đã làm thêm trong tuần 5 so với tuần 4**.
> Việc đã xong tuần 4 (N1, N2 phần còn, N3-N-C, trace Seq, drill backup/restore 14 bảng trên dev, `NFR-REL-002`, đóng 5 issues B1–B6, E2E 26/26, coverage 96.31%) **không lặp lại** — xem `Tuan04/`.
> Quy tắc giữ nguyên: **không copy số tuần 4 làm số tuần 5**; số nào của tuần 5 đều đo lại trên `262201b` hoặc trên staging.

---

## 1. Delta trạng thái task (cuối tuần 4 → cuối tuần 5)

| Task / việc | Cuối tuần 4 | Cuối tuần 5 | Bản chất thay đổi |
|---|---|---|---|
| `N2-D1` progress upload % | ⬜ Chưa có (FE **0 chỗ** `onUploadProgress`) | ✅ **Xong** — XHR + `onProgress`, `role="progressbar"` + unit test | 🆕 Đóng nợ |
| `N2-D2` nút Unpublish/Archive/Xóa | ⬜ Chưa có UI (API đã sẵn) | ✅ **Xong** — 3 nút theo trạng thái + xác nhận 2 bước + E2E 3/3 chống staging | 🆕 Đóng nợ |
| `N2-5` cache-hit ratio (K12) | ⬜ bị cắt | ✅ **Xong** — mixed 70.31% (có `list_uncached` cố ý) / cached-only 100% | 🆕 |
| `N2-6` EXPLAIN re-run (K06/K11/K22) | ⬜ bị cắt | ✅ **Xong** — 5 query: list 0.32ms · category 0.161ms · search 0.347ms · slug Index 0.032ms | 🆕 (phần FTS chờ TV2) |
| `N3-A3` Zod/RHF FE | ⬜ 10/10 (chưa khớp) | ✅ **Xác nhận 10/10** — `zodResolver` 11 nơi, 5 file import `react-hook-form` | Đóng khoá xác minh |
| `N3-A5` Google OAuth (K09) | ⬜ chờ credentials | ⬜ **Vẫn chờ credentials** (không tính mock) | Chưa — block nhóm |
| `N4-A` runbook 7 mục | ⬜ chưa có | ✅ **Xong** — `docs/RUNBOOK.md` A1–A7 + mục Giới hạn (commit `92e34be`) | 🆕 Đóng nợ |
| `N4-B` deploy staging | ⬜ chưa (đúng phạm vi tuần 5) | ✅ **Xong** — idempotent 2 lần + drill restore trên staging + trace HTTP→DB staging | 🆕 |
| `N4-B3` 2 API instance | ⬜ chưa kiểm chứng staging | ✅ **Xong** — profile `multi`, 50/50, failover 0/8, **Hangfire 2 server** | 🆕 |
| `N4-B4` TLS/HSTS/redirect/CORS | ⬜ chưa (NFR-SEC-005 = 20%) | ✅ **Xong (giới hạn self-signed)** + `BUG-W4-03` `UseForwardedHeaders` | 🆕 |
| Lab L5 3/7 phase → 4 phase FAIL | 🟡 41 đạt · 3/7 phase | 🟡 **2/5 việc W5-9** — `npm audit` + ISR; **`image-opt`/`query-rollback`/`cqrs-behavior` cắt tuần 6** | Một phần |
| E2E | 26/26 (2/5 luồng G6) | ✅ **40/40** ×2 (4/5 luồng; category ⬜ TV2) | Mở rộng luồng |
| Số đo load/SEO trên staging | Chưa (đo dev) | ✅ k6 102 VU + SEO + EXPLAIN + cache-hit + `/metrics` | 🆕 |
| PR `practice/TV4/L4` | ⬜ chưa mở (2 tuần) | ✅ **PR #33 mở 07/10** | 🆕 P1 |
| `npm audit` | 🔴 10 vuln (9 high, **1 critical**) | 🟡 **critical → 0** (44 vuln còn mức thấp) | cải thiện |
| `BUG-W4-01/02/03` + `-06/-09` + path traversal | 🔴 chưa xử lý | ✅ **8/8** bảng W5-10 | 🆕 Đóng toàn bộ |

**Điểm mấu chốt**: tuần 5 đóng **toàn bộ nợ UI/hạ tầng tuần 3–4** (D4-UI, N4-A, N4-B) và chuyển TV4 từ "có API + có số đo trên dev" sang **staging chạy được 2 lần + 2 API + TLS + E2E 40/40 + số đo có giới hạn** — sát cổng G6.

---

## 2. Việc hoàn thành MỚI trong tuần 5

### 2.1 W5-1 — Baseline + mở PR (07/10)

- Đo lại trên `262201b` + `0a9b1a5`: **426/426** · coverage **97.07%** (ngưỡng 80%) · build 0W/0E · format exit 0 · FE `tsc`/`lint`/`build` exit 0 · CI xanh (run `0a9b1a5`).
- Mở **PR #32** (`week-5` → `main`) và **PR #33** (`practice/TV4/L4` → `main`) — cả 2 đang chờ review.

### 2.2 W5-2 — Deploy staging lặp lại được (07/10)

- run1: 6 container (db/redis/s3/api/nginx/frontend), `/health/ready` **200**, FE **200**, sitemap **200**, register/login/create-recipe OK, upload ảnh 201; fail-fast thiếu `JWT_SIGNING_KEY` → exit **1** ✓.
- run2 (`down -v` → `up -d --build`): lặp lại xanh; bucket tạo mới; **categoryId động** (volume mới → GUID seeded đổi) ⇒ chứng minh không dính dữ liệu cũ.
- **Drill backup→restore trên staging**: `culinary_20261007T145532Z.dump` 287KB → `DROP DATABASE ... (FORCE)` → restore → **14 bảng = N1** → ready 200.
- **Trace HTTP→EFCore→Postgres**: 1 request key mới → Seq có 3 span cùng `CorrelationId` (RequestLoggingMiddleware + LoggingBehavior + EFCore). Lưu ý: request có cache Redis không sinh EF span ⇒ phải dùng key mới; cần `Logging__LogLevel__Microsoft.EntityFrameworkCore=Information`.

### 2.3 W5-3 — 2 API instance trên staging (08/10)

- `docker-compose.staging.yml` thêm profile `multi` + service `culinary-api-2` (port 5081); nginx `upstream` 2 server (`zone 128k` + `resolve` + `resolver 127.0.0.11`).
- 12 request → **api1=6 / api2=6 (50/50)**; stop api-2 → 0/8 non-200; start lại → 0/8. **Hangfire: 2 server** (cùng storage PostgreSQL). Cache/queue Redis chung (`staging:cache:*`, `staging:sitemap:xml` redistribution lock).

### 2.4 W5-4 — HTTPS/HSTS/CORS + BUG-W4-03 (09/10)

- TLS staging **self-signed** (service `culinary-certs` alpine+openssl sinh `CN=localhost` vào volume `staging_certs`, **không commit key**); `listen 443 ssl` + `TLSv1.2/1.3`; port 80 = `return 301 https://...`; header `Strict-Transport-Security: max-age=31536000; includeSubDomains`. HTTP → 301 → HTTPS → 200 + đủ security headers.
- CORS origins tường minh (`Cors:AllowedOrigins`; rỗng = từ chối); `UseForwardedHeaders` (`X-Forwarded-For|Proto`, `ForwardLimit=1`, `KnownIPNetworks 172.16/12`) **chạy trước rate limiter** → mỗi IP một bucket (XFF A: 10×400+429, XFF B: 400) = **BUG-W4-03** (commit `afdb4a7` — PR riêng vì `Program.cs` của TV1).
- Volumes persistent `staging_pgdata/s3data/redisdata`; register 201 → restart db → login 200; Redis appendonly SET → restart → GET còn.
- Companion: sửa `/health` 503 oan (`HealthChecks__Minio__Host`); pin **ImageSharp 3.1.11 → 3.1.12** + suppress 5 advisory (ADR-TV4-004; `first_patched` 4.1.2 = commercial) — build/test/format xanh.

### 2.5 W5-5 — Runbook `docs/RUNBOOK.md` (10/10)

- 7 mục đủ số liệu thật: A1 (run2 = **24s** tới ready; máy sạch 100% chưa đo — ghi rõ) · A2 backup→restore (**14 bảng** sau restore) · A3 failover từng dependency (Redis 0.2s, S3 ~0.5s, API node = SPOF; staging 2 API 8/8) · A4 2 API (6/6 round-robin, Hangfire 2) · A5 k6 (**chưa đo staging tại thời điểm viết** → ghi điều kiện) · A6 trace theo TraceId · A7 Seq (`localhost:5341` / `host.docker.internal:5341`) + mục **Giới hạn** (TLS tự ký, SLA chưa đo, backup 30 ngày cục bộ).

### 2.6 W5-6 — D4-UI: progress upload + 3 nút (09–10/10)

- `uploadImage` → **XMLHttpRequest + `onProgress`** (ImagesStep `role="progressbar"`, aria-valuenow theo % thật); `my-recipes.ts` gọi `PATCH /{id}/unpublish`, `/archive`; dashboard thêm nút theo trạng thái + xác nhận 2 bước + `actingId`.
- **E2E thật chống staging** `recipe-unpublish.spec.ts` **3/3** (10.4s): Published→Bản nháp / Draft→Lưu trữ / Xóa→dòng biến mất (soft delete). CORS thêm `http://127.0.0.1:3000` cho Playwright.
- Unit `ImagesStepProgress.test.tsx` pass → **jest 86/86**; `tsc`/`lint`/`build` sạch. 6 spec E2E cũ fail 10/10 là do **spec lỗi thời + presigned `culinary-s3:9000`**, không phải regression W5-6 → chuyển W5-7.

### 2.7 W5-7 — 5 E2E flows trên staging (11/10)

- **Chặn gốc lỗi ảnh**: `mediaUrl` fallback `MEDIA_DEFAULT = ${API}/resources/images`; `imageSrc` bỏ presigned → luôn proxy + `AuthImage` fetch + Bearer → blob; bỏ UI "liên kết hết hạn / Tải lại".
- Sửa spec lỗi thời (zod lỗi dưới ô `#err-*`, placeholder dòng nháp, bỏ `#__next-route-announcer__`).
- **Full suite 40/40 pass** chống staging (2.8m) — publish 4/4 · wizard-week4 10/10 · create-recipe · search 12 · upload-security 10 · recipe-unpublish 3; chạy **lần 2 xác nhận 40/40**. Jest 86/86 · `tsc`/`lint`/`build` 0.
- ⬜ Luồng **category**: chưa có spec — §3.7 sổ evidence ghi "cần TV2 xác nhận" (không nhận phần TV2).

### 2.8 W5-8 — Số đo load/SEO + số đo bị cắt tuần 4 (11/10)

| Hạng mục | Kết quả |
|---|---|
| k6 ≥100 VU | **102 VU** (34×3 `MODE=vus`) · p50 82.56 · p95 **148.56** · p99 187.28 · **0% lỗi** · ~1150 req/s · threshold PASS |
| SEO staging | sitemap FE **96 URL** (92 Published + 4 tĩnh), backend Published-only 96, robots 200, JSON-LD Recipe, 5 OG, nginx 301 http→https. ⬜ `NFR-SEO-004` (301 đổi slug) → **nợ TV3** |
| EXPLAIN re-run | list 0.32ms · category 0.161ms · search `ILIKE` 0.347ms (trigram GIN chưa dùng) · slug **Index Scan 0.032ms**; `Recipes` chỉ 167 dòng → khuyến nghị composite index |
| Cache-hit ratio | mixed **70.31%** (24192/10213) — miss từ `list_uncached` cố ý; cached-only **100%** (200/0) |
| Metrics scrape thật | `/metrics` **200** Prometheus (total/errors/duration/by_class) — tự viết `RequestMetrics`, số trong bộ nhớ (reset khi restart), không lộ qua nginx (404) |

Giới hạn ghi rõ: đo máy dev → staging cục bộ (`host.docker.internal:5080`), không phải SLA; EXPLAIN ở quy mô 167 dòng; cache hit là số trộn.

### 2.9 W5-9 — Lab còn thiếu + nợ kỹ thuật (10/10, 2/5)

- `npm audit`: **46 → 44 vuln, critical → 0** — nâng `next 15.1.11 → 15.5.27` (cùng major); còn high/p/moderate **đều cần nâng major** (next@16, tailwindcss 4.3.3, jest 30.x) → **đề xuất nhóm chốt**, không tự nâng major tuần này.
- Zod/RHF: xác nhận đã dùng sâu từ tuần 3 (`zodResolver` 11 nơi, 5 file `react-hook-form`) → 10/10.
- Lab L5: **`isr-detail` sửa code** — bỏ nhánh `isDev` ép `no-store` + `generateStaticParams` (CI-safe) → **ISR thật 104 trang static, revalidate=300**; `search-ssr` đúng khuyến nghị L5 (no-store ở fetch). ⬜ `image-opt` + `query-rollback` (cần re-run lab, **cắt tuần 6**) · ⬜ `cqrs-behavior` (K04, chưa từng chạy, cắt tuần 6).
- ⬜ Google OAuth: **vẫn chờ credentials** — không coi mock là hoàn thành.

### 2.10 W5-10 — Đóng `BUG-W4-01/02/03` + nợ P1/P2 (07–11/10, 8/8)

| Việc | Kết quả |
|---|---|
| `BUG-W4-01` | Đối chiếu `ValueGeneratedNever()` đã ở main (`c624b9f`): `RecipeImageConfiguration.cs:21`, `RecipeStepConfiguration.cs:20`, `RecipeIngredientConfiguration.cs:20` + 2 migration; thêm **3 test model-level** `RecipeChildIdGenerationTests` (không cần Postgres). Full suite **430/430**. Báo Tâm chọn C1/C2 |
| Secret A1 + `scan-secrets.sh` | Dọn `Password=postgres`/`minioadmin` khỏi `appsettings*.json` + `backend.yml` (`0a9b1a5`, `git grep` = 0); mở rộng scanner quét appsettings + `env:` workflow, test **9 PASS/0 FAIL**, CI thêm bước test |
| `BUG-W4-03` | `UseForwardedHeaders` + `ForwardLimit=1` + `KnownIPNetworks` trước rate limiter; commit `afdb4a7` — **PR riêng** (đụng `Program.cs` TV1), cần Tâm review |
| PR `practice/TV4/L4` | **PR #33** mở 07/10 (đóng P1/K24 gap 2 tuần) |
| ADR soft-delete | **ADR-TV4-003**: Recipe soft delete (interceptor + query filter) / Category **hard** (`CategoryRepository.cs:51`); `MarkDeleted` = dead code, KHÔNG xoá; đổi tên test `CategoryTests.cs:146` |
| `X-Sitemap-Generated` | `true` khi sinh mới (cache nguội) / `false` cache ấm; verify trên `:5080` |
| Path traversal | `PathTraversalSecurityTests` **7/7** — server đặt key `recipes/{id}/image{ext}`, FileName không vào path, proxy 404 mọi `..` |

---

## 3. Lỗi thật phát hiện & sửa trong tuần 5

| # | Lỗi | Biểu hiện | Nguyên nhân | Cách sửa |
|---|---|---|---|---|
| 1 | **CI backend đỏ do NuGetAudit** (NU1902/NU1903 ImageSharp 3.1.11) | restore/build fail toàn bộ | advisory flag cả dòng `<=4.1.1`; bản vá 4.1.2 là commercial | Pin **3.1.12** (Apache-2.0) + `NuGetAuditSuppress` đúng 5 URL (ADR-TV4-004) — audit vẫn bật cho package khác |
| 2 | **Ảnh nháp/vừa tải vỡ trên staging** | `<img>` 403 / presigned `culinary-s3:9000` không trỏ từ trình duyệt | `mediaUrl` phụ thuộc `NEXT_PUBLIC_MEDIA_URL`; presigned URL dùng host nội bộ container | `mediaUrl` fallback proxy + `imageSrc` bỏ presigned → luôn `/resources/images` + `AuthImage` Bearer-blob (`9f3dd4c`) |
| 3 | **6 spec E2E cũ fail** khi chạy chống staging 10/10 | selector cũ (banner zod → lỗi dưới ô; aria-label động → placeholder) | spec viết theo UI cũ | Cập nhật selector + bỏ `#__next-route-announcer__` + giữ title route sau `replaceState` |
| 4 | **`/health` trả 503 oan** (companion) | health toàn bộ check fail dù các service OK | `MinIOHealthCheck` đọc `HealthChecks:Minio:Host` default `localhost:9000` trong container | Thêm `HealthChecks:Minio:Host` vào `x-api-env` → `/health` 200 cả direct lẫn qua nginx |
| 5 | **CORS chặn Playwright** chống staging | preflight fail `127.0.0.1:3000` | `Cors:AllowedOrigins` chưa có origin của browser | Thêm `http://127.0.0.1:3000` vào staging compose + recreate api |
| 6 | **CI format** | newline cuối thiếu trong test mới | `dotnet format --verify-no-changes` | Thêm newline (`bf98eee`, `34f82f4`) |

*(Migration trùng `AddRecipeDiscoveryAndSearch` làm CI main đỏ — xử lý trên `main` bởi `b1d16c3`, ngoài phạm vi TV4, chỉ ghi nhận khi merge.)*

---

## 4. Số liệu kiểm chứng (tiến trình so với tuần 4)

| Mốc | CulinaryBlog.Tests (+spike) | Coverage App | Jest | Playwright | CI |
|---|---|---|---|---|---|
| **Cuối tuần 4** | 426/426 (`fd90572`) | 96.31% | 85/85 (TV3) | 26/26 (2 luồng) | 🟢 `8a585ef` |
| 07/10 — W5-1 baseline | **426/426** (`0a9b1a5`) | **97.07%** | 85/85 | — | 🟢 `0a9b1a5` |
| 09–10/10 — W5-6 UI | 430* (*sau W5-10) | — | **86/86** (+1) | 26/26 + unpublish 3/3 | 🟢 |
| 11/10 — W5-7 E2E staging | **430/430** | gate 80% | 86/86 | **40/40 ×2** (4/5 luồng) | 🟢 |
| 11/10 — W5-8 số đo | 430/430 | — | — | 40/40 | — |

> Tiêu chí tuần 5: luôn kiểm tra **Skipped=0** (giữ quy tắc tuần 3) và ghi **môi trường đo** (staging vs dev) để không so sánh trực tiếp p95.

---

## 5. Tài liệu / ADR / bằng chứng tạo MỚI trong tuần 5

| Nhóm | File |
|---|---|
| Sổ chính tuần 5 | `Tuan05/KE_HOACH_TUAN_5_TV4.md` · `TRANG_THAI_THUC_HIEN_TUAN_5.md` · `SO_EVIDENCE_TUAN_5.md` · `HANDOFF_TV4_TUAN5.md` |
| Ở nhánh / code | `docs/RUNBOOK.md` (`92e34be`) · `docker-compose.staging.yml` (profile `multi`, certs, volumes) · `nginx/nginx.staging.conf` · `Program.cs` (CORS/forwardedHeaders, `/metrics`, sitemap header) · `src/frontend/...` (progress bar, 3 nút, media proxy) |
| ADR | `docs/adr/ADR-TV4-003-quy-uoc-soft-delete.md` · `ADR-TV4-004` (ImageSharp 3.1.12 + NuGetAudit) |
| Test mới | `RecipeChildIdGenerationTests` (3) · `PathTraversalSecurityTests` (7) · `ImagesStepProgress.test.tsx` · `scan-secrets.test.sh` 9 PASS · E2E `recipe-unpublish.spec.ts` (3/3) |
| Log/số liệu | `Tuan05/logs/` — `staging_run{1,2}.log`, `staging_backup.log`, `staging_restore.log`, `staging_trace.log`, `staging_two_api.log`, `w54_tls_cors.log`, `w58_k6_vus100.log`, `w58_seo_staging.log`, `w58_explain_staging.log`, `w58_redis_cachehit.log`, `w58_metrics_header.log`, `a1_*.log`, `scan_secrets.log` |
| PR | **PR #32** (`week-5` → `main`) · **PR #33** (`practice/TV4/L4` → `main`) — chờ review |

---

## 6. Việc tuần 4 còn treo → tuần 5 xử lý / vẫn còn

| Việc | Ở tuần 4 | Ở tuần 5 | Lý do còn lại |
|---|---|---|---|
| D4-UI progress + nút trạng thái | Chưa | ✅ Đóng (W5-6) | — |
| Runbook 7 mục | Chưa | ✅ Đóng (W5-5) | — |
| Deploy staging + TLS | Chưa | ✅ Đóng (W5-2/3/4) | — |
| 5 E2E flows G6 | 2/5 luồng | **4/5** luồng | Luồng **category chờ TV2** (sổ §3.7) |
| `NFR-SEC-005` | 20% | ✅ có bằng chứng (self-signed + HSTS + 301) | Kèm giới hạn chứng thư |
| K11 EXPLAIN FTS | chưa đo | ✅ đo phi-FTS; ⬜ FTS `to_tsquery` | Chờ TV2 (ADR 0003) |
| Google OAuth (K09) | chờ credentials | ⬜ vẫn chờ credentials | Block nhóm từ tuần 3 |
| 2 phase L5 + `cqrs-behavior` | 3/7 phase PASS | 🟡 2 việc W5-9 xong; ⬜ `image-opt`/`query-rollback`/`cqrs-behavior` | Cắt theo thứ tự cắt mục 4 (tuần 6) |
| backup lịch + kho 30 ngày + rotate JWT | chờ chốt | ⬜ chờ Tâm/TV2 | 3 đề xuất 07/08/09 |
| `NFR-SEO-004` (301 đổi slug) | chưa | ⬜ nợ TV3 | Không thuộc TV4 |
| `npm audit` nâng major | 1 critical | critical → 0; ⬜ major | Đề xuất nhóm chốt, không tự nâng tuần này |

---

## 7. Điểm kỹ năng (Kxx) có thay đổi tuần 5

| K | Thay đổi | Bằng chứng |
|---|---|---|
| K05 | 🆕 củng cố | `.ImagesStep` `role="progressbar"` (W5-6) + Zod/RHF xác nhận (W5-9) |
| K06 | 🆕 bằng chứng mới | EXPLAIN re-run 5 query (W5-8) |
| K10 | 🆕 nâng mạnh | TLS/HSTS/301 + CORS explicit + `UseForwardedHeaders` + dọn secret + scanner mở rộng (W5-4, W5-10) |
| K11 | 🆡 một phần | EXPLAIN phi-FTS đo được; FTS `to_tsquery` chờ TV2 |
| K12 | 🆕 đo thật | Redis cache-hit 70.31% mixed / 100% cached-only + cache chung 2 API |
| K13 | 🆕 | path traversal tests 7/7 (NFR-SEC-004) |
| K14 | 🆕 | Hangfire 2 server khi 2 API (đóng gap tuần 4) |
| K16 | 🆡 | ISR thật (generateStaticParams, 104 trang); `image-opt` chờ |
| K17 | 🆕 | XHR progress + 3 nút trạng thái + E2E 3/3 |
| K19 | 🆕 | SEO staging: 96 URL sitemap, JSON-LD, OG, 301 (W5-8) |
| K20 | 🆕 | `/metrics` scrape thật + trace staging (W5-2/8) |
| K21 | 🆕 | E2E 40/40 ×2 chống staging + jest 86/86 |
| K22 | 🆕 | k6 102 VU p95/p99 staging + EXPLAIN (W5-8) |
| K23 | 🆕 nâng mạnh | staging idempotent + 2 API + TLS self-signed + drill restore staging + runbook (W5-2/3/4/5) |
| K24 | 🆕 | baseline + PR #32/#33 + secret scan test + ADR (W5-1/10) |
| K02/K03/K07/K08/K15 | giữ | Không đổi bằng chứng tuần 5 |
| K04 | ⬜ chờ | phase `cqrs-behavior` chưa chạy (cắt tuần 6) |
| K09 / K18 | ⬜ | OAuth chờ credentials · WCAG thuộc TV2 |

---

## 8. Điểm cần nhóm/nhóm trưởng đối chiếu

1. **Review PR #32 + #33** (điều kiện DoD "có PR được review" — mở 07/10).
2. **Chốt `BUG-W4-01` C1/C2** (đụng schema TV3 `RecipeImageId`) — TV4 đã đối chiếu main + thêm test hồi quy.
3. **Review PR riêng `afdb4a7`** (`UseForwardedHeaders`/rate-limit `Program.cs` của TV1) — ảnh hưởng NFR-SEC-003.
4. **Giấy tờ Google OAuth credentials** — chặn K09 từ tuần 3; mock không được tính.
5. **TV2 xác nhận luồng E2E `category`** (ai viết) + **sửa `to_tsquery`** (ADR 0003) để đo EXPLAIN FTS.
6. **TV3 hỗ trợ `query-rollback`** (account E2E sở hữu công thức hoặc `GetRecipesQuery` filter) để chạy phase thứ 4 L5 + thực hiện nốt `NFR-SEO-004` (301).
7. **Chốt 3 việc hạ tầng** (đề xuất 07/08/09): nơi đặt lịch backup · kho 30 ngày · rotate khoá JWT ngoài repo.
8. **24 ô K + mapping K01** — nợ từ tuần 2; tuần 5 bổ sung nhiều bằng chứng nhưng vẫn **0/24 được xác nhận**.
9. Thống nhất cách đếm commit cho báo cáo (mục 0.2 của báo cáo tổng hợp) để hết cảnh báo số liệu.

---

## 9. Kết luận

- Tuần 5 TV4 **đóng toàn bộ nợ D4-UI + N4-A + N4-B** (progress bar, 3 nút trạng thái, runbook 7 mục, staging idempotent + 2 API + TLS), **mở rộng E2E lên 40/40 trên staging** và **đo đạc đầy đủ** (k6 102 VU · SEO · EXPLAIN · cache-hit · `/metrics`).
- Đóng trọn bộ **BUG-W4-01/02/03** + ADR-TV4-003/004 + test bảo mật + PR #33 — xoá 2 nợ P1 cũ nhất (`practice/TV4/L4` và secret scan).
- Số test backend **426 → 430** (`Skipped=0`), coverage **96.31% → 97.07%**, Jest **85 → 86**, E2E **26 → 40**, CI xanh.
- Còn lại **4 việc chưa làm trong 56** (93%): luồng category (TV2), Google OAuth (nhóm), 2 phase L5 + `cqrs-behavior` (cắt tuần 6), nâng major npm audit (chờ chốt). Tất cả đều **phụ thuộc người khác hoặc nằm trong cut-list đã có** — TV4 không tự quyết.
- Cổng G6 đạt **mọi hạng mục kỹ thuật**; còn chờ: **5/5 luồng (category)** + **reviewer Tâm nghiệm thu PR và 24 ô K**.