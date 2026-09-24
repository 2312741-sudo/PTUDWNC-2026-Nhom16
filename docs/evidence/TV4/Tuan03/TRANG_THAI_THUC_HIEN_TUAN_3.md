# TRẠNG THÁI THỰC HIỆN TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git đề xuất**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3), khởi động từ main đã cập nhật
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 24/09/2026 (T4 — hoàn tất D3/D4/D5 + E2E D1.3 trên MinIO; 133/133 + 5/5 pass)

> Chi tiết kế hoạch xem `KE_HOACH_TUAN_3_TV4.md`; nền tảng trạng thái tuần 2 xem `docs/evidence/TV4/Tuan02/`.

> **Bản sửa đổi 23/09 T2 — merge `origin/main` `a651c8a` vào nhánh tuần 3 + test lại**:
> main đã được sửa lỗi migration trùng lặp (`a651c8a` "loai bo migration trung lap"): xoá `AddRecipeDiscoveryAndSearch` (20260916102353)
> và `AddRefreshTokens` (20260923104044) — bảng `RefreshTokens` giờ chỉ do `20260919061954_AddRecipeAggregate` tạo, kèm CORS + auto migrate/seed khi deploy.
> → Merge vào `2312739_NHTSon_D3-D4-D5-D6` (commit `e026dc9` + `75a8bf5`): **N0 trong kế hoạch tuần 3 đã được gỡ**. Kiểm chứng local:
> build Release **0 warning/0 error**, `dotnet format` sạch, CulinaryBlog.Tests **120/120 pass** (gồm 16 test Auth/Week3 — trước đây fail), spike **5/5 pass** (chạy với TEST_DATABASE local). Cần push + CI xanh xác nhận.

---

## 0. Điểm mốc bước vào tuần 3 (ghi nhận 23/09)

| # | Sự kiện | Trạng thái |
|---|---|---|
| 1 | **Block 2.1 gỡ hoàn toàn** (merge C2/C3 TV3 `2086ee8`) — recipe CRUD + ingredient/step có trong API | ✅ Đã xong tuần 2 |
| 2 | **D3.1/D3.2 publish/unpublish CQRS** + 2 endpoint + 13 test (`0659d45`, amend `183c055`) | ✅ Đã xong tuần 2 |
| 3 | **PR #14 bị merge nhầm vào main** (`3eb6de3` + `c512943`) — D1.3 + D3.1/D3.2 + docs đã nằm trên main | ⚠️ Giữ nguyên theo quyết định nhóm; đưa việc rà soát/khắc phục vào tuần 3 |
| 4 | **CI main đỏ (pre-existing từ tuần 2)** — 16 test Auth/Week3 fail do duplicate migration `RefreshTokens` | ✅ **Đã gỡ 23/09 T2** — main `a651c8a` xoá migration trùng; local 120/120 + 5/5 pass; chờ CI xanh |
| 5 | Local branch TV4 hiện tại = `75a8bf5` (đã merge main fix migration) | ✅ 120/120 + 5/5 pass local |
| 6 | D2 resize / D1.1c MinIO-down / E2E D1.3 thật / D3.3 logout revoke (chờ TV3 C5) | 📋 Kế thừa vào tuần 3 |

---

## 1. Đã hoàn thành (đầu tuần 3)

> Tất cả task bắt đầu ở trạng thái **Chưa làm** ở tuần 3 này; các mục sau đã hoàn thành trong tuần 2 và là nền tảng. **(+) = mới cập nhật 23/09 T2.**

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| D1.1 | `MinioStorageService` implement `IFileStorageService` bằng MinIO SDK 7.0.0 + DI + lockfile (D25) | `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs`; `Program.cs` (DI) | Đã làm tuần 2 — còn D1.1c MinIO-down/log redacted |
| D1.2 | Validator upload: 4 MIME + ≤5MiB + magic bytes | `src/backend/CulinaryBlog.Application/ImageUpload.cs` | Đã làm tuần 2 — 16 test pass |
| D1.5 | Domain RecipeImage: primary invariant + race spike | `tests/CulinaryBlog.Tests/RecipeImageDomainTests.cs`, `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs` | Đã làm tuần 2 |
| D1.6 | `docs/IMAGE_CONTRACT.md` bàn giao TV3 | `docs/IMAGE_CONTRACT.md` | Đã làm tuần 2 |
| D1.3 | API upload/PATCH primary/DELETE image (FR-RCP-008, D17) | `src/backend/CulinaryBlog.API/Program.cs` + `src/backend/CulinaryBlog.Application/RecipeImages.cs` | Đã làm tuần 2 — **trong main** (qua PR #14); còn E2E thật trên MinIO |
| D3.1/D3.2 | `PATCH /recipes/{id}/publish` & `/unpublish` (FR-RCP-005/006, C02, 422 `RECIPE_PUBLISH_INCOMPLETE`, ownership 403, idempotent) | `src/backend/CulinaryBlog.Application/Recipes.cs` (region D3.1/D3.2) + `Program.cs` + `tests/CulinaryBlog.Tests/RecipeLifecycleTests.cs` | Đã làm tuần 2 — **trong main** (qua PR #14); 13 test pass |
| 120 tests | Build Release 0 warning + 120 CulinaryBlog.Tests | `tests/CulinaryBlog.Tests` | (+)**Đã xác nhận lại 120/120 + 5/5 spike pass local** (Postgres local, sau merge main fix migration) |
| N0-1 (+) | **Fix duplicate migration `RefreshTokens`** | main `a651c8a` (xoá `AddRecipeDiscoveryAndSearch` + `AddRefreshTokens`); merge vào branch tuần 3 (`e026dc9` + `75a8bf5`) | ✅ **Xong** — build/format/test local pass; cần push + CI xanh |
| N0-3 (+) | Chạy lại toàn bộ test sau merge | CulinaryBlog.Tests 120/120 + spike 5/5 (TEST_DATABASE local) | ✅ Xong local |

---

## 2. Đang làm / Chưa thực hiện (tuần 3)

> N0 (fix migration + CI) đã được gỡ bởi main `a651c8a` (merge vào branch tuần 3). **Còn lại là xác nhận CI xanh trên GitHub + rà soát PR #14.**

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| N0-2 | Rà soát diff PR #14 so với main + chờ CI xanh | PR đã merge nhầm, CI local đã xanh | Push branch tuần 3 → CI GitHub xanh → review nhóm |
| ~~D3.1c~~ | ~~Archive `PATCH /recipes/{id}/archive` (FR-RCP-006)~~ | ✅ **Xong 24/09** — `ArchiveRecipeCommand` + endpoint + test (RecipeLifecycleTests) | — |
| ~~D3.2c~~ | ~~DELETE recipe soft theo D08 (FR-RCP-007)~~ | ✅ **Xong 24/09** — `DeleteRecipeCommand` + `Recipe.MarkDeleted()` + global filter + test | — |
| D3.3 | Logout revoke refresh family (FR-AUTH-005) | ✅ **Xong** — C5 refresh đã có trên main | — |
| ~~D3.4~~ | ~~E2E D1.3 trên MinIO + D1.1c MinIO down/log redacted~~ | ✅ **Xong 24/09** — `MinioE2ETests` 3/3 pass trên MinIO local; CI đã thêm service MinIO | — |
| D2 | Resize original/300×300/800×600 + job nền (FR-JOB-002/003) | Chưa bắt đầu | Chốt D23 (Hangfire/BackgroundService) + ImageSharp |
| ~~D4~~ | ~~Uploader UI/progress/primary + sitemap/robots/OG/JSON-LD~~ | ✅ **Xong 24/09 (SEO)** — `/sitemap` endpoint Published-only + `sitemap.ts`/`robots.ts`/detail page SEO; **uploader/status UI chờ D27 + TV3 C4** | Chốt D27 (presigned/proxy) + ghép TV3 C4 |
| ~~D5~~ | ~~OTEL/metrics/health/EXPLAIN/k6~~ | ✅ **Xong 24/09** — OTEL trace+metrics HTTP→DB, health db/redis/minio, README hướng dẫn; còn EXPLAIN/k6 ghi sổ khi có k6 script | — |
| D6 | Lab `practice/TV4/L4` (4 MIME + resize + Mailhog + Hangfire) + sổ K | Chưa bắt đầu | G1/G2 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3.3 logout revoke | ✅ Xong — C5 refresh đã có trên main | — | Đã gỡ |
| D1/D4 ảnh display | Uploader UI hiển thị ảnh upload qua API cần presigned/proxy | Quyết định D27 + CR | Đầu tuần 3 |
| D2 resize | Queue chưa chốt | Quyết định D23 | Đầu tuần 3 |
| ~~N0-1 CI xanh~~ | ~~Migration trùng `RefreshTokens`~~ | ✅ **Đã gỡ** — main `a651c8a` đã fix; local 120/120 + 5/5 | Đã gỡ 23/09 T2; chờ CI GitHub xác nhận |
| PR #14 giữ/xoá | PR đã merge nhầm — cần thống nhất nhóm | Nhóm trưởng + nhóm | Đầu tuần 3 |

---

## 4. Cần bàn luận / cần CR (SRS v1.1.1)

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 (từ tuần 1) | Bucket policy MinIO private vs public-read | Ảnh public theo SRS 2.4.1 nhưng Draft/Archived không lộ | Giữ private + presigned/proxy; xác nhận nhóm + giảng viên |
| CR-2 | Resize job Hangfire vs BackgroundService | SRS mô tả Hangfire persistent; phụ thuộc mới có thể phá lockfile | Chốt D23 với nhóm |
| CR-3 | Soft delete recipe → ảnh xử lý thế nào | Giữ hay xoá object khi recipe bị soft delete | Ghi vào ADR TV4-001/D08 |
| CR-4 (mới) | PR #14 merge nhầm vào main | Giữ nguyên và rà soát, hay revert? | Giữ nguyên theo quyết định nhóm 23/09; ghi ADR |

---

## 5. Ghi chú kỹ thuật cho review

1. **Fix migration phải giữ index** `IDX_RefreshToken_Hash` (unique) và `IX_RefreshTokens_UserId` — tránh tạo lại migration gây mất index hiện có.
2. **CI main đỏ là pre-existing** (main `29b171c` đã fail trước khi nhánh TV4 merge) — không phải do D3 gây ra; nhưng là gánh nặng chung cần xử lý đầu tuần.
3. **D3.1/D3.2 đã theo đúng ADR D07**: publish cần ≥1 ingredient VÀ ≥1 step; không bắt buộc ảnh.
4. **Mọi build** phải: Release 0 warning → `dotnet format` sạch → `dotnet test` đủ → mới push (CI check 4 bước).
5. **Không commit secret**: MinIO accessKey/secretKey, JWT keys chỉ trong `.env`/`MinioOptions`, CI dùng env test.

---

## 6. Kết quả kiểm chứng tuần 3 (24/09 T4)

| Hạng mục | Kết quả | Lệnh/Môi trường |
|---|---|---|
| CulinaryBlog.Tests | **133/133 pass** (130 + 3 E2E MinIO) | `dotnet test tests/CulinaryBlog.Tests -c Release` + `TEST_DATABASE` local |
| Concurrency spike | **5/5 pass** | `dotnet test tests/concurrency-spike -c Release` + `TEST_DATABASE` local |
| Build API | 0 warning / 0 error | `dotnet build CulinaryBlog.sln -c Release` |
| `dotnet format` | sạch (verify-no-changes exit 0) | `dotnet format CulinaryBlog.sln --verify-no-changes` |
| Frontend build | **OK** (`next build` exit 0; 13 routes + robots.txt + sitemap.xml) | `npx next build` trong `src/frontend` |
| E2E MinIO | upload/PATCH primary/DELETE (+ unpublish/archive) 3/3 pass lặp nhiều lần | MinIO local `127.0.0.1:9000`, bucket `culinary-blog` |
| Fix thật phát hiện bởi E2E | **JWT policy 403 → chỉ cần `RoleClaimType="role"`** (`MapInboundClaims=false`) | `Program.cs` `TokenValidationParameters` — trước đây AuthorPolicy luôn 403 khi gọi API thật |

### Chi tiết E2E MinIO (`tests/CulinaryBlog.Tests/MinioE2ETests.cs`)

- **Flow**: đăng ký → tạo recipe (201) → upload ảnh JPEG (201) → đọc lại (chiều xiêm + slug) → PATCH primary → publish → unpublish → archive → delete → chưa còn trong public list.
- **Điều kiện chạy**: MinIO reachable (TCP + health) nếu không → skip (CI cũng skip khi không có MinIO); postgres local `culinary_test`.
- **Bài học E2E thật**: (1) token từ register (không phải login) mới mang role; (2) enum dạng int (không có `JsonStringEnumConverter`); (3) multipart phải set `ContentType` để `IFormFile.ContentType` đúng → validator magic bytes pass.
- **CI**: `.github/workflows/backend.yml` đã thêm service MinIO + env `MINIO_*` + bước chờ `minio/health/live`.

### Đã fix khi làm D4/D5 (đã trong working tree)

| Nội dung | Nơi |
|---|---|
| `GetSitemapQuery`/`GetPublishedForSitemapAsync` — chỉ Published, không Draft/Archived/Deleted | `Recipes.cs`, `Discovery.cs`, `RecipeRepository.cs` |
| Endpoint `GET /recipes/sitemap` Published-only | `Program.cs` |
| `ArchiveRecipeCommand`/`DeleteRecipeCommand` + `Recipe.MarkDeleted()` (soft D08) | `Recipes.cs`, `Recipe.cs` |
| OTEL trace + metrics (ASP.NET/HttpClient/EF) | `Program.cs` + `CulinaryBlog.API.csproj` + `packages.lock.json` |
| robots.txt + sitemap.xml + SEO metadata trang công thức | `src/frontend/src/app/robots.ts`, `sitemap.ts`, `app/recipes/[slug]/` |