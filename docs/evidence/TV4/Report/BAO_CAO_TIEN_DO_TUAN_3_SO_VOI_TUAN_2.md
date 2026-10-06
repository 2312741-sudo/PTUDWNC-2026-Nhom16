# BÁO CÁO TIẾN ĐỘ TUẦN 3 SO VỚI TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh chính tuần 3**: `2312739_NHTSon_D3-D4-D5-D6` (đã merge `origin/main`, đã mở & merge PR #16)
> **Nhánh lab**: `practice/TV4/L4` (commit `3642428`, đã push)
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Khoảng thời gian**: 23/09/2026 – 28/09/2026
> **Nguồn đối chiếu**: `Tuan02/TRANG_THAI_THUC_HIEN_TUAN_2.md`, `KE_HOACH_TUAN_3_TV4.md`, `TRANG_THAI_THUC_HIEN_TUAN_3.md`, `SO_EVIDENCE_TUAN_3.md`, `HANDOFF_TV4_TUAN3.md`, lịch sử `git` trên `origin/main`.

> [!IMPORTANT]
> **Phạm vi file này**: CHỈ ghi phần **đã làm thêm trong tuần 3 so với tuần 2**.
> Những task đã xong ở tuần 2 (D1.1, D1.2, D1.3, D1.5, D1.6, D3.1/D3.2, D3.3, 120/120 test) **không lặp lại** ở đây — xem `Tuan02/TRANG_THAI_THUC_HIEN_TUAN_2.md`.

---

## 1. Delta trạng thái task D1–D7 (cuối tuần 2 → cuối tuần 3)

| Task | Cuối tuần 2 | Cuối tuần 3 | Bản chất thay đổi |
|---|---|---|---|
| D1.1 | Xong (MinioStorageService) | Xong, **bổ sung** `ReadAsync` copy đồng bộ (fix stream cắt cụt) | Sửa lỗi |
| D1.1c | Chưa — chờ chạy integration storage | **Xong** — E2E storage thật 3/3 + CI có service storage + `Skipped=0` | 🆕 Đóng block |
| D1.2 | Xong (16 test) | Không đổi | — |
| D1.3 | Xong nhưng E2E cần seed recipe | **Xong hoàn chỉnh** — E2E thật trên storage + proxy ảnh + resize nền | Mở rộng |
| D1.5 | Xong (9 test + race spike) | Không đổi | — |
| D1.6 | Xong (`IMAGE_CONTRACT.md`) | **Bổ sung §5 (D27 proxy) + §7 (D2 resize)** | Mở rộng |
| **D1.7 / D27** | 🔴 Block — bucket policy chưa chốt | ✅ **Xong 27/09** — chốt PA-2 proxy có auth, E2E 6/6 | 🆕 Gỡ block |
| **D2 resize** | 🔴 Block — queue chưa chốt (D23) | ✅ **Xong 27/09** — chốt PA-1 Hangfire, job thật `Succeeded`, E2E 4/4 | 🆕 Gỡ block |
| D3.1/D3.2 | Xong (trong main) | Không đổi | — |
| **D3.1c Archive** | ⬜ Chưa làm | ✅ **Xong 24/09** — `PATCH /recipes/{id}/archive` + test | 🆕 |
| **D3.2c Delete** | ⬜ Chưa làm | ✅ **Xong 24/09** — soft delete D08 (`Recipe.MarkDeleted()`) + test | 🆕 |
| D3.3 logout | Xong (C5 có sẵn trên main) | Không đổi | — |
| **D3 invalidation** | ⬜ Chưa định nghĩa | ✅ **Đóng 27/09 bằng xác minh** — không tồn tại cache recipe nào | 🆕 |
| **D4-SEO** | ⬜ Chưa bắt đầu (FE chưa có) | ✅ **Xong 24/09** — sitemap Published-only + robots + canonical/OG/JSON-LD | 🆕 |
| **D4-UI** | ⬜ Chưa bắt đầu | ⬜ **Vẫn chưa** — thiếu thanh progress upload + nút Unpublish/Archive | ⬜ Chưa |
| **D5** | ⬜ Chưa (chỉ Serilog/Seq + `/health`) | ✅ **Xong** — OTEL trace+metrics + health + **số liệu EXPLAIN & k6 thật** | 🆕 |
| **D6 Lab L4** | ⬜ Chưa bắt đầu | ✅ **Xong 28/09 (mục 1)** — 4 phase · 39/39 check | 🆕 |
| **D6 mục 2** | (chưa tới hạn tuần 2) | ⬜ **Chưa làm** — Identity/Google/refresh/forms/FTS (**là yêu cầu của tuần 3**) | 🆕 Gap |
| D7 | ⬜ Thuộc tuần 4 theo kế hoạch | ⬜ Chưa (đúng hạn) | — |

**Điểm mấu chốt**: 4 block lâu nhất của tuần 2 (D1.1c, D27, D23, D2) đều đã gỡ trong tuần 3.

---

## 2. Việc hoàn thành MỚI trong tuần 3

### 2.1 N0 — Mở đầu: gỡ block CI + đồng bộ với `main`

| Việc | Chi tiết |
|---|---|
| Kiểm chứng fix migration trùng `RefreshTokens` của main | main `a651c8a` xoá 2 migration trùng; TV4 merge `e026dc9`→`75a8bf5`, chạy lại: build Release 0 warning, `dotnet format` sạch, **120/120 + spike 5/5** (16 test Auth/Week3 trước đây fail nay pass). Fix do main thực hiện, TV4 kiểm chứng. |
| **Fix connection string eager-read** (TV4 tự làm) | Commit `4830e57`: chuyển đọc connection string vào **lambda `AddDbContext`** để biến `TEST_DATABASE` override có hiệu lực (trước đó đọc ở top-level làm 18 test fail `28P01`). **Chưa có trên main → chờ PR.** |
| CI GitHub branch tuần 3 | ✅ xanh (run `818522b`) |

### 2.2 D3 — Archive + soft delete (N1, 24/09)

- `ArchiveRecipeCommand` + `PATCH /recipes/{id}/archive`: Published/Draft → Archived, ẩn public ngay, owner/Admin (403 non-owner), idempotent.
- `DeleteRecipeCommand` + `Recipe.MarkDeleted()`: **soft delete theo ADR D08**, global query filter ẩn khỏi mọi truy vấn, **không xoá vật lý ảnh** cần restore.
- Test: `tests/CulinaryBlog.Tests/RecipeLifecycleTests.cs` (+117 dòng) → **133/133 pass**.
- Sửa 1 lỗi do TV3 phát hiện (`e3e8315`): entity con mới bị EF đánh dấu `Modified` do Guid key khác rỗng → 422 giả; interceptor chuyển `Added` khi `RowVersion` gốc rỗng.

### 2.3 D3 invalidation — đóng bằng xác minh (27/09)

- Kết luận: **không tồn tại cache recipe nào** cần invalidate → `Program.cs` không có `AddOutputCache`/`IDistributedCache` dữ liệu (chỉ `RedisHealthCheck`); FE `src/frontend/src/lib/api.ts` recipe list/search/detail/sitemap đều `cache: 'no-store'`; `RecipeCacheService` (in-memory) **orphan, chưa wire DI**.
- Tiêu chí "Draft/Archived không được phục vụ bởi cache cũ" **thỏa mặc định**.
- Đã ghi handoff cho TV2/TV3: nếu sau này thêm ISR/output-cache cho recipe thì phải nối revalidate hook.

### 2.4 D4-SEO — sitemap / robots / metadata (24/09)

- `GetSitemapQuery` + `GetPublishedForSitemapAsync` + `GET /recipes/sitemap`: **chỉ Published**, loại Draft/Archived/soft-deleted.
- FE: `src/frontend/src/app/sitemap.ts`, `robots.ts`, SEO metadata + canonical + OG ở `app/recipes/[slug]/`.
- `npx next build` exit 0; test sitemap Published-only trong `DiscoveryAndSearchTests.cs` (+35 dòng).

### 2.5 D27 — Proxy ảnh có auth PA-2 (N2, 27/09) — **gỡ block 2.3**

- Chốt hướng: `docs/DE_XUAT_GIAI_QUYET_D23_D27.md` → **PA-2: base media URL qua proxy có auth**.
- Endpoint `GET /api/v1/resources/images/{**key}`:
  - Published → công khai + `Cache-Control: public, max-age=3600`
  - Draft/Archived → chỉ owner/Admin, nếu không `403 image.forbidden` (`no-store`)
  - key sai / ảnh không tồn tại / recipe soft-deleted → `404 image.not_found`
- `IObjectStorageReader` + `MediaContent` + `ReadAsync` **tách khỏi `IFileStorageService`/`StoredFile`** (giữ nguyên contract bàn giao TV3).
- Contract chốt tại `docs/IMAGE_CONTRACT.md §5`.
- Test `tests/CulinaryBlog.Tests/ImageProxyD27Tests.cs` → **6/6 pass**; suite **139/139 + 5/5**.

### 2.6 D2 — Resize ảnh Hangfire PA-1 (N4, 27/09) — **gỡ block 2.4**

- Chốt hướng: **PA-1 Hangfire** (queue persistent PostgreSQL + dashboard Admin + retry 3), khớp SRS tr.38.
- Package: `Hangfire.AspNetCore 1.8.25` + `Hangfire.PostgreSql 1.21.1` + `SixLabors.ImageSharp 3.1.11`; regenerate `packages.lock.json`.
- `ResizeImageJob`: đọc original → resize `ResizeMode.Max` 300×300 / 800×600 → ghi `IObjectStorageWriter` với key chủ động `{base}_300x300.ext` / `{base}_800x600.ext` → cập nhật `MediumUrl`/`ThumbnailUrl`.
- Bất biến đã kiểm chứng: **idempotent** (`ExistsAsync`), **delete-vs-resize** (row biến mất → không tái sinh ảnh đã xoá), **original fallback** (ảnh hỏng/AVIF → giữ `null`, không 5xx), xoá ảnh → xoá luôn 2 object phái sinh, `[AutomaticRetry(Attempts = 3)]`.
- `IImageResizeQueue` (Application) → `HangfireImageResizeQueue` / `InlineImageResizeQueue` (Testing) để test xác định.
- Dashboard `/hangfire` **chỉ Admin** (`AdminDashboardAuthorizationFilter`): anon 401 / member 403 / Admin 200.
- Test `ImageResizeD2Tests.cs` **4/4** + 5 unit `RecipeImageTests`; **chạy thật**: API + storage + Postgres → upload JPEG 1200×800 → **Hangfire job `Succeeded` (~1s)** → proxy trả `300×200`, `800×533`; suite **148/148 + 5/5** (2 vòng).
- Contract: `docs/IMAGE_CONTRACT.md §7`.

### 2.7 D5 — OTEL/metrics/health + số liệu EXPLAIN & k6 (N3, 24/09 + 27/09)

- Xác minh cấu hình OTEL còn nguyên (commit `2bbee0d`): tracing ASP.NET/HttpClient/EF Core/OTLP; metrics ASP.NET/HttpClient/EF meter; lock 1.19.x.
- Health `/health`, `/health/live`, `/health/ready` (db/redis/storage).
- **Số liệu thật ghi vào `Tuan03/logs/`** (không secret):

| Đo | Kết quả |
|---|---|
| EXPLAIN publish list (page 1, size 12) | `Execution Time: 0.339 ms`, 12 rows, Seq Scan (50/66 rows — hợp lý ở quy mô dữ liệu test); index `IDX_Recipe_Status/IsDeleted/PublishedAt` đã có |
| EXPLAIN count published | `Execution Time: 0.044 ms` |
| k6 smoke 20 VU × 30s (`/recipes?page=1&pageSize=12` + `/categories`) | **3310 req · 0% fail · check 100% · avg 81.58ms · p90 193.45ms · p95 225.63ms (<250) · 109.42 req/s** |

Log: `logs/explain_publish_culinary_test.txt`, `logs/k6_smoke_recipes.log`, `logs/k6_smoke_summary.json`.

### 2.8 D6 — Lab L4 (N5, 28/09) — commit `3642428`, nhánh `practice/TV4/L4`

- Hình thức: console app .NET 10 **độc lập**, tham chiếu `Application` + `Infrastructure` để dùng **đúng đường code thật**; **không** thêm vào `CulinaryBlog.sln`, **không** sửa code sản phẩm.
- **4 phase · 39/39 check PASS** (exit 0):

| Phase | Kết quả |
|---|---|
| `media` 25/25 | MIME nhận diện theo **magic bytes** (JPEG/PNG/WebP/AVIF); file MIME giả → `file.invalid_type`; vượt giới hạn → `file.too_large`; upload + đọc lại khớp byte; resize `ResizeMode.Max` → 300×200 / 800×533; AVIF fallback giữ original; idempotent; dọn dẹp sạch |
| `email` 3/3 | MailKit gửi plain + HTML qua SMTP Mailhog `127.0.0.1:1025`; đếm trước/sau qua Mailhog API; đối chiếu đúng subject |
| `xml` 3/3 | `sitemap.xml` sinh từ DB thật `culinary_test` (chỉ đọc) — **93 URL Published**; parse lại bằng `XDocument` |
| `jobs` 8/8 | Hangfire + PostgreSQL (DB riêng `culinary_lab`): fire-and-forget `Succeeded`; **tắt worker → job còn `Scheduled` trong DB → chạy khi restart**; retry quan sát được (`Retry attempt 1,2 of 5`) rồi `Succeeded`; recurring do scheduler kích hoạt chạy 2 lần; `RemoveIfExists` → `hangfire.hash` trống, `jobqueue` 0 dòng |

- Thêm 2 phase chẩn đoán **không tính vào yêu cầu**: `db` (dump bằng chứng bằng Npgsql trong chính process: 20 job `Succeeded`) + `purge` (dọn 24 job `Enqueued` mồ côi).
- Sổ K: `Tuan03/SOK_LAB_L4.md` (nằm trên nhánh `practice/TV4/L4`) + `practice/TV4/L4/README.md`; log `Tuan03/logs/lab_l4_run.log`, `lab_l4_db.txt`.
- Verify: lab build Release 0 warning (repo bật `TreatWarningsAsErrors`), `dotnet format` sạch, solution **148/148 + 5/5**.

### 2.9 D1.1c / E2E storage thật (24/09)

- `tests/CulinaryBlog.Tests/MinioE2ETests.cs` + `ApiFactoryWithMinio`: **3/3 pass**, lặp nhiều lần ổn định.
- Flow: register → create recipe (201) → upload JPEG (201) → readback → PATCH primary → publish → unpublish → archive → delete → không còn trong public list.
- Storage down → **skip an toàn** (không fail) + log không lộ secret.

### 2.10 Gỡ sự cố CI: image object storage bị gỡ khỏi registry (28/09)

| Hạng mục | Kết quả |
|---|---|
| Triệu chứng | **5 run đỏ liên tiếp** (`ef358e0` → `6bbc542`); job chết ở bước "Initialize containers", mọi bước restore/build/format/test đều `skipped` |
| Nguyên nhân | **Ngoài code**: `quay.io/minio/minio` → HTTP 401, `minio/minio` (Docker Hub) → HTTP 404 (MinIO đã gỡ toàn bộ image public) |
| Sửa lần 1 (`cd72b27`) | Workflow: service `minio` → `objectstorage`, image `rustfs/rustfs:1.0.0` (S3-compatible, Apache-2.0), env `RUSTFS_*`, health-cmd `/health` (RustFS **không** phục vụ `/minio/health/live`). `MinioStorageService` **không đổi dòng nào**. CI xanh run `36343309464` |
| Sửa lần 2 (`d78e25c`) | `docker-compose.dev.yml` cũng hỏng trên máy mới của thành viên khác: service `minio` → `s3`, bỏ `minio-init` (app tự `BucketExists → MakeBucket`), volume `miniodata` → `s3data`, port 9000/9001 giữ nguyên; `.env.example` + `README` + `HUONG_DAN_TEST_APP.md` cập nhật. **CI xanh run `36344662570`** |
| Hardening CI | Ghim image **tag + digest** `sha256:8cc9801…`; ghim SDK `10.0.401` khớp `global.json`; `timeout-minutes: 30`; `concurrency` + `cancel-in-progress`; `--blame-hang-timeout 10m` |
| Minh chứng không bỏ trống | Local **148/148 + 5/5, `Skipped=0`** trên **cả hai** đường (endpoint CI và service `s3` thật từ compose), bucket `culinary-blog/recipes` có thật → E2E storage thực sự chạy chứ không skip |

### 2.11 PR #16 — merge `origin/main` + chốt CR-6 (RustFS cho dev + CI)

- **CR-6 chốt**: dùng `rustfs/rustfs` cho **cả dev compose + CI**, ghim tag+digest ở cả hai; bỏ `minio-init`; giữ nguyên tên env `MINIO_*` và `MinioStorageService` → **cấu hình app của mọi thành viên không phải đổi**. ADR: `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`.
- Gỡ **7 conflict** khi merge `origin/main` (TV3 đã đổi dev image sang `coollabsio/minio` trong `84dddd4`):
  - `docker-compose.dev.yml` — giữ RustFS, bỏ `coollabsio/minio` + `minio-init`
  - `Program.cs` — bỏ DELETE trùng route, **giữ bản có `If-Match`/`RowVersion` → 422** + `NameClaimType=sub`
  - `Recipes.cs` — giữ `ArchiveRecipeCommand` (TV4) + `DeleteRecipeCommand(id, rowVersion)` (TV3)
  - `Entities/Recipe.cs` — giữ `MarkDeleted` + alias `SoftDelete`
  - FE `api.ts` / `RecipeCard.tsx` / `recipes/[slug]/page.tsx` — giữ JSON-LD/canonical/OG (D4) + UI của main
- Kết quả verify: build 0 warning · test `Skipped=0` · `tsc --noEmit` exit 0 · `next build` exit 0 (16/16 trang) · `docker compose config --quiet` exit 0, container `culinaryblog-s3` healthy.
- Sửa default `TEST_DATABASE` của `AuthTests`/`SpikeDbFixture` cho khớp `POSTGRES_PASSWORD` của dev compose.

### 2.12 Sửa lỗi 500 upload ảnh + nạp `.env` + test chống tái diễn (28/09)

Phát hiện khi ghép FE + BE + hạ tầng thật trên `main`.

| Hạng mục | Nội dung |
|---|---|
| Nguyên nhân gốc | `appsettings.Development.json` **thiếu section `Minio`** ⇒ `MinioOptions` giữ default rỗng ⇒ storage trả `401 UnauthorizedAccess` ⇒ `MinioException` không khớp nhánh nào trong `ApiExceptionHandler` ⇒ rơi vào generic **500 `server.error`** |
| Nguyên nhân thứ hai cùng lớp (báo cáo gốc chưa nêu) | Mật khẩu PostgreSQL lệch giữa `appsettings.Development.json` (`postgres`) và `docker-compose.dev.yml` (`admin123`) ⇒ `28P01` ở **mọi** endpoint cần DB |
| Sửa | Thêm `Minio` vào `appsettings.Development.json` (dev-only, lấy đúng credential đã có sẵn trong `docker-compose.dev.yml`, **không** đụng `appsettings.json`); đưa default trong repo về `postgres`; tạo `.env` gitignored chứa giá trị thật của máy; thêm `EnvFileLoader.cs` nạp `.env` trước `CreateBuilder` với 2 chốt chặn (bỏ qua `Production`, không ghi đè biến đã có); chặn `.env` ở `.gitignore` **và** `.dockerignore` |
| Test chống tái diễn | `tests/CulinaryBlog.Tests/DevConfigParityTests.cs` — 3 test khoá parity `appsettings.Development.json` ↔ `docker-compose.dev.yml`; đọc file trực tiếp, **không cần** DB/storage nên chạy được mọi máy và trong CI. **Negative control**: gỡ section `Minio` → 2 FAIL; đổi `POSTGRES_PASSWORD` trong compose → 1 FAIL |
| Bộ test tích hợp FE+BE | `Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md` — **41/41 PASS, 0 FAIL**: A Auth 6/6 · B Category 3/3 · C Recipe+ingredient+step 7/7 · **D Ảnh 7/7 (trước đây D01 = 500)** · E Publish+discovery 8/8 · F Update+concurrency+soft delete 5/5 · G Token lifecycle 3/3 · H Health+docs 2/2 |
| Sửa tài liệu sai | `docs/HUONG_DAN_CHAY_TV4.md` L9 bảo để trống `NEXT_PUBLIC_MEDIA_URL` để dùng proxy — thực tế `mediaUrl()` trả `null` khi biến trống ⇒ không hiện ảnh. Đã sửa thành `NEXT_PUBLIC_MEDIA_URL=http://localhost:5080/api/v1/resources/images` |

---

## 3. Lỗi thật phát hiện & sửa trong tuần 3

| # | Lỗi | Biểu hiện | Nguyên nhân | Cách sửa |
|---|---|---|---|---|
| 1 | **JWT `RoleClaimType`** | AuthorPolicy luôn **403** khi gọi API thật (test trước đó toàn dùng handler nên không lộ) | `MapInboundClaims=false` nhưng thiếu khai báo claim type | `TokenValidationParameters`: `RoleClaimType="role"`, `NameClaimType="sub"` |
| 2 | **MinIO SDK `WithCallbackStream` + `async` lambda** | Ảnh cắt cụt/ảnh hỏng (~50% test D2 fail) + unobserved exception làm **crash test host** | Delegate là `Action<Stream>` nên `async` biến thành **async void fire-and-forget**, `GetObjectAsync` trả về khi copy chưa xong | Copy **đồng bộ** + kiểm tra `buffer.Length == stat.Size` (thiếu → `IOException`) |
| 3 | **EF đánh dấu entity con mới là `Modified`** | Trả 422 giả khi thêm ảnh/nguyên liệu/bước | Guid key khác rỗng bị coi là đã tồn tại | Interceptor chuyển `Added` khi `RowVersion` gốc rỗng (`e3e8315`) |
| 4 | **Image container biến mất khỏi registry** | CI 5 run đỏ, mọi test bị skip; dev compose hỏng trên máy mới | MinIO gỡ toàn bộ image public (`quay.io` 401, Docker Hub 404) | Đổi `rustfs/rustfs` (tag+digest) cho cả CI + dev compose |
| 5 | **500 upload ảnh do thiếu cấu hình storage** | `POST /recipes/{id}/images` → 500 `server.error` | `appsettings.Development.json` thiếu `Minio`; `MinioException` không có nhánh mapping | Bổ sung cấu hình dev + 3 test parity chống tái diễn |
| 6 | **Lệch mật khẩu PostgreSQL giữa appsettings và compose** | `28P01` mọi endpoint; test vẫn xanh vì tự nạp config riêng | Hai file có default khác nhau | Đưa default về `postgres`, giá trị thật ở `.env` gitignored + `EnvFileLoader` |
| 7 | **Nhánh lệch `main` 23 commit** | Không sửa được `dashboard/recipes/**`; 2 image storage khác nhau (rustfs vs coollabsio/minio) → conflict | PR #15 (wizard ảnh TV3) merge vào main giữa lúc đang làm N4/N5 | Merge `origin/main` sớm, chốt CR-6, gỡ 7 conflict trong PR #16 |
| 8 | `Hangfire.PostgreSql` 1.21 ctor obsolete + `TreatWarningsAsErrors` | Build fail khi làm lab | API mới yêu cầu `NpgsqlConnectionFactory` + `JobStorage.Current` | Chuyển sang factory mới |
| 9 | **Queue mismatch trong lab** | Job kẹt `Enqueued` mãi, không bao giờ chạy | `BackgroundJob.Enqueue` mặc định queue `default` còn worker nghe queue `lab`; `RecurringJobOptions` **không có** thuộc tính `Queue` | Truyền queue tường minh |
| 10 | Mailhog subject đọc sai vị trí | Đếm email được nhưng không đối chiếu được subject | Subject nằm ở `items[].Content.Headers.Subject` (Mailhog v2) | Sửa đường dẫn đọc |
| 11 | Xoá object trong lab thất bại | Dọn dẹp không sạch | `IObjectStorageWriter` chỉ có `Exists`/`Upload` | Xoá phải qua `IFileStorageService` |
| 12 | Bẫy encoding PowerShell 5.1 | Tiếng Việt trong file bị hỏng | `Get-Content`/`Set-Content` mặc định encoding sai | Ghi file UTF-8 tường minh |
| 13 | **Ảnh Draft không xem được trong wizard** (phát hiện khi soát tích hợp) | `<img src=proxy>` không gửi Bearer ⇒ **403** | Proxy D27 yêu cầu auth cho Draft/Archived nhưng thẻ `<img>` không gửi header | **Chưa sửa** — block **B5**, cần chọn cơ chế (presigned / token query / cookie) |

---

## 4. Số liệu kiểm chứng (tiến trình so với tuần 2)

| Mốc | CulinaryBlog.Tests | Spike | Frontend | CI GitHub |
|---|---|---|---|---|
| **Cuối tuần 2** | 120/120 | 5/5 | `next build` OK (13 routes) | ✅ `818522b` |
| 24/09 — archive/delete + E2E storage | **133/133** (+13) | 5/5 | OK | — |
| 27/09 — D27 proxy (N2) | **139/139** (+6) | 5/5 | — | — |
| 27/09 — D2 resize (N4) | **148/148** (+9) | 5/5 (2 vòng) | — | — |
| 28/09 — Lab L4 (N5) | 148/148 (`Skipped=0`) | 5/5 | — | ✅ `36344662570` |
| 28/09 — PR #16 (merge main) | 154/154 | 5/5 | `tsc` 0, `next build` 16/16 trang | — |
| 28/09 — Fix config + `DevConfigParityTests` | **157/157** (+3) | 5/5 | 17 route | — |
| 28/09 — Bộ QA FE+BE | **41/41 PASS, 0 FAIL** | — | `tsc --noEmit` 0 | — |

Tiêu chí kiểm chứng bắt buộc thêm tuần 3: **luôn kiểm tra `Skipped=0`** để tránh "CI xanh giả" khi E2E storage skip âm thầm.

---

## 5. Tài liệu / ADR / bằng chứng tạo MỚI trong tuần 3

| Nhóm | File |
|---|---|
| Sổ chính tuần 3 | `Tuan03/KE_HOACH_TUAN_3_TV4.md`, `Tuan03/TRANG_THAI_THUC_HIEN_TUAN_3.md`, `Tuan03/SO_EVIDENCE_TUAN_3.md`, `Tuan03/HANDOFF_TV4_TUAN3.md` |
| Log/số liệu | `Tuan03/logs/` — `explain_publish_culinary_test.txt`, `k6_smoke_recipes.log`, `k6_smoke_summary.json`, `d2_resize_hangfire.log`, `d2_resize_hangfire_db.txt`, `d2_resize_verify.log`, `lab_l4_run.log`, `lab_l4_db.txt` |
| Báo cáo lỗi | `Tuan03/Report/BAO_CAO_LOI_UPLOAD_ANH_500.md`, `..._DA_SUA.md`, `Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md` |
| Bộ test case tích hợp | `Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md` (41 case) |
| Đề xuất cần quyết định | `Tuan03/Report/DE_XUAT_01_LOI_STORAGE_TRA_503_CO_MA_LOI.md` · `DE_XUAT_02_FAIL_FAST_KHI_THIEU_CAU_HINH.md` · `DE_XUAT_03_NAP_FILE_DOT_ENV.md` (✅ đã làm) · `DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md` · `DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md` · `DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md` |
| ADR / quyết định | `docs/DE_XUAT_GIAI_QUYET_D23_D27.md` (D23 → PA-1, D27 → PA-2) · `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` (CR-6) · `docs/IMAGE_CONTRACT.md §5, §7` |
| Hướng dẫn | `docs/HUONG_DAN_CHAY_TV4.md` (sửa), `docs/HUONG_DAN_TEST_APP.md` (cập nhật), `README.md` (bảng quyết định) |
| Lab (nhánh riêng) | `practice/TV4/L4/` + `README.md` + `SOK_LAB_L4.md` — commit `3642428` |

---

## 6. Phần chuyển từ "chưa làm" của tuần 2 → còn lại hoặc phát sinh mới

| Việc | Ở tuần 2 | Ở tuần 3 | Lý do còn lại / điểm mới phát sinh |
|---|---|---|---|
| D4-UI: progress upload + nút Unpublish/Archive | Chưa bắt đầu | **Chưa làm** | Không còn lý do "chờ D27". Rà `main`: `ImagesStep.tsx` đã có upload/delete/set-primary + optimistic rollback + `NEXT_PUBLIC_MEDIA_URL`; còn thiếu thanh progress và 2 nút status (API archive/unpublish đã sẵn sàng) |
| D6 mục 2: lab Identity/Google/refresh/forms/FTS | Chưa tới hạn | **Chưa làm — đây là gap của tuần 3** | `PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 + `KE_HOACH_DU_AN.md` mục 4.2 **đều xếp việc này vào tuần 3**, không phải tuần 4 → cần nhóm trưởng chốt (làm tối giản / ghi nhận dời) |
| Sitemap job 02:00 UTC + distributed lock | Chưa định nghĩa | **Chưa làm (CR-7)** | Hiện chỉ `GET /recipes/sitemap` on-demand + `sitemap.ts`/`robots.ts`; đề yêu cầu cron 02:00 UTC + distributed lock khi nhiều worker |
| Trace thật vào Seq (K20) | Chưa | **Chưa** | Mới có cấu hình; cần bật service `seq` + gửi OTLP để chụp trace HTTP→DB làm bằng chứng |
| Bằng chứng `/health/ready` 503 khi Redis down (D22) | Chưa | **Chưa** | `PHAN_CHIA` xếp "health failure" ở tuần 4 |
| Bảng mapping K01 (FR ↔ ADR ↔ evidence) | Chưa | **Chưa** | Rẻ, làm được ngay; reviewer cần để nghiệm thu |
| PR `4830e57` (fix connection string) lên main | Chưa | **Chưa** | CI main có nguy cơ `28P01` khi có commit deploy mới |
| Rà soát diff PR #14 (đã merge nhầm) | Chưa | **Chưa** | Nhóm đã chốt giữ nguyên; đưa vào báo cáo nhóm |
| PR cho nhánh lab `practice/TV4/L4` | — | **Chưa mở** (branch đã push) | `PHAN_CHIA` yêu cầu "có PR lab ngoài phần chính" |
| D1/D4 ảnh hiển thị trong wizard (ảnh Draft) | Chưa nêu | **Block B5 (mới)** | `<img>` không gửi Bearer ⇒ proxy 403; phải chọn presigned / token query / cookie — đụng `IMAGE_CONTRACT` D27, cần nhóm + TV3 |
| Lỗi storage trả 500 chung chung (B1) + fail-fast (B2) | Chưa nêu | **Block B1/B2 (mới)** | Đổi contract lỗi (thêm `storage.unavailable`, 500 → 503) và/hoặc validate lúc startup — cần nhóm thống nhất, có TV3 là consumer |
| `/health` báo storage `Healthy` khi credential sai (B4) | Chưa nêu | **Block B4 (mới)** | Health chỉ TCP probe, báo sai hướng nguy hiểm; cần chốt ngữ nghĩa |
| Không có user Admin seed (B6) | Chưa nêu | **Block B6 (mới)** | Không ai mở được `/hangfire`, không ai test được CRUD category |

---

## 7. Điểm kỹ năng (Kxx) thay đổi trong tuần 3

| K | Thay đổi | Bằng chứng |
|---|---|---|
| K02 | 🆕 Nâng lên "đã làm đầy đủ" | Thêm endpoint archive (`PATCH /{id}/archive`) + delete soft; giữ 422/403/404 đúng contract |
| K03 | 🆕 Có phần interface mới | `IObjectStorageReader` (đọc) + `IObjectStorageWriter` (ghi key phái sinh) tách khỏi `IFileStorageService` |
| K07 | ✅ Có test concurrency thật | RowVersion 422, race 2 writer set primary, delete-vs-resize, `Delete_stale_row_version_throws_422` |
| K12 | 🟡 Đóng bằng **xác minh** + LAB | Xác minh không có cache recipe nào; LAB L4 ghi rõ giới hạn (chưa có tích hợp cache thật để kiểm chứng) → **không đóng bằng giả định** |
| K13 | ✅ Hoàn chỉnh | SP upload/delete + E2E 3/3 + LAB `media` 25/25 (4 MIME magic bytes, MIME giả, vượt giới hạn) |
| K14 | ✅ Cả SP lẫn LAB | SP `ResizeImageJob` chạy thật `Succeeded`, retry 3, dashboard Admin-only; LAB `jobs` 8/8 (tắt worker → job còn trong DB → chạy khi restart) |
| K15 | ✅ LAB | `email` 3/3 (Mailhog) + `xml` 3/3 (93 URL Published) + resize 300×200/800×533 |
| K19 | ✅ SP (SEO) | Sitemap Published-only + robots + canonical/OG/JSON-LD |
| K20 | 🟡 Cấu hình + số liệu | OTEL config xác minh + EXPLAIN/k6; **còn thiếu trace thật vào Seq** |
| K22 | ✅ SP | EXPLAIN 0.339/0.044 ms; k6 3310 req, p95 225.63ms, 109.42 req/s |
| K23 | ✅ Nâng mạnh | CI thật sự xanh (chứng minh bằng `Skipped=0`); queue service chạy thật ở cả SP lẫn LAB; dev stack dựng được trên máy mới |
| K24 | 🟡 Đang làm | Còn: PR `4830e57` lên main, rà soát diff PR #14, mở PR cho nhánh lab |
| K01 / K16 / K17 / K18 | ⬜ Chưa đạt | ADR có nhưng **thiếu bảng mapping FR ↔ ADR ↔ evidence**; D4-UI chưa làm nên chưa có bằng chứng frontend của TV4 |

---

## 8. Điểm cần nhóm/nhóm trưởng đối chiếu

1. ~~**Lệch số liệu test cần thống nhất**~~ — **Đã chốt 30/09**: chạy lại `dotnet test CulinaryBlog.sln -c Release` ⇒ **157/157 pass, Skipped=0** (+ 5/5 spike). `HANDOFF_TV4_TUAN3.md` và `SO_EVIDENCE_TUAN_3.md` đã sửa theo con số này. Con số `159/159` trước đó là ghi nhầm, không có test tương ứng.
2. **PR #14 đã merge nhầm vào main** — nhóm đã chốt giữ nguyên; cần ghi vào báo cáo nhóm để tránh sai lệch số liệu giữa các sổ.
3. **RustFS chỉ dùng cho dev + CI** — SRS v1.1.1 và evidence tuần 1 vẫn ghi "MinIO" (giữ nguyên vì là spec/lịch sử). **Production bắt buộc dùng object storage có license.**
4. **Cần nhóm trưởng chốt** 3 việc: (a) gap lab Identity/Google/refresh/forms/FTS của tuần 3; (b) 5 block B1/B2/B4/B5/B6 trong `Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`; (c) CR-7 sitemap job 02:00 UTC + distributed lock.
5. **Quy tắc không phá vỡ mới rút ra tuần 3** (đã ghi trong `HANDOFF_TV4_TUAN3.md` mục 5):
   - Không dùng image container tag `latest` hoặc image đã bị gỡ khỏi registry.
   - Sau khi sửa CI, **kiểm tra `Skipped=0`** để tránh CI xanh giả.
   - `MinioStorageService` copy **đồng bộ** trong `ReadAsync`; không dùng `async` lambda cho `WithCallbackStream`.
   - **Không nâng ImageSharp lên 4.x** (bắt buộc commercial license key, build fail) — giữ `3.1.11`.
   - **Rà `origin/main` trước khi làm việc frontend hoặc hạ tầng** để tránh lệch nhánh.

---

## 9. Kết luận

- Trong tuần 3, TV4 **gỡ được cả 4 block lâu nhất** của tuần 2 (D1.1c, D27, D23, D2) và **hoàn thành thêm 3 task mới** (D3 archive/delete, D4-SEO, D5 số liệu đo) cùng **1 lab** (L4).
- Số lượng test backend tăng từ **120 → 157** (`Skipped=0`), thêm **41 test case tích hợp FE+BE** chạy qua API thật, **4 phase lab 39/39 check**, và **số liệu k6/EXPLAIN** thay cho ước lượng.
- Ba việc chưa làm trong tuần 3: **D4-UI** (progress + 2 nút status), **lab Identity/Google/forms/FTS** (gap so với kế hoạch tuần 3), và **sitemap job theo lịch + trace Seq**; kèm 5 block chờ nhóm quyết định.
