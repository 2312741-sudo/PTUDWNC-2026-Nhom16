# SỔ EVIDENCE TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Trạng thái**: Tất cả bắt đầu ở **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.
> **Cập nhật 24/09**: D3 archive/delete, D4 SEO, D5 OTEL, E2E D1.3 MinIO đã hoàn thành — 133/133 + 5/5 pass local, frontend build OK; chờ reviewer xác nhận.
> **Cập nhật 27/09**: D3 invalidation đóng bằng xác minh; **D27 proxy ảnh PA-2 xong** — E2E `ImageProxyD27Tests` 6/6; **D5 EXPLAIN/k6 ghi số liệu thật** (list 0.339ms; k6 3310 req, 0% fail, p95 225.63ms); toàn suite **139/139 + 5/5**; `IMAGE_CONTRACT.md §5` chốt PA-2. Chờ reviewer xác nhận.
> **Cập nhật 28/09**: **D6 Lab L4 (N5) xong** — 4 phase · **39/39 check PASS** (media 25/25, email 3/3, xml 3/3, jobs 8/8), sổ K `SOK_LAB_L4.md`, log `lab_l4_{run.log,db.txt}`; **CI 5 run đỏ đã gỡ** (nguyên nhân: image MinIO bị gỡ khỏi registry — `quay.io` 401, Docker Hub 404, **không phải lỗi code**) → thay `rustfs/rustfs` + harden, **CI xanh run `36344662570`**. Toàn suite **148/148 + 5/5** (`Skipped=0`). Chờ reviewer xác nhận.

---

## 1. Bảng trạng thái kỹ năng tuần 3

| K | Loại | Kỹ thuật con | Sẽ chứng minh ở | Tuần 3 | Trạng thái |
|---|---|---|---|---|---|
| K01 | SP | SRS/FR-NFR/ADR/API contract | ADR-TV4-001 (D08/D17/D21/D22/D23/D26/D27) + mapping FR | N0, D3, D4, D5 | Chưa làm |
| K02 | SP | .NET10 Minimal APIs, REST/version, Scalar/RFC7807 | Archive/delete endpoints + 422/403 | D3 | Đã làm (chờ review) |
| K03 | SP | Clean Architecture, interface, DI, value object | Presigned/proxy qua `IFileStorageService` extension + domain methods | D3, D4 | ✅ Đã làm 27/09 (D27 — `IObjectStorageReader` tách khỏi `IFileStorageService`) |
| K04 | SP+LAB | CQRS/MediatR + logging/validation/caching behaviors | Archive/delete CQRS handlers + LAB behavior | D3 + D6 | Đã làm (D3) |
| K05 | SP | FluentValidation + sanitization | Validator transition trạng thái + sanitize UI input | D3, D4 | Đã làm (D3) |
| K06 | SP | EF Core Code First, migration/config/seed, LINQ/index | Fix migration `RefreshTokens` index (N0) + migration soft-delete | N0, D3 | Đã làm (N0) |
| K07 | SP+LAB | UoW/transaction/audit/soft delete/RowVersion | Race đổi trạng thái + soft delete | D3 | Đã làm (D3 — MarkDeleted + RowVersion 422) |
| K10 | SP | RBAC/ownership/policy/rate limit/secrets | Ownership archive/delete + 403 non-owner | D3 | Đã làm (D3) |
| K12 | SP+LAB | Redis cache-aside, OutputCache, invalidation | Invalidation archive/unpublish/delete + LAB | D3 + D6 | 🟡 Đã xác minh 27/09 — không cache recipe (thỏa mặc định); **28/09: LAB cache L4 cũng đóng** — cache-aside ở mức lab chưa có tích hợp thật để kiểm chứng, ghi rõ giới hạn trong `SOK_LAB_L4.md` thay vì đóng bằng giả định |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID path | E2E D1.3 trên MinIO + 4 MIME + SP upload/delete | D3 + D6 | ✅ Đã làm — E2E 3/3 + **LAB 28/09: `media` 25/25 (magic bytes 4 MIME + file MIME giả + quá giới hạn + idempotent + dọn dẹp)** |
| K14 | SP+LAB | Hangfire fire-and-forget/delayed/recurring/retry | SP resize job + LAB delayed/restart | D2 + D6 | ✅ **Xong cả SP lẫn LAB 28/09** — SP: `ResizeImageJob` chạy thật `Succeeded`, retry 3, dashboard Admin-only, E2E 4/4; LAB: `jobs` 8/8 gồm **tắt worker → job còn `Scheduled` trong DB → chạy khi restart** + retry quan sát được + recurring do scheduler kích hoạt |
| K15 | LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB L4 Mailhog + resize + XML | D6 | ✅ **Đã làm 28/09 (N5)** — `email` 3/3 (MailKit plain+HTML qua Mailhog, đối chiếu subject) + `xml` 3/3 (sitemap từ DB thật, 93 URL Published, parse `XDocument`) + resize 300×200/800×533 |
| K16 | SP | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | Uploader UI hoàn thiện (D4) | D4 | Chưa làm (chờ D27) |
| K17 | SP | TanStack Query, optimistic rollback, next/image | Uploader progress/gallery/primary optimistic | D4 | Chưa làm (chờ D27) |
| K18 | SP | Responsive, WCAG2.1 AA, keyboard/loading/error | Upload/status checklist | D4 | Chưa làm |
| K19 | SP | SEO metadata/OG/canonical/robots/JSON-LD | Sitemap/robots/OG/JSON-LD Published-only | D4 | Đã làm (SEO) |
| K20 | SP | Serilog/Seq/correlation, OTEL, metrics, health | OTEL trace HTTP→DB + health thành phần | D5 | ✅ Đã làm (cấu hình + xác minh; trace thật qua Seq khi stack bật) |
| K22 | SP | k6/EXPLAIN/cache hit/CWV | EXPLAIN publish query + k6 + cache hit | D5 | ✅ Đã làm 27/09 (EXPLAIN + k6 smoke ghi số liệu) |
| K23 | SP | Docker/Compose/Nginx/volumes/backup-restore | CI xanh (N0) + stack vận hành + queue service | N0, D2 | ✅ **Đã làm + gia cố 28/09** — stack vận hành; **CI 5 run đỏ do image MinIO bị gỡ khỏi registry đã gỡ** (thay `rustfs/rustfs` tag+digest trong workflow lẫn dev compose, bỏ `minio-init`); CI xanh run `36344662570`; queue service (Hangfire + PostgreSQL) chạy thật ở cả SP lẫn LAB |
| K24 | SP | Git/PR/review/CI/static analysis/secret scan/docs | PR nhỏ từng task + review Tâm + note PR #14 | N0, Tất cả | Đang làm |

---

## 2. Mẫu bản ghi evidence

```text
Evidence: TV4-Kxx (FR/NFR: ...)
Tuần / Người / Task: [tuần] / TV4 / [task]
Đường dẫn code/config: [đường dẫn cụ thể trong repo]
Nhánh / PR / commit: [nhánh] / PR #[số] (review: Nguyễn Thanh Tâm)
Test/lệnh chạy + môi trường: [lệnh cụ thể]
Kết quả thực tế (ảnh/log/coverage): [chụp log/ảnh, không có secret]
Minh chứng demo: [link ảnh/video khi có]
Reviewer + ngày xác nhận: Nguyễn Thanh Tâm / [ngày]
Lỗi còn lại / ảnh hưởng: [ghi rõ nếu có]
```

---

## 3. Evidence chi tiết tuần 3

> Các mục được bổ sung khi có kết quả triển khai (kèm lệnh chạy + bằng chứng thật). Không điền trước nội dung chưa có.

### TV4-K06 (N0 — fix duplicate migration `RefreshTokens` + CI xanh) ✅

```text
Evidence: TV4-K06 (kỹ thuật migration; FR-AUTH liên quan bảng RefreshTokens)
Tuần 3 / TV4 / N0
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/Migrations/ (xoá 20260923104044_AddRefreshTokens.cs + 20260916102353_AddRecipeDiscoveryAndSearch.cs trên main)
Nhánh/PR: main `a651c8a` (fix) → merge vào 2312739_NHTSon_D3-D4-D5-D6 (`e026dc9` + `75a8bf5`)
Test/lệnh: dotnet build --no-restore Release (0 warning); dotnet format (sạch); dotnet test tests/CulinaryBlog.Tests → 120/120; spike → 5/5 (TEST_DATABASE local)
Kết quả: Migrate() trên DB mới không còn lỗi "RefreshTokens already exists"; 16 test Auth/Week3 trước đây fail giờ pass
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: chờ CI GitHub xanh (push branch tuần 3)
```

### TV4-K02/K10 (D3 — Archive/delete + ownership) ✅

```text
Evidence: TV4-K02/K10 (FR-RCP-006/007; D08)
Tuần 3 / TV4 / D3
Đường dẫn: src/backend/CulinaryBlog.Application/Recipes.cs (region D3 — ArchiveRecipeCommand/DeleteRecipeCommand/Validator/Handler); Program.cs endpoints PATCH /{id}/archive + DELETE /{id}; Recipe.cs MarkDeleted(); tests/RecipeLifecycleTests.cs (+117 dòng)
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6 (commit chưa push — sẽ ghi sau push/CI)
Test/lệnh: dotnet test CulinaryBlog.Tests -c Release + TEST_DATABASE local → 133/133 pass; archive ẩn khỏi public list ngay; delete soft ẩn mọi truy vấn, không xoá vật lý ảnh
Kết quả: Published/Draft→Archived idempotent; non-owner 403; deleted không xuất hiện public/search/list
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: —
```

### TV4-K13 (D3.4 — E2E D1.3 trên MinIO + MinIO down) ✅

```text
Evidence: TV4-K13 (FR-FILE-001/002; D1.1c)
Tuần 3 / TV4 / D3.4
Đường dẫn: tests/CulinaryBlog.Tests/MinioE2ETests.cs (ApiFactoryWithMinio); MinioStorageService.cs; Program.cs image endpoints; .github/workflows/backend.yml (service MinIO + wait health)
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6
Test/lệnh: dotnet test CulinaryBlog.Tests --filter MinioE2E -c Release + TEST_DATABASE + MinIO local (127.0.0.1:9000, minioadmin, bucket culinary-blog) → 3/3 pass, lặp nhiều lần ổn định; MinIO tắt → test skip an toàn (không fail)
Kết quả: register→create(201)→upload JPEG(201)→readback→PATCH primary→publish→unpublish→archive→delete→không còn trong public list; log không lộ secret
Fix phát hiện bởi E2E: JWT RoleClaimType="role" (MapInboundClaims=false) — trước đây AuthorPolicy API thật luôn 403
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: —
```

### TV4-K03 (D27 — Proxy ảnh PA-2: interface + DI + stream MinIO, E2E 6/6) ✅

```text
Evidence: TV4-K03 (FR-FILE-005/RCP-007; D27)
Tuần 3 / TV4 / N2
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs (IObjectStorageReader + MediaContent + ReadAsync); Program.cs (DI AddScoped<MinioStorageService> + AddScoped<IObjectStorageReader>; endpoint GET /api/v1/resources/images/{**key}); tests/CulinaryBlog.Tests/ImageProxyD27Tests.cs; docs/IMAGE_CONTRACT.md §5
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6
Test/lệnh: docker compose -f docker-compose.dev.yml up -d minio; dotnet test CulinaryBlog.Tests --filter ImageProxyD27Tests -c Release + TEST_DATABASE local (MinIO 127.0.0.1:9000) → 6/6 pass; toàn suite 139/139 + 5/5 spike; dotnet format sạch
Kết quả: Published public + Cache-Control max-age=3600; Draft/Archived → anonymous 403 image.forbidden / owner 200 no-store; non-owner member 403; invalid/unknown/soft-deleted key → 404 image.not_found; không đụng IFileStorageService/StoredFile (HANDOFF 5.1); log không lộ secret
Lỗi cố định trong lúc làm: DI ban đầu thiếu đăng ký concrete MinioStorageService → 500 "No service for type" → sửa + 6/6 pass
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: —
```

### TV4-K14/K23 (D2 — Resize ảnh Hangfire PA-1: job 300×300/800×600, idempotent + original fallback) ✅

```text
Evidence: TV4-K14 + TV4-K23 (FR-JOB-002/003, FR-FILE-003; D23 PA-1)
Tuần 3 / TV4 / N4
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/ResizeImageJob.cs; ImageResizeQueue.cs (Hangfire + Inline); MinioStorageService.cs (IObjectStorageWriter: ExistsAsync/UploadAsync với key chủ động); src/backend/CulinaryBlog.Application/RecipeImages.cs (IImageResizeQueue + RecipeImageKeys + hook enqueue + xoá object phái sinh); src/backend/CulinaryBlog.API/Program.cs (AddHangfire/AddHangfireServer/dashboard /hangfire Admin-only); src/backend/CulinaryBlog.API/AdminDashboardAuthorizationFilter.cs; tests/CulinaryBlog.Tests/ImageResizeD2Tests.cs; docs/IMAGE_CONTRACT.md §7
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6
Test/lệnh: (1) dotnet test --filter ImageResizeD2Tests (MinIO 127.0.0.1:9000 + culinary_test) → 4/4; (2) RecipeImageTests → 5 test mới; (3) toàn suite 148/148 + 5/5 spike (2 vòng) + dotnet format sạch; (4) chạy thật: dotnet bin/Release/net10.0/CulinaryBlog.API.dll --no-auto-migrate (PORT=5099, culinary_test) → upload JPEG 1200×800 → Hangfire job Succeeded
Kết quả: ảnh phái sinh {base}_300x300.jpg + {base}_800x600.jpg (proxy trả 300×200 / 800×533 từ ảnh 1200×800); DB cập nhật MediumUrl/ThumbnailUrl sau job (response upload luôn null — FE reload detail); idempotent (ExistsAsync), delete-vs-resize không tái sinh, ảnh hỏng/AVIF → giữ original (không 5xx); xoá ảnh → xoá luôn object phái sinh; dashboard /hangfire: anon 401 / member 403 / Admin 200; Hangfire tự tạo 12 bảng schema `hangfire` trong DB
Lỗi cố định trong lúc làm: (1) SixLabors.ImageSharp 4.x **bắt buộc license key thương mại** (build fail) → khoanh 3.1.11; (2) MinIO SDK `WithCallbackStream` nhận `Action<Stream>` — truyền `async` lambda tạo **async void** fire-and-forget → ảnh đọc bị cắt cụt (~50% test D2 fail) + unobserved exception làm crash test host → sửa copy đồng bộ + kiểm tra `buffer.Length == stat.Size`; (3) `AddHangfireServer`/PostgreSQL API dùng `UseNpgsqlConnection` + lambda 2 tham số; (4) `DashboardContext.GetHttpContext()` thay vì property
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: —
Log: Tuan03/logs/d2_resize_hangfire.log (log API/Hangfire), Tuan03/logs/d2_resize_hangfire_db.txt (query RecipeImages + trạng thái Hangfire job + HTTP code dashboard/proxy)
```

### TV4-K16/K17/K19 (D4 — Uploader UI + status + SEO) 🔶 (SEO + proxy D27 xong; còn UI hoàn thiện sau merge PR #15)

```text
Evidence: TV4-K16/K17/K19 (FR-RCP-008; FR-SEO; D26/D27)
Tuần 3 / TV4 / D4
Đường dẫn: Program.cs GET /recipes/sitemap; Discovery.cs GetSitemapQuery; RecipeRepository.cs GetPublishedForSitemapAsync; frontend src/app/sitemap.ts, robots.ts, app/recipes/[slug]/ (SEO metadata + canonical + JSON-LD)
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6
Test/lệnh: npx next build (exit 0 — sitemap.xml + robots.txt có trong routes); dotnet test → sitemap test chỉ Published (draft/archived/deleted không có)
Kết quả: sitemap XML Published-only; robots.txt đúng; **chưa xong uploader UI/status buttons** (block D27 + TV3 C4)
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: uploader UI progress/rollback/gallery/primary + ảnh hiển thị qua proxy D27 (`NEXT_PUBLIC_MEDIA_URL`) — sau merge PR #15; status buttons ghép TV3 C4
```

### TV4-K20/K22 (D5 — OTEL/metrics/health + EXPLAIN/k6) ✅ (config 24/09; số liệu 27/09)

```text
Evidence: TV4-K20/K22 (FR-OBS-001/003; D20/D21/D22)
Tuần 3 / TV4 / D5
Đường dẫn: Program.cs AddOpenTelemetry (tracing: ASP.NET/HttpClient/EF Core/OTLP; metrics: ASP.NET/HttpClient/Microsoft.EntityFrameworkCore meter); CulinaryBlog.API.csproj + packages.lock.json (OTEL 1.19.x + EF instrumentation); health /health{/live,/ready}
Ghi chú: chạy lệnh EXPLAIN/k6 dưới đây (DB culinary_test local, MinIO+nginx tùy chọn)
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6
Test/lệnh: (1) dotnet run API PORT=5099 ConnectionStrings__Database=...culinary_test; (2) EXPLAIN ANALYZE publish list + count (log: logs/explain_publish_culinary_test.txt); (3) docker run grafana/k6 20 VUs × 30s GET /api/v1/recipes?page=1&pageSize=12 + /api/v1/categories (log: logs/k6_smoke_recipes.log + k6_smoke_summary.json)
Kết quả: EXPLAIN list 0.339ms (12 rows, Seq Scan 50/66 — hợp lý cỡ nhỏ; ảnh primary qua ux_recipe_images_one_primary); count 0.044ms; k6 3310 req, 0% fail, check 100%, avg 81.58ms, p95 225.63ms (<250 ✓), 109.42 req/s
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: chụp trace thật vào Seq (stack nginx+seq bật) — cấu hình sẵn sàng
```

### TV4-K13/K14/K15 (D6 — Lab L4: 4 MIME + resize + Mailhog + Hangfire delayed/restart) ✅

```text
Evidence: TV4-K13 + TV4-K14 + TV4-K15 (FR-FILE-003, FR-JOB-002/003, FR-SEO; D23 PA-1)
Tuần 3 / TV4 / N5
Đường dẫn: practice/TV4/L4/ (Program.cs, MediaPhase.cs, LabImageScaler.cs, EmailPhase.cs, SitemapPhase.cs, JobsPhase.cs, LabJobs.cs, Fixtures.cs, DbEvidencePhase.cs, PurgePhase.cs, Lab.L4.csproj); Tuan03/SOK_LAB_L4.md; practice/TV4/L4/README.md
Nhánh/PR: practice/TV4/L4 (commit 3642428) — console app .NET 10, KHÔNG thêm vào CulinaryBlog.sln, không sửa code sản phẩm
Test/lệnh: dotnet run --project practice/TV4/L4 -c Release -- all  (cần PostgreSQL + object storage + Mailhog + TEST_DATABASE/Minio__*); verify thêm: dotnet build -c Release (0 warning vì TreatWarningsAsErrors) + dotnet format --verify-no-changes + dotnet test CulinaryBlog.sln (148/148) + spike 5/5
Kết quả: **4 phase · 39/39 check PASS**, exit 0 — media 25/25 (MIME theo magic bytes 4 định dạng; file MIME giá → file.invalid_type; ảnh quá giới hạn → file.too_large; upload+đọc lại khớp byte; resize ResizeMode.Max → 300×200/800×533; AVIF fallback giữ original; idempotent; dọn dẹp sạch) · email 3/3 (MailKit plain+HTML qua Mailhog 127.0.0.1:1025, đếm trước/sau qua API, đối chiếu subject) · xml 3/3 (sitemap từ DB thật culinary_test, 93 URL Published, parse XDocument) · jobs 8/8 (Hangfire+PostgreSQL culinary_lab: fire-and-forget Succeeded; tắt worker → job còn Scheduled trong DB → chạy khi restart; retry Retry attempt 1,2 of 5 rồi Succeeded; recurring do scheduler kích hoạt chạy 2 lần; RemoveIfExists → hangfire.hash trống, jobqueue 0 dòng)
Bằng chứng bổ sung: 2 phase chẩn đoán KHÔNG tính vào yêu cầu — `db` (dump bằng Npgsql trong chính process: 20 job đều Succeeded, hash trống) + `purge` (dọn 24 job Enqueued mồ côi còn sót)
Lỗi thật tìm được + sửa: (1) ctor Hangfire.PostgreSql obsolete + TreatWarningsAsErrors → NpgsqlConnectionFactory + JobStorage.Current; (2) queue mismatch — BackgroundJob.Enqueue mặc định queue "default" còn worker nghe "lab" nên job kẹt Enqueued mãi, RecurringJobOptions không có thuộc tính Queue; (3) subject Mailhog nằm ở items[].Content.Headers.Subject; (4) xoá object phải qua IFileStorageService (IObjectStorageWriter chỉ Exists/Upload); (5) bẫy encoding PowerShell 5.1 Get-Content/Set-Content làm hỏng tiếng Việt
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: (1) AVIF mới có bằng chứng ở mức MIME/upload/xoá — fixture ftyp hợp lệ nhưng không decode được nên chưa chạy tay nhánh resize AVIF (đã có test ở ImageResizeD2Tests); (2) worker lab chỉ nghe một queue, chưa kiểm thử nhiều worker cùng lúc; (3) cache-aside L4 chưa có tích hợp thật nên K12 ghi là giới hạn chứ không đóng
Log: Tuan03/logs/lab_l4_run.log (log đầy đủ 1 lần chạy all), Tuan03/logs/lab_l4_db.txt (truy vấn DB bằng Npgsql)
```

### TV4-K23 (Sự cố CI: image storage bị gỡ khỏi registry) ✅

```text
Evidence: TV4-K23 (NFR về pipeline/CI; D25)
Tuần 3 / TV4 / CI incident + hardening
Đường dẫn: .github/workflows/backend.yml; docker-compose.dev.yml; .env.example; README.md; docs/HUONG_DAN_TEST_APP.md; tests/CulinaryBlog.Tests/MinioE2ETests.cs (comment)
Nhánh/PR: 2312739_NHTSon_D3-D4-D5-D6 (commit cd72b27 sửa CI, d78e25c sửa compose + harden)
Test/lệnh: GitHub Actions run 36338124928 (đỏ) → 36343309464 (xanh) → 36344662570 (xanh); local: docker compose -f docker-compose.dev.yml up -d s3 + dotnet test CulinaryBlog.sln với MINIO_ENDPOINT=127.0.0.1:9000
Kết quả: nguyên nhân — service dùng image quay.io/minio/minio:latest; MinIO đã gỡ toàn bộ image public (quay.io/minio/minio → HTTP 401, minio/minio Docker Hub → HTTP 404) nên runner không pull được image, job chết ở bước "Initialize containers" và MỌI bước build/format/test đều bị skip (5 run đỏ liên tiếp từ ef358e0 đến 6bbc542) — KHÔNG phải lỗi code. Sửa: thay bằng rustfs/rustfs (S3-compatible Apache-2.0) + health-cmd /health (RustFS không phục vụ /minio/health/live); MinioStorageService không đổi dòng nào. Cùng nguyên nhân làm docker-compose.dev.yml hỏng trên máy mới → sửa luôn (service minio → s3, bỏ minio-init vì app tự tạo bucket, volume s3data, port 9000/9001 giữ nguyên). Hardening: ghim image tag+digest sha256:8cc9801…, ghim SDK 10.0.401 khớp global.json, timeout-minutes 30, concurrency cancel-in-progress, --blame-hang-timeout 10m
Minh chứng không bỏ trống: local 148/148 + 5/5 với Skipped=0 trên CẢ hai đường (endpoint CI và service s3 thật từ compose), bucket culinary-blog/recipes có thật trong storage → E2E storage thực sự chạy chứ không skip; CI 36344662570 cả 10 bước success
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: SRS v1.1.1 + evidence Tuan01 vẫn ghi "MinIO" (để nguyên vì là spec/record lịch sử; RustFS chỉ là bản thay thế cùng giao thức S3 ở mức dev/CI). Cần nhóm biết khi deploy môi trường thật: production KHÔNG dùng RustFS, phải dùng object storage có license
```

---

## 4. Checklist cá nhân tuần 3 (chốt G5)

- [x] Fix duplicate migration `RefreshTokens` (main `a651c8a`) → local 120/120 + 5/5 pass + CI GitHub branch success (run `818522b`).
- [x] Merge main mới 23/09 (deploy Render, 100 ảnh thật, UI Emerald) + fix connection string lazy đọc trong lambda `AddDbContext` (`4830e57`).
- [x] D3.3 logout revoke refresh family — xác minh C5 refresh đã có trên main, 7 test Week3 + 16 test Auth pass.
- [x] **E2E D1.3 trên MinIO** (`MinioE2ETests`) 3/3 pass nhiều lần; MinIO down → skip an toàn; log không lộ secret.
- [x] **JWT AuthorPolicy bug** (403 API thật) fix bằng `RoleClaimType="role"` + `NameClaimType="sub"` — phát hiện bởi E2E.
- [x] Archive `PATCH /recipes/{id}/archive` + ẩn public ngay + ownership test.
- [x] DELETE soft theo ADR D08 (`Recipe.MarkDeleted()` + global filter) + không mất ảnh restore + test.
- [x] Sitemap Published-only (`GET /recipes/sitemap` + frontend `sitemap.ts`/`robots.ts`) + `next build` OK.
- [x] OTEL trace+metrics (ASP.NET/Http/EF) cấu hình + health db/redis/minio; **27/09 ghi số liệu EXPLAIN publish query (0.339ms/0.044ms) + k6 smoke (3310 req, 0% fail, p95 225.63ms)** — log `Tuan03/logs/`.
- [x] CI thêm service storage + env + bước chờ health — **28/09: CI thật sự xanh** sau khi gỡ sự cố image MinIO bị gỡ khỏi registry (run `36344662570`; xem TV4-K23).
- [x] **Lab L4 (28/09, N5)** — `practice/TV4/L4` commit `3642428`: `media` 25/25 · `email` 3/3 · `xml` 3/3 · `jobs` 8/8 = **4 phase · 39/39 check PASS**; thêm `db`/`purge` chẩn đoán; sổ K `Tuan03/SOK_LAB_L4.md`; log `lab_l4_{run.log,db.txt}` không secret; suite **148/148 + 5/5**, `Skipped=0`.
- [ ] PR `4830e57` lên main (fix CI main 6 commit mới).
- [ ] Rà soát diff PR #14 đã merge (giữ nguyên theo quyết định nhóm).
- [x] Invalidation cache archive/unpublish/delete — **đóng bằng xác minh 27/09**: không cache recipe (backend không OutputCache/Redis-dữ-liệu, FE `no-store`, `RecipeCacheService` orphan); handoff TV2/TV3 nếu nhóm thêm cache.
- [x] **Proxy ảnh D27 PA-2** (27/09) — `GET /api/v1/resources/images/{**key}` + `IObjectStorageReader` + E2E `ImageProxyD27Tests` 6/6; `IMAGE_CONTRACT.md §5` chốt; suite 139/139 + 5/5.
- [x] **Resize D2 (27/09, N4)** — Hangfire PA-1: `ResizeImageJob` 300×300/800×600, `IObjectStorageWriter` key chủ động, retry 3, dashboard `/hangfire` chỉ Admin, idempotent + original fallback + delete-vs-resize; E2E `ImageResizeD2Tests` 4/4; **chạy thật: Hangfire job `Succeeded`, proxy trả 300×200**; suite 148/148 + 5/5. Log `Tuan03/logs/d2_resize_hangfire*.{log,txt}`.
- [ ] ~~Resize original/300×300/800×600 + queue persistent + original fallback + restart/retry test~~ → **xong 27/09**; phần LAB delayed/restart + Mailhog **xong 28/09 (N5)**.
- [ ] N5 mục 2: lab Identity/Google/refresh/forms/FTS theo `PHAN_CHIA` tuần 3.
- [ ] Uploader UI progress/rollback/gallery/primary + ảnh hiển thị qua proxy D27 (`NEXT_PUBLIC_MEDIA_URL`) sau merge PR #15.
- [ ] Status buttons Publish/Unpublish/Archive ghép TV3 C4.
- [ ] Lab `practice/TV4/L4` commit + sổ evidence K cập nhật. → **xong 28/09**: commit `3642428` + `SOK_LAB_L4.md` (mục trên).
- [x] CI pass sau mỗi task; không commit secret/token/password. → **28/09**: CI xanh run `36344662570`; quét staged diff trước mỗi lần commit (0 credential mới).