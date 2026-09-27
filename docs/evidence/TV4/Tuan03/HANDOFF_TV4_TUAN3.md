# Blooms & Handoff — TV4 Tuần 3 (Blocked tasks / Khi người phụ trách vắng mặt)

> **Tác giả**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Mục đích**: tổng hợp task TV4 tuần 3 **đã đóng** + những mục **còn block/cần quyết định**, điều kiện gỡ, và **hướng dẫn tự túc để người khác tiếp tục/kiểm tra** khi TV4 vắng mặt.
> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026) · **Nhánh**: `2312739_NHTSon_D3-D4-D5-D6` · **Reviewer**: Nguyễn Thanh Tâm
> **Cập nhật lần cuối**: 27/09/2026 (T7 — chốt D23/D27; đóng N1 invalidation bằng xác minh; chuẩn bị triển khai N2/N4)

---

## 0. Tóm tắt một nén

| Hạng mục | Kết quả |
|---|---|
| CulinaryBlog.Tests | **133/133 pass** (130 + 3 E2E MinIO trong `MinioE2ETests.cs`) |
| Concurrency spike | **5/5 pass** |
| Build + format | Release 0 warning/error; `dotnet format --verify-no-changes` exit 0 |
| Frontend `next build` | exit 0 — 13 routes + `robots.txt` + `sitemap.xml` |
| Nội dung mới trong branch | D3 archive/delete (soft D08), D4 SEO (sitemap Published-only/robots/detail SEO), D5 OTEL (trace+metrics HTTP→DB), E2E D1.3 trên MinIO, CI thêm service MinIO |
| Fix thật phát hiện bởi E2E | JWT `RoleClaimType="role"` (+ `NameClaimType="sub"`) — trước đây AuthorPolicy/AdminPolicy luôn 403 khi gọi API thật vì `MapInboundClaims=false` |

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
| CI | Thêm service MinIO + env `MINIO_*` + bước chờ `minio/health/live` | `.github/workflows/backend.yml` | ✅ Xong 24/09 — cần CI GitHub xanh sau push |

---

## 2. Bị block / chờ quyết định (24/09)

| Task | Nội dung | Block bởi | Điều kiện gỡ |
|---|---|---|---|
| **PR `4830e57`** | Fix connection string lazy (CI main 6 commit deploy/100 ảnh/UI có thể dính 28P01) | Reviewer nhóm | TV4/trưởng nhóm tạo PR → main; CI main xanh |
| **D2/D23** | Resize original/300×300/800×600 + queue persistent (Hangfire/BackgroundService) | Quyết định nhóm D23 | ✅ **Chốt 27/09 — PA-1 Hangfire** (`DE_XUAT_GIAI_QUYET_D23_D27.md`); TV4 đang thực hiện D2 |
| **D27** | Bucket policy + ảnh upload hiển thị (presigned/proxy) → uploader UI | Quyết định nhóm D27 + CR | ✅ **Xong 27/09** — chốt **PA-2 proxy có auth** + endpoint `GET /resources/images/{key}` + `IObjectStorageReader`; E2E `ImageProxyD27Tests` 6/6; `IMAGE_CONTRACT.md §5` cập nhật |
| **D4-Uploader UI** | Uploader progress/rollback/gallery/primary + status buttons ghép TV3 C4 | D27 ✅ xong + TV3 C4 | D27 proxy đã sẵn sàng; còn TV4 review PR #15 + progress/rollback + status buttons ghép TV3 C4 |
| **D3-Invalidation (N1 item 3, 27/09)** | Không tồn tại cache recipe nào để invalidate — backend không OutputCache/Redis-dữ-liệu; FE recipe `no-store`; `RecipeCacheService` orphan chưa wire → tiêu chí thỏa mặc định. **Đã đóng bằng xác minh 27/09.** | TV2/TV3 (nếu họ thêm ISR/output-cache cho recipe list/detail/ảnh) | Nếu TV2/TV3 thêm cache → TV4 kết nối revalidate hook (hoặc wire `RecipeCacheService` + `InvalidatePrefixAsync` khi archive/unpublish/delete) |
| **D6 Lab L4** | `practice/TV4/L4`: 4 MIME + resize + Mailhog + Hangfire + sổ K | Không ai block — độc lập | TV4 tự làm song song, nhánh riêng |
| **PR #14** | Giữ nguyên trên main (D1.3 + D3.1/D3.2 đã merge nhầm) | Nhóm trưởng | Giữ nguyên theo quyết định nhóm 23/09; rà soát diff trong tuần |

---

## 3. Hướng dẫn kiểm tra lại (tự túc khi TV4 vắng)

```powershell
# 1) Yêu cầu môi trường
#    - Postgres local 127.0.0.1:5432, user postgres, pass admin123 (DB culinary_test do test auto-migrate)
#    - MinIO local 127.0.0.1:9000, minioadmin/minioadmin, bucket culinary-blog (docker compose up -d)
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=admin123"

# 2) Build + format + full test
dotnet build CulinaryBlog.sln -c Release
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore
dotnet test CulinaryBlog.sln --no-build -c Release        # kỳ vọng 133/133 pass, 0 skip
dotnet test tests/concurrency-spike/ConcurrencySpike.csproj -c Release   # 5/5 pass

# 3) Frontend
cd src/frontend; npx next build                          # robots.txt + sitemap.xml có trong routes

# 4) CI tương đương
dotnet restore CulinaryBlog.sln --locked-mode
```

**Lưu ý**: không set `TEST_DATABASE` thì E2E MinIO vẫn chạy nhưng sẽ dùng cấu hình mặc định host (`appsettings.Testing`) có thể 28P01; nếu MinIO không reachable các test E2E **skip** (không fail).

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
| `src/backend/CulinaryBlog.API/Program.cs` | `.MapGet("/sitemap")`, OTEL, JWT `RoleClaimType/NameClaimType`, endpoints archive/delete |
| `.github/workflows/backend.yml` | service MinIO + bước chờ health |
| `src/frontend/src/app/` | `sitemap.ts`, `robots.ts`, `recipes/[slug]/` SEO |

---

## 5. Nguyên tắc không phá vỡ (bổ sung tuần 3)

1. **Không sửa `IFileStorageService`/`StoredFile`** trước khi chốt D27 — là contract bàn giao TV3.
2. **RoleClaimType/NameClaimType đã fix** — đừng revert `MapInboundClaims=false` hoặc bỏ 2 dòng đó; mọi test Author/Admin API thật sẽ 403.
3. **MinIO creds chỉ trong env/`MinioOptions`** — CI dùng env test, không commit secret.
4. **Package mới (ImageSharp/Hangfire) phải regenerate `packages.lock.json`** bằng `dotnet restore` (không `--locked-mode` khi thêm), CI `--locked-mode` mới khớp.
5. **Không xoá `ux_recipe_images_one_primary` / RowVersion** — phòng thủ D19.
6. **Conflicts RowVersion** trả 422 `recipe.version_conflict` qua `ApiExceptionHandler` — giữ mapping đó (E2E đã dựa trên nó, ổn định).

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