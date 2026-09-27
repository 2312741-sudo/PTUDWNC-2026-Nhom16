# SỔ EVIDENCE TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Trạng thái**: Tất cả bắt đầu ở **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.
> **Cập nhật 24/09**: D3 archive/delete, D4 SEO, D5 OTEL, E2E D1.3 MinIO đã hoàn thành — 133/133 + 5/5 pass local, frontend build OK; chờ reviewer xác nhận.
> **Cập nhật 27/09**: D3 invalidation đóng bằng xác minh; **D27 proxy ảnh PA-2 xong** — E2E `ImageProxyD27Tests` 6/6; **D5 EXPLAIN/k6 ghi số liệu thật** (list 0.339ms; k6 3310 req, 0% fail, p95 225.63ms); toàn suite **139/139 + 5/5**; `IMAGE_CONTRACT.md §5` chốt PA-2. Chờ reviewer xác nhận.

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
| K12 | SP+LAB | Redis cache-aside, OutputCache, invalidation | Invalidation archive/unpublish/delete + LAB | D3 + D6 | 🟡 Đã xác minh 27/09 — không cache recipe (thỏa mặc định); LAB cache L4 còn mở |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID path | E2E D1.3 trên MinIO + 4 MIME + SP upload/delete | D3 + D6 | Đã làm (E2E 3/3) |
| K14 | SP+LAB | Hangfire fire-and-forget/delayed/recurring/retry | SP resize job + LAB delayed/restart | D2 + D6 | Chưa làm (chờ D23) |
| K15 | LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB L4 Mailhog + resize + XML | D6 | Chưa làm |
| K16 | SP | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | Uploader UI hoàn thiện (D4) | D4 | Chưa làm (chờ D27) |
| K17 | SP | TanStack Query, optimistic rollback, next/image | Uploader progress/gallery/primary optimistic | D4 | Chưa làm (chờ D27) |
| K18 | SP | Responsive, WCAG2.1 AA, keyboard/loading/error | Upload/status checklist | D4 | Chưa làm |
| K19 | SP | SEO metadata/OG/canonical/robots/JSON-LD | Sitemap/robots/OG/JSON-LD Published-only | D4 | Đã làm (SEO) |
| K20 | SP | Serilog/Seq/correlation, OTEL, metrics, health | OTEL trace HTTP→DB + health thành phần | D5 | ✅ Đã làm (cấu hình + xác minh; trace thật qua Seq khi stack bật) |
| K22 | SP | k6/EXPLAIN/cache hit/CWV | EXPLAIN publish query + k6 + cache hit | D5 | ✅ Đã làm 27/09 (EXPLAIN + k6 smoke ghi số liệu) |
| K23 | SP | Docker/Compose/Nginx/volumes/backup-restore | CI xanh (N0) + stack vận hành + queue service | N0, D2 | Đã làm (N0) |
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
- [x] CI thêm service MinIO + env + bước chờ health (cần push + xanh).
- [ ] PR `4830e57` lên main (fix CI main 6 commit mới).
- [ ] Rà soát diff PR #14 đã merge (giữ nguyên theo quyết định nhóm).
- [x] Invalidation cache archive/unpublish/delete — **đóng bằng xác minh 27/09**: không cache recipe (backend không OutputCache/Redis-dữ-liệu, FE `no-store`, `RecipeCacheService` orphan); handoff TV2/TV3 nếu nhóm thêm cache.
- [x] **Proxy ảnh D27 PA-2** (27/09) — `GET /api/v1/resources/images/{**key}` + `IObjectStorageReader` + E2E `ImageProxyD27Tests` 6/6; `IMAGE_CONTRACT.md §5` chốt; suite 139/139 + 5/5.
- [ ] Resize original/300×300/800×600 + queue persistent (tuỳ D23 — ✅ chốt Hangfire PA-1) + original fallback + restart/retry test.
- [ ] Uploader UI progress/rollback/gallery/primary + ảnh hiển thị qua proxy D27 (`NEXT_PUBLIC_MEDIA_URL`) sau merge PR #15.
- [ ] Status buttons Publish/Unpublish/Archive ghép TV3 C4.
- [ ] Lab `practice/TV4/L4` commit + sổ evidence K cập nhật.
- [ ] CI pass sau mỗi task; không commit secret/token/password.