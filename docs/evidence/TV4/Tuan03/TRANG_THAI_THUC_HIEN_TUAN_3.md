# TRẠNG THÁI THỰC HIỆN TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git đề xuất**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3), khởi động từ main đã cập nhật
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 27/09/2026 (T7 — chốt đề xuất D23/D27: **D23 → Hangfire PA-1**, **D27 → base media URL proxy PA-2** theo `docs/DE_XUAT_GIAI_QUYET_D23_D27.md`; khớp PR #15 TV3) → **đã implement xong cả hai: D27 proxy (N2) + D2 resize Hangfire (N4)**

> **Bản sửa đổi 27/09 (mốc D23/D27)**:
> Nhóm chốt hướng giải quyết 2 task bị block lâu nhất của tuần 3 (ghi trong `docs/DE_XUAT_GIAI_QUYET_D23_D27.md`):
> - **D23 → PA-1 Hangfire**: queue persistent PostgreSQL + dashboard Admin + retry 3 + regenerate `packages.lock.json` → mở D2 resize.
> - **D27 → PA-2 base media URL qua proxy có auth** (`GET /resources/images/{key}`): Published công khai, Draft/Archived chỉ owner/Admin; FE TV3 (PR #15) đã ghép `NEXT_PUBLIC_MEDIA_URL` → chỉ cần cấu hình env, không phá UI đã merged.
> D1/D4 display + D2 resize chuyển từ **block** sang **có phương án được duyệt, bắt đầu thực hiện**.
> **Kết quả 27/09 (tối)**: D27 proxy xong (N2) và **D2 resize xong (N4)** — job Hangfire chạy thật (`Succeeded`), ảnh 300×300/800×600, idempotent + original fallback, dashboard `/hangfire` chỉ Admin. Chi tiết ở mục "Implement D2" bên dưới.

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
| ~~D3-invalid~~ | ~~Invalidation archive/unpublish/delete — clear cache (Redis/OutputCache/ISR)~~ | ✅ **Đóng 27/09 bằng xác minh (N1 item 3)**: không tồn tại cache recipe nào — backend không OutputCache/Redis-dữ-liệu (chỉ `RedisHealthCheck`); FE recipe/sitemap luôn `no-store`; `RecipeCacheService` (`Infrastructure`) orphan/chưa wire → "Archived/Draft không phục vụ bởi cache cũ" thỏa mặc định. Ghi handoff TV2/TV3 nếu nhóm sau này thêm ISR/output-cache cho recipe | — |
| ~~D1.7/D27~~ | ~~Proxy ảnh `GET /resources/images/{key}` (Published public; Draft/Archived owner/Admin; stream MinIO)~~ | ✅ **Xong 27/09** — endpoint `GET /api/v1/resources/images/{**key}` + `IObjectStorageReader` (tách khỏi `IFileStorageService`); Published public cache, Draft/Archived owner/Admin else `403 image.forbidden`; `404` key/recipe/soft-deleted; `IMAGE_CONTRACT.md §5` đã chốt PA-2 (chi tiết `DE_XUAT_GIAI_QUYET_D23_D27.md` Phần B) | **Test E2E `ImageProxyD27Tests` 6/6 pass** |
| ~~D2~~ | ~~Resize original/300×300/800×600 + job nền (FR-JOB-002/003)~~ | ✅ **Xong 27/09 (N4)** — `ResizeImageJob` + `HangfireImageResizeQueue` (queue PostgreSQL, retry 3, dashboard `/hangfire` chỉ Admin) + ImageSharp 3.1.11; `IObjectStorageWriter` (key phái sinh chủ động, tách khỏi `IFileStorageService`); `IImageResizeQueue` (Testing chạy inline); xoá ảnh → xoá luôn object phái sinh; `IMAGE_CONTRACT.md §7` | **Test E2E `ImageResizeD2Tests` 4/4 + unit `RecipeImageTests` (5 test mới); full 148/148 + spike 5/5; chạy thật Hangfire job `Succeeded`** |
| D4-UI | Uploader/editor ảnh + status buttons | **FE editor đã có trong PR #15** (`ImagesStep.tsx`); **D27 proxy đã xong** (endpoint sẵn sàng); còn: TV4 review + bổ sung progress/rollback, status ghép TV3 C4, FE set `NEXT_PUBLIC_MEDIA_URL` | Sau khi PR #15 merge → set `NEXT_PUBLIC_MEDIA_URL`; status buttons nối API đã có |
| ~~D4-SEO~~ | ~~Sitemap/robots/OG/JSON-LD~~ | ✅ **Xong 24/09** — `/sitemap` endpoint Published-only + `sitemap.ts`/`robots.ts`/detail page SEO | — |
| ~~D5~~ | ~~OTEL/metrics/health/EXPLAIN/k6~~ | ✅ **Xong 24/09 + 27/09** — OTEL trace+metrics HTTP→DB, health db/redis/minio, README hướng dẫn; **27/09: EXPLAIN publish query + k6 smoke 20 VU/30s** ghi số liệu (`logs/explain_publish_culinary_test.txt`, `logs/k6_smoke_recipes.log` + `k6_smoke_summary.json`) | — |
| D6 | Lab `practice/TV4/L4` (4 MIME + resize + Mailhog + Hangfire) + sổ K | Chưa bắt đầu | G1/G2 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3.3 logout revoke | ✅ Xong — C5 refresh đã có trên main | — | Đã gỡ |
| D1/D4 ảnh display | Uploader UI hiển thị ảnh upload qua API cần base media URL/proxy | ~~D27~~ → ✅ **Chốt + implement 27/09 — PA-2 proxy** (`DE_XUAT_GIAI_QUYET_D23_D27.md`; `IMAGE_CONTRACT.md §5`) | **Đã gỡ 27/09; proxy xong** — còn UI review (PR #15) |
| ~~D2 resize~~ | ~~Queue chưa chốt~~ | ~~D23~~ → ✅ **Chốt 27/09 — PA-1 Hangfire**; **đã implement xong (N4)** | **Đã gỡ + xong 27/09** |
| ~~N0-1 CI xanh~~ | ~~Migration trùng `RefreshTokens`~~ | ✅ **Đã gỡ** — main `a651c8a` đã fix; local 120/120 + 5/5 | Đã gỡ 23/09 T2; chờ CI GitHub xác nhận |
| PR #14 giữ/xoá | PR đã merge nhầm — cần thống nhất nhóm | Nhóm trưởng + nhóm | Đầu tuần 3 |

---

## 4. Cần bàn luận / cần CR (SRS v1.1.1)

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 (từ tuần 1) | Bucket policy MinIO private vs public-read | Ảnh public theo SRS 2.4.1 nhưng Draft/Archived không lộ | ✅ **Chốt 27/09: giữ private + proxy D27 (PA-2)** — `DE_XUAT_GIAI_QUYET_D23_D27.md`; vẫn gửi giảng viên xác nhận quy trình |
| CR-2 | Resize job Hangfire vs BackgroundService | SRS mô tả Hangfire persistent; phụ thuộc mới có thể phá lockfile | ✅ **Chốt 27/09: Hangfire (PA-1)** — `DE_XUAT_GIAI_QUYET_D23_D27.md`; regenerate `packages.lock.json` |
| CR-3 | Soft delete recipe → ảnh xử lý thế nào | Giữ hay xoá object khi recipe bị soft delete | Ghi vào ADR TV4-001/D08 |
| CR-4 (mới) | PR #14 merge nhầm vào main | Giữ nguyên và rà soát, hay revert? | ✅ Giữ nguyên theo quyết định nhóm 23/09; ghi ADR |
| CR-5 (mới) | Review PR #15 (TV3 C4) | Wizard ảnh đã có editor; `ValueGeneratedNever` child có thể liên quan fix `e3e8315` | Review diff; xác nhận không trùng/quyện fix concurrency TV4; MinIO mirror `coollabsio` |

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

### Xác minh N1 item 3 — Invalidation cache (27/09)

- **Kết quả**: không có cache recipe nào để invalidate → tiêu chí "Draft/Archived không được phục vụ bởi cache cũ" **thỏa mặc định**.
- Bằng chứng: `Program.cs` không có `AddOutputCache`/`IDistributedCache`; chỉ `RedisHealthCheck` + docker redis (không cache dữ liệu); `RecipeCacheService` (in-memory) **orphan** — chưa đăng ký DI, không nơi nào gọi; FE `src/frontend/src/lib/api.ts` recipe list/search/detail/sitemap đều `cache: 'no-store'` (category ISR 3600/600s không chứa nội dung recipe).
- Hành động: ghi `HANDOFF_TV4_TUAN3.md` — nếu TV2/TV3 thêm ISR/output-cache cho recipe thì cần revalidate hook; bay giờ không wire cache để tránh rủi ro vô ích.

### Implement D27 — Proxy ảnh PA-2 (27/09)

| Hạng mục | Kết quả |
|---|---|
| Endpoint | `GET /api/v1/resources/images/{**key}` (`Program.cs`) — parse `recipes/{recipeId}/{uuid}.ext`; Published public (`Cache-Control: public, max-age=3600`); Draft/Archived → owner/Admin else `403 image.forbidden` (`no-store`); invalid/unknown/soft-deleted → `404 image.not_found` |
| Storage | `MinioStorageService` thêm `IObjectStorageReader.ReadAsync` (stat → get → stream buffer; missing → null); **không đụng `IFileStorageService`/`StoredFile`** (HANDOFF 5.1) |
| Test | `tests/CulinaryBlog.Tests/ImageProxyD27Tests.cs` — **6/6 pass** E2E MinIO (Published public+cache; Draft 403→owner 200; non-owner 403; Archived 403→owner 200; Unpublished 403; invalid/unknown/deleted 404) |
| Contract | `docs/IMAGE_CONTRACT.md §5`: chốt PA-2 + rule quyền + cách stream |
| Toàn suite | **139/139 + 5/5 spike pass** |

### Implement D2 — Resize ảnh PA-1 Hangfire (27/09)

| Hạng mục | Kết quả |
|---|---|
| Package | API: `Hangfire.AspNetCore 1.8.25` + `Hangfire.PostgreSql 1.21.1`; Infrastructure: `Hangfire.Core 1.8.25` + `SixLabors.ImageSharp **3.1.11**` (bản 4.x **bắt buộc license key thương mại** → phải khoanh 3.1.x). `packages.lock.json` regenerate (không `--locked-mode`) |
| Job | `ResizeImageJob.ExecuteAsync(recipeId, imageId, originalKey)` — `[AutomaticRetry(Attempts = 3)]`; đọc original (`IObjectStorageReader`) → resize `ResizeMode.Max` 300×300 / 800×600 → ghi `IObjectStorageWriter` (key chủ động `{base}_300x300.{ext}`, `{base}_800x600.{ext}`) → cập nhật `RecipeImage.MediumUrl/ThumbnailUrl` |
| Bất biến | **Idempotent** (`ExistsAsync` trước khi upload) · **delete-vs-resize** (row biến mất → không tái sinh) · **original fallback** (ảnh hỏng / AVIF không decode → URL giữ `null`, KHÔNG lỗi 5xx cho request upload) · xoá ảnh → xoá luôn 2 object phái sinh |
| Queue | `IImageResizeQueue` (Application, không phụ thuộc Hangfire) → `HangfireImageResizeQueue` (`BackgroundJob.Enqueue`); `Testing` dùng `InlineImageResizeQueue` (job chạy ngay sau upload → assert deterministic, không worker nền) |
| Wiring | `Program.cs`: `AddHangfire(UsePostgreSqlStorage(UseNpgsqlConnectionString(conn)))` + `AddHangfireServer(WorkerCount = 4)`; dashboard `/hangfire` **chỉ Admin** (`AdminDashboardAuthorizationFilter`); không đăng ký Hangfire ở `Testing` |
| Test | `tests/CulinaryBlog.Tests/ImageResizeD2Tests.cs` — **4/4 pass**: resize đúng kích thước qua proxy (1200×800 → 300×200 / 800×533), idempotent khi chạy 2 lần, ảnh hỏng → fallback original (vẫn 201, original phục vụ đúng bytes), ảnh đã xoá → không tái sinh object (proxy 404). `RecipeImageTests` +5 unit test (enqueue hook, `ResizedKeys`, xoá object phái sinh) |
| Chạy thật (evidence) | API thật + MinIO + Postgres: upload JPEG 1200×800 → **Hangfire job id=1 `Succeeded`** (~1s) → DB có `MediumUrl`/`ThumbnailUrl`; proxy trả `300x200` (`200 image/jpeg`); `/hangfire`: anon **401**, member **403**, Admin **200**. Log: `Tuan03/logs/d2_resize_hangfire.log` + `d2_resize_hangfire_db.txt` |
| Bug thật phát hiện khi test | `MinioStorageService.ReadAsync` truyền `async stream => …` cho `WithCallbackStream` (delegate `Action<Stream>`) → biến thành **async void fire-and-forget**: `GetObjectAsync` có thể trả về khi copy chưa xong → **ảnh cắt cụt** (proxy D27 trả ảnh hỏng) và lỗi nền không ai quan sát (**crash test host**: `ArgumentOutOfRangeException` trong `HttpConnection.CopyFromBufferAsync`). Đã sửa: copy **đồng bộ** + kiểm tra `buffer.Length == stat.Size` (thiếu → `IOException`). Test D2 flaky ~50% trước khi sửa, 6/6 xanh sau khi sửa |
| Contract | `docs/IMAGE_CONTRACT.md §7` (chuyển §6 → §8): key phái sinh, bảng bất biến, hành vi response upload (`mediumUrl`/`thumbnailUrl` = `null` lúc upload — FE reload detail) |
| Toàn suite | **148/148 + 5/5 spike pass** (2 vòng liên tiếp), `dotnet format --verify-no-changes` sạch |

### Số liệu D5 — EXPLAIN + k6 (27/09, DB `culinary_test` local: 66 recipe / 50 published)

| Hạng mục | Số liệu thật |
|---|---|
| EXPLAIN publish list (page 1, size 12) | `Execution Time: 0.339 ms`; 12 rows; **Seq Scan** `Recipes` (filter `Status=0 AND NOT IsDeleted`, 50/66 rows) — tối ưu ở quy mô 66 rows; Author dùng `PK_AspNetUsers`, ảnh primary dùng `ux_recipe_images_one_primary`; `IDX_Recipe_Status`/`IDX_Recipe_IsDeleted`/`IDX_Recipe_PublishedAt` đã có cho data lớn hơn |
| EXPLAIN count published | `Execution Time: 0.044 ms` (Aggregate + Seq Scan, 50 rows) |
| k6 smoke `GET /api/v1/recipes?page=1&pageSize=12` + `/api/v1/categories` (20 VU × 30s) | **3310 req, 0% fail, check 100%**; `http_req_duration` avg **81.58ms**, p90 **193.45ms**, **p95 225.63ms** (<250 threshold ✓); **109.42 req/s** |
| OTEL | Config có từ commit `2bbee0d`, còn nguyên `Program.cs` (trace ASP.NET/Http/EF + metrics + OTLP, packages.lock 1.19.x) — xác minh 27/09 |

→ Log thô không secret tại `Tuan03/logs/`.

### N5 — Lab L4 (D6) chạy thật (27/09 tối)

| Hạng mục | Kết quả |
|---|---|
| Hình thức | Console app độc lập `practice/TV4/L4` (không thêm tính năng vào sản phẩm, không thêm vào `CulinaryBlog.sln`); tham chiếu `CulinaryBlog.Application` + `CulinaryBlog.Infrastructure` để dùng đúng đường code thật (`MinioStorageService`, `RecipeImageKeys`, `AuthDbContext`) |
| Chạy | `dotnet run --project practice/TV4/L4 -c Release -- all` → **4 phase, 39/39 check PASS**, mã thoát `0`. Log: `Tuan03/logs/lab_l4_run.log` |
| `media` (25/25) | MIME theo **nội dung** file: JPEG/PNG/WebP/AVIF đều đúng; file MIME giả → `file.invalid_type`; ảnh vượt giới hạn → `file.too_large`; upload+đọc lại khớp byte; resize `300×200` / `800×533` (`ResizeMode.Max`) cho JPEG/PNG/WebP; AVIF fallback giữ original; resize chạy 2 lần không ghi đè; xoá original + 2 biến thể sạch |
| `email` (3/3) | MailKit gửi 2 mail (plain + HTML) qua SMTP Mailhog không exception; Mailhog đếm trước/sau 6→8; API trả đúng subject `[LAB L4] Plain text 20260927-182205` |
| `xml` (3/3) | `sitemap.xml` sinh từ DB thật `culinary_test` (chỉ đọc): **92 URL**; parse lại hợp lệ; `Published 92 / tổng 288` (`Draft 181`, `Archived 21` bị loại) |
| `jobs` (8/8) | Fire-and-forget job `35` `Succeeded`; tắt worker → job `36` còn `Scheduled` trong DB; restart worker → job delayed `Succeeded` (~23s); retry job `37` `Processing` 3 lần / `Failed` 2 lần / `Scheduled` với `Retry attempt 1,2 of 5` → `Succeeded`; recurring job `38`,`39` với reason `Triggered by recurring job scheduler`, sitemap chạy 2 lần cron `*/5 * * * * *`; dọn: `RemoveIfExists` → `hangfire.hash` trống, `jobqueue` 0 dòng, object lab đã xoá |
| Đối chiếu DB độc lập | `Tuan03/logs/lab_l4_db.txt` (dump bằng Npgsql trong chính process lab): 15 job đều `statename = Succeeded`, `so_job_Failed_cuoi = 0`, `hangfire.jobqueue_con_lai = 0`, `hangfire.hash` trống |
| Bổ sung | 2 phase chẩn đoán **không tính** vào 4 phase yêu cầu: `db` (dump bằng chứng) và `purge` (xoá job `Enqueued`/`Scheduled` còn sót trong DB lab) |
| Lỗi lab đã gặp và sửa | ① API obsolete của `Hangfire.PostgreSql 1.21` (ctor `connectionString`) + `TreatWarningsAsErrors` → dùng `NpgsqlConnectionFactory` + `JobStorage.Current`; ② **queue mismatch**: `BackgroundJob.Enqueue` mặc định queue `default` còn worker nghe `lab` → job kẹt `Enqueued` mãi, phải truyền queue tường minh (`RecurringJobOptions` không có thuộc tính `Queue`); ③ subject Mailhog nằm ở `items[].Content.Headers.Subject`; ④ xoá object phải qua `IFileStorageService` (writer chỉ `Exists`/`Upload`) |
| Tài liệu | `practice/TV4/L4/README.md` (cách chạy, biến môi trường, giới hạn) + `Tuan03/SOK_LAB_L4.md` (bằng chứng + **giới hạn cần nói rõ**) |
| Giới hạn đã nêu rõ | ① AVIF mới chỉ kiểm thử ở mức MIME/upload/xoá — fixture AVIF hợp lệ về `ftyp` nhưng không decode được nên **không** có bằng chứng chạy tay cho nhánh resize AVIF (nhánh này do test của N4 phụ trách); ② Mailhog là hạ tầng lab, `jobs` bắt buộc có nó; ③ cron 5 giây chỉ dùng cho lab, sản phẩm giữ `02:00 UTC` theo D26; ④ số liệu (job ID, 92 URL, số mail) phụ thuộc dữ liệu máy dev nên sổ ghi kèm cả lệnh và log |

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