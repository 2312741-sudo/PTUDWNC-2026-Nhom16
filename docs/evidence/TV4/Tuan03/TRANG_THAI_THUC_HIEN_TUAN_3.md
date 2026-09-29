# TRẠNG THÁI THỰC HIỆN TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git đề xuất**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3), khởi động từ main đã cập nhật
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 30/09/2026 (T9) — **sửa xong bug upload ảnh `500` + chuẩn hóa nạp `.env` (gỡ block B3)**, thêm 3 test `DevConfigParityTests`, QA tích hợp 41/41, toàn hệ thống **172/172 test xanh** sau khi sync `origin/main`; đóng gói trong **PR #19** đang chờ review. Chi tiết ở mục "Sự cố upload 500" bên dưới.
> Lịch sử: 28/09/2026 (T8) — **N5/D6 Lab L4 xong** (4 phase · 39/39 check PASS, nhánh riêng `practice/TV4/L4`, commit `3642428`) và **CI 5 run đỏ liên tiếp đã gỡ** (nguyên nhân ngoài code: image MinIO bị gỡ khỏi registry) → commit `cd72b27` + `d78e25c`, **CI xanh** run `36344662570`. PR #16 đã merge vào `main`.
> Lịch sử: 27/09/2026 (T7 — chốt đề xuất D23/D27: **D23 → Hangfire PA-1**, **D27 → base media URL proxy PA-2** theo `docs/DE_XUAT_GIAI_QUYET_D23_D27.md`; khớp PR #15 TV3) → **đã implement xong cả hai: D27 proxy (N2) + D2 resize Hangfire (N4)**

> **Bản sửa đổi 28/09 T8 (N5 + sự cố CI)**:
> - **N5/D6 — Lab L4 xong** trên nhánh riêng `practice/TV4/L4` (commit `3642428`, không thêm vào `CulinaryBlog.sln`): `media` 25/25 · `email` 3/3 · `xml` 3/3 · `jobs` 8/8 → **4 phase · 39/39 check PASS** (exit 0). Bổ sung 2 phase chẩn đoán **không tính vào yêu cầu**: `db` (dump bằng chứng bằng Npgsql trong chính process) + `purge` (dọn job kẹt). Sổ K riêng: `Tuan03/SOK_LAB_L4.md`; log: `Tuan03/logs/lab_l4_run.log` + `lab_l4_db.txt`.
> - **Sự cố CI (5 run đỏ liên tiếp, từ `ef358e0` đến `6bbc542`)**: job chết ở bước "Initialize containers" vì service dùng image `quay.io/minio/minio:latest` — MinIO đã **gỡ toàn bộ image public** (`quay.io/minio/minio` → HTTP 401, `minio/minio` Docker Hub → HTTP 404). Không phải lỗi code; mọi bước build/format/test đều bị skip. Đã thay bằng `rustfs/rustfs` (server S3-compatible Apache-2.0) — `MinioStorageService` **không đổi dòng nào**. CI xanh từ run `36343309464`.
> - **Giảm rủi ro lần sau** (`d78e25c`): ghim image theo **tag + digest**, ghim SDK `10.0.401` khớp `global.json`, `timeout-minutes: 30`, `concurrency` huỷ run cũ, `--blame-hang-timeout 10m`.
> - **Cùng nguyên nhân, cảnh báo cho cả nhóm**: `docker-compose.dev.yml` cũng trỏ image MinIO + `minio/mc` (đều đã biến mất) → **máy mới của thành viên khác không dựng được dev stack**. Đã sửa: service `minio` → `s3` (RustFS), bỏ hẳn `minio-init` vì app tự tạo bucket, port 9000/9001 giữ nguyên nên cấu hình app không phải đổi.

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

> N0 (fix migration + CI) đã được gỡ bởi main `a651c8a` (merge vào branch tuần 3). **CI GitHub đã xanh thật từ 28/09** (run `36343309464` + `36344662570`) sau khi gỡ sự cố image storage; còn lại là rà soát PR #14 và phần FE D4.

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
| D4-UI | Uploader/editor ảnh + status buttons | 🔶 **Đổi nguyên nhân 28/09**: D27 **đã xong** nên không còn "chờ D27"; thật ra **PR #15 đã merge vào main** (`b591e74`) nhưng **nhánh này chưa có** (behind 23 / ahead 20) → không thấy `dashboard/recipes/**` để sửa. Rà main: `ImagesStep.tsx` **đã có** upload/delete/set-primary + optimistic rollback + `NEXT_PUBLIC_MEDIA_URL`; **còn thiếu**: thanh progress upload + **nút Unpublish/Archive** (chỉ có Publish trong `ReviewStep.tsx`) | Merge `origin/main` vào nhánh → làm progress + 2 nút status → trả về qua PR (không sửa trực tiếp trên nhánh TV3) |
| ~~D4-SEO~~ | ~~Sitemap/robots/OG/JSON-LD~~ | ✅ **Xong 24/09** — `/sitemap` endpoint Published-only + `sitemap.ts`/`robots.ts`/detail page SEO | — |
| ~~D5~~ | ~~OTEL/metrics/health/EXPLAIN/k6~~ | ✅ **Xong 24/09 + 27/09** — OTEL trace+metrics HTTP→DB, health db/redis/minio, README hướng dẫn; **27/09: EXPLAIN publish query + k6 smoke 20 VU/30s** ghi số liệu (`logs/explain_publish_culinary_test.txt`, `logs/k6_smoke_recipes.log` + `k6_smoke_summary.json`) | — |
| D6 | Lab `practice/TV4/L4` (4 MIME + resize + Mailhog + Hangfire) + sổ K | ✅ **Xong 28/09 (N5)** — commit `3642428`; `media` 25/25 · `email` 3/3 · `xml` 3/3 · `jobs` 8/8 = **4 phase · 39/39 check**; sổ K `SOK_LAB_L4.md` + `README.md`; log `lab_l4_run.log`/`lab_l4_db.txt` | — |
| CI-28/09 | **CI 5 run đỏ** vì image MinIO bị gỡ khỏi registry (401/404) — không phải lỗi code | ✅ **Xong 28/09** — thay `quay.io/minio/minio` bằng `rustfs/rustfs` (S3-compatible), `MinioStorageService` không đổi dòng; ghim tag+digest, SDK, timeout, concurrency, blame-hang; dev compose cũng sửa theo; CI xanh run `36344662570` | — |
| D6-Identity | Phần Identity/Google/refresh/forms/FTS trong `PHAN_CHIA` tuần 3 | ⬜ **Chưa làm** — mục 2 của N5. **Đối chiếu 28/09**: `PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 dòng tuần 3 và `KE_HOACH_DU_AN.md` mục 4.2 đều **yêu cầu** lab Identity/Google/refresh/forms/FTS ngay tuần 3 (không phải tuần 4) → đây là **gap lớn nhất** của tuần 3 chứ không phải việc optional như sổ đang ghi | Cần nhóm trưởng chốt: (a) làm tối giản Identity/refresh/forms + FTS trên dataset thật để kịp hạn, (b) xin ghi nhận dời sang tuần 4 (tuần 4 vốn đã kế hoạch "Hoàn tất LAB") — hoặc (c) chấp nhận thiếu và bù bằng D4/trace/sitemap job |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3.3 logout revoke | ✅ Xong — C5 refresh đã có trên main | — | Đã gỡ |
| **D4-UI (mới 28/09)** | Uploader progress + nút Unpublish/Archive | **Nhánh chưa merge main** (behind 23) → không có `dashboard/recipes/**` (wizard + `ImagesStep.tsx` của PR #15) để sửa | Merge `origin/main` → giải quyết conflict `docker-compose.dev.yml`/workflow → làm D4 |
| **Ảnh storage dev/CI (mới 28/09)** | Nhánh dùng `rustfs/rustfs` (digest), **main dùng `coollabsio/minio:RELEASE.2025-10-15T17-29-55Z`** (TV3 sửa ở `84dddd4`, vẫn giữ `minio-init` + volume `miniodata`) | Hai nhánh chọn 2 image khác nhau → **conflict khi merge**; CI trên main **chưa có** service storage nên E2E storage vẫn **skip âm thầm** | ✅ **Đã chốt + gỡ (PR #16)**: chọn **`rustfs/rustfs`** cho dev + CI (S3-compatible, Apache-2.0, ghim tag+digest `sha256:8cc9801.`); `docker-compose.dev.yml` + `.github/workflows/backend.yml` cùng dùng image này, bỏ `minio-init` (app tự tạo bucket), `MinioStorageService` **không đổi dòng nào**. Chi tiết: `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` |
| **Fix `4830e57` (mới 28/09)** | Đọc connection string trong lambda `AddDbContext` (chặn 28P01 khi `TEST_DATABASE` bị override) | **Chưa có trên main** (`git merge-base --is-ancestor` → false) → CI main có nguy cơ đỏ | PR lên main (TV1/nhóm trưởng duyệt) |
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
| CR-6 (mới 28/09) | **Image object storage cho dev + CI** | MinIO đã gỡ image public; nhánh TV4 dùng `rustfs/rustfs`, main dùng `coollabsio/minio` | ✅ **Đã chốt 28/09 (PR #16): `rustfs/rustfs` cho cả dev + CI**, ghim tag+digest; `MinioStorageService`/tên env giữ nguyên; `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`. **Production vẫn phải dùng storage có license (không phải RustFS)** |
| CR-7 (mới 28/09) | **Sitemap theo lịch 02:00 UTC + distributed lock** | `KE_HOACH_DU_AN.md` mục 8 và N2 item 4 yêu cầu cron 02:00 UTC + "nhiều worker cùng chạy sitemap phải có distributed lock"; hiện chỉ có `GET /recipes/sitemap` on-demand + `sitemap.ts`/`robots.ts` (chưa merge main), **không có job theo lịch** | ⬜ Chốt: dùng Hangfire recurring (đã có sẵn storage + `WorkerCount=4`) để khớp đề, hay chấp nhận sitemap sinh theo request/ISR và ghi rõ giới hạn |

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

### Chi tiết E2E MinIO (`tests/CulinaryBlog.Tests/MinioE2ETests.cs`)

- **Flow**: đăng ký → tạo recipe (201) → upload ảnh JPEG (201) → đọc lại (chiều xiêm + slug) → PATCH primary → publish → unpublish → archive → delete → chưa còn trong public list.
- **Điều kiện chạy**: MinIO reachable (TCP + health) nếu không → skip (CI cũng skip khi không có MinIO); postgres local `culinary_test`.
- **Bài học E2E thật**: (1) token từ register (không phải login) mới mang role; (2) enum dạng int (không có `JsonStringEnumConverter`); (3) multipart phải set `ContentType` để `IFormFile.ContentType` đúng → validator magic bytes pass.
- **CI**: `.github/workflows/backend.yml` đã có service object storage (từ 28/09 là **RustFS** thay MinIO — xem mục "Sự cố CI" bên dưới) + env `MINIO_*` + bước chờ health.

### Lab L4 — N5/D6 (28/09, nhánh `practice/TV4/L4`)

| Hạng mục | Kết quả |
|---|---|
| Hình thức | Console app `practice/TV4/L4` (.NET 10), tham chiếu `Application` + `Infrastructure` để dùng **đúng đường code thật** (`MinioStorageService`, `RecipeImageKeys`, `AuthDbContext`); **không** thêm vào `CulinaryBlog.sln`, không sửa code sản phẩm |
| `media` (25/25) | MIME nhận diện theo **magic bytes** (JPEG/PNG/WebP/AVIF); file MIME giả → `file.invalid_type`; ảnh vượt giới hạn → `file.too_large`; upload + đọc lại khớp byte; resize `ResizeMode.Max` → `300×200` / `800×533`; AVIF fallback giữ original; idempotent; dọn dẹp sạch |
| `email` (3/3) | MailKit gửi plain + HTML qua SMTP Mailhog `127.0.0.1:1025`; đếm trước/sau qua Mailhog API; đối chiếu đúng subject (MailKit MIME-encode tiếng Việt) |
| `xml` (3/3) | `sitemap.xml` sinh từ DB thật `culinary_test` (chỉ đọc) — **93 URL Published**; parse lại bằng `XDocument` |
| `jobs` (8/8) | Hangfire + PostgreSQL (`culinary_lab`), worker trong chính process: fire-and-forget `Succeeded`; **tắt worker → job vẫn `Scheduled` trong DB → chạy khi restart**; retry quan sát được `Retry attempt 1,2 of 5` rồi `Succeeded`; recurring do scheduler kích hoạt chạy 2 lần; `RemoveIfExists` → `hangfire.hash` trống, `jobqueue` 0 dòng |
| Phase bổ sung (không tính vào yêu cầu) | `db` — dump bằng chứng bằng Npgsql trong chính process lab (20 job đều `Succeeded`); `purge` — dọn 24 job `Enqueued` mồ côi còn sót từ các lần chạy trước |
| Tổng | **4 phase · 39/39 check PASS**, exit `0`; lab build Release **0 warning** (repo bật `TreatWarningsAsErrors`), `dotnet format` sạch, suite **148/148 + 5/5** |
| Sổ K + log | `Tuan03/SOK_LAB_L4.md` (kết quả từng phase, lỗi gặp, giới hạn), `practice/TV4/L4/README.md`, `Tuan03/logs/lab_l4_run.log`, `Tuan03/logs/lab_l4_db.txt` (không secret) |
| Lỗi thật lab đã tìm + sửa | ① ctor `Hangfire.PostgreSql` obsolete + `TreatWarningsAsErrors` → `NpgsqlConnectionFactory` + `JobStorage.Current`; ② **queue mismatch** (`BackgroundJob.Enqueue` mặc định `default` còn worker nghe `lab` → job kẹt `Enqueued` mãi; `RecurringJobOptions` không có `Queue`); ③ subject Mailhog ở `items[].Content.Headers.Subject`; ④ xoá object phải qua `IFileStorageService` (`IObjectStorageWriter` chỉ `Exists`/`Upload`); ⑤ bẫy encoding PowerShell 5.1 làm hỏng tiếng Việt |

### Sự cố CI — image storage biến mất khỏi registry (28/09)

| Hạng mục | Kết quả |
|---|---|
| Triệu chứng | 5 run đỏ liên tiếp (`ef358e0` → `6bbc542`); job **chết ở bước 2 "Initialize containers"**, các bước restore/build/format/test đều `skipped` |
| Nguyên nhân | Service dùng `quay.io/minio/minio:latest`; MinIO đã **gỡ toàn bộ image public**: `quay.io/minio/minio` → **HTTP 401**, `minio/minio` (Docker Hub) → **HTTP 404 repository không tồn tại** → runner không pull được image. **Không phải lỗi code** |
| Sửa (lần 1 — `cd72b27`) | Service `minio` → `objectstorage`, image `rustfs/rustfs:1.0.0` (S3-compatible Apache-2.0), env `RUSTFS_*`, health-cmd `curl /health` (RustFS **không** phục vụ `/minio/health/live`); `MinioStorageService` **không đổi dòng nào** (6 thao tác `BucketExists`/`MakeBucket`/`PutObject`/`GetObject`/`StatObject`/`RemoveObject` hoạt động y hệt) |
| Sửa (lần 2 — `d78e25c`) | Sửa luôn `docker-compose.dev.yml` (cùng bệnh, **máy mới của thành viên khác không dựng được dev stack**): service `minio` → `s3`, bỏ hẳn `minio-init` vì app tự `BucketExists → MakeBucket`, volume `miniodata` → `s3data`, port 9000/9001 giữ nguyên; `.env.example` + README + `HUONG_DAN_TEST_APP.md` + comment `MinioE2ETests.cs` cập nhật theo |
| Giảm rủi ro lần sau | Ghim image theo **tag + digest** `sha256:8cc9801…`; ghim SDK `10.0.401` khớp `global.json` (SDK mới có thể làm đổi kết quả `dotnet format`/warning); `timeout-minutes: 30`; `concurrency` + `cancel-in-progress` (push liên tiếp không tốn runner); `--blame-hang-timeout 10m` (test treo bị bắt dump thay vì treo hết giờ) |
| Bằng chứng | Local: **148/148 + 5/5, `Skipped=0`** trên **cả hai** đường — endpoint CI và service `s3` thật từ compose (`127.0.0.1:9000`, console 9001 trả 200), bucket `culinary-blog/recipes` có thật trong storage → E2E MinIO chạy chứ không skip. CI: run đỏ `36338124928` → **xanh `36343309464`** (sửa lần 1) → **xanh `36344662570`** (sửa lần 2), cả 10 bước `success` |

### Đã fix khi làm D4/D5 (đã trong working tree)

| Nội dung | Nơi |
|---|---|
| `GetSitemapQuery`/`GetPublishedForSitemapAsync` — chỉ Published, không Draft/Archived/Deleted | `Recipes.cs`, `Discovery.cs`, `RecipeRepository.cs` |
| Endpoint `GET /recipes/sitemap` Published-only | `Program.cs` |
| `ArchiveRecipeCommand`/`DeleteRecipeCommand` + `Recipe.MarkDeleted()` (soft D08) | `Recipes.cs`, `Recipe.cs` |
| OTEL trace + metrics (ASP.NET/HttpClient/EF) | `Program.cs` + `CulinaryBlog.API.csproj` + `packages.lock.json` |
| robots.txt + sitemap.xml + SEO metadata trang công thức | `src/frontend/src/app/robots.ts`, `sitemap.ts`, `app/recipes/[slug]/` |

### Sự cố upload ảnh trả `500` — điều tra, sửa và chuẩn hóa `.env` (30/09 T9)

| Nội dung | Chi tiết |
|---|---|
| **Triệu chứng** | `POST /api/v1/recipes/{id}/images` trả `500 server.error`, dù `GET /resources/images/{key}` (D27 proxy) chạy được. Lỗi xuất hiện sau khi cài máy mới / đổi môi trường. |
| **Nguyên nhân gốc** | Section cấu hình `Minio`/S3 **không được nạp** → `AccessKey`/`SecretKey` rỗng → RustFS trả `401` → service ném exception → middleware bọc thành `500` chung. Không phải lỗi logic upload, không phải do đổi MinIO → RustFS. |
| **Sửa cấu hình** | Bổ sung section `Minio` cho `appsettings.Development.json`; chuẩn hóa default chuỗi PostgreSQL về `postgres` để khớp `docker-compose.dev.yml`; `CulinaryBlog.Infrastructure/appsettings.json` dùng `postgres` (còn file local dùng `admin123` theo volume cũ — không commit). |
| **Nạp `.env` (B3)** | Thêm `DotNetEnv` 3.2.0 + `src/backend/CulinaryBlog.API/EnvFileLoader.cs`, gọi **trước** `WebApplication.CreateBuilder(args)`. Thứ tự ưu tiên: biến môi trường/CI > `.env` > `appsettings`. Production **không** nạp `.env`. |
| **File mới** | `.env.example` (chỉ placeholder), cập nhật `.gitignore` + `.dockerignore`; file `.env` thật chứa credential của TV4 được gitignore, **không** đưa vào commit. |
| **Test chống hồi quy** | `tests/CulinaryBlog.Tests/DevConfigParityTests.cs` — 3 test đối chiếu `appsettings.Development.json` với biến môi trường và nội dung `.env` cho chuỗi kết nối PostgreSQL và object storage. |
| **QA tích hợp FE–BE** | **41/41 PASS** — xem `Report/TEST_CASE_TICH_HOP_FE_BE.md`. |
| **Kết quả kiểm chứng** | `dotnet build` Release **0 warning / 0 error**; `dotnet format --verify-no-changes` exit 0; `dotnet test` **167/167 + 5/5**, `Skipped=0` (tổng **172/172**) sau khi merge `origin/main`; `npx tsc --noEmit` và `npm run build` exit 0; `docker compose config --quiet` exit 0. |
| **Bàn giao** | Commit `b833aa3` (cấu hình + fix) và `997ef5f` (merge `origin/main`), đã push lên `2312739_NHTSon_D3-D4-D5-D6`; mở **PR #19** — chờ review, **chưa merge**. |
| **Tài liệu** | Báo cáo trước/sau: `docs/report/BAO_CAO_LOI_UPLOAD_ANH_500.md` và `docs/report/BAO_CAO_LOI_UPLOAD_ANH_500_DA_SUA.md`; đề xuất B1–B6 trong `docs/proposal/`; tổng hợp block tại `Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`. |
| **Còn lại** | B1 (`500` → `503 storage.unavailable`), B2 (fail-fast khi thiếu cấu hình), B4, B5, B6 — **chờ quyết định nhóm**. |

---

## 7. Đối chiếu với kế hoạch dự án + `PHAN_CHIA_CONG_VIEC_6_TUAN.md` (28/09)

> Nguồn đối chiếu: `docs/KE_HOACH_DU_AN.md` mục 4.2 (dòng tuần 3) + mục 8 (task D1–D7, dòng 339 về sitemap/backup) + `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 dòng tuần 3 và dòng tuần 4. Rà bằng `git` trên `origin/main` (commit `b591e74`).

### 7.1. Task D1–D7

| Task | Yêu cầu tuần 3 theo kế hoạch | Trạng thái 28/09 | Còn thiếu |
|---|---|---|---|
| D1 | Upload/magic bytes/size/GUID path/primary/delete | ✅ Xong (tuần 2 + E2E 3/3, lab 25/25) | — |
| D2 | Resize 300×300/800×600 + retry/race | ✅ Xong 27/09 (Hangfire PA-1, E2E 4/4) | AVIF chạy tay nhánh resize (đã có test) |
| D3 | Archive/delete theo D08 + logout revoke family | ✅ Xong 23–24/09 | — |
| D4 | Sitemap/robots/OG/JSON-LD **+ uploader UI + status buttons** | 🟡 SEO xong; **UI chưa** | ⚠️ progress upload + nút Unpublish/Archive; ⚠️ cron 02:00 UTC + distributed lock sitemap (CR-7) |
| D5 | OTEL/metrics/health (+ EXPLAIN/k6) | 🟡 Cấu hình + số liệu xong | ⚠️ **chưa có bằng chứng trace thật vào Seq**; ⚠️ chưa có bằng chứng `/health/ready` 503 khi Redis down (PHAN_CHIA xếp "health failure" ở tuần 4) |
| D6 | Lab Identity/Google/refresh/**forms/FTS** | 🟡 Mục 1 xong (39/39) | ⚠️ **Mục 2 chưa làm — yêu cầu của tuần 3** (xem 7.3) |
| D7 | Integration/UI/publish E2E, resilience/load, runbook | ⬜ Theo `PHAN_CHIA` thuộc **tuần 4** | Chưa đến hạn tuần 3 |

### 7.2. Cổng G5 cuối tuần 3 (theo sổ + `KE_HOACH_DU_AN` mục 4.2)

| Tiêu chí cổng | Kết quả |
|---|---|
| Đủ FR media (upload/ảnh phái sinh/proxy) | ✅ |
| Status (archive/delete) | ✅ API + test; ❌ **UI Unpublish/Archive** |
| Jobs (queue persistent) | ✅ Hangfire + PostgreSQL, job thật `Succeeded`, restart không mất job |
| Health | ✅ `/health`, `/health/live`, `/health/ready` (db/redis/storage) — thiếu bằng chứng failure |
| Sitemap/OG/JSON-LD | ✅ Published-only; ⚠️ thiếu job theo lịch 02:00 UTC |
| OTEL trace HTTP→DB | ❌ **chưa chụp trace thật** (mới có cấu hình) |
| CI xanh | ✅ run `36344662570` |
| → Kết luận | **6/8 đạt**; 2 mục còn lại đều **làm được ngay** (không cần người khác) trừ UI phụ thuộc merge main |

### 7.3. Khoảng cách so với yêu cầu của tuần 3 (xếp theo mức ưu tiên)

| # | Việc còn lại | Vì sao chưa làm | Đề xuất |
|---|---|---|---|
| 1 | **Lab mục 2** (Identity/Google/refresh/forms/FTS) | Chưa bắt tay; sổ từng ghi "để tuần 4" | Làm tối giản (Identity/refresh rotation + form validation + FTS trên DB thật) hoặc **xin nhóm trưởng ghi nhận dời tuần 4** — không nên để im vì đề yêu cầu ở tuần 3 |
| 2 | **Merge `origin/main` → làm D4 UI** | Nhánh behind 23 → không có wizard/`ImagesStep.tsx` | Merge, xử lý conflict compose/workflow, thêm progress + 2 nút status, mở PR |
| 3 | **Trace thật vào Seq** (K20) | Cần bật `seq` trong compose + gửi OTLP | Làm luôn tuần 3: hạ tầng đã có, chỉ thiếu bằng chứng |
| 4 | **Sitemap job 02:00 UTC + distributed lock** (CR-7) | Chốt chưa xong giữa cron lịch và on-demand | Hangfire recurring (đã có `WorkerCount=4`) hoặc ghi giới hạn trong sổ |
| 5 | **Bằng chứng health khi dependency chết** (D22) | Chưa chạy | Tuần 4 theo `PHAN_CHIA` (`health failure`) |
| 6 | **K01 — bảng mapping FR ↔ ADR ↔ evidence** | Bảng kỹ năng vẫn ghi "Chưa làm" | Rẻ, làm ngay — reviewer Tâm cần để nghiệm thu |
| 7 | **K24 — PR cho lab + PR `4830e57` lên main + rà soát diff PR #14** | Chờ duyệt | `PHAN_CHIA` yêu cầu "có PR lab ngoài phần chính": hiện mới có **branch** `practice/TV4/L4` đã push, chưa mở PR |
| 8 | **Backup/restore, multi-instance, load** | Theo kế hoạch ở tuần 4–5 | Không tính vào gap tuần 3 |

### 7.4. Cảnh báo lịch

Khối lượng còn lại của tuần 3 (**4 việc tự làm được + 1 việc cần merge main + 1 việc cần quyết định của nhóm**) lớn hơn phần thời gian còn lại nếu làm tuần tự. Đề xuất cho nhóm trưởng chốt theo thứ tự: **(2) merge main + D4 UI** → **(3) trace Seq** → **(4) sitemap job** → **(6) K01** → còn lại cân nhắc dời tuần 4 vì `PHAN_CHIA` dòng tuần 4 vốn đã ghi "Hoàn tất LAB; retry/race/security/publish E2E; backup/restore, health failure, shared cache/multi-worker".