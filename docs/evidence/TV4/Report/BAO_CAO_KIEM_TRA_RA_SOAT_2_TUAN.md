# BÁO CÁO KIỂM TRA & RÀ SOÁT TƯƠNG HỖ 2 TUẦN ĐẦU — NHÓM 16

> **Ngày kiểm tra**: 22/09/2026
> **Phạm vi**: Tuần 1 & Tuần 2 theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md` và `KE_HOACH_DU_AN.md` (tasks A1–A7 / B1–B7 / C1–C7 / D1–D7), milestones G0–G3.
> **Cách làm**: Đóng vai lần lượt **TV1 (Thanh Tâm)**, **TV2 (Trường Vĩ)**, **TV3 (Quốc Trung)**, **TV4 (Trung Sơn)** — mỗi người tự rà soát phần mình và phần liên quan, sau đó đối chiếu chéo với code/evidence thực tế. **Chỉ kiểm tra, không sửa tài nguyên** (ngoài việc tạo file báo cáo này trong `docs/`).

---

## 0. Kết quả kiểm chứng thực tế (dùng làm căn cứ đối chiếu)

Trước khi đánh giá, nhóm đã chạy lại trên máy kiểm tra (.NET 10.0.401, PostgreSQL 18 local, port 5432):

| Hạng mục | Lệnh / đối tượng | Kết quả |
|---|---|---|
| Build `main` | `dotnet build CulinaryBlog.sln -c Release` (worktree sạch `62fcec5`) | ✅ 0 warning, 0 error |
| Build workspace hiện tại (nhánh `2312739_NHTSon_D1-D2-D3-D4` + thay đổi chưa commit của TV4) | `dotnet build CulinaryBlog.sln -c Release` | ✅ 0 warning, 0 error |
| Test `main` | `dotnet test CulinaryBlog.sln` (TEST_DATABASE → Postgres) | ✅ 72 unit + 5 spike = **77/77 pass** |
| Test workspace hiện tại (WIP TV4 D1.3) | `dotnet test CulinaryBlog.sln` | ✅ CulinaryBlog.Tests **88/88 pass**; ⚠️ ConcurrencySpike **1/5 FAIL ở 1 trong 4 lượt** (flaky) |
| Frontend | `package.json` đối chiếu | ⚠️ Không có TanStack Query, Auth.js, Jest/RTL, Playwright; build chưa chạy lại (thiếu `node_modules`) |
| CI | `.github/workflows/backend.yml` | ✅ Chỉ có Postgres service; **không có** MinIO/Redis service cho integration |

Ký hiệu dùng trong báo cáo: **✅ Hoàn thành** · **🔄 Đang làm / làm dở** · **❌ Chưa làm** · **⛔ Block (có ghi lý do + điều kiện gỡ)**.

---

# TUẦN 1

## 1.1. TV1 — Nguyễn Thanh Tâm (A1, A2 nền, A5 nền, A6 nền)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| A1 – Identity/roles/migration/PBKDF2/JWT | ✅ Hoàn thành, đã merge main | `InitialIdentity` migration, `IdentityService.cs`/`JwtService.cs`, PBKDF2 100k iterations, JWT 15p |
| A2 nền – register/login/me + ICurrentUser | ✅ Hoàn thành, đã merge main | Endpoint `POST /auth/register|login`, `GET /auth/me`; unique email 409, login sai 401, inactive 403 |
| A5 nền – Problem Details/Serilog/CorrelationId/CI | ✅ Hoàn thành, đã merge main | `ApiExceptionHandler.cs`, `Pipeline.cs`, `backend.yml` |
| A6 nền – Lab kiến trúc + Domain value object | ✅ Hoàn thành (minh chứng mở) | `ArchitectureTests.cs` (reflection chống Domain/Application tham chiếu Infra/Web), `DisplayName.cs` |

**Nhận định vai TV1**: Phần việc tuần 1 của TV1 đã bàn giao đúng hạn, chạy được và được các bạn khác dùng thật (auth contract để TV2/TV3/TV4 nối). Tự rà soát thấy **2 ghi chú cần thẳng thắn**:
- Tôi vừa làm vừa tự duyệt PR của chính mình (theo quy định nhóm), nên thiếu một mắt review độc lập cho phần auth/CI. Đã điều tiết bằng Architecture/structure test nhưng không thay được review người tuổi.
- Tôi có commit UI trang `/recipes` (placeholder “đang hoàn thiện”) — **trùng phạm vi B2 của TV2**. Không gây hại nhưng là ví dụ chồng việc cần tránh ở tuần sau.

---
## 1.2. TV2 — Ngô Quốc Trường Vĩ (B1, B2 nền, B6 nền, chốt Google contract)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| B1 – Category entity/config/migration/CRUD CQRS/Admin | ✅ Hoàn thành, đã merge main | `Category.cs`, `CategoryRepository.cs`, migration `AddCategoryModule`, `Categories.cs`, `CATEGORY_CONTRACT.md` |
| B2 nền – DTO list + phân trang/data-meta | ✅ Hoàn thành, đã merge main | `Pagination.cs` (PagedResult/data-meta, pageSize=12) |
| B6 nền – Lab spike auth | 🟡 Làm dở | `LAB_DANH_MUC_AUTH_SPIKE.md` (tài liệu; chưa có nhánh lab thật trên repo) |
| Chốt Google contract (D04) | ✅ Hoàn thành | `GOOGLE_AUTH_CONTRACT.md`, `docs/adr/0002-category-and-google-auth-week1.md` |

**Sai sót phát hiện khi đối chiếu SRS v1.1.1:**
> ⚠️ **Category hiện đang dùng SOFT DELETE (`MarkDeleted`/`IsDeleted`) trong khi SRS v1.1.1 C07 đã chốt HARD DELETE** (xóa entity khỏi DB, 204; 409 nếu còn recipe). Test của TV2 còn đặt tên `DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty`.
> **Ảnh hưởng**: dòng Category đã xóa vẫn nằm trong DB kèm ràng buộc `Name`/`Slug` UNIQUE (C09) — tạo lại cùng tên/slug sẽ vướng 409 dù category đã “xóa”; dữ liệu chết ẩn trong bảng. Trái chỉ đạo FR-CAT-005/C07.
> **Điều kiện gỡ**: TV2 chuyển handler sang Hard Delete (giữ logic 409 khi còn recipe), cập nhật domain (bỏ `MarkDeleted`/`Restore` hoặc giữ nhưng không dùng cho delete), sửa tên/nội dung test, cập nhật `CATEGORY_CONTRACT.md`, mở PR có TV1 review.

---
## 1.3. TV3 — Huỳnh Quốc Trung (C1 nền)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| C1 – Recipe aggregate/Nutrition/entity/config/UoW/RowVersion | ✅ Code nền đã merge (Phase 0, PR #10) | `Recipe.cs`, `RecipeNutrition.cs`, `RecipeIngredient`, `RecipeStep`, `RecipeImage`, configs, `EfUnitOfWork`, `AuditableEntityInterceptor` |
| C1 – migration `AddRecipeAggregate` | ✅ Đã sinh & merge | `20260919061954_AddRecipeAggregate` |
| C1 – concurrency spike (2 writer, rollback, publish invariant) | ✅ Pass (chạy lại 5/5 nhiều lượt trên main) | `tests/concurrency-spike/RowVersionConcurrencyTests.cs` |
| ADR-0001 (schema/version/nutrition...) | ✅ Merge | `docs/adr/ADR-0001-recipe-schema-va-concurrency.md` |

**Nhận định vai TV3**: C1 tuần 1 coi như xong ở mức “schema + spike”, nhưng tài liệu `C1-HOAN-THIEN.md` của tôi bản thân ghi “môi trường soạn thảo không có .NET SDK, bạn chạy giúp 4 bước dưới máy mình” — tức **migration chưa được chính tay tôi áp lên DB sạch trước khi merge**. Nay main đã build/test sạch nên chuyện đó đã được hóa giải, nhưng tôi phải bổ sung evidence `TV3-K06` (ảnh `dotnet ef database update` trên DB rỗng) thay vì để bạn khác chạy hộ.
> **Điểm cần cả nhóm chốt lại**: G1 bị khai nhận “Draft tạo được” nhưng thực tế **không có endpoint tạo recipe trên main** (Program.cs chỉ có auth + categories). Draft chỉ hình thành được qua seed DB/spike chứ chưa qua API/UI → **G1 đạt một phần** (đúng cho auth + category, chưa đúng cho Draft).

---
## 1.4. TV4 — Nguyễn Hữu Trung Sơn (D5 nền, D1 contract, D3 logout, D6 nền)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| D5 – docker-compose full-stack (PG16/Redis7/MinIO/Mailhog/Seq/Nginx) | ✅ Hoàn thành, đã merge | `docker-compose.dev.yml`, `nginx/nginx.dev.conf` |
| D5 – health `/health`, `/health/live`, `/health/ready` | ✅ Hoàn thành | `Health.cs` + `HealthTests` |
| D5 – `.env.example` không secret + bucket `culinary-blog` private + `minio-init` | ✅ Hoàn thành | `.env.example`, `docker-compose.dev.yml` |
| D1 contract – `IFileStorageService`/`StoredFile`/`MinioOptions` | ✅ Hoàn thành, đã merge | `Storage.cs`, `MinioOptions.cs` |
| D3 – `POST /auth/logout` Bearer 204 + cập nhật AUTH_CONTRACT | ✅ Hoàn thành, đã merge | endpoint logout + `AUTH_CONTRACT.md` |
| D6 nền – mở sổ skill | 🟡 Mới mở sổ | `docs/evidence/TV4/...` |

**Nhận định vai TV4**: Tuần 1 của tôi hoàn thành và đã merge (PR #8). Tự rà soát thấy cần ghi rõ: (1) báo cáo tuần 1 chép “máy local chỉ có .NET 9” — hiện máy kiểm tra đã có .NET 10.0.401 nên khuyến nghị cả nhóm tự build/test; (2) **D27 bucket policy chưa chốt** — tôi đang để bucket private + `minio-init`, nhưng SRS 2.4.1 ghi `public-read`; chờ quyết định nhóm/giảng viên trước khi làm UI hiển thị ảnh (gây block D4, ghi ở Tuần 2).

---

## 1.5. Tổng kết TUẦN 1

| Milestone | Trạng thái | Ghi chú |
|---|---|---|
| G0 giữa tuần — stack chạy | ✅ Đạt | Compose + health 200 + `.env.example` |
| G1 cuối tuần — đăng ký → đăng nhập → category → Draft | 🟡 **Đạt một phần** | Auth + Category đủ; **Draft chưa có endpoint tạo recipe** → chưa thật sự “đạt” như khai nhận |

Vẫn ổn tổng thể: không thành viên nào “bỏ trống” tuần 1. Sai sót cần xử lý sớm nhất: **Category soft-delete trái C07** (TV2).

---

# TUẦN 2

## 2.1. TV1 — Nguyễn Thanh Tâm (A2 hoàn thiện, A3, A4, A7)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| A2 phần còn lại – Lockout 5 lần/15p + Rate limit auth | ✅ Hoàn thành, đã merge | `Program.cs` (Lockout=5, 423 `auth.locked`), `AddRateLimiter` auth 10 req/phút → 429 + Retry-After |
| A3 – PATCH /auth/me + dashboard/profile RHF/Zod | ✅ Hoàn thành, đã merge | `PATCH /auth/me`, `src/frontend/src/app/dashboard/profile/page.tsx` |
| A4 – Welcome email (Channel worker + retry 0/1/5/30p) | ✅ Hoàn thành, đã merge | `WelcomeEmail.cs` (SmtpClient, encode HTML) |
| A7 – tests reg cộng đồng | ✅ Hoàn thành | main 77/77 pass (gồm cả phần này) |

**Sai sót/số liệu cần đính chính (tự rà soát vai TV1):**
- Bảng kỹ năng trong `PHAN_CHIA_CONG_VIEC_6_TUAN.md` ghi tôi **14/24**. Đối chiếu thực tế: **một số ô chưa đủ minh chứng theo stack SRS**:
  - **K14 (Hangfire)** — tôi ghi nhận nhưng triển khai bằng **Channel + BackgroundService thủ công**, Không phải Hangfire persistent/dashboard như SRS K14 yêu cầu → chỉ đạt “retry”, **chưa đạt ô trọn**.
  - **K16/K17 (TanStack Query, optimistic rollback, next/image)** — **không có** thư viện nào trong `package.json` (chỉ RHF+Zod), không có optimistic update → **chưa đạt ô trọn**.
- → Cần bổ sung lab hoặc refactor đúng stack vào tuần 3–4 (theo đúng L1–L6), **không để bảng 14/24 đứng yên** vì khi giảng viên kiểm tra sẽ vỡ minh chứng.

---
## 2.2. TV2 — Ngô Quốc Trường Vĩ (B2, B3, B4, B7)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| B2 phần còn lại – category UI/detail + list recipes scope/filter/sort/page | 🔄 **Có code trên nhánh, CHƯA merge** | Nhánh `2312796-ngo-quoc-truong-vi-feat/TV2-week2-discovery-search-google` (`ae7de7e`, `10e4edc`, `7a5c2d6`) |
| B3 – FTS unaccent/pg_trgm/GIN/search | 🔄 **Có code trên nhánh, CHƯA merge** | `Discovery.cs`, `RecipeRepository.cs`, migration `AddRecipeDiscoveryAndSearch`, `search/page.tsx`, `DiscoveryAndSearchTests.cs` |
| B4 – Google login (Auth.js + backend verify + UI) | 🔄 **Có code trên nhánh, CHƯA merge** | `GoogleAuth.cs`, `GoogleAuthService.cs`, `GoogleSignInButton.tsx` |
| B7 – tests | 🔄 **Trên nhánh; chưa thành PR hợp lệ** | `DiscoveryAndSearchTests.cs` (~380 dòng) |
| Evidence tuần 2 | ❌ **Không có file** | Không tồn tại `docs/evidence/TV2/TUAN_2.md` |

**⛔ BLOCK / RỦI RO NGHIÊM TRỌNG (ảnh hưởng cả nhóm):**
> Nhánh tuần 2 của TV2 được fork từ `main` cũ (merge-base `6e64a3b`, **trước khi TV4 merge PR #8**). Đối chiếu với main hiện tại, nhánh này sẽ **XÓA các file của TV4** nếu merge kiểu nông/giải conflict sai:
> `CulinaryBlog.Application/ImageUpload.cs`, `CulinaryBlog.Infrastructure/MinioStorageService.cs`, `CulinaryBlog.Tests/ImageUploadValidatorTests.cs`, `CulinaryBlog.Tests/RecipeImageDomainTests.cs`, `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs`.
> **Lý do**: git tự nhận đây là “xóa file” vì nhánh không có file (fork trước khi TV4 thêm), nếu nhập thiếu kiểm tra sẽ mất toàn bộ D1 của TV4 và vỡ CI.

**Điều kiện gỡ block (todo bắt buộc trước khi mở PR):**
1. TV2 **rebase** nhánh lên `main` mới nhất (`git rebase origin/main`).
2. Giải toàn bộ conflict **có sự tham gia TV4** (file trùng: `Recipe.cs`, `ImageUpload.cs`, `MinioStorageService.cs`, migrations, `packages.lock.json`, snapshots).
3. Kiểm tra migration `AddRecipeDiscoveryAndSearch` nằm đúng thứ tự sau `AddRecipeAggregate` (TV3 điều phối rebase migration).
4. Bổ sung file `docs/evidence/TV2/TUAN_2.md` + chạy lại `dotnet test` toàn bộ trên nhánh sau rebase.

**Thêm kiểm chứng**: code có sẵn Discovery/search/Google trên nhánh là tín hiệu tốt; nhưng **không thấy Redis cache (B5)/OutputCache** trong nhánh → B5 còn thiếu, nên ghi nhận “đang làm” chứ không “hoàn thành” như bảng phân việc hiện đang thể hiện.

---
## 2.3. TV3 — Huỳnh Quốc Trung (C2, C3, C5)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| C2 – Create/Update/Detail CQRS recipe + validator + ownership + slug | ❌ **CHƯA CÓ endpoint** | `main`: chỉ groups auth + categories; **không có `POST|PUT|GET /recipes`** |
| C3 – Ingredient/Step CRUD + renumber | ❌ **CHƯA CÓ endpoint** | Program.cs không có /ingredients, /steps |
| C5 – Refresh token hash/rotation/reuse | ❌ **CHƯA CÓ** | Chỉ có entity `RefreshToken` (schema tối thiểu); **`JwtService` vẫn trả `RefreshToken: null`**; không có `POST /auth/refresh` |
| PR #10 nhánh `C2-C3-recipe-api` | ⚠️ **Sai khổ PR** | Merged nhưng chỉ chứa **Phase 0** (gộp AuthDbContext, wiring DI, migration). Tên nhánh “C2-C3 recipe API” nhưng **không có API recipe nào** |
| Evidence tuần 2 | ❌ **Không có file** | Không tồn tại `docs/evidence/TV3/TUAN_2.md` |

**⛔ BLOCK đây là rào chặn chính của dự án ngay lúc này:**
- Không có recipe CRUD endpoint → **CHẶN TV4**: test E2E upload ảnh (D1.3), publish/unpublish (D3), archive/delete (D3).
- Không có refresh rotation (C5) → **CHẶN TV4** logout revoke family (D3.3), và **chưa đạt FR-AUTH-004**.
- Không có ingredient/step → **chưa thể đạt G2** (Draft đủ nguyên liệu/bước rồi publish).

**Điều kiện gỡ block:**
1. TV3 bàn giao đủ **condition 3** mà `HANDOFF_TV4_TUAN2_BLOCKED.md` đang chờ: endpoint `POST /recipes` (tạo Draft), `GET /recipes/{slug}`, `PUT /recipes/{id}` (kèm ownership 403, slug, RowVersion/422 theo D02), và CRUD ingredient/step theo D15/D16.
2. C5: sinh refresh 512-bit, lưu SHA-256, rotation trong transaction, revoke family khi reuse (D05 đã có hướng trong ADR-0001) + `POST /auth/refresh`.
3. Viết test theo tiêu chí (title biên, cookTime=0, servings<=0, non-owner bị chặn, childId recipe khác bị chặn, concurrent refresh không tạo hai nhánh), cập nhật Scalar/types, mở PR nhỏ để TV1 review.
4. Lập `docs/evidence/TV3/TUAN_2.md` ghi rõ cái nào “xong PR” vs “chờ merge”.

---
## 2.4. TV4 — Nguyễn Hữu Trung Sơn (D1, D2, D3, D4)

| Công việc | Trạng thái | Minh chứng đối chiếu |
|---|---|---|
| D1.1 MinioStorageService + DI + lockfile | ✅ Đã merge (PR #8) | `MinioStorageService.cs`, `packages.lock.json` |
| D1.2 validator 4 MIME + ≤5MiB + magic bytes | ✅ Đã merge + test | `ImageUpload.cs`, `ImageUploadValidatorTests.cs` (16 test) |
| D1.5 domain primary invariant + race test | 🔄 **Test FLAKY** | `RecipeImageDomainTests.cs`, `RecipeImagePrimaryConcurrencyTests.cs` |
| D1.3 Image endpoints upload/PATCH/DELETE | 🟡 **Có code nhưng CHƯA commit/push** | Trong working tree nhánh `2312739_NHTSon_D1-D2-D3-D4`: `RecipeImages.cs`, `RecipeImageRepository.cs`, 3 endpoint, map race → 422 |
| D2 resize 300×300/800×600 + job nền | ❌ Chưa làm (0%) | ⛔ Block chờ chốt queue D23 (Hangfire vs BackgroundService) + package ảnh |
| D3 publish/unpublish/archive/delete | ❌ Chưa làm | ⛔ Block chờ **TV3 recipe CRUD endpoint** |
| D3 logout revoke refresh family | ❌ Chưa làm | ⛔ Block chờ **TV3 C5** |
| D4 UI uploader/status + sitemap/robots + SEO | ❌ Chưa làm | ⛔ Block chờ **D27 bucket policy** + recipe API |
| D5 phần còn lại (OTEL/metrics/backup/restore/multi-instance) | 🟡 Làm dở (~55%) | Nền Compose/health/CI đã xong; OTEL + backup còn thiếu |
| D6 lab | 🟡 D6 thực tế **chưa bắt đầu** (Mặc dù báo cáo ghi 8/24) | Xem phần “Sai sót” dưới |

**Sai sót/kẽ hở khi đối chiếu (tự rà soát vai TV4):**
- **D1.3 đang nằm trong working tree chưa commit** → rủi ro mất việc; đồng thời **chưa ai review**. Điều kiện gỡ: commit + push ngay sau có 1 review (TV1/TV3) trên nhánh, rồi merge.
- **Test race `Concurrent_primary_setters_never_produce_two_primaries` FLAKY**: chạy 4 lượt có **1 lượt FAIL** (`failures>=1` không thỏa — hai writer không trùng giao dịch nên không kích hoạt `ux_recipe_images_one_primary`). Đây là nguy cơ **CI fail ngẫu nhiên** và là **bằng chứng race không ổn định**. Điều kiện gỡ: sửa test dùng cơ chế đảm bảo trùng (VD `Barrier`/chạy thật song song, hoặc 2 lần save trong transaction giả) để test thành deterministic; giữ nguyên unique index và RowVersion.
- **Bảng kỹ năng 8/24 đang bị phóng đại**: `BAO_CAO_TIEN_DO_2_TUAN_TV4.md` tự ghi K11 (FTS) và K12 (cache) là “có minh chứng” — trong khi **không tồn tại code FTS/Redis cache nào của TV4** (FTS là của TV2 trên nhánh chưa merge; cache chưa ai làm). Các ô có căn cứ hiện thực: K01, K05 (validator), K13, K20, K23, K24. K11/K12 cần lab thật (L3) trên nhánh `practice/TV4/`.
- **D6 (lab `practice/TV4/L4`) chưa bắt đầu** dù tuần 1 ghi “mở vào đầu tuần 2”.

---

## 3. BLOCK tổng hợp và điều kiện gỡ (dùng chung cả nhóm)

| # | Block | Nguyên nhân | Ảnh hưởng đến ai | Điều kiện gỡ |
|---|---|---|---|---|
| BL-1 | **TV3 chưa bàn giao recipe CRUD (C2/C3)** | Phase 0 chỉ mới wiring + migration; không có `POST /recipes` | Chặn TV4 (D1.3 E2E, D3 publish/archive/delete); chặn **G2/G3** | TV3 hoàn thiện C2+C3 (endpoint + test + Scalar), merge PR, cung cấp fixture recipe có ingredient+step |
| BL-2 | **TV3 chưa có C5 refresh rotation** | Chỉ có entity; `JwtService` trả `RefreshToken: null`; không có `POST /auth/refresh` | Chặn TV4 logout revoke (D3.3); FR-AUTH-004 chưa đạt | TV3 làm C5 theo D05, expose cơ chế revoke family, merge PR |
| BL-3 | **Nhánh TV2 week-2 có thể xóa code TV4 khi merge** | Fork từ main cũ (merge-base trước PR #8) | Nguy cơ **mất D1 của TV4 + vỡ CI** | TV2 rebase lên main mới, giải conflict cùng TV4, chạy đủ test, rồi mới mở PR |
| BL-4 | **D27 bucket policy chưa chốt** | SRS `public-read` vs đề xuất private/presigned của TV4 | Chặn TV4 D4 (UI ảnh), D1.4 | Nhóm họp chốt CR-1; nếu đổi chuẩn SRS phải **CR đăng ký giảng viên**, sau đó TV4 cập nhật ADR + code |
| BL-5 | **D23 queue resize chưa chốt** | Hangfire (thêm package + lockfile + CI) vs BackgroundService | Chặn TV4 D2 (resize) | Nhóm chốt 1 phương án; nếu thêm package chạy `dotnet restore` cập nhật `packages.lock.json`, bổ sung service vào CI nếu cần |
| BL-6 | **Từng ô kỹ năng bị phóng đại** | K14 (TV1) không dùng Hangfire; K16/K17 không có TanStack Query; K11/K12 (TV4) không có code | Rủi ro **vỡ minh chứng 24/24** khi giảng viên kiểm tra | Trả về trạng thái “chưa đạt ô” trong ma trận, bổ sung lab đúng stack (L1–L6) tuần 3–4 |
| BL-7 | **Category soft-delete trái C07** | Handler dùng `MarkDeleted` thay vì delete cứng | FR-CAT-005, C7, C09 (unique sau xóa) | TV2 chuyển Hard Delete + sửa test + contract, PR có review |

---

## 4. Đánh giá chéo: ai làm thiếu / sai sót ảnh hưởng người khác hoặc cả project?

| Thành viên | Làm tốt | Thiếu / sai | Ảnh hưởng thực tế |
|---|---|---|---|
| **TV1** | Tuần 1+2 auth/CI/profile/email chắc chắn, đã merge, main luôn build/test sạch | K14/K16/K17 khai đạt sớm; tự duyệt PR mình; nhảy vào trang `/recipes` của TV2 | Chưa ảnh hưởng ai trực tiếp; nguy cơ concept 24/24 khi chấm |
| **TV2** | Tuần 1 Category đầy đủ; tuần 2 code bằng branch (Discovery/Search/Google rất đầy đủ) | (1) Chưa merge tuần 2; (2) không có evidence TUAN_2; (3) nhánh fork cũ **nguy cơ xóa code TV4**; (4) Category **soft-delete trái C07** | Nếu merge khéo sẽ khiến **toàn nhóm mất D1**; nếu không merge thì G3 (search) trễ |
| **TV3** | C1 schema/spike/ADR tốt, Phase-0 đã gỡ wiring cho TV4 | (1) **Toàn bộ C2/C3/C5 tuần 2 chưa có**; (2) PR #10 đặt tên “C2-C3-recipe-api” nhưng giao Phase 0; (3) không evidence TUAN_2 | **Block TV4 D1.3 E2E + D3 + G2/G3** — rào cản lớn nhất hiện tại |
| **TV4** | D1 (storage/validator/domain) + D5 + logout đã merge; D1.3 code xong | (1) D1.3 **chưa commit/push**; (2) test race **flaky**; (3) khai K11/K12 không có code; (4) D2/D4 treo nên tuần 2 mới ~35% | Flaky test → CI thất thường; D1.3 chưa push khiến người khác không dùng được |

**Kết luận chính của phần rà soát chéo:**
1. **Người gây block lớn nhất: TV3** (thiếu C2/C3/C5). Đây là rào cản đường găng: gỡ BL-1/BL-2 thì TV4 mới chạy được D3 và mới có fixture cho mọi thứ platform.
2. **Người có nguy cơ làm mất mã của người khác: TV2** (BL-3) — phải rebase trước khi mở PR, tuyệt đối không force-merge.
3. **Cả nhóm chưa chốt 2 quyết định** (D27 bucket, D23 queue) làm TV4 D2/D4 đứng im — đây là block thuộc về **nhóm**, không phải của riêng TV4.
4. **Ma trận kỹ năng 24/skill đang lạc quan** ở mức 14/24 (TV1) và 8/24 (TV4); cần hạ chuẩn về số thực có code/test rồi bổ sung lab, không để tự khai thay vì minh chứng.
5. **G2 (cuối tuần 2) chưa đạt** — cần xác nhận lại lịch và bù nhanh ở đầu tuần 3, ưu tiên theo thứ tự: (1) TV3 C2/C3 → (2) merge TV2 search/Google (sau rebase) → (3) TV4 D3 publish + commit D1.3 → (4) chốt D27/D23.

---

## 5. Ghi chú quản trị & rủi ro bổ sung

- **Reviewer duy nhất**: toàn bộ PR do TV1 duyệt, kể cả PR của chính TV1. Đã biết đến nhưng nên ghi vào ADR như một rủi ro được chấp nhận (khuyến nghị tối thiểu: một thành viên khác xác nhận “build/test OK” trên PR của TV1).
- **Secret**: `admin123` (Postgres dev) và `REPLACE_WITH_RANDOM_SECRET` (JWT) chỉ ở env dev — hợp lệ; CI dùng credential ephemeral — OK.
- **Frontend**: chưa có Jest/RTL/Playwright, chưa build lại được (không có `node_modules` trên máy kiểm tra) — công việc tuần 4 (K21/K18/K22) đang chờ cài đặt test runner. Không chạy lại `npm run build` ở báo cáo này vì không sửa tài nguyên (không `npm install`).
- **Migration**: hiện 3 migration đã có trên main (Identity, Category, RecipeAggregate). Nhánh TV2 có migration FTS `AddRecipeDiscoveryAndSearch` — thứ tự rebase phải do TV3 điều phối; tránh 2 snapshots trùng.

---

*Báo cáo này chỉ phục vụ kiểm tra/rà soát; không thay đổi bất kỳ mã nguồn, cấu hình hay văn bản hiện có. Mọi con số “test pass” trong file là kết quả chạy lại thực tế ngày 22/09/2026 trên máy kiểm tra.*