# SỔ EVIDENCE TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh**: `2312739_NHTSon_D5-D7` (từ `origin/main` = `262201b`) · **Lab**: `practice/TV4/L4`, `practice/TV4/L5`
> **Trạng thái 07/10**: mọi mục bắt đầu ở **Chưa làm**. Ô K chỉ chuyển "đủ bằng chứng" khi có
> **code/config + test + kết quả chạy thật**, và chỉ chốt khi **reviewer Tâm xác nhận + ghi ngày**.
> Ô nào thiếu ghi **Chưa làm**, không làm tròn số, không tự đánh dấu ✅.

> [!IMPORTANT]
> 1. **Không dùng số của tuần 4 làm số của tuần 5.** Số 426/426, 96.31%, 26/26, k6 0.00% là số đo
>    trên nhánh/điều kiện tuần 4 (`fd90572`) — tuần 5 phải đo lại trên `262201b` (W5-1) và trên
>    **staging** (W5-7, W5-8).
> 2. **Không lấy 4 file `Lab 04` ở `docs/evidence/TV4/` làm minh chứng của TV4** (do TV1 tạo ở
>    commit `80b2c0e`, quyết định nhóm 30/09).
> 3. **`docs/evidence/TV4/Report/` là báo cáo phi chính thống của TV4** — không dùng làm căn cứ
>    nghiệm thu cho bất kỳ ai.
> 4. Việc của thành viên khác (TV1/TV2/TV3) được ghi ở mục 5 để đối chiếu **phụ thuộc**, không tính
>    vào tiến độ của TV4.

---

## 1. Baseline tuần 5 — ⬜ CHƯA ĐO (việc W5-1)

> Lệnh và môi trường y hệt cách đo tuần 4 (`../Tuan04/SO_EVIDENCE_TUAN_4.md` §1) để so sánh được.
> Điền bảng này sau khi chạy xong trên `262201b`. **Không** copy số tuần 4 vào đây.

| Hạng mục | Lệnh | Kết quả | Ngày | Log |
|---|---|---|---|---|
| Build backend | `dotnet build CulinaryBlog.sln -c Release` | **0 warning · 0 error** | 07/10 | `logs/baseline_build.log` |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes` | exit 0 | 07/10 | `logs/baseline_format.log` |
| Test | `dotnet test CulinaryBlog.sln -c Release` | **426/426** (421 + 5 `ConcurrencySpike`), Skipped 0 | 07/10 | `logs/baseline_test.log` |
| Coverage `CulinaryBlog.Application` | `bash deploy/check-coverage.sh` | **97.07%** (ngưỡng 80%) | 07/10 | `logs/baseline_coverage.log` |
| Frontend typecheck | `npx tsc --noEmit` | exit 0 | 07/10 | `logs/baseline_frontend.log` |
| Frontend lint | `npm run lint` | exit 0 | 07/10 | `logs/baseline_frontend.log` |
| Frontend build | `npm run build` | exit 0 | 07/10 | `logs/baseline_frontend.log` |
| Secret scan | `git grep` phạm vi W5-10 (A1) | **0** khớp `Password=postgres`/`minioadmin` còn lại | 07/10 | `logs/a1_*.log` ; commit `0a9b1a5` |
| CI `Backend week 1` + `Frontend CI` | GitHub Actions | **success** (run `0a9b1a5`, 2026-10-07T12:33–34Z) | 07/10 | [runs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/actions) |

---

## 2. Bảng 24 ô kỹ năng — trạng thái kế thừa 07/10 (chưa ô nào được Tâm xác nhận)

> Nguồn: `../Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` §7 (cập nhật 05/10). Tuần 5 chỉ **thêm bằng chứng
> mới** vào các ô W5-x mở lại (K04, K05, K06, K10, K11, K12, K13, K17, K19, K22, K23, K24), không tự đổi
> trạng thái ô khác. *(`07/10` — bổ sung K04/K06/K11/K12/K13 sau rà soát nợ, xem §3.10.)*

| Ô | Trạng thái kế thừa | Việc tuần 5 có bổ sung bằng chứng |
|---|---|---|
| K01 Phân tích SRS/FR-NFR/ADR/contract | 🟡 có nền — mapping đã lập, chờ Tâm | Nộp lại mapping |
| K02 .NET10 Minimal API/REST/Scalar/RFC7807 | 🟢 gần đạt | — |
| K03 Clean Architecture/DI | 🟢 gần đạt | — |
| K04 CQRS/MediatR behaviors | 🟡 có nền | ✅ **W5-9** (phase `cqrs-behavior` — L5 tuần 4 chưa chạy) |
| K05 FluentValidation/Zod/RHF | 🟡 có nền | ✅ **W5-6**, **W5-9** (Zod/RHF FE) |
| K06 EF Core/PG16 migration/index | 🟡 có nền | ✅ **W5-8** (EXPLAIN re-run — `N2-6` bị cắt) |
| K07 UoW/transaction/concurrency | 🟢 gần đạt | — |
| K08 Identity/PBKDF2/JWT/refresh/logout | 🟢 đủ bằng chứng | — |
| K09 Google OAuth2/PKCE | ⬜ thiếu thật (chờ credentials) | W5-9 — vẫn ghi "còn chờ" nếu chưa có credentials |
| K10 RBAC/rate limit/HTTPS/CORS/secrets | 🟢 gần đạt | ✅ **W5-4** (HTTPS/HSTS/CORS) · **W5-10** (dọn secret A1) |
| K11 FTS/GIN/ts_rank | 🟡 có nền | ✅ **W5-8** (EXPLAIN FTS — phụ thuộc TV2 sửa `to_tsquery`) |
| K12 Redis cache-aside/invalidation/fallback | 🟢 gần đạt | ✅ W5-3 (cache chung ở staging) · **W5-8** (cache-hit ratio) |
| K13 MinIO/S3 upload/delete/magic bytes | 🟢 đủ bằng chứng | ✅ **W5-10** (test path traversal `../`) |
| K14 Hangfire/retry/persistence | 🟢 gần đạt | ✅ **W5-3** (số Hangfire server khi 2 API) |
| K15 SMTP/resize/sitemap XML | 🟢 gần đạt | — |
| K16 Next.js App Router/SSR/ISR/CSR | 🟡 có nền | W5-9 (image-opt/ISR của Lab L5) |
| K17 TanStack Query/next/image/progress | 🟡 có nền | ✅ **W5-6** (progress upload, nút trạng thái + Xóa) |
| K18 Responsive/WCAG | ⬜ thiếu thật — **thuộc TV2** | — |
| K19 SEO metadata/OG/JSON-LD/robots/sitemap | 🟢 đủ bằng chứng | ✅ **W5-8** (SEO + 301 trên staging) |
| K20 Serilog/Seq/OTEL/health | 🟢 đủ bằng chứng | ✅ **W5-8** (metrics scrape thật — `FR-OBS-003`) |
| K21 xUnit/API/Jest/Playwright | 🟢 đủ bằng chứng | ✅ **W5-7** (5 luồng G6) |
| K22 k6/p95/p99/EXPLAIN/CWV | 🟡 có nền | ✅ **W5-8** (k6 trên staging ≥100 VU + EXPLAIN) |
| K23 Docker/Compose/Nginx/volumes/backup/restore | 🟢 gần đạt | ✅ **W5-2, W5-3, W5-5** |
| K24 Git/PR/review/CI/secret scan/docs | 🟢 đủ bằng chứng | ✅ **W5-1** (baseline + PR) · **W5-10** (PR `practice/TV4/L4`, scan-secrets mở rộng) |

**Tổng đầu tuần 5**: 🟢 đủ 6 · 🟢 gần đạt 8 · 🟡 có nền 8 · ⬜ thiếu thật 2 — **0/24 được Tâm xác nhận**.

---

## 3. Bằng chứng mới của tuần 5 (điền khi làm)

> Mỗi bản ghi dùng mẫu ở mục 4. Không có bằng chứng thì **để trống**, không viết "đã làm".

### 3.1. W5-1 — Baseline + PR

| Ngày | Việc | Kết quả | Log/commit/PR |
|---|---|---|---|
| 07/10 | Baseline đo lại trên nhánh tuần 5 (gốc `262201b` + commit `0a9b1a5`) | 426/426 · coverage 97.07% · build 0W/0E · format exit 0 · FE tsc/lint/build exit 0 · CI xanh | `logs/baseline_*.log`; CI run `0a9b1a5` |
| 07/10 | Mở PR #32 (`week-5` → `main`) + PR #33 (`practice/TV4/L4` → `main`) | 2 PR đang chờ review | PR #32, PR #33 |

### 3.2. W5-2 — Deploy staging (2 lần)

| Ngày | Việc | Kết quả | Log |
|---|---|---|---|
| 07/10 | **run 1** — dựng staging từ `docker-compose.staging.yml` | 6 container lên (db/redis/s3/api/nginx/frontend), `/health/ready` **200** (database + redis + object-storage), FE **200**, sitemap **200** (nginx + API), register/login OK, create recipe OK, upload 1 ảnh **201**; fail-fast thiếu `JWT_SIGNING_KEY` exit **1** ✓ | `logs/staging_run1.log` |
| 07/10 | **run 2** — `down -v` → `up -d --build` (volume sạch) | toàn bộ checklist xanh lặp lại; `culinary-init` tạo bucket mới (aws-cli exit 0); recipe dùng categoryId **động** (volume mới → GUID seeded đổi) | `logs/staging_run2.log` |
| 07/10 | **Drill backup→restore** trên staging (client PG16 trong container `postgres:16-alpine`) | `backup.sh` exit 0 (`culinary_20261007T145532Z.dump` 287KB) → `DROP DATABASE ... WITH (FORCE)` → `restore.sh` exit 0 → **14 bảng public = đúng N1** → `up -d` → ready 200 + FE 200 + truy vấn `"Categories"` | `logs/staging_backup.log` + `logs/staging_restore.log` |
| 07/10 | **Trace HTTP→EFCore→Postgres** (mục A6) | 1 request `GET /api/v1/recipes?sort=<key mới chưa cache>` → Seq: **HTTP span** (RequestLoggingMiddleware) + **LoggingBehavior** + **EFCore span** `CommandExecuted` (`SELECT r."Id"... FROM "Recipes"...`, `SELECT count(*)...`) — **3 span cùng `CorrelationId=54b376a2...`** | `logs/staging_trace.log` |
| 07/10 | Ghi chú trace | `/api/v1/categories` bị **cache Redis** (`staging:cache:categories:all`) nên request sau không sinh EF — phải dùng request key mới. EF span cần `Logging__LogLevel__Microsoft.EntityFrameworkCore=Information` + `Program.cs` cho phép override (default vẫn Error như tuần 4) | `logs/staging_trace.log` ; `src/backend/CulinaryBlog.API/Program.cs` |
| 07/10 | item 6 — giới hạn hạ tầng | `render.yaml` vẫn `plan: free`; staging chạy trên máy dev → **không** tuyên bố `NFR-REL-001` 99,5%. Chi tiết đi sâu ở mục "Giới hạn" runbook (W5-5, 10/10) | — |

### 3.3. W5-3 — 2 API instance trên staging

| Ngày | Việc | Kết quả | Log |
|---|---|---|---|
| 08/10 | Thêm profile `multi` + service `culinary-api-2` (cùng image/env, port 5081) | `docker compose --profile multi config --quiet` exit 0 (default + multi) | `docker-compose.staging.yml` |
| 08/10 | Nginx upstream 2 server + failover | `nginx -t` "syntax is ok" — `zone 128k` + `server culinary-api-2 resolve` + `resolver 127.0.0.11`; single mode vẫn chạy (không phá DoD W5-2) | `nginx/nginx.staging.conf` |
| 08/10 | Trải đều request | 12 request qua nginx: **api1=6 api2=6 (50/50)**, 0 non-200 | `logs/staging_two_api.log` |
| 08/10 | Failover FR-OBS-001 | stop api-2 → 0/8 non-200 (api1 gánh); start lại → 0/8 non-200 | `logs/staging_two_api.log` |
| 08/10 | Số Hangfire server | `hangfire."Server"` có **2** server (1/instance, cùng storage PostgreSQL); console 2 instance "Starting Hangfire Server..." | `logs/staging_two_api.log` |
| 08/10 | Cache/queue chung + khoá phân tán | Cả 2 instance cùng `Redis__Host: culinary-redis`; key `staging:cache:*`/`staging:sitemap:xml` dùng chung; `SitemapLockTests` trong bộ 426 test | `logs/staging_two_api.log` |

### 3.4. W5-4 — HTTPS / HSTS / CORS / volumes

| Ngày | Việc | Kết quả | Cấu hình + header đo được |
|---|---|---|---|
| 09/10 | TLS staging (chứng thư tự ký) + redirect HTTP→HTTPS + HSTS | `service culinary-certs` (alpine+openssl) sinh `CN=localhost` vào volume `staging_certs` (không commit key); nginx `listen 443 ssl` + `ssl_protocols TLSv1.2/1.3`; port 80 = `return 301 https://$host$request_uri`; header `Strict-Transport-Security: max-age=31536000; includeSubDomains` | `nginx/nginx.staging.conf`, `docker-compose.staging.yml` ; HTTP / → **301** → HTTPS / → **200** + đủ security headers |
| 09/10 | CORS origin tường minh + `UseForwardedHeaders` (`BUG-W4-03`) | `Cors:AllowedOrigins` (compose staging đặt http/https localhost; Production phải khai, rỗng = từ chối) thay `AllowAnyOrigin`; `UseForwardedHeaders` (`X-Forwarded-For|Proto`, `ForwardLimit=1`, `KnownIPNetworks 172.16/12`) chạy trước rate limiter → mỗi IP một bucket | `Program.cs` (commit `afdb4a7` — **PR riêng, thuộc TV1**) ; OPTIONS origin `evil.example` → không có `Allow-Origin`, origin localhost → có; login XFF A: 10×400 + 429, XFF B: 400 (bucket riêng) |
| 09/10 | Volumes persistent (DB/S3/Redis) | named `staging_pgdata`/`staging_s3data`/`staging_redisdata` (+ `staging_certs`); DB: register 201 → `docker compose restart culinary-db` → login 200 (user còn); Redis appendonly: SET → restart → GET còn | `logs/w54_tls_cors.log` |
| 09/10 | Sửa `/health` = 503 oan (companion) | `MinIOHealthCheck` đọc `HealthChecks:Minio:Host` riêng (default `localhost:9000`) — thêm vào `x-api-env` → `/health` (toàn bộ check) 200 cả direct lẫn qua nginx | `docker-compose.staging.yml` |
| 09/10 | ImageSharp 3.1.11 → 3.1.12 + suppress 5 advisory (chưa có bản vá miễn phí) | NuGetAudit flag cả nhánh `<= 4.1.1`; `first_patched=4.1.2` = **commercial** → giữ 3.1.12 (Apache-2.0 mới nhất), `NuGetAuditSuppress` đúng 5 URL; build/test/format xanh; audit vẫn bật cho package khác | `Directory.Build.props`, `ADR-TV4-004` |

### 3.5. W5-5 — Runbook `docs/RUNBOOK.md`

| Ngày | Việc | Kết quả | Đường dẫn |
|---|---|---|---|
| 10/10 | Viết runbook đủ 7 mục, mỗi mục có **số liệu/log thật** | A1 dựng stack từ đầu (run2 = **24s** tới `/health/ready` 200; fail-fast thiếu `JWT_SIGNING_KEY`; chưa đo máy sạch 100%); A2 backup→restore (`culinary_20261007T145532Z.dump` 287KB, **14 bảng** sau restore); A3 failover từng dependency (drill tuần 4: Redis hồi **0.2s**, S3 ~0.5s, API node = SPOF; staging 2 API: stop api-2 → 8/8 200); A4 2 API (round-robin **6/6 = 50/50**, Hangfire 2 server); A5 k6 (**chưa đo staging** → ghi rõ + điều kiện ghi kèm); A6 trace theo TraceId (HTTP→EFCore→Postgres, 3 span cùng `CorrelationId`); A7 Seq (`localhost:5341`; staging gửi qua `host.docker.internal:5341`); mục **Giới hạn** (TLS tự ký, SLA chưa đo, backup cục bộ 30 ngày, k6/chỉ số staging W5-8) | `docs/RUNBOOK.md` ; commit `92e34be` |

### 3.6. W5-6 — D4-UI: progress upload + Unpublish/Archive/**Xóa**

| Ngày | Việc | Kết quả | Test/E2E |
|---|---|---|---|
| 09/10 | `uploadImage` chuyển sang **XMLHttpRequest** + `onProgress(%)` | `ImagesStep` có `role="progressbar"` (aria-valuenow tăng theo % thật, có nhãn %); gỡ phụ thuộc fetch-response không báo progress | `src/lib/recipe-editor.ts`, `_wizard/ImagesStep.tsx` ; commit `d65cc17` |
| 09/10 | Nút **Gỡ đăng** (Published→Draft), **Lưu trữ** (Draft→Archived), **Xóa** (soft delete) trong dashboard công thức | `my-recipes.ts` gọi `PATCH /recipes/{id}/unpublish`, `/archive`; `page.tsx` (MyRecipes) thêm nút theo trạng thái + xác nhận 2 bước + state `actingId`; after mutation refresh danh sách từ API (dữ liệu thật, không mock) | commit `d65cc17` |
| 10/10 | **E2E thật trên staging** luồng Gỡ đăng/Lưu trữ/Xóa | `recipe-unpublish.spec.ts` chạy chống staging (API 5080 + FE thật), **3/3 pass** (10.4s): Published→Bản nháp / Draft→Lưu trữ (hết nút Lưu trữ) / Xóa→dòng biến mất (soft delete); trước khi chạy phải `docker stop staging-culinary-frontend` để nhả port 3000, bật lại sau đó | `e2e/recipe-unpublish.spec.ts` ; commit `367b42e` |
| 10/10 | CORS cho Playwright chống staging | thêm `http://127.0.0.1:3000` vào `Cors__AllowedOrigins` → OPTIONS preflight **204 + `Access-Control-Allow-Origin`**; đã recreate `culinary-api` (+certs) | `docker-compose.staging.yml` (commit `367b42e`) |
| 10/10 | Unit test progress bar | `ImagesStepProgress.test.tsx` — **pass** (phải `findByRole` vì zod resolver bất đồng bộ); full suite **86/86** pass | commit `2969f94` |
| 10/10 | Verify overall | `tsc`/lint/build sạch sau dồn commit; **6 spec E2E cũ fail trên staging là do spec cũ lỗi thời + presigned URL `culinary-s3:9000` không trỏ từ trình duyệt** (gap `NEXT_PUBLIC_MEDIA_URL`, xử lý W5-7), **không phải regression W5-6** (commit d65cc17 chỉ chạm 4 file nêu trên) | log chạy E2E staging 10/10 |

### 3.7. W5-7 — 5 E2E flows trên staging

| Luồng | Spec | Do ai viết | Kết quả trên staging |
|---|---|---|---|
| register | `create-recipe.spec.ts` | TV4 | ✅ pass (trong bộ 40/40) |
| login | `create-recipe.spec.ts` | TV4 | ✅ pass (trong bộ 40/40) |
| category | chưa có | **cần TV2 xác nhận** | ⬜ (chờ TV2) |
| draft / create-recipe | `wizard-week4.spec.ts`, `create-recipe.spec.ts` | TV4/TV3 | ✅ **10/10** `wizard-week4` pass |
| publish | `recipe-publish.spec.ts` | TV4 | ✅ **4/4** pass (B1-1…B1-4) |

| Ngày | Việc | Kết quả | Log/commit |
|---|---|---|---|
| 11/10 | **Chặn gốc lỗi ảnh nháp/vừa tải (presigned `culinary-s3:9000` không trỏ từ trình duyệt)** | `recipe-editor.ts`: `mediaUrl` fallback `MEDIA_DEFAULT = ${API}/resources/images` khi thiếu `NEXT_PUBLIC_MEDIA_URL`; `imageSrc` bỏ `presignedUrl` → **luôn** ra proxy `/api/v1/resources/images` → `AuthImage` fetch + Bearer → blob; `ImagesStep` bỏ UI "liên kết hết hạn / Tải lại" (placeholder "Ảnh chưa có đường dẫn") | commit `9f3dd4c` |
| 11/10 | Sửa spec E2E lỗi thời (6 spec fail 10/10 = finding) | `recipe-publish.spec.ts`: lỗi zod dưới ô (`#err-title`/`#err-category`) thay banner, dòng nháp chọn theo **placeholder** (`Tên *`, `SL`, `Tiêu đề bước *`) thay aria-label động `(dòng N)`, nav-scope `Các bước soạn công thức`; `wizard-week4.spec.ts`: loại `#__next-route-announcer__` (giữ tiêu đề route sau `replaceState ?step=`) | commit `9f3dd4c` |
| 11/10 | **Chạy E2E chống staging thật** (stop `staging-culinary-frontend` nhả port 3000 → `next dev` 127.0.0.1:3000 → API 5080) | **full suite 40/40 pass** (2.8m): `recipe-publish` 4/4 · `wizard-week4` 10/10 · `create-recipe` · `search` 12 · `upload-security` 10 · `recipe-unpublish` 3; ảnh blob `^blob:` + JSON-LD ảnh tuyệt đối trả 200; sau đó `docker start staging-culinary-frontend` | `npm run test:e2e` 11/10 |
| 11/10 | Verify jest + build | jest **86/86** (recipe-jsonld cập nhật kỳ vọng mediaUrl mặc định = proxy tuyệt đối, không còn null khi thiếu `NEXT_PUBLIC_MEDIA_URL`) · `tsc`/`lint`/`build` exit 0 | commit `9f3dd4c` |
| 11/10 | Hồi quy full suite sau fix | 40/40 xanh lần 2 (xác nhận không phải fix "ăn may") | `npm run test:e2e` 11/10 |

### 3.8. W5-8 — Số đo load/SEO + số đo bị cắt tuần 4

| Ngày | Hạng mục | Số đo | Ghi giới hạn |
|---|---|---|---|
| | k6 ≥100 VU (p50/p95/p99, lỗi%) | | |
| | SEO (sitemap/robots/metadata/JSON-LD/301) | | |
| | EXPLAIN re-run (`N2-6`) | | |
| | Cache-hit ratio (K12) | | |
| | Metrics scrape thật (`FR-OBS-003`: count/duration/error) | | |

### 3.9. W5-9 — Lab còn thiếu + nợ kỹ thuật

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| `N3-A3` Zod/RHF FE | ✅ **10/10 (xác nhận lại)** — đã dùng sâu trong wizard từ tuần 3: zod schema (`recipe-schemas.ts`, `IngredientsStep.tsx`, `StepsStep.tsx`), `zodResolver` + React Hook Form toàn bộ form tạo/sửa (dùng 11 nơi `zodResolver`, 5 file import `react-hook-form`) → **không còn "chưa import"**; kết hợp E2E publish `recipe-publish.spec.ts` | `src/lib/recipe-schemas.ts`, `src/app/dashboard/recipes/_wizard/*` |
| `N3-A5` Google OAuth | ⬜ (chờ credentials từ nhóm — không tự sinh; không tính đạt đến khi có client-id/secret) | — |
| **4** phase Lab L5 (`isr-detail`, `image-opt`, `search-ssr`, `query-rollback`) | phần sửa code **2/4**: `isr-detail` **đã sửa** — bỏ nhánh `isDev` ép `no-store` trong `getRecipeBySlug` + thêm `generateStaticParams` (lấy slug từ sitemap API, CI-safe trả `[]`) → build ra route `●` **ISR thật** (104 trang static, `revalidate=300`); `search-ssr` **đã đúng khuyến nghị L5** — `no-store` vốn nằm ở fetch (`getRecipes`/`searchRecipes`) chứ không do route bị ép dynamic; `image-opt` + `query-rollback` **còn ⬜** (cần lab re-run `practice/TV4/L5`, để W5-13) | `src/lib/api.ts:379-389`, `src/app/recipes/[slug]/page.tsx` (commit W5-9) ; `docs/evidence/TV4/Tuan04/report/SOK_LAB_L5.md` |
| Phase `cqrs-behavior` (K04) | ⬜ (ngoài harness L5, để W5-14) | — |
| `npm audit` | ✅ **10/10** — **46 → 44 vuln, critical → 0**: `next` 15.1.11 → **15.5.27** (cùng major, không breaking) mù 1 CVE critical; còn lại **đều cần nâng major** (next p/moderate qua postcss → fix `next@16`, `tailwindcss` 4.3.3, `jest`/`@types/jest`/`jest-environment-jsdom` 30.x) → **đề xuất lên nhóm chốt**, không tự nâng major trong tuần | `package.json` (commit W5-9) ; `npm audit` 10/10 |

### 3.10. W5-10 — Đóng `BUG-W4-01/02/03` + nợ nhỏ P1/P2 *(bổ sung 07/10)*

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| Đối chiếu + chốt `BUG-W4-01` (`ValueGeneratedNever` đã thấy ở `main` — `c624b9f`) | ⬜ | |
| Dọn secret còn lại: `appsettings*.json` (`Password=postgres`, `minioadmin`) + `.github/workflows/backend.yml` | ✅ **07/10** — dọn + `git grep` = 0 trong phạm vi; test/format/coverage lại đều xanh | commit `0a9b1a5` ; `logs/a1_test.log`/`a1_format.log`/`a1_coverage.log` |
| Mở rộng `deploy/scan-secrets.sh` (quét appsettings + `env:` workflow) + test | ✅ **08/10** — thêm `check_json_config_secrets` + `check_workflow_env_secrets`; ngưỡng riêng (`${…}`, `${{…}}`, `ci-*`); test **9 PASS/0 FAIL** + full-scan exit 0; CI thêm bước test | `deploy/scan-secrets.sh`, `deploy/scan-secrets.test.sh`, `.github/workflows/backend.yml` ; `logs/scan_secrets.log` |
| `BUG-W4-03` — sửa trực tiếp trong W5-10 (chưa từng có bản vá trong dự án; `Program.cs` của TV1 → PR riêng) | ✅ **09/10** — `UseForwardedHeaders` + `KnownIPNetworks 172.16/12` + `ForwardLimit=1` chạy trước rate limiter; XFF A: 10×400+429, XFF B: 400 (mỗi IP một bucket). Commit `afdb4a7` (**PR riêng — Tâm review**) | `logs/w54_tls_cors.log`, `Program.cs:409-421` |
| Mở PR cho `practice/TV4/L4` | ✅ **07/10** — PR #33 (`practice/TV4/L4` → `main`); CI Frontend xanh | ![PR #33](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/33) |
| ADR soft-delete `Recipe` vs `Category` + đổi tên test (`BUG-W4-06`) | ✅ **09/10** — ADR `ADR-TV4-003` ghi rõ Recipe soft delete (interceptor+query filter) / Category hard delete (`CategoryRepository.cs:51`, C07); `MarkDeleted` = dead code, KHÔNG xoá; test `CategoryTests.cs:146` đổi → `...hard_deletes_when_empty` | `docs/adr/ADR-TV4-003-quy-uoc-soft-delete.md` ; commit `0084bb9` |
| Header `X-Sitemap-Generated` (`BUG-W4-09`) | ⬜ | |
| Test path traversal `../` (`NFR-SEC-004`) | ✅ **09/10** — `PathTraversalSecurityTests`: 5 unit (server đặt key `recipes/{id}/image{ext}`, FileName client không vào path) + proxy 404 cho mọi `..`, không phục vụ ngoài prefix (guard `minioUp`); **7/7 pass** | `tests/CulinaryBlog.Tests/PathTraversalSecurityTests.cs` ; commit `2d40fb0` |

---

## 4. Mẫu bản ghi evidence (giữ nguyên format các tuần trước)

```text
Tuần / Người / Task: 5 / Nguyễn Hữu Trung Sơn (TV4) / W5-x
FR/NFR/Kỹ năng: <mã FR/NFR>; K<...>
Trạng thái: Chưa làm | Đang làm | Chờ tích hợp | Chờ review | Hoàn thành
Đầu ra, đường dẫn code/config, PR/commit:
Test/lệnh chạy, môi trường, kết quả thực tế:
Minh chứng ảnh/log/video/coverage (không có secret):
Reviewer (Nguyễn Thanh Tâm) và ngày xác nhận:
Trở ngại, người hỗ trợ, hạn xử lý:
```

**Điều kiện một task hoàn thành** (DoD 12.3): đúng FR/ADR · UI/API/DB nối thật · quyền/validation/cache/job
phù hợp · test và CI pass · docs cập nhật · **Tâm duyệt** · có evidence trong sổ này.

---

## 5. Phụ thuộc thành viên khác (đối chiếu, không tính vào tiến độ TV4)

| Việc | Thành viên | Ảnh hưởng với TV4 |
|---|---|---|
| Luồng E2E `category` | TV2 | W5-7 — thiếu thì G6 chưa đủ 5 luồng |
| Checklist WCAG/responsive | TV2 | G6 có mục a11y — số đo do TV2, TV4 ghi dẫn chiếu |
| Jest/RTL frontend unit test | TV1 | Không chặn việc nào của TV4 |
| Rate limit phân tán (`BUG-W4-03` = `UseForwardedHeaders`) — TV4 sửa trực tiếp W5-10, cần Tâm review PR (đụng `Program.cs`) | Tâm (review) | Ảnh hưởng số k6 (W5-8) — chưa sửa xong thì phải ghi giới hạn |
| Chốt `BUG-W4-01` C1/C2 (đụng schema TV3) | Tâm + TV3 | W5-10 — đối chiếu trạng thái trên `main` trước khi chốt |
| `query-rollback` (RowVersion): cần account E2E **sở hữu** công thức / `GetRecipesQuery` filter chủ sở hữu | TV3/Tâm | W5-9 — thiếu thì phase thứ 4 của L5 không chạy được |
| K11 EXPLAIN FTS phụ thuộc sửa `to_tsquery` (ADR 0003) | TV2 | W5-8 — phần EXPLAIN FTS chưa đo được |
| Chốt lịch backup / kho 30 ngày / rotate JWT | Tâm + TV2 | W5-5 phải ghi đúng giới hạn hiện tại (backup giữ 7 ngày) |
| Google credentials | Nhóm | K09 / `N3-A5` không đạt được |

---

## 6. Quy tắc khi ghi sổ tuần 5

1. **Không copy số tuần 4** vào mục 1 hoặc mục 3 — số cũ chỉ được trích kèm nguồn + ngày đo.
2. Bằng chứng phải **tái lập được**: ghi đúng lệnh, môi trường, commit.
3. Không ghi secret, token, khoá vào sổ (test `JwtSigningKeyNotCommittedTests` quét cả tài liệu `.md`).
4. Việc chưa làm để **⬜**, không ghi "hoàn thành một phần" trừ khi ghi rõ phần nào xong, phần nào thiếu.
5. Ô K chỉ đổi trạng thái khi có **cả 3**: code/config · test/log chạy thật · **Tâm xác nhận + ngày**.
