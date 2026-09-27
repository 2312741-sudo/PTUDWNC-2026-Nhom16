# Blooms & Handoff — TV4 Tuần 3 (Blocked tasks / Khi người phụ trách vắng mặt)

> **Tác giả**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Mục đích**: tổng hợp task TV4 tuần 3 **đã đóng** + những mục **còn block/cần quyết định**, điều kiện gỡ, và **hướng dẫn tự túc để người khác tiếp tục/kiểm tra** khi TV4 vắng mặt.
> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026) · **Nhánh**: `2312739_NHTSon_D3-D4-D5-D6` · **Reviewer**: Nguyễn Thanh Tâm
> **Cập nhật lần cuối**: 27/09/2026 (T7 — chốt D23/D27; đóng N1 invalidation bằng xác minh; **N2 proxy D27 + N4 resize Hangfire đã xong**: suite **148/148 + 5/5**)

---

## 0. Tóm tắt một nén

| Hạng mục | Kết quả |
|---|---|
| CulinaryBlog.Tests | **148/148 pass** (139 cũ + 5 unit resize/queue + 4 E2E `ImageResizeD2Tests`) |
| Concurrency spike | **5/5 pass** |
| Build + format | Release 0 warning/error; `dotnet format --verify-no-changes` exit 0 |
| Frontend `next build` | exit 0 — 13 routes + `robots.txt` + `sitemap.xml` |
| Nội dung mới trong branch | D3 archive/delete (soft D08), D4 SEO, D5 OTEL, E2E D1.3 trên MinIO, CI MinIO, **D27 proxy ảnh (N2)**, **D2 resize Hangfire (N4)** |
| Fix thật phát hiện bởi E2E | JWT `RoleClaimType="role"` (+ `NameClaimType="sub"`); **MinIO `WithCallbackStream` với `async` lambda → async void làm stream cắt cụt/ảnh hỏng** (N4) |
| ImageSharp | Giữ **3.1.11** — 4.x bắt buộc license key thương mại, build fail |

---

## 1. Đã đóng trong tuần 3 (24/09)

| Task | Nội dung | Code/Test | Trạng thái |
|---|---|---|---|
| N0 | Fix duplicate migration `RefreshTokens` (main `a651c8a`) + CI branch success | main; branch merge `e026dc9`→`75a8bf5` | ✅ Xong 23/09 |
| N0b | Fix connection string eager-read → đọc trong lambda `AddDbContext` (`4830e57`) | `Program.cs` | ✅ Xong — **chờ PR lên main** |
| D3.1/D3.2 | Publish/unpublish CQRS (đã trong main qua PR #14) | `Recipes.cs` | ✅ Xong (tuần 2, trong main) |
| D3.1c | `PATCH /recipes/{id}/archive` (Published/Draft→Archived, ẩn public ngay, owner/Admin, idempotent) | `Recipes.cs` (region D3) + `Program.cs` + `RecipeLifecycleTests.cs` | ✅ Xong 24/09 |
| D3.2c | `DELETE /recipes/{id}` soft delete theo D08 (`Recipe.MarkDeleted()`, global filter ẩn mọi truy vấn, không xoá vật lý ảnh) | `Recipes.cs` + `Recipe.cs` + tests | ✅ Xong 24/09 |
| D3.3 | Logout revoke refresh family — C5 refresh đã có trên main | main (`IdentityService.cs`) | ✅ Xong 23/09 (7 test Week3 + 16 test Auth) |
| D3.4/D1.1c | **E2E D1.3 trên MinIO**: register→create→upload JPEG→readback→PATCH primary→publish→unpublish→archive→delete→public list; MinIO down → skip an toàn; log không lộ secret | `tests/CulinaryBlog.Tests/MinioE2ETests.cs` (factory `ApiFactoryWithMinio`) | ✅ Xong 24/09 — 3/3 pass lặp lại nhiều lần |
| D4-SEO | `GET /recipes/sitemap` (Published-only) + `sitemap.ts`/`robots.ts`/SEO metadata trang công thức | `Discovery.cs`, `RecipeRepository.cs`, `Program.cs`, `src/frontend/src/app/{sitemap,robots}.ts`, `app/recipes/[slug]/` | ✅ Xong 24/09 |
| D5 | OTEL trace (ASP.NET/Http/EF) + metrics + health db/redis/minio đã có từ trước | `Program.cs` + `CulinaryBlog.API.csproj` + `packages.lock.json` | ✅ **Xong** — config xác minh 27/09 + EXPLAIN/k6 số liệu (`Tuan03/logs/`) |
| CI | Thêm service storage + env `MINIO_*` + bước chờ health | `.github/workflows/backend.yml` | ✅ Xong 24/09 — **CI xanh thật 28/09** run `36344662570` |
| **N5 / D6** | **Lab L4**: `media` 25/25 · `email` 3/3 · `xml` 3/3 · `jobs` 8/8 (+ `db`/`purge` chẩn đoán) | `practice/TV4/L4/` (14 file), `Tuan03/SOK_LAB_L4.md`, `Tuan03/logs/lab_l4_{run.log,db.txt}` | ✅ **Xong 28/09** — commit `3642428` (nhánh `practice/TV4/L4`); **4 phase · 39/39 check PASS** |
| **CI incident** | 5 run đỏ vì image MinIO bị gỡ khỏi registry (`quay.io` 401, Docker Hub 404) — **không phải lỗi code**; mọi test bị skip | `.github/workflows/backend.yml` + `docker-compose.dev.yml` (thay bằng `rustfs/rustfs`), `.env.example`, `README.md`, `docs/HUONG_DAN_TEST_APP.md` | ✅ **Xong 28/09** — `cd72b27` + `d78e25c`; CI xanh; dev stack dựng được trên máy mới |

---

## 2. Bị block / chờ quyết định (24/09)

| Task | Nội dung | Block bởi | Điều kiện gỡ |
|---|---|---|---|
| **PR `4830e57`** | Fix connection string lazy (CI main 6 commit deploy/100 ảnh/UI có thể dính 28P01) | Reviewer nhóm | TV4/trưởng nhóm tạo PR → main; CI main xanh |
| **D2/D23** | Resize original/300×300/800×600 + queue persistent (Hangfire/BackgroundService) | Quyết định nhóm D23 | ✅ **Xong 27/09 (N4)** — PA-1 Hangfire: `ResizeImageJob` + `HangfireImageResizeQueue` + ImageSharp 3.1.11, retry 3, dashboard Admin-only, idempotent + original fallback; E2E 4/4, job thật `Succeeded`; `IMAGE_CONTRACT.md §7` |
| **D27** | Bucket policy + ảnh upload hiển thị (presigned/proxy) → uploader UI | Quyết định nhóm D27 + CR | ✅ **Xong 27/09** — chốt **PA-2 proxy có auth** + endpoint `GET /resources/images/{key}` + `IObjectStorageReader`; E2E `ImageProxyD27Tests` 6/6; `IMAGE_CONTRACT.md §5` cập nhật |
| **D4-Uploader UI** | Uploader progress/rollback/gallery/primary + status buttons ghép TV3 C4 | D27 ✅ xong + TV3 C4 | D27 proxy đã sẵn sàng; còn TV4 review PR #15 + progress/rollback + status buttons ghép TV3 C4 |
| **D3-Invalidation (N1 item 3, 27/09)** | Không tồn tại cache recipe nào để invalidate — backend không OutputCache/Redis-dữ-liệu; FE recipe `no-store`; `RecipeCacheService` orphan chưa wire → tiêu chí thỏa mặc định. **Đã đóng bằng xác minh 27/09.** | TV2/TV3 (nếu họ thêm ISR/output-cache cho recipe list/detail/ảnh) | Nếu TV2/TV3 thêm cache → TV4 kết nối revalidate hook (hoặc wire `RecipeCacheService` + `InvalidatePrefixAsync` khi archive/unpublish/delete) |
| **D6 Lab L4** | `practice/TV4/L4`: 4 MIME + resize + Mailhog + Hangfire + sổ K | Không ai block — độc lập | ✅ **Xong 28/09 (N5)** — commit `3642428`, 39/39 check; ⚠️ còn mục 2 `PHAN_CHIA` (Identity/Google/refresh/forms/FTS) **là yêu cầu của tuần 3**, cần nhóm trưởng chốt làm tối giản hay dời tuần 4 |
| **D4 UI (28/09)** | progress upload + nút Unpublish/Archive | **Phải merge `origin/main` trước** (behind 23) — wizard/`ImagesStep.tsx` của PR #15 nằm trên main; Publish đã có, Unpublish/Archive chưa | TV4 tự làm sau khi merge; xin nhóm trưởng duyệt PR |
| **Ảnh storage (28/09)** | Nhánh `rustfs/rustfs` vs main `coollabsio/minio` | CR-6 — chọn 1 image cho dev + CI trước khi merge | Nhóm trưởng |
| **Fix `4830e57`** | Lazy connection string chưa có trên main | PR lên main | Nhóm trưởng |
| **N5 mục 2 (lab Identity/Google/forms/FTS)** | Phần TV4 cần học theo `PHAN_CHIA` tuần 3 | Ưu tiên sau D6-Lab | TV4 tự làm ở nhánh lab (không chặn G5 vì D6-Lab đã đủ bằng chứng) |
| **PR #14** | Giữ nguyên trên main (D1.3 + D3.1/D3.2 đã merge nhầm) | Nhóm trưởng | Giữ nguyên theo quyết định nhóm 23/09; rà soát diff trong tuần |

---

## 3. Hướng dẫn kiểm tra lại (tự túc khi TV4 vắng)

```powershell
# 1) Yêu cầu môi trường
#    - Postgres local 127.0.0.1:5432, user postgres, pass admin123 (DB culinary_test do test auto-migrate)
#    - Object storage S3-compatible 127.0.0.1:9000, minioadmin/minioadmin, bucket culinary-blog
#      (từ 28/09: docker compose -f docker-compose.dev.yml up -d s3 — image RustFS, KHÔNG phải image minio/minio
#       vì MinIO đã bị gỡ khỏi registry; xem mục 5.10)
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=admin123"

# 2) Build + format + full test
dotnet build CulinaryBlog.sln -c Release
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore
dotnet test CulinaryBlog.sln --no-build -c Release        # kỳ vọng 148/148 pass, 0 skip
dotnet test tests/concurrency-spike/ConcurrencySpike.csproj -c Release   # 5/5 pass
dotnet test CulinaryBlog.sln --no-build -c Release --filter ImageResizeD2Tests   # 4/4 resize (cần storage)

# 2b) Lab L4 (nhánh practice/TV4/L4) — cần Mailhog + storage + Postgres
docker run -d --name lab-mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog:v1.0.1
dotnet run --project practice/TV4/L4 -c Release -- all   # kỳ vọng 4 phase · 39/39 check PASS

# 3) Frontend
cd src/frontend; npx next build                          # robots.txt + sitemap.xml có trong routes

# 4) CI tương đương
dotnet restore CulinaryBlog.sln --locked-mode
```

**Lưu ý**: không set `TEST_DATABASE` thì E2E storage vẫn chạy nhưng sẽ dùng cấu hình mặc định host (`appsettings.Testing`) có thể 28P01; nếu storage không reachable các test E2E **skip** (không fail) — nên khi test xanh, **kiểm tra `Skipped=0`** để chắc chắn E2E thực sự chạy chứ không bị skip âm thầm (bài học từ sự cố CI 28/09).

---

## 4. Code hiện trạng liên quan E2E/Kiểm chứng tuần 3

| File | Vai trò |
|---|---|
| `tests/CulinaryBlog.Tests/MinioE2ETests.cs` | E2E thật trên MinIO + `ApiFactoryWithMinio` (env-configurable endpoint/creds/bucket, TCP khả dụng → skip nếu down) |
| `tests/CulinaryBlog.Tests/RecipeLifecycleTests.cs` (+117 dòng) | Unit/API test archive/delete/publish lifecycle |
| `tests/CulinaryBlog.Tests/DiscoveryAndSearchTests.cs` (+35) | Test `GetSitemapQuery` Published-only |
| `src/backend/CulinaryBlog.Application/Recipes.cs` | region D3: `ArchiveRecipeCommand`/`DeleteRecipeCommand` |
| `src/backend/CulinaryBlog.Domain/Entities/Recipe.cs` | `MarkDeleted()` (soft D08) |
| `src/backend/CulinaryBlog.Application/Discovery.cs` | `GetSitemapQuery`/`SitemapRecipeDto` |
| `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs` | `GetPublishedForSitemapAsync` |
| `src/backend/CulinaryBlog.API/Program.cs` | `.MapGet("/sitemap")`, OTEL, JWT `RoleClaimType/NameClaimType`, endpoints archive/delete, **Hangfire PostgreSQL + server 4 worker + dashboard `/hangfire` (Admin-only, tắt ở `Testing`)** |
| `src/backend/CulinaryBlog.Infrastructure/ResizeImageJob.cs` + `ImageResizeQueue.cs` | **N4**: job resize 300×300/800×600 (idempotent, original fallback, delete-vs-resize) + `HangfireImageResizeQueue` / `InlineImageResizeQueue` |
| `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs` | `IObjectStorageReader` (D27) + **`IObjectStorageWriter`** (N4, ghi object key phái sinh) — `ReadAsync` copy **đồng bộ** |
| `src/backend/CulinaryBlog.API/AdminDashboardAuthorizationFilter.cs` | N4: dashboard Hangfire chỉ Admin (anon 401 / member 403 / Admin 200) |
| `tests/CulinaryBlog.Tests/ImageResizeD2Tests.cs` | N4: E2E resize 4/4 (kích thước, idempotent, ảnh hỏng fallback, ảnh đã xoá) |
| `practice/TV4/L4/` | **N5**: lab L4 console app — `MediaPhase`, `EmailPhase`, `SitemapPhase`, `JobsPhase`, `LabJobs`, `LabImageScaler`, `Fixtures`, `DbEvidencePhase`, `PurgePhase`, `LabConfig`, `LabLog`, `README.md` (không thêm vào `CulinaryBlog.sln`) |
| `.github/workflows/backend.yml` | service `objectstorage` (`rustfs/rustfs` tag+digest) + health `/health`, SDK pin `10.0.401`, `timeout-minutes 30`, `concurrency`, `--blame-hang-timeout 10m` |
| `docker-compose.dev.yml` | service `s3` (RustFS, volume `s3data`, console 9001); đã bỏ `minio-init` — app tự tạo bucket |
| `src/frontend/src/app/` | `sitemap.ts`, `robots.ts`, `recipes/[slug]/` SEO |

---

## 5. Nguyên tắc không phá vỡ (bổ sung tuần 3)

1. **Không sửa `IFileStorageService`/`StoredFile`** trước khi chốt D27 — là contract bàn giao TV3.
2. **RoleClaimType/NameClaimType đã fix** — đừng revert `MapInboundClaims=false` hoặc bỏ 2 dòng đó; mọi test Author/Admin API thật sẽ 403.
3. **MinIO creds chỉ trong env/`MinioOptions`** — CI dùng env test, không commit secret.
4. **Package mới (ImageSharp/Hangfire) phải regenerate `packages.lock.json`** bằng `dotnet restore` (không `--locked-mode` khi thêm), CI `--locked-mode` mới khớp.
5. **Không xoá `ux_recipe_images_one_primary` / RowVersion** — phòng thủ D19.
6. **Conflicts RowVersion** trả 422 `recipe.version_conflict` qua `ApiExceptionHandler` — giữ mapping đó (E2E đã dựa trên nó, ổn định).
7. **Đừng nâng ImageSharp lên 4.x** — 4.x bắt buộc commercial license key và build fail; giữ `3.1.11` (AVIF không decode → job đã fallback original).
8. **Đừng dùng `async` lambda cho `WithCallbackStream`** (MinIO SDK) — `Action<Stream>` sẽ biến thành async void fire-and-forget → stream cắt cụt/ảnh hỏng. `ReadAsync` phải copy **đồng bộ** + kiểm tra `buffer.Length == stat.Size`.
9. **Response upload có `mediumUrl`/`thumbnailUrl = null`** là chuẩn (job nền). FE (TV3) cần reload `GET /recipes/{slug}`; `imageSrc()` đã fallback `originalUrl`.
10. **Đừng dùng image container với tag `latest`/image đã bị gỡ khỏi registry** (28/09) — MinIO đã xoá cả `quay.io/minio/minio` lẫn `minio/minio` trên Docker Hub, khiến **toàn bộ** CI và dev compose hỏng mà không có báo lỗi code nào. Dùng `rustfs/rustfs` (tag + digest) như hiện tại. **Production KHÔNG dùng RustFS** — cần object storage có license.
11. **Sau khi sửa CI, kiểm tra `Skipped=0`** — test E2E skip âm thầm khi storage down làm CI "xanh giả" mà không kiểm thử gì.
12. **Rà `origin/main` trước khi làm việc FE hoặc fix hạ tầng** (28/09): PR #15 (wizard ảnh) đã merge vào main trong lúc nhánh này đang làm N4/N5, và TV3 đã tự đổi image dev sang `coollabsio/minio` — nếu không rà main thì (a) không sửa được code vì file chưa có trong nhánh, (b) merge sau sẽ đụng image storage khác nhau.

---

## Phụ lục: bản ghi thay đổi bàn giao tuần 3

| Ngày | Ai | Nội dung |
|---|---|---|
| 23/09 | main `a651c8a` | Fix duplicate migration `RefreshTokens` → TV4 verify 120/120 + 5/5; CI branch success |
| 23/09 | TV4 `4830e57` | Fix connection string lazy (đọc trong lambda AddDbContext) để `TEST_DATABASE` override có hiệu lực |
| 24/09 | TV4 | D3 archive/delete + D4-SEO + D5-OTEL + E2E MinIO (3/3) + CI MinIO service; **133/133 + 5/5 pass local**, frontend build OK |
| 24/09 | TV4 | Phát hiện & fix JWT `RoleClaimType="role"` (bug 403 API thật, không lộ qua test trước đây vì test toàn dùng handler) |
| 27/09 | TV4 | Chốt D23 → PA-1 Hangfire; D27 → PA-2 base media URL proxy (`DE_XUAT_GIAI_QUYET_D23_D27.md`); PR #14 giữ nguyên (nhóm chung tay sửa); **đóng N1 invalidation bằng xác minh** (không cache recipe; `RecipeCacheService` orphan) |
| 27/09 | TV4 | **N2 xong**: proxy ảnh D27 (`GET /api/v1/resources/images/{**key}` + `IObjectStorageReader`) + E2E 6/6; `IMAGE_CONTRACT.md §5` chốt PA-2; suite **139/139 + 5/5**; push `0cc279e` |
| 27/09 | TV4 | **N3 xong**: xác minh OTEL (`2bbee0d` còn nguyên) + ghi số liệu EXPLAIN publish (0.339ms/0.044ms) + k6 smoke 20 VU×30s (3310 req, 0% fail, p95 225.63ms) — log `Tuan03/logs/` |
| 27/09 | TV4 | **N4 xong (D2/D23 PA-1)**: package Hangfire (API 1.8.25 + PostgreSQL 1.21.1) + ImageSharp **3.1.11** (4.x cần license thương mại → build fail); `ResizeImageJob` 300×300/800×600 idempotent + original fallback + delete-vs-resize; `IObjectStorageWriter` (key chủ động, tách khỏi `IFileStorageService`); `IImageResizeQueue` (Hangfire + Inline cho Testing); dashboard `/hangfire` Admin-only; E2E `ImageResizeD2Tests` 4/4 + 5 unit; **job thật `Succeeded`**, proxy 300×200; suite **148/148 + 5/5**; `IMAGE_CONTRACT.md §7`; log `Tuan03/logs/d2_resize_hangfire*.{log,txt}` |
| 28/09 | TV4 | **N5 xong (D6 Lab L4)**: nhánh `practice/TV4/L4` commit `3642428` — 4 phase · **39/39 check PASS** (`media` 25/25, `email` 3/3, `xml` 3/3, `jobs` 8/8), thêm phase chẩn đoán `db`/`purge`; sổ K `Tuan03/SOK_LAB_L4.md` + `practice/TV4/L4/README.md`; log `lab_l4_{run.log,db.txt}` không secret; sửa 5 lỗi thật (ctor Hangfire obsolete, queue mismatch, subject Mailhog, xoá object qua `IFileStorageService`, encoding PowerShell 5.1) |
| 28/09 | TV4 | **Gỡ sự cố CI 5 run đỏ**: nguyên nhân ngoài code — MinIO gỡ toàn bộ image public (`quay.io` 401, Docker Hub 404) → runner không pull được, job chết ở "Initialize containers", mọi test bị skip. `cd72b27`: thay bằng `rustfs/rustfs` + health `/health`, `MinioStorageService` không đổi dòng → CI xanh run `36343309464`. `d78e25c`: sửa luôn dev compose (service `minio`→`s3`, bỏ `minio-init` vì app tự tạo bucket, volume `s3data`) + harden (tag+digest, SDK `10.0.401`, `timeout-minutes 30`, `concurrency`, `--blame-hang-timeout 10m`) → **CI xanh run `36344662570`**; local 148/148 + 5/5 `Skipped=0` trên cả hai đường |