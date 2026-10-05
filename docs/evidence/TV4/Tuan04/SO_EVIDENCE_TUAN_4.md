# SỔ EVIDENCE TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh**: `2312739_NHTSon_D5-D6-D7` (từ `origin/main` = `7fe8fc2`) · **Lab**: `practice/TV4/L4`, `practice/TV4/L5`
> **Trạng thái**: tất cả bắt đầu ở trạng thái **Chưa làm**; ô K chuyển sang "có minh chứng" khi có
> **code/config + test + kết quả thật + reviewer Tâm xác nhận**. Ô nào chưa đủ thì ghi **Chưa làm**,
> không làm tròn số.

> [!NOTE]
> **📌 Cập nhật 03/10/2026 — các con số dưới đây đã cũ, đọc mục này trước.**
>
> | Mục | Ghi cũ | **Thực tế hiện tại** |
> |---|---|---|
> | Baseline nhánh | `7fe8fc2` | Đã merge `origin/main` (`3d0695d`); HEAD = **`55b4c2b`** |
> | Số test | đo lúc mở tuần | **`210/210`** pass · build 0 warning · `dotnet format` exit 0 |
> | Tổng K của TV4 | — | **9/24 đã được reviewer xác nhận** + **8 ô có bằng chứng mới từ N1, đang chờ duyệt** |
> | Trạng thái N2–N4 | — | **N2 gần đóng** (7/8 việc xong, dở dang `D1/D2/D3` UI) · **N3 gần đóng** (B/C1/C2 xong 05/10, còn A3/A5) · **N4-C xong** (runbook + deploy → tuần 5). Chi tiết [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md) |
>
> **Nguyên tắc bổ sung (rút từ lab 03/10):**
>
> 1. ⛔ **`214/214` không phải số của nhánh này.** Đó là nhánh lab `lab/TV4-audit-tuan4` (+4 test hồi quy),
>    **chưa merge**. Số đưa vào sổ này là `210/210`.
> 2. ⚠️ **Có bằng chứng ≠ đã được duyệt.** 8 ô K của N1 có code + test + log nhưng **chưa** ô nào
>    reviewer xác nhận → vẫn ghi "chờ duyệt", không đánh ✅.
> 3. ⚠️ **Lỗi đã sửa trên lab vẫn tính là lỗi đang mở** cho tới khi merge. Chi tiết:
>    [`BAO_CAO_LOI_TUAN_4_TV4.md`](BAO_CAO_LOI_TUAN_4_TV4.md).

> [!IMPORTANT]
> **Không lấy 4 file `Lab 04` ở `docs/evidence/TV4/` làm minh chứng của sổ này.**
> `TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_2312739_NguyenHuuTrungSon.docx`,
> `Lab4_2312739_NguyenHuuTrungSon.docx` do commit `80b2c0e` của **Nguyễn Thanh Tâm (TV1)** tạo
> (sinh cùng bộ cho TV2/TV3). Theo quyết định nhóm 30/09: giữ nguyên, không sửa, không dùng để
> chấm điểm kỹ năng cá nhân của TV4.

---

## 1. Baseline đầu tuần 4 — **đã đo thật 30/09/2026**

> Môi trường đo: Windows, **SDK .NET 10.0.401** (khớp `global.json` và CI), Docker Desktop 29.6.2 /
> Compose v5.3.1, hạ tầng lấy từ `docker-compose.dev.yml` (`postgres` 16-alpine, `redis` 7-alpine,
> `s3` RustFS 1.0.0, `mailhog`) với cấu hình thật trong `.env` cục bộ (`.env` **không** commit).
> Đo tại commit `a4fc8d8` (bằm `origin/main` = `7fe8fc2` + bộ tài liệu Tuan04), **trước** khi merge
> `1492b39` — commit đó chỉ đụng frontend nên **không làm thay đổi** kết quả build/test backend.
> Log gốc: `Tuan04/logs/baseline_build.log`, `baseline_format.log`, `baseline_test.log`,
> `baseline_coverage.log`.

| Hạng mục | Lệnh | Kết quả | Ngày |
|---|---|---|---|
| Build backend | `dotnet build CulinaryBlog.sln --no-restore -c Release` | ✅ **0 warning / 0 error** (11.66 s) | 30/09 |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | ✅ **exit 0** (không có diff) | 30/09 |
| Test | `dotnet test CulinaryBlog.sln --no-build -c Release --collect "XPlat Code Coverage"` | ✅ **178/178 pass** — `CulinaryBlog.Tests` **173/173** (2 m 39 s) + `ConcurrencySpike` **5/5**; Failed 0, **Skipped 0** | 30/09 |
| Coverage `CulinaryBlog.Application` | đọc `coverage.cobertura.xml` | ✅ **line 83.37%** / branch 67.44% — **đã vượt ngưỡng G5 80%** | 30/09 |
| Coverage tổng | cùng trên | 31.29% (2446/7817 dòng) — thấp vì `Infrastructure` chỉ 11.29% | 30/09 |
| Hạ tầng test | `docker compose -f docker-compose.dev.yml up -d postgres redis s3 mailhog` | ✅ 4 container lên; test E2E storage **thật** (không skip — `Skipped=0`) | 30/09 |
| Frontend typecheck | `npx tsc --noEmit` | ✅ **exit 0** | 03/10 |
| Frontend build | `npm run build` (`src/frontend`) | ✅ `✓ Compiled successfully`, **exit 0** — ⚠️ in `TypeError: fetch failed`/`ECONNREFUSED` khi static fetch vì API không chạy, **không** làm build fail | 03/10 |

### Kiểm định sau B5/B6/QD3 (03/10/2026)

| Hạng mục | Lệnh | Kết quả | Ngày |
|---|---|---|---|
| Build backend | `dotnet build CulinaryBlog.sln` | ✅ **0 warning / 0 error** | 03/10 |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | ✅ **exit 0** (phải chạy `dotnet format` không-verify 1 lần trước để sửa whitespace file test mới) | 03/10 |
| Test — targeted B5/B6 | `dotnet test --filter "…B5…\|…PromoteAdmin…\|…Image…"` | ✅ **71/71** | 03/10 |
| Test — toàn bộ | `dotnet test CulinaryBlog.sln` | ✅ **265/265 pass**, Failed 0, **Skipped 0** (5 m 16 s) | 03/10 |
| Test — chặn hồi quy JWT | `dotnet test --filter "…JwtSigningKeyNotCommittedTests"` | ✅ **6/6**; đã **cấy khoá thật** vào `appsettings.Development.json` để chứng minh test **bắt lỗi**, rồi hoàn tác | 03/10 |
| Quét secret (tay) | `git grep -n "<khoá dev đã thu hồi>"` (chuỗi khoá không ghi lại trong tài liệu này) | ✅ chỉ còn 1 chỗ hợp lệ — danh sách chặn trong `JwtService.cs`. Đã dọn 2 chỗ rò trong `docs/HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md` | 03/10 |
| Quét secret (script) | `bash deploy/scan-secrets.sh` | ⛔→✅ **Đã gỡ blocker 04/10.** Lần đầu chạy `bash` trên PATH của TV4 là **stub WSL** nên báo `execvpe(/bin/bash) failed`. Chạy bằng **Git Bash** thì được: `C:\Program Files\Git\bin\bash.exe deploy/scan-secrets.sh` → **exit 0**. Bài học: `bash` trên PATH **không** bảo đảm có bash thật | 03/10 → 04/10 |
| B6 kiểm chứng tay | `dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email>` | ✅ thiếu/sai tham số → `2`; `Testing`/`Production` → từ chối; user không tồn tại → `1`; lần đầu → `0`; chạy lại → `0` (idempotent, không trùng join). ⚠️ DB local đã promote `masterchef@culinary.local` + `trung.huynh@culinary.local` thành `Admin` | 03/10 |
| E2E hạ tầng thật | `docker compose -f docker-compose.dev.yml up -d …` | ⚠️ Docker/RustFS **không** chạy → các test E2E storage **return sớm** (xUnit báo pass chứ không phải `Skipped`). Cần bật lại hạ tầng để có bằng chứng E2E thật cho B5/N2 | 03/10 |
| Verify `/search` hết lỗi 500 | TC1–TC3 của `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` | 🟡 **Xác nhận bằng tĩnh, chưa chạy runtime** — xem mục 1.1 | 30/09 |

### Kiểm định N2 (04/10/2026)

Hạ tầng thật đã bật: `docker compose -f docker-compose.dev.yml up -d redis s3 mailhog seq otel-collector`
(PostgreSQL dùng bản local ở `5432`, không bật container `postgres` để tránh tranh port).

| Hạng mục | Lệnh | Kết quả | Ngày |
|---|---|---|---|
| Hạ tầng | `docker compose -f docker-compose.dev.yml up -d redis s3 mailhog seq otel-collector` | ✅ `redis`,`s3`,`mailhog`,`seq`,`otel-collector` đều `running`; port `6379/9000/9001/8025/5341/4317/4318` mở. Bỏ `nginx` và `postgres` (đã có Postgres local) | 04/10 |
| Test storage/DB với hạ tầng thật | `dotnet test --filter "…MinioE2ETests\|…ImageResizeD2Tests\|…ImageProxyD27Tests\|…StorageFailureContractTests"` | ✅ **44/44 pass** — MinIO/S3 thật, không còn return sớm | 04/10 |
| N2-A2 ESLint | `npx eslint .` trong `src/frontend` | ✅ **exit 0**; 8 warning `<img>` là cố ý (B5 `<img>` + `no-referrer`). Probe hook conditional → **exit 1** (gate bắt lỗi thật). `@typescript-eslint/no-explicit-any` tắt: repo còn 20 `any` có chủ đích, nằm ngoài phạm vi | 04/10 |
| N2-A2 CI | `.github/workflows/frontend.yml` | ✅ thêm bước `npm run lint` (hiện chạy `tsc → lint → build`) | 04/10 |
| N2-A1 Playwright | `npx playwright test --list` | ✅ liệt kê đủ **12** test | 04/10 |
| N2-B2 search E2E | `npx playwright test` | ✅ **12/12 pass** (25,6 s). TC1–TC12 `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md`. TC8 tự tạo recipe Published (kèm ingredient + step) rồi cleanup, đã xác nhận không còn recipe tiền đề E2E | 04/10 |
| N2-E6 magic bytes | `dotnet test --filter "…ImageMagicBytesE6Tests"` | ✅ **25/25** — chứng minh signature phải đủ chuỗi (WebP/AVIF kiểm tới byte 11), `.exe` đổi tên `.jpg` bị chặn, `RIFF…WAVE` không phải WebP, brand `mp42` không phải AVIF | 04/10 |
| N2-E3/E4/E4b/E5/E7 | `dotnet test --filter "…ImageConcurrencyE7Tests"` | ✅ **5/5**, chạy lại **3 lần đều 5/5** (không flaky) | 04/10 |
| Build + format + full test | `dotnet build` → `dotnet format --verify-no-changes` → `dotnet test CulinaryBlog.sln` | ✅ **0 warning / 0 error** → **exit 0** → ✅ **295/295 pass** + ConcurrencySpike **5/5** (42 s) *(bản ghi trước khi làm N2-C; sau N2-C là 309/309 — xem bảng dưới)* | 04/10 |
| N2-C6 coverage | `dotnet test CulinaryBlog.sln --collect:"XPlat Code Coverage"` → `bash deploy/check-coverage.sh 80 TestResults` | ✅ **84.35%** line coverage `CulinaryBlog.Application` ≥ ngưỡng 80 → **exit 0**. Probe âm (ngưỡng 90) → **exit 1**; thư mục không có report → **exit 1** | 04/10 |
| N2-C6 CI gate | `.github/workflows/backend.yml` | ✅ thêm bước `bash deploy/check-coverage.sh 80 TestResults` ngay sau `dotnet test` (trước đó CI **chỉ thu** coverage, không ai siết) | 04/10 |
| Quét secret (chạy được) | `C:\Program Files\Git\bin\bash.exe deploy/scan-secrets.sh` | ✅ **exit 0** — "OK: không phát hiện secret bị gửi vào repo". Mục bị chặn ở trên đã **gỡ** | 04/10 |
| N2-B2 TC8 (sau khi sửa) | `npx playwright test --reporter=list` | ✅ **12/12 pass** (22,1 s), TC8 **thật sự chạy** (không skip) — xem "Sự cố TC8" bên dưới | 04/10 |
| N2-E7 ma trận quyền (Development) | `GET /hangfire` bằng anonymous / Author / Admin | ✅ **401** / **403** / **200** — xem bên dưới | 04/10 |
| Lint + typecheck frontend | `npm run lint` → `npx tsc --noEmit` → `npm run build` | ✅ `exit 0` / `exit 0` / `exit 0` (`✓ Compiled successfully`, 15/15 static pages) | 04/10 |
| **N2-C3 k6 script trong repo** | `tests/performance/read-load.js` + `README.md` | ✅ script **đã commit** (trước đây chỉ gõ tay heredoc → không ai tái lập được). 3 nhánh đo riêng: cache / uncached / workload thật, có threshold làm hợp đồng | 04/10 |
| **N2-C4 số đo k6** | `deploy/…` xem TV4-K22 | ✅ **3 lần × 3606–3607 req ≈ 120 req/s, `http_req_failed` 0.00% (0/3607)**. Số của lần giữa: p50 **6.23ms** / p95 **14.14ms** / p99 **25.06ms**. Tất cả threshold xanh | 04/10 |
| **N2-C1/C2 outage drill** | `powershell -File deploy/outage-drill.ps1` | ✅ Redis tắt → đọc 200 + ready 503; S3 tắt → media 404→**503**; DB không truy cập → mọi endpoint **503 `database.unavailable`, không 500**. Phục hồi: Redis 0.2s · S3 0.5s · API 3.5s. ⛔ Phát hiện + sửa 2 lỗi thật (xem TV4-K24) | 04/10 |
| **N2-C1c hợp đồng retry** | `dotnet test --filter "FullyQualifiedName~BackgroundJobRetryContractTests"` | ✅ **4/4** — sitemap **2** (trước đó **không có** `[AutomaticRetry]` ⇒ Hangfire không retry lần nào), resize **3**, xoá ảnh **3**, welcome **3** lần theo lịch 0/1/5/30 phút | 04/10 |
| **Bug 500 khi DB chết** | `dotnet test --filter "FullyQualifiedName~ApiExceptionHandlerDbUnavailableTests"` | ✅ **6/6** — trước khi sửa, DB không truy cập được ⇒ 3 endpoint đọc trả **500** | 04/10 |
| Build + format + full test (sau N2-C) | `dotnet build` → `dotnet test CulinaryBlog.sln` | ✅ **0 warning / 0 error** → ✅ **309/309 pass** + ConcurrencySpike **5/5**. Thêm 10 test so với 299/299 trước đó | 04/10 |

#### N2-C6 — siết ngưỡng coverage thay vì chỉ "thu được"

CI trước đó đã có `--collect:"XPlat Code Coverage"` nhưng **không có bước nào fail khi coverage tụt**, nên
con số thu được là vô nghĩa về mặt kiểm soát. Đã thêm `deploy/check-coverage.sh` đọc `line-rate` của
package `CulinaryBlog.Application` trong `coverage.cobertura.xml` và fail nếu dưới ngưỡng.

| Tầng | Line coverage |
|---|---|
| **`CulinaryBlog.Application`** | **84.35%** ← đặt ngưỡng 80% ở đây |
| `CulinaryBlog.Domain` | 77.09% |
| `CulinaryBlog.API` | 58.99% |
| `CulinaryBlog.Infrastructure` | 11.55% |

Chọn **Application** vì đó là tầng chứa logic nghiệp vụ (validator, handler, guard) — cùng tầng với hai
bug N2-E vừa tìm ra. `Infrastructure` còn thấp vì phần lớn mã nằm ở `DbContext`/migration, siết ngay
sẽ tạo cảm giác an toàn giả.

Script được kiểm chứng bằng **cả probe âm lẫn probe dương** (không chỉ chạy một lần thấy exit 0):
ngưỡng 80 → exit 0; ngưỡng 90 → exit 1; thư mục không có report → exit 1. Chạy được local nhờ
**Git Bash** ở `C:\Program Files\Git\bin\bash.exe` — cùng cái bash đã gỡ được blocker của
`deploy/scan-secrets.sh`.

#### N2-E7 — đã kiểm chứng đủ ma trận quyền ở `Development`

Hạn chế đã ghi ở mục dưới ("E7 chỉ kiểm được mức không lộ") **đã được xử lý xong** bằng cách chạy thật
ở `Development` — nơi `Program.cs` thực sự map dashboard và `AdminDashboardAuthorizationFilter` có tác dụng:

| Vai trò | `GET /hangfire` | Kết luận |
|---|---|---|
| Anonymous | **401** | không lộ dashboard cho người chưa đăng nhập |
| Author (`e2e.playwright@culinary.local`) | **403** | `AdminDashboardAuthorizationFilter` chặn đúng |
| Admin (`e7.probe@culinary.local`, promote bằng CLI B6) | **200** | Admin vào được dashboard |

⚠️ Tài khoản `e7.probe@culinary.local` là **dữ liệu kiểm chứng tạm** trên DB local (đăng ký qua
`/register` rồi `dotnet run -- --promote-admin …`). Cần xoá trước khi nộp/bảo vệ để không để lại tài
khoản Admin có mật khẩu đã biết trong môi trường demo.

#### Sự cố TC8 — helper E2E im lặng `skip` (đã sửa)

Lần chạy lại cho ra **11/12**: TC8 bị `skip`, tức `seeded.total <= 12` — tức không tạo được recipe nào.
Truy nguyên thì **không phải hư test mà là dữ liệu + cache**:

1. `firstCategoryId()` lấy thẳng `list[0]` từ `GET /api/v1/categories`.
2. Danh sách này được **cache 60 phút** (`GetCategoriesHandler` → `cache.GetOrSetAsync`), còn
   `POST /api/v1/recipes` kiểm tra danh mục bằng truy vấn thẳng DB (`RecipeGuard.EnsureCategoryExistsAsync`).
3. Trong lúc debug lỗi 500 ở tuần trước, ~915 danh mục lab (`LAB cat*`, `Món ảnh D2*`) đã bị dọn
   **bằng SQL ngoài API** ⇒ không có bước `InvalidatePrefixAsync` nào chạy ⇒ cache vẫn trả id chết.
4. Bằng chứng: `redis-cli TTL dev:cache:categories:all:False` = **108 giây** còn lại; sau khi
   `DEL` key đó, list về đúng **25 danh mục thật** và `POST /api/v1/recipes` trả **201**.

Kết luận quan trọng: **`RecipeCacheService.InvalidatePrefixAsync` không hỏng** — nó dùng `SCAN` đúng
(`RecipeCacheService.cs:186`). Đây là hệ quả của việc sửa DB ngoài API, và tự hết hạn sau 60 phút.

Nhưng cách `skip` im lặng thì không chấp nhận được: một môi trường sạch (CI, máy người khác) hoặc bất kỳ
lần sửa DB tay nào sẽ khiến TC8 "xanh" mà **không hề kiểm thử phân trang**. Đã sửa `firstCategoryId()`
trong `src/frontend/e2e/api.ts` để **xác minh** từng danh mục bằng `GET /api/v1/categories/{slug}`
(đường này đọc DB thật, không qua cache) và trả về danh mục đầu tiên còn sống; hết danh sách mới `skip`
với lý do rõ ràng. Sau đó: **12/12 pass**.

> Ghi chú khi đọc log: `POST /api/v1/recipes` của Playwright trả **404** trong khi log ghi
> "Handled CreateRecipeCommand" và **không** có dòng `ERR` nào. Đây là `AppException(404, "category.not_found")`
> được map sang `ProblemDetails` — handler trả `RecipeDto` bình thường, nên "Handled" không có nghĩa là
> request thành công. Nếu sau này thấy 404 mà log không đỏ, đừng vội tìm lỗi serialization.


#### N2-B3/N2-B4 — 10 kịch bản tấn công file upload ảnh (đã chạy thật)

Spec `src/frontend/e2e/upload-security.spec.ts`, gọi API thật (không mock), upload thật lên MinIO.
Kết quả: **10/10 pass** (B3-1…B3-7, B4-1…B4-3). Toàn bộ E2E: **26/26** (xem TV4-K21).

**Lỗ hổng thật phát hiện khi làm B3 — file JPEG 3 byte được nhận với `201`.**

Magic bytes chỉ kiểm **tiền tố**, mà chữ ký JPEG chỉ dài đúng 3 byte (`FF D8 FF`). Nên một file rỗng
bị cắt cụt khớp *toàn bộ* chữ ký và lọt qua biên API. Hậu quả: file đó đẩy tới job resize mới chết,
lỗi nằm **ngoài request** nên người dùng không nhận được mã lỗi có nghĩa — đúng thứ mà yêu cầu
B3 cấm ("từ chối đúng mã lỗi, không lọt 500").

Đã sửa tại `src/backend/CulinaryBlog.Application/ImageUpload.cs`:

- Thêm `ImageFormats.MinBytes = 64` và mã lỗi riêng `file.too_small` (HTTP 400).
- Con số 64 có cơ sở, không phải chọn cảm tính: **PNG hợp lệ nhỏ nhất cần 67 byte**
  (8 byte signature + 25 byte chunk IHDR + 12 byte chunk IEND); JPEG hợp lệ nhỏ nhất ~125 byte.
  64 nằm dưới cả hai nên không loại o ảnh thật nào.

Bốn test mới (E6.10 + validator) khoá hành vi: JPEG 3 byte → `file.too_small`; PNG 63 byte →
`file.too_small`; **PNG 1×1 67 byte thật vẫn được nhận** (chống "vá quá tay" làm hỏng ảnh hợp lệ).

> Đệm fixture phải sửa ở 6 file test (`ImageUploadValidatorTests`, `RecipeImageTests`, `MinioE2ETests`,
> `StorageFailureContractTests`, `ImageProxyD27Tests`, `ImageResizeD2Tests`). Đây **không** phải hư hỏng
> của các test đó: chúng dùng payload 4–22 byte, nay bị chặn ở nhánh `file.too_small` **trước** khi tới
> nhánh MIME mà test muốn kiểm. Đã đệm tới kích thước ảnh thật (giữ nguyên phần đầu nên chữ ký không đổi).

**Sự cố lần 2 của `firstCategoryId()` — im lặng `skip` vẫn quay lại, lần này nguy hiểm hơn.**

Sau khi vá B3, chạy spec ra **10/10 `skipped`**. Nguyên nhân là chuỗi, không phải một lỗi:

1. Cache `categories:all:False` còn giữ list cũ **969 mục** (dữ liệu lab đã bị dọn bằng SQL ngoài API
   *trước khi* có `InvalidatePrefixAsync` ⇒ cache không được xoá). TTL 60 phút.
2. Các mục đầu list đều đã xoá ⇒ `GET /api/v1/categories/{slug}` trả **404** cho từng mục.
3. `firstCategoryId()` **dò không giới hạn** ⇒ phát ra hàng trăm request ⇒ chạm rate limit.
4. Từ đó **mọi** request trả **429**; code cũ coi `!res.ok()` là "danh mục hỏng" ⇒ hết vòng dò ⇒ trả `null`.
5. `test.skip(!recipeId, ...)` ⇒ **10 test xanh giả, không test gì cả.**

Đã sửa `firstCategoryId()` (`src/frontend/e2e/api.ts`):

- Giới hạn `MAX_CATEGORY_PROBES = 12` mục — không bao giờ dò hết list.
- **429 được ném ra như lỗi hạ tầng**, không còn bị hiểu nhầm thành "danh mục hỏng".
- Hết số lần dò mà không tìm được ⇒ **ném lỗi có nêu các slug bị loại**, thay vì `skip` im lặng.

Bằng chứng list là cache chứ không phải DB: sau khi tạo/xoá một danh mục probe (gọi API ⇒ có
`InvalidatePrefixAsync`), list về đúng **25 mục** và **100% slug tra được đều trả 200**. Danh mục probe
đã bị xoá lại (HTTP 204), list cuối cùng xác nhận = **25**.

> Bài học để ghi vào quy trình: `skip` là trạng thái nguy hiểm nhất trong E2E — nó biến lỗi hạ tầng
> thành kết quả tốt. Mọi `skip` trong spec phải là quyết định có chủ ý và có lý do in ra.

**B4 — quyền upload ảnh.** B4-1 khách không token → `401`; B4-2 Author khác → `403 recipe.forbidden`;
B4-3 payload **hoàn toàn hợp lệ** nhưng sai chủ vẫn bị chặn, và số ảnh trước/sau **không đổi** — đây là
bằng chứng handler chặn *trước khi ghi*, không chỉ trả `403` sau khi đã lưu.

> Sửa kèm: B3-6/B4-3 ban đầu gọi `GET /api/v1/recipes/{id}` — **route này không tồn tại**, chỉ có
> `GET /api/v1/recipes/{slug}`. Gọi bằng `id` trả 404 và làm test fail *vì lý do sai*, che mất lỗi thật.
> Spec nay giữ cả `{id, slug}`: `id` để upload, `slug` để đọc chi tiết.

**Bền vững của login helper.** `loginAsAuthor()` có cache token theo email trong phạm vi worker và retry
429 theo `Retry-After`. Đã thêm `LOGIN_BUDGET_MS = 20_000`: nếu chờ sẽ vượt ngân sách thì **ném lỗi nói rõ
còn bị 429**, thay vì để `beforeAll` chết theo timeout 30s — mà khi đó `auth`/`owner` chưa kịp gán nên
lỗi hiện ra là `Cannot read properties of undefined`, hoàn toàn lệch với nguyên nhân thật.

Kết quả kiểm tra đầy đủ sau khi sửa: backend **299 + 5 = 304/304 pass**; `dotnet format --verify-no-changes`
exit `0`; `npx tsc --noEmit` exit `0`; `npm run lint` exit `0` (2 cảnh báo `<img>` có sẵn từ trước);
`npm run build` exit `0`; Playwright **26/26 pass** (publish 4 + search 12 + upload 10).


#### Hai lỗi hạ tầng test phát hiện khi chạy lặp — đã sửa

Phần này ghi lại hai lỗi **không thuộc tính năng** nhưng lại làm báo cáo E2E/CI đỏ ngẫu nhiên.
Cả hai đều do bỏ qua thực tế khi chạy thật, và đều chỉ lộ ra khi chạy lặp nhiều lần.

**1. `dotnet test` ghi đè cache Redis của môi trường Development.**

`RecipeCacheService` dựng prefix key từ `RedisOptions.Instance` và **mặc định `"dev"`**
(`RecipeCacheService.cs:54`). Integration test dùng `UseEnvironment("Testing")` nhưng
`appsettings.Testing.json` **không tồn tại**, nên chúng kế thừa `Instance = "dev"` từ
`appsettings.json` và ghi vào đúng namespace `dev:cache:` mà API Development đang dùng.

Bằng chứng:

| Bước | Quan sát |
|---|---|
| `GET /api/v1/categories` | **1013** mục, tất cả đều là `lab-*` đã bị xoá |
| Tạo rồi xoá 1 danh mục probe qua API (gọi `InvalidatePrefixAsync`) | list về **25** |
| `redis-cli --scan` sau khi chạy `dotnet test` | key `dev:cache:categories:all:False` **bị xoá** bởi test |

⇒ 1013 mục **không nằm trong DB** (DB chỉ có 25 danh mục sống); đó là cache bị test ghi đè.
Hệ quả kép: E2E đỏ ngẫu nhiên, và test có thể đọc được cache của môi trường dev.

Đã sửa bằng `src/backend/CulinaryBlog.API/appsettings.Testing.json` đặt `Redis.Instance = "test"`,
nên test không còn chạm `dev:cache:`. Ghi chú: chính comment trong
`TracingObservabilityTests` đã từng phản ánh vấn đề này — "cache ... dùng chung Redis giữa các lần
chạy test" — nhưng chưa ai xử lý.

Bằng chứng **sau khi sửa** (nạp cache dev rồi chạy full suite):

| Bước | Kết quả |
|---|---|
| `GET /api/v1/categories` (nạp cache dev) | `dev:cache:categories:all:False` = **25** slug |
| Chạy `dotnet test CulinaryBlog.sln` | **304/304 xanh** |
| Kiểm tra lại key dev | **vẫn 25** — không bị xoá hay ghi đè |

Giới hạn của bằng chứng, nói rõ để không hậu kiện: sau khi tách namespace, **không quan sát thấy**
key `test:cache:categories:*` nào, nghĩa là ở lần chạy này các test không đi qua nhánh cache danh mục.
Vì vậy cái lệch 1013 mục **không thể quy kết lại chắc chắn** là do test ghi vào `dev:cache:` đúng thời
điểm đó — đó là suy luận từ dấu vết (key dev bị test xoá, giá trị từng là 1013 mục `lab-*` đã xoá).
Điều đã **chứng minh chắc chắn** là trước đây test và app Development dùng chung namespace `dev:cache:`
(cùng `Instance` mặc định), và giờ đã tách. Đây là cấu hình đúng để giữ, bất kể lỗi lịch sử cụ thể là gì.

**2. Test tracing đỏ ngẫu nhiên do race condition trong chính test.**

`TracingObservabilityTests.Request_to_database_bearing_endpoint_produces_http_span_with_child_db_span`
fail với `InvalidOperationException: Collection was modified; enumeration operation may not execute`.

Nguyên nhân: listener ghi span vào `List<Activity>` **có** `lock`, nhưng vòng `foreach` dùng để log
chẩn đoán lại duyệt thẳng danh sách đó **ngoài `lock`**, trong khi thread HTTP server và EF Core vẫn
đang `Add`. Lock bảo vệ writer nhưng không bảo vệ reader. Đây đúng là loại lỗi "chạy riêng thì xanh,
chạy song song thì đỏ".

Đã sửa: chụp `snapshot = activities.ToList()` **bên trong** `lock` rồi duyệt bản sao.

Đo trước/sau (full suite `dotnet test CulinaryBlog.sln`, có bắt tên test qua trx logger):

| Trạng thái | Kết quả |
|---|---|
| Trước khi sửa | **2/10 lần đỏ** (một lần 2 test, một lần 1 test) |
| Sau khi sửa | **7/7 lần xanh** |

**3. Token đăng nhập E2E: cache trong RAM không đủ.**

API rate-limit đăng nhập theo IP với cửa sổ 60s (`Retry-After` ≈ 61s). Cache token ban đầu nằm ở
phạm vi module, nhưng Playwright dựng worker process mới cho **mỗi lần chạy** ⇒ chạy E2E hai lần
liên tiếp là lần thứ hai gặt 429 ngay ở `beforeAll`. Đã chuyển cache xuống đĩa
(`.playwright/token-cache.json`, đã gitignore), tự đọc `exp` từ JWT và **coi token là hết hạn sớm
hơn 60s** để không dùng token sắp chết.

Kết quả: **3 lần `npx playwright test` liên tiếp đều 26/26** (trước đó lần thứ hai là đỏ vì 429).

> Ghi nhận trung thực: trong quá trình làm N2-B3/B4 có **một** lần full backend suite đỏ 2 test mà
> không bắt được tên (lúc đó chưa bật trx logger); các lần chạy sau đã bắt được đúng test tracing ở
> trên và sửa triệt để. Không có bằng chứng nào cho thấy nguyên nhân khác ngoài race condition này.


#### Hai bug thật phát hiện nhờ N2-E3/E4 và đã sửa

Cả hai đều do làm N2-E, không phải do test hỏng — đã tái hiện được ở tầng EF trước khi sửa.

**Bug 1 — `DELETE /api/v1/recipes/{id}/images/{imageId}` không xoá dòng trong DB.**
`Recipe.Images` chỉ expose `IReadOnlyList` qua backing field, nên EF không nhận orphan: với
`DeleteBehavior.Cascade` trên quan hệ bắt buộc, DB chỉ tự xoá khi xoá chính recipe (mà recipe là
soft-delete). Hậu quả: endpoint trả **204** và xoá object S3, nhưng dòng `RecipeImages` vẫn còn.

> Bằng chứng đo được: sau `RemoveImageDeferringPromotion` + `SaveChanges`, `rowsForRecipe` giảm
> từ 2 xuống 1 **nhưng** `db.Entry(target).State` vẫn là `Unchanged` — phát hiện orphan không đáng tin.
> Đổi sang `ClientCascade` một mình vẫn không đủ.

**Bug 2 — `23505 duplicate key ... ux_recipe_images_one_primary`, API trả 422.**
Unique index partial `(RecipeId) WHERE IsPrimary = true` được Postgres kiểm **từng câu lệnh**, và
Postgres **không cho unique index partial deferrable**. EF lại không bảo đảm thứ tự phát lệnh giữa
các entity, nên khi gộp "xoá ảnh primary" + "bật ảnh thay thế" (hoặc "chuyển primary") vào một
`SaveChanges`, EF có thể phát `UPDATE(bật ảnh mới)` khi dòng primary cũ còn nằm trong bảng → `23505`.

> Bằng chứng đo được: `failedInPhase=phase2`, `rowsAfterPhase1=1`, `primariesAfterPhase1=0` — DB đã
> còn **0** dòng primary trước khi lệnh `UPDATE` bật primary mới vẫn nhận `23505`.

**Cách sửa (đã áp dụng).**
1. `IRecipeImageRepository.MarkImageDeleted(RecipeImage)` — xoá tường minh qua `DbSet.Remove`,
   không còn phụ thuộc phát hiện orphan. `Images` chuyển sang `DeleteBehavior.ClientCascade`.
2. Mọi thao tác đổi primary lưu **nhiều lần trong một transaction** (`IUnitOfWork.ExecuteInTransactionAsync`,
   dùng execution strategy retry-safe của Npgsql):
   - xoá ảnh: hạ tất cả primary → bật ảnh thay thế → `DELETE` dòng cũ;
   - `PATCH isPrimary`: hạ tất cả primary → bật ảnh mới.
   Chỉ chạy nhánh "dựng lại primary" khi ảnh bị xoá **đang là** primary, nếu không thì xoá ảnh
   thường sẽ làm mất ảnh chính của công thức (đã bị test `Delete_removes_image_and_deletes_object`
   bắt và sửa).
3. Aggregate thêm `ClearPrimaryImages()`, `PromotePrimaryImage()`, `GetPrimaryReplacementCandidate()`;
   `RemoveImage()` giữ nguyên hành vi cũ (vẫn promote) để không phá domain test.

**Hồi quy được bảo vệ bằng test.** `E4` nay kiểm cả ba điều: xoá trả `204`, còn **đúng một** primary
là ảnh thay thế, và dòng ảnh đã xoá **thực sự biến mất khỏi DB** (`Assert.False(...AnyAsync(i => i.Id == first.Id))`).
`E3` chấp nhận `422 recipe.version_conflict` cho request thua cuộc — đó là hành vi đúng của
optimistic concurrency (D19), đặt kỳ vọng "cả hai phải 200" sẽ che mất đúng cơ chế bảo vệ.

#### Hạn chế còn lại của N2-E (ghi rõ, không che)

- **E7 đã kiểm đủ cả hai mức.** Test tự động `E7_Hangfire_dashboard_is_never_publicly_exposed` vẫn chỉ
  chạy được ở `Testing` (Hangfire không được đăng ký ⇒ `/hangfire` trả **404** chứ không phải 403 từ
  `AdminDashboardAuthorizationFilter`). Phần "Author 403 / Admin 200" **đã kiểm chứng tay ở `Development`**:
  401 / 403 / 200 — xem bảng ở mục "N2-E7" phía trên. Đây là giới hạn của môi trường test, không phải
  của logic phân quyền.
- **E1/E2 đã có test sẵn ở tuần trước**, không nhân bản: `ImageResizeD2Tests.Deleted_image_is_not_regenerated_by_resize_job`
  và `ImageResizeD2Tests.Resize_is_idempotent_when_job_runs_twice`. Riêng "không xoá object ngoài
  bucket/prefix" mới chỉ được bảo vệ gián tiếp qua kiểm tra key `recipes/{recipeId}/...` trong các
  test D2/D27, chưa có test riêng gọi thẳng vào `MinioStorageService.DeleteAsync`.
- `dotnet test` vẫn có một test flaky về tracing: `TracingObservabilityTests.Request_to_database_bearing_endpoint_produces_http_span_with_child_db_span`
  từng fail khi chạy song song, pass khi chạy riêng. Không liên quan N2-E.


> So với tuần 3 (`80b2c0e`: 172/172) ⇒ **178/178, +6 test**, không có test nào bị skip.

### 1.1 Xác nhận lỗi `500` ở `/search` — không tự sửa, lấy bản sửa từ `main`

Theo chỉ đạo 30/09: **không tự gỡ lỗi**, dùng bản sửa đã có trên `main`. Đã xác nhận:

| # | Kiểm tra | Kết quả |
|---|---|---|
| 1 | Commit sửa trên `main` | ✅ `e523579` *"fix(frontend): extract SearchFilterSelect to client component for search page SSR"* (TV2, 30/09 11:22) — **Phương án B** trong báo cáo |
| 2 | `e523579` đã nằm trong lịch sử nhánh tuần 4 | ✅ `git merge-base --is-ancestor e523579 origin/main` ⇒ có (nên không cần merge thêm cho lỗi này) |
| 3 | `src/frontend/src/app/search/page.tsx` còn `onChange` không | ✅ **0 kết quả**; file vẫn là Server Component (đúng — không thêm `'use client'`) |
| 4 | File mới `src/frontend/src/components/SearchFilterSelect.tsx` | ✅ có `'use client'`; `onChange` nằm trong Client Component; props truyền vào (`name`, `defaultValue`, `options`) **đều serialize được** |
| 5 | Đã merge `main` vào nhánh tuần 4 | ✅ merge `4770602`, kéo thêm `1492b39` (TV2: sửa hiển thị ảnh + thêm ô tìm kiếm ở trang `/recipes`) |
| 6 | Chạy thật TC1–TC4 (`/search`, `?q=a`, `?q=gà`, `?q=pho`) | ⬜ **Chưa chạy** — dừng theo chỉ đạo, không tự dựng app để test vì việc sửa đã ở trên `main`; chuyển sang làm cùng **N2-2 (E2E luồng search)** để có bằng chứng lặp lại được |
| 7 | Frontend build sau khi merge `main` | ✅ `npx tsc --noEmit` **exit 0**; `npm run build` **exit 0** (log: `Tuan04/logs/baseline_frontend.log`) |

> **Lưu ý về ý nghĩa của `next build`**: build xanh **không** phủ được lỗi `500` của `/search` — route
> `/search` được Next.js đánh dấu là **dynamic** (`ƒ /search`), lỗi RSC "Event handlers cannot be
> passed to Client Component props" chỉ nổ lúc render runtime. Vì vậy mục #7 chỉ chứng minh
> *TypeScript + build không hỏng*, còn bằng chứng lỗi đã hết vẫn phải lấy bằng E2E ở **N2-2**.

> Báo cáo `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` **giữ nguyên 100%** (không sửa, không xoá)
> theo quyết định trước đó; trạng thái "đã có bản sửa trên `main`" được ghi ở đây và ở
> `TRANG_THAI_THUC_HIEN_TUAN_4.md` thay vì sửa báo cáo gốc.

---

## 2. Bảng 24 ô kỹ năng của TV4

> Cột **Đã có (trước tuần 4)** = 9 ô đã được `PHAN_CHIA_CONG_VIEC_6_TUAN.md` ghi nhận.
> Cột **Tuần 4** = việc sẽ bù; chi tiết ở `KE_HOACH_TUAN_4_TV4.md` mục 5.
> Bản **mapping đầy đủ 24 dòng** (FR/NFR ↔ ADR ↔ đường dẫn evidence ↔ lệnh kiểm chứng ↔ kết quả đo
> ↔ reviewer) nằm ở **`docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`** (đặt ở gốc `TV4/` vì
> phục vụ cả 4 tuần) — lập 30/09, chờ Tâm xác nhận.
> Bảng dưới đây là bản rút gọn theo nhóm người làm, **không** tự nâng trạng thái ô lên ✅.

| K | Kỹ thuật con cần chứng minh | Đã có (trước tuần 4) | Việc tuần 4 | Evidence key | Trạng thái |
|---|---|---|---|---|---|
| K01 | SRS/FR-NFR/ADR/API contract | ✅ (ADR media/vận hành) | Bảng mapping FR ↔ ADR ↔ evidence 24 dòng (N0-5) | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | 🟡 **Mapping xong 30/09** · 24 dòng K01–K24 · chờ Tâm xác nhận |
| K02 | .NET 10 Minimal APIs, REST/version, Scalar/RFC7807 | 🟡 | `/health` + `/health/ready`; lỗi storage trả `503 storage.unavailable`; `/sitemap.xml` (N2-4 mã lỗi từ chối file chưa làm) | `Health.cs`, `Program.cs`, `StorageFailureContractTests` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K03 | Clean Architecture, interface, DI, value object | 🟡 | `ObjectStorageHealthCheck` (API) gọi `IObjectStorageReader` (Infrastructure) qua DI; probe S3 thật | `Health.cs`, `ObjectStorageCredentialProbe.cs` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K04 | CQRS/MediatR + behavior logging/validation/caching | ❌ | Lab L5 tự viết behavior tối thiểu (N3-2) | TV4-K04 | 🟡 Sản phẩm **có** `ValidationBehavior` + `LoggingBehavior` và `ArchitectureTests` chặn ranh giới; ⬜ phần "tự viết behavior trong lab L5" → **tuần 5** |
| K05 | FluentValidation + sanitization + Zod/RHF | ✅ | Bổ sung form status + form RHF/Zod trong lab (N2-8, N3-1) | `ArchitectureTests.cs`, wizard `RecipeWizard.tsx` | 🟡 FluentValidation **xong + test** (validator biên XSS/ký tự điều khiển/độ dài/scheme); ⬜ **Zod chưa dùng** — `zod` có trong `package.json` nhưng `src/` **không import chỗ nào**, FE chỉ có `react-hook-form` → **tuần 5** |
| K06 | EF/PG16 Code First, migration/config/seed, index | ❌ | Index phục vụ sitemap + EXPLAIN lại (N1-6, N2-6) | TV4-K06 | 🟡 Có **migration `AddFtsAndGinIndex`** + cấu hình GIN trong `RecipeConfiguration.cs` + test; ⬜ **EXPLAIN không đo lại tuần 4** vì index không đổi (theo `N2-C5` đã loại khỏi phạm vi) |
| K07 | UoW/transaction/audit/soft delete/RowVersion | 🟡 | Race sitemap chặn bằng Redis lock (1/1 thắng); audit/RowVersion còn ở phần lab | `SitemapGenerator.cs`, `SitemapLockTests` | 🟢 Lock đã làm 30/09 · lab chưa |
| K08 | Identity/PBKDF2, JWT, refresh rotation/reuse/logout | ✅ | Bù lab mục 2 (refresh hash/rotation/reuse) (N3-1) | `AuthTests.cs`, `Week3AuthAndPersonalLabTests.cs` | 🟢 **Code + test + log đủ** — PBKDF2 V3/SHA512 **≥100 000 vòng** assert tại `AuthTests.cs:108-111`; refresh rotation, **reuse → thu hồi cả family**, logout thu hồi token, token hết hạn, refresh đồng thời → 8/8 test xanh. ⬜ Chỉ thiếu phần lab tự viết lại (theo quy tắc plan: dùng PR sản phẩm làm minh chứng, không viết lại cho đủ số file) |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify/link | ❌ | Lab mục 2; **thiếu credentials ⇒ ghi "còn chờ", không tính hoàn thành** (N3-1) | TV4-K09 | ⬜ Chưa làm |
| K10 | RBAC/ownership/policy/rate limit/secrets/HTTPS/CORS | 🟡 | Bỏ secret khỏi `render.yaml` (`sync:false` + `fromService`); CI chạy `deploy/scan-secrets.sh` (bắt được JWT hardcode khi thử) | `render.yaml`, `deploy/scan-secrets.sh`, `backend.yml` | 🟢 Đã làm 30/09 · ⛔ **còn rotate key thật** — ngoài repo, xem [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) |
| K11 | FTS tsvector/unaccent/pg_trgm/GIN/ts_rank, filter/sort/page | ✅ | EXPLAIN lại + đo sau thay đổi (N2-6) | `RecipeConfiguration.cs`, `DiscoveryAndSearchTests.cs`, `Week3DiscoverySearchCacheTests.cs` | 🟡 Sản phẩm **có** tsvector + GIN + unaccent + `ts_rank`, có test (AND, phân trang, FTS không dấu "pho" → "Phở Bò Gia Truyền"); ⬜ **EXPLAIN lại tuần 4 chưa chạy** — số đo EXPLAIN chỉ có từ D5 tuần 3 |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | 🟡 | `RecipeCacheService` dùng Redis thật + JSON + xoá theo prefix + fallback local khi Redis chết; 2 service dùng chung key (6/6 test) | `RecipeCacheService.cs`, `RedisSharedCacheTests` | 🟢 Gap đã lấp 30/09 · k6 cache-hit đo ở N2-5 |
| K13 | MinIO/S3 upload/delete, stream/MIME/magic bytes/GUID | ✅ | Kịch bản tấn công file ở mức E2E (N2-4) | `ImageMagicBytesE6Tests.cs`, `ImageUploadValidatorTests.cs`, `ImageConcurrencyE7Tests.cs` | 🟢 **Code + test + log** — nhận diện MIME theo **magic bytes** (file JPEG giả mang đuôi `.jpg` bị từ chối), validate size **trước khi buffer**, upload/delete, chống 2 writer đặt primary đồng thời |
| K14 | Hangfire fire-and-forget/delayed/recurring, retry, persistence, dashboard | 🟡 | Recurring "sitemap-daily" `0 2 * * *` UTC qua `IRecurringJobManager`; storage PostgreSQL dùng chung cho mọi worker | `Program.cs`, `SitemapGenerator.cs` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | 🟡 | `deploy/backup.sh` + `deploy/restore.sh` (drill 14 bảng); sitemap XML sinh theo lịch, `/sitemap.xml` trả 200 | `deploy/backup.sh`, `deploy/restore.sh`, `SitemapGenerator.cs` | 🟢 Đã làm 30/09 · 🔴 nơi đặt lịch 03:00 ICT **chưa chốt** → [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) |
| K16 | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | ❌ | Hạ tầng CI build FE + lab L5 (`search-ssr`, `isr-detail`) (N2-3, N3-2) | TV4-K16 | 🟡 Sản phẩm **là Next.js App Router** (`src/app/**`), `revalidate` dùng ở 8 file → có ISR trong sản phẩm, CI có job build FE (`Frontend CI` run `37213966761` thành công); ⬜ **lab L5 `search-ssr`/`isr-detail` chưa làm** → tuần 5 |
| K17 | TanStack Query/server state, optimistic rollback, next/image | ❌ | Progress upload + rollback; lab `query-rollback`, `image-opt` (N2-8, N3-2) | TV4-K17 | 🟡 **Lab L5 đã đo 05/10 (9/10 + 1/7)**: optimistic rollback **có thật** — `RecipeWizard.tsx` dùng `useOptimistic` + `rollbacks`, rollback đúng; nhưng `next/image` **0 file**, có **9** file `<img>` thô. ⬜ **TanStack Query (`useQuery`) vẫn 0 file**, RowVersion chưa kiểm chứng được, và **không có** progress upload % (`N2-D1` đã loại) |
| K18 | Responsive, WCAG 2.1 AA, keyboard, loading/error | ❌ | Checklist 320/768/1200 px + focus/aria (N2-8) | TV4-K18 | ⬜ **Chưa làm** — `N2-D3` đã **loại khỏi tuần 4**, thuộc **TV2** theo phân chia công việc tuần 5 |
| K19 | SEO metadata/OG/Twitter/canonical/301/robots/JSON-LD | 🟡 | Sitemap theo lịch; lab `seo` đủ metadata/robots/redirect (N1-6, N3-2) | `SOK_LAB_L5.md` (phase `seo`), `logs/lab_l5_run.log` | 🟢 **Lab L5 đã làm 05/10 — 15/15 PASS**: metadata, JSON-LD Schema.org Recipe, robots, sitemap, canonical đều đạt; sitemap theo lịch **02:00 UTC** + distributed lock. ⬜ **301 redirect chưa kiểm chứng bằng test** |
| K20 | Serilog/Seq/correlation, OTEL HTTP/DB/metrics, health probes | 🟢 | Trace thật qua collector: span HTTP + span `db.system=postgresql` cùng TraceId; log Serilog cùng TraceId; health probe credential S3 thật | `SOK_LAB_L5.md` (phase `observability`), `logs/seq_trace_recipes.log`, `logs/lab_l5_run.log`, `TracingObservabilityTests`, `HealthTests` | 🟢 **Lab L5 đã làm 05/10 — 10/10 PASS**: `traceId` xuất hiện trong **body lỗi** lẫn server log, Seq/OTLP liên lạc được. Chờ Tâm duyệt |
| K21 | xUnit/unit ≥80%, API happy+error, Jest/RTL, Playwright | ❌ | Playwright thật + 5 luồng; ngưỡng coverage trong CI (N2-1, N2-2, N2-7) | `recipe-publish.spec.ts`, `recipe-search.spec.ts`, `backend.yml`, `frontend.yml` | 🟢 **Code + test + log** — xUnit **316/316** + `ConcurrencySpike` **5/5** (sau merge `main`: 321 test; trước merge tính `311/311` + 5), coverage `Application` **84.13%** vượt ngưỡng cổng **80%** (gate chạy trong CI); Playwright **26/26**, 3 lần liên tiếp, `--repeat-each=4` là 16/16. ⬜ **Jest/RTL chưa có** (thuộc **TV1**, `N2-A3` đã loại) và mới phủ **2/5** luồng E2E |
| K22 | k6 p50/p95/p99, EXPLAIN/N+1/cache hit, CWV/Lighthouse | ❌ | Commit script k6 tái lập được + đo p95/p99; số đo resilience (N2-5, N2-6) | `tests/performance/read-load.js`, `tests/performance/README.md`, 3 log k6 | 🟡 **k6 xong**: script commit được, **3 log chuẩn**, `~3606 request`, `~120 req/s`, `http_req_failed` **0.00%**, đủ p50/p95/p99. ⬜ Chưa đo: **cache-hit ratio**, **EXPLAIN lại**, **N+1**, **CWV/Lighthouse** |
| K23 | Docker multi-stage/Compose/Nginx/env/volumes/backup-restore/scaling | 🟡 | Backup/restore drill thật + OTEL collector trong Compose + Redis shared; 2 instance đã đo bằng lab `multi-instance` | `SOK_LAB_L5.md` (phase `multi-instance`), `logs/multi_instance_two_api.log`, `deploy/backup.sh`, `deploy/restore.sh` | 🟡 **Lab L5 đã làm 05/10 — 7/7**: 2 API (5080/5081) cùng dữ liệu, dùng chung Redis, khoá `lab:l5:...:shared-probe` ghi/đọc khớp. ⬜ **Số Hangfire server khi chạy 2 API chưa xác nhận** (thiếu `LAB_APP_DB`); profile compose 2 API + kho backup 30 ngày để kỳ sau · [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) |
| K24 | Git/PR/review/CI/static analysis/architecture test/secret scan/docs | 🟢 | Secret scan trong CI + Redis service cho test; CI frontend; **PR lab** đã mở 05/10 | `backend.yml`, `frontend.yml`, `deploy/scan-secrets.sh`, `ArchitectureTests.cs`, `KiemChung_Commit_Week5_TV1.md` | 🟢 secret scan + **2 workflow CI** + `ArchitectureTests` + tài liệu đã cập nhật + **PR #28 đã mở rồi đóng** theo quyết định nhóm. ⬜ **Runbook đầy đủ** (`N4-A` → tuần 5). ⛔ **CI backend đang ĐỎ** ở `03564c4`/`d4edfa2`/`0dfac4e` vì `.md` của TV4 chứa khoá JWT đã thu hồi (đã gỡ); còn flaky `TracingObservabilityTests` từ `870d6e3`. Xanh gần nhất: `8d9d62b` / `6caf2cf` |

**Đếm (cập nhật 05/10/2026 — sau N3-B + việc cuối GĐ3 — theo quy tắc N3-C3: chỉ tính khi có **code + test + log**):**

| Mức | Ô | Ghi chú |
|---|---|---|
| 🟢 **đủ bằng chứng** | K08, K13, K19, K20, K21, K24 | có code + test + log thật (K19/K20/K24 nhận thêm bằng chứng từ lab L5 05/10) |
| 🟢 **gần đạt, còn 1 việc nhỏ** | K02, K03, K07, K10, K12, K14, K15, K23 | đã có bằng chứng, nhưng còn phần hoàn thiện (xem cột Trạng thái) |
| 🟡 **có nền sản phẩm, thiếu phần lab/đo lại** | K01, K04, K05, K06, K11, K16, K17, K22 | sản phẩm đã có nhưng chưa đủ "code + test + log" của phần bổ sung (K17 đã có số đo lab nhưng còn thiếu TanStack Query + `next/image`) |
| ⬜ **còn thiếu thật** | K09, K18 | K09 chờ credentials Google · K18 thuộc TV2 |

> ⛔ **Không ô nào tự đánh dấu đạt.** Toàn bộ 24 ô đang chờ **Nguyễn Thanh Tâm** xác nhận và ghi ngày.
> Ô nào cuối tuần vẫn thiếu đã ghi rõ ở `TRANG_THAI_THUC_HIEN_TUAN_4.md` và trong
> [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md).

> **Thay đổi so với 04/10:** `K17` chuyển từ ⬜ sang 🟡 (lab L5 đã đo: optimistic rollback có
> thật, `next/image` 0 file). `K19` và `K20` lên 🟢 (phase `seo` 15/15, `observability` 10/10).
> `K23` giữ 🟢-gần-đạt (7/7 nhưng **chưa xác nhận** số Hangfire server khi chạy 2 API).
> Ô **còn thiếu thật** giảm **3 → 2**.
>
> Lưu ý trung thực cho K23: đã đo bằng **hai tiến trình API thật** (5080/5081) qua lab
> `multi-instance`, nhưng **số Hangfire server khi chạy 2 API chưa xác nhận** vì thiếu
> `LAB_APP_DB`. Chưa được tính là đạt.
>
> Lưu ý cho K24: đã mở PR #28 rồi đóng theo quyết định nhóm.
>
> ⛔ **CI backend hiện ĐỎ — đã kiểm tra log thật 05/10, không phải "chưa chạy".**
>
> | Commit | `Backend week 1` | Nguyên nhân |
> |---|---|---|
> | `8d9d62b` (04/10) | ✅ **success** — run `37213966752` | — |
> | `870d6e3` | ❌ failure — run `37295446878` | `TracingObservabilityTests.Request_to_database_bearing_endpoint_produces_http_span_with_child_db_span` — `Assert.NotNull()` Value is null (trace span không đọc được) |
> | `03564c4`, `d4edfa2`, `0dfac4e` | ❌ failure | `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist` — **tài liệu của chính TV4 chép lại khoá JWT đã thu hồi vào `.md`** ⇒ đúng bất biến mà nhóm đã viết test để chặn |
>
> Lần xanh gần nhất của cả 2 job: **`6caf2cf`**. Nguyên nhân đỏ đã **gỡ** ở commit sau
> (xoá khoá khỏi 2 file `.md`). Còn lại **flaky trace span** ở `870d6e3` — chưa xử lý.

---

## 3. Mẫu bản ghi evidence

```text
Evidence: TV4-Kxx (FR/NFR: ...)
Tuần / Người / Task: 4 / Nguyễn Hữu Trung Sơn (TV4) / [task]
Đường dẫn code/config: [đường dẫn cụ thể trong repo]
Nhánh / PR / commit: [nhánh] / PR #[số] (review: Nguyễn Thanh Tâm)
Test/lệnh chạy + môi trường: [lệnh cụ thể]
Kết quả thực tế (ảnh/log/coverage): [chụp log/ảnh, không có secret]
Minh chứng demo: [ảnh/video khi có]
Reviewer + ngày xác nhận: Nguyễn Thanh Tâm / [ngày]
Lỗi còn lại / ảnh hưởng: [ghi rõ nếu có]
```

---

## 4. Evidence tuần 4 (điền sau khi thực hiện)

> Các mục dưới đây **trống là chưa làm**. Không điền trước nội dung rồi chạy sau.
> File log dự kiến lưu tại `Tuan04/logs/`: `search_500_verify.log`, `health_ready_503.log`,
> `seq_trace_recipes.log`, `backup_restore_drill.log`, `resilience_matrix.log`, `k6_week4*.log`,
> `explain_week4_*.txt`, `e2e_playwright.log`, `ci_frontend.log`.

### TV4-K20 (N1-3) — Trace HTTP→DB thật vào Seq/OTLP

```text
Evidence: TV4-K20
Tuần / Người / Task: 4 / TV4 / N1-3
Đường dẫn: docker-compose.dev.yml (service otel-collector), deploy/otel-collector-config.yaml,
           Program.cs (AddOtlpExporter + Serilog sink Seq), CulinaryBlog.API.csproj,
           tests/CulinaryBlog.Tests/TracingObservabilityTests.cs
Lệnh chạy: docker compose -f docker-compose.dev.yml up -d seq otel-collector
           # API thật: OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317, Seq__Url=http://localhost:5341
           Invoke-WebRequest http://127.0.0.1:5198/api/v1/recipes?page=106\&pageSize=5
           Invoke-WebRequest http://127.0.0.1:5198/health/ready
           docker logs culinaryblog-otel --since 5m
           # Seq: http://localhost:5341/api/events?filter=Application%20%3D%20'CulinaryBlog.API'
           dotnet test --filter FullyQualifiedName~TracingObservabilityTests
Kết quả: 4 endpoint trả 200 (live / recipes x2 / ready). Collector nhận 2 lô span
         ({"resource spans": 1, "spans": 15} rồi {"spans": 3}). Cùng TraceId b19ee91b6746a491fbf8d79640262639
         có span HTTP "GET /api/v1/recipes/" VÀ span con "db.system=postgresql" (peer.service=127.0.0.1)
         => trace HTTP→DB thật, không phải trace giả lập. attributes/redact đã xoá db.statement.
         Seq nhận log Serilog của app: "HTTP GET /api/v1/recipes responded 200" với
         TraceId=a52518d1826ff3d3c833b0c229e22f60 — khớp span HTTP của chính request đó
         => log và trace liên kết được bằng TraceId. Test tự động 2/2 pass.
         Log gốc: Tuan04/logs/seq_trace_recipes.log
Reviewer + ngày: ⬜
```

> [!NOTE]
> **Hai lỗi thật đã phát hiện và sửa trong lúc làm N1-3** — ghi lại vì đều là lỗi mà test tự động bỏ sót:
> 1. `Program.cs` đăng lịch sitemap bằng static API `RecurringJob.AddOrUpdate` ngay trong lúc đăng ký DI.
>    Điều này ném `InvalidOperationException: Current JobStorage instance has not been initialized yet`
>    và làm **app không khởi động được ở mọi môi trường thật** (Development/Production). Test không bắt được vì
>    môi trường `Testing` không bật Hangfire. Đã sửa: đăng lịch **sau** `builder.Build()` qua
>    `IRecurringJobManager` lấy từ DI.
> 2. Sink Seq được gán vào `Log.Logger` *sau* `builder.Host.UseSerilog(...)` — mà `UseSerilog` lập tức thay thế
>    logger đó, nên **Seq nhận 0 event của app** (chỉ có span từ OTLP). Đã sửa: gộp `.WriteTo.Seq(url)` vào
>    đúng `LoggerConfiguration` của `UseSerilog`, bọc try/catch để Seq chết không làm app không khởi động.

### TV4-K21 (N2-1, N2-2) — Playwright + 5 luồng E2E

```text
Evidence: TV4-K21
Tuần / Người / Task: 4 / TV4 / N2-1, N2-2 (luồng publish + search của TV4)
Đường dẫn: src/frontend/playwright.config.ts,
            src/frontend/e2e/recipe-publish.spec.ts   (N2-B1 publish — MỚI 04/10),
            src/frontend/e2e/search.spec.ts           (search TC1–TC12),
            src/frontend/e2e/upload-security.spec.ts  (N2-B3/B4, 10 test),
            src/frontend/e2e/api.ts                   (helper: login cache, browserAccessToken,
                                                          firstCategoryId, deleteRecipe),
            src/frontend/src/app/dashboard/recipes/_wizard/RecipeWizard.tsx   (sửa lỗi mất bước),
            src/frontend/src/app/dashboard/recipes/[id]/edit/EditRecipeClient.tsx,
            package.json scripts
Lệnh chạy: # Postgres + docker compose (redis/s3) phải lên trước
           npm run test:e2e                       # toàn bộ; Playwright tự bật frontend + backend
           npm run test:e2e -- recipe-publish     # riêng luồng publish
           E2E_START_BACKEND=0 npm run test:e2e   # tự bật backend (dùng trong CI)
Kết quả: Toàn bộ **26/26 pass** = publish **4** + search **12** (TC1–TC12) + upload **10**
         (B3-1…B3-7, B4-1…B4-3). `--retries=0` chạy **3 lần liên tiếp đều 26/26**
         (51.1s / 52.9s / 51.8s); `recipe-publish --repeat-each=4` → **16/16**.

         Luồng publish (recipe-publish.spec.ts) — B1-1…B1-4, tất cả đi qua **UI thật**
         (wizard 5 bước), không gọi fetch:
         B1-1 validate client chặn tiêu đề <5 ký tự và thiếu danh mục, không nhảy bước, không tạo draft.
         B1-2 nút "Xuất bản" `disabled` khi thiếu bước thực hiện.
         B1-3 trọn vẹn: lưu nháp → 2 nguyên liệu → 2 bước → xuất bản → badge Draft→Published →
              link "Xem trang công khai" → trang `/recipes/{slug}` hiện tên món, **không còn nhãn
              "Bản nháp"** → tab "Công thức của tôi" hiện "Đã đăng" + nút "Xem".
         B1-4 recipe đã publish mở ở bước 5 hiện badge Published và **không còn** nút "Xuất bản".

         🟢 Phát hiện **1 lỗi sản phẩm thật** trong lúc làm luồng publish (đã sửa + khoá bằng test):
         **Wizard mất bước khi lưu công thức mới.** `saveBasic` (`RecipeWizard.tsx:161`) đổi URL từ
         `/dashboard/recipes/new` sang `/dashboard/recipes/{id}/edit`. Next.js render theo URL mới nên
         trang `edit` **thay thế** trang `new`: `EditRecipeClient` mount và dựng `RecipeWizard` mới
         với `step: 0`. Hệ quả người dùng thấy: bấm "Lưu & tiếp" xong **bị quay ngược về đúng bước
         cơ bản** (dữ liệu đã lưu, nhưng phải bấm "Nguyên liệu" lần nữa) — và nếu đang gõ dở ở bước
         sau thì mất trắng.
         Sửa: mang bước hiện tại trên URL (`?step=`), `EditRecipeClient` đọc lại khi mount
         (`parseStepParam`) và có `useEffect` đồng bộ mỗi khi đổi bước. Bonus: F5/bookmark giữ đúng bước.
         Bài **B1-3** giờ khẳng định wizard **tự** sang bước 2 sau lần lưu đầu — lỗi quay lại bước 1
         làm form nguyên liệu không bao giờ hiện và test đỏ đúng chỗ.
         Bằng chứng trước khi sửa: `--repeat-each=6` → **5/6 đỏ**; log API cho thấy `POST /recipes` 201
         rồi `GET` chi tiết nhưng **không có** `POST .../ingredients` nào.

         🟢 Sửa 3 lỗi trong **test** (không phải lỗi sản phẩm):
         1. `getByRole('alert')` khớp 2 phần tử vì Next.js tự sinh `#__next-route-announcer__`
            cũng mang `role="alert"` → phải loại nó (`div[role="alert"]:not(#__next-route-announcer__)`).
         2. `getByRole('button', { name: 'Xuất bản' })` mặc định khớp **substring**, nên cũng khớp
            nút bước `5. Xem lại & Xuất bản` → `strict mode violation`; sửa bằng `exact: true`.
         3. `gotoStep` giờ kiểm `aria-current="step"` sau khi bấm, và `settleStep` chờ `networkidle`.
            Trước đó lỗi thật hiện ra là `timeout` 60s ở một ô nhập của bước *kế tiếp* — dấu hiệu không
            liên quan gì tới nguyên nhân thật.

         🟢 `playwright.config.ts` giờ **tự bật backend** trong `webServer` (chờ `/health/ready`, không
         phải chỉ cổng) thay vì bắt người chạy tự mở terminal khác. Lý do: khi tiến trình API bị dọn
         giữa lúc test đang chạy, các test còn lại nhận `ECONNREFUSED` — **lỗi hạ tầng bị quy nhầm thành
         lỗi sản phẩm**. Đo được một lần mất **14/26 test** chỉ vì lý do này. Đặt `E2E_START_BACKEND=0`
         để tự quản lý (CI dùng chế độ này vì đã có sẵn Postgres/Redis/S3).

         Kết quả ổn định: `recipe-publish --repeat-each=4` → **16/16**; full suite `--retries=0` chạy
         **3 lần liên tiếp đều 26/26** (51.1s / 52.9s / 51.8s); và 1 lần chạy khi **tắt hẳn API** để
         Playwright tự bật cũng **26/26** (1.1m, gồm cả lúc build + khởi động backend).

         ⚠️ **Phạm vi còn thiếu, đã ghi rõ**: plan ghi "5 luồng E2E đầy đủ" nhưng N2-B1 của TV4 chỉ
         nhận **publish + search**; `register/login` (TV1), `category` (TV2), `create-recipe`
         (TV3) do thành viên khác viết và **tuần 5 mới có** → mục này hiện mới phủ 2/5 luồng.
         Ngoài ra frontend **chưa có** nút Unpublish/Archive (API `PATCH /{id}/unpublish`,
         `/{id}/archive` đã có ở `Program.cs:620,627` nhưng chưa nối vào UI) → gỡ xuất bản hiện chỉ
         kiểm được ở tầng API, chưa kiểm được bằng UI.
Reviewer + ngày: ⬜
```

### TV4-K23 (N1-4, N1-5) — Backup/restore drill + 2 API instance

```text
Evidence: TV4-K23
Tuần / Người / Task: 4 / TV4 / N1-4, N1-5
Đường dẫn: deploy/backup.sh, deploy/restore.sh, deploy/otel-collector-config.yaml,
           docker-compose.dev.yml, src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs,
           src/backend/CulinaryBlog.Infrastructure/RedisOptions.cs,
           tests/CulinaryBlog.Tests/RedisSharedCacheTests.cs,
           docs/adr/0003-category-and-search-caching-week3.md (quyết định cache-aside + fallback)
Lệnh chạy: # backup trên PostgreSQL native 18 (E:\PostgreSQL\bin)
           DATABASE_URL=... bash deploy/backup.sh
           # restore vào DB mới
           DATABASE_URL=... TARGET_DB=culinary_restore_drill bash deploy/restore.sh
           # cache dùng chung: 2 service/2 connection Redis
           dotnet test --filter FullyQualifiedName~RedisSharedCacheTests
Kết quả: Backup culinary_20260930T113019Z.dump = 728653 byte. Restore vào DB sạch
         culinary_restore_drill → 14 bảng, dữ liệu khớp nguồn (1344 users, 495 recipes, 25 categories).
         Restore lần 2 vào DB đã tồn tại → script TỪ CHỐI (exit 1), không ghi đè.
         Cache: 6/6 test pass — ghi ở service A thì service B (2 connection riêng) đọc trúng key,
         TTL có hiệu lực, JSON deserialize đúng, xoá theo prefix Recipe: xoá hết entry cũ,
         tắt Redis (SimulateServerDown) → tự fallback local, app vẫn 200.
         2 sitemap generator cùng tranh lock → đúng 1 thắng (xem TV4-K14).
         HAI TIẾN TRÌNH THẬT (tầng HTTP): chạy 2 process dotnet API (5080, 5081) + nginx round-robin
         (:8088, `nginx/nginx.multiinstance.conf`). 10 request qua nginx → api-1: 5, api-2: 5.
         api-1 làm nóng cache page 7; api-2 đọc lại CÙNG page đó trong 13 ms và đọc trúng key
         `multiinstance:cache:recipes:list:7:5:createdAt:desc::::` mà api-1 đã ghi ⇒ cache thuộc về Redis,
         không thuộc process. Tắt Redis: cả hai vẫn trả 200 từ DB, cả hai `/health/ready` trả 503.
         Log gốc: Tuan04/logs/multi_instance_two_api.log
Giới hạn: Hai tiến trình API đã chạy thật trên máy (5080/5081) sau nginx round-robin, nhưng
         chưa đưa vào docker-compose như một profile sẵn dùng cho cả nhóm, và chưa đo trên hạ tầng
         Render thật. Không có số đo RPO/RTO trên production.
         Lịch 03:00 Asia/Ho_Chi_Minh đã có trong repo: .github/workflows/backup.yml với cron
         '0 20 * * *' UTC (= 20:00 UTC hôm trước). Cần secret DATABASE_URL trong repository
         settings. Artifact của GitHub chỉ giữ 7 ngày nên 30 ngày phải chạy trên host có ổ đĩa riêng.
         ⛔ CHƯA ĐẠT "giữ 30 ngày": biến BACKUP_KEEP_DAYS=30 trong backup.yml chỉ dọn file trên
           runner, không phải thời hạn lưu thật. Cần bucket/NAS riêng -> DE_XUAT_08.
         ⛔ CHƯA CHỐT nơi đặt lịch: GitHub Actions có thể trễ/bỏ qua job khi repo lâu không commit
           -> DE_XUAT_07 (khuyến nghị: giữ GH Actions + thêm job canh).
         ⛔ CHƯA ROTATE khoá JWT đã lộ trong git history -> DE_XUAT_09.
Reviewer + ngày: ⬜
```

### TV4-K14 (N1-6) — Sitemap cron 02:00 UTC + distributed lock

```text
Evidence: TV4-K14
Tuần / Người / Task: 4 / TV4 / N1-6
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/SitemapGenerator.cs (LockTakeAsync/LockReleaseAsync +
           cache XML trong Redis), Program.cs (đăng lịch qua IRecurringJobManager sau builder.Build()),
           tests/CulinaryBlog.Tests/SitemapLockTests.cs
Lệnh chạy: dotnet test --filter FullyQualifiedName~SitemapLockTests
           # app thật: GET /sitemap.xml
Kết quả: 3/3 test pass. 2 generator cùng cố sinh sitemap trên cùng Redis → ĐÚNG 1 generator thắng lock,
         generator còn lại nhận "đã có người sinh" và không ghi đè; sau khi khoá được giải phóng thì
         lần sau sinh lại bình thường (lock có TTL nên job treo không chặn vĩnh viễn).
         Lịch: recurring job "sitemap-daily", cron "0 2 * * *", TimeZoneInfo.Utc
         = 02:00 UTC = 09:00 Asia/Ho_Chi_Minh.
         GET /sitemap.xml trên app thật trả 200 với XML hợp lệ (trang tĩnh + trang công thức).
Ghi chú: /sitemap.xml trả 200 với XML rỗng khi generator khác đang giữ lock — đây là hành vi có chủ đích
         (không phát sinh tải), KHÔNG phải lỗi; giá đổi là lần crawl kế tiếp lấy dữ liệu mới.
Reviewer + ngày: ⬜
```

### TV4-K22 (N2-5, N2-6) — k6 tái lập được + số đo resilience

```text
Evidence: TV4-K22
Tuần / Người / Task: 4 / TV4 / N2-C3, N2-C4, N2-C5, N2-C6
Đường dẫn: tests/performance/read-load.js (script k6 ĐÃ COMMIT — trước đây chỉ gõ tay heredoc
           nên người khác không tái lập được workload), tests/performance/README.md (runbook),
           deploy/outage-drill.ps1 (kịch bản dừng dịch vụ), Tuan04/logs/k6_c4_run{1,2,3}.log,
           Tuan04/logs/c1_outage_drill.log
Lệnh chạy: # C3/C4 — k6 (máy này không có binary k6 nên dùng Docker grafana/k6)
           docker exec culinaryblog-redis redis-cli FLUSHALL
           docker run --rm --network host -e BASE_URL=http://host.docker.internal:5080 \
             -e RATE=20 -e DURATION=30s -v "%CD%/tests/performance:/scripts:ro" \
             grafana/k6 run /scripts/read-load.js
           # C1/C2 — dừng lần lượt Redis / S3 / DB rồi đo trạng thái + thời gian phục hồi
           powershell -NoProfile -ExecutionPolicy Bypass -File deploy/outage-drill.ps1
Kết quả:  ✅ C3 — script k6 nằm trong repo, có threshold (hợp đồng) + README nêu rõ cách chạy
           lại và những gì BẮT BUỘC phải ghi kèm số đo.
           ✅ C4 — chạy 3 lần, đều FLUSHALL trước (đo đường xuống DB, không đo cache ấm).
              Dữ liệu: 101 recipe Published · 25 danh mục · Redis còn 605 key sau k6.
              Máy đo: i5-12450H (8C/12T) · 31.7 GB · Windows 11 Home.
              Workload: 3 scenario × 20 req/s × 30s = 3606–3607 request ≈ 120 req/s.
              http_req_failed = 0.00% (0/3607) ở cả 3 lần.

              Lấy số của LẦN GIỮA (run 2) — không phải lần đẹp nhất:
              | nhánh             | p50    | p95    | p99    |
              |-------------------|--------|--------|--------|
              | list_cached       |  6.85ms| 14.61ms| 26.46ms|
              | list_uncached     | 10.89ms| 18.07ms| 29.22ms|
              | read_mixed        |  5.69ms| 11.20ms| 18.01ms|
              | tất cả            |  6.23ms| 14.14ms| 25.06ms|

              Cả 3 lần đều xanh threshold: cached p95<200ms, uncached p95<800ms, mixed p95<500ms.
              Nhận xét phải nói kèm: uncached CHẬM HƠN cached ~1.5–1.8× ở p95, đúng như mong đợi
              của cache-aside. Nhưng ở tải này cả hai đường đều dưới 20ms nên CACHE CHƯA TẠO
              RA KHÁC BIỆT Ý NGHĨA — nói "cache giảm tải" ở mức p95 hiện tại là nói quá.
Giới hạn: k6 chạy trên chính máy dev có API + Postgres + Redis + MinIO cùng chạy (không tách
          tải), nên con số KHÔNG đại diện hạ tầng production. Chưa đo khi DB thật bị nghẽn tải
          (không có môi trường staging). Không có môi trường đo p95/p99 hữu ích khi tải cao.
Reviewer + ngày: ⬜
```

### TV4-K24 (N2-C1, N2-C1b, N2-C1c, N2-C2) — Outage drill: dừng Redis / S3 / DB / worker

```text
Evidence: TV4-K24
Tuần / Người / Task: 4 / TV4 / N2-C1, N2-C1b, N2-C1c, N2-C2
Đường dẫn: deploy/outage-drill.ps1, Tuan04/logs/c1_outage_drill.log,
           src/backend/CulinaryBlog.API/ApiExceptionHandler.cs (map lỗi DB → 503),
           src/backend/CulinaryBlog.API/Program.cs (fail-soft đăng ký lịch sitemap),
           src/backend/CulinaryBlog.Infrastructure/SitemapGenerator.cs ([AutomaticRetry(2)]),
           src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs (retry xoá 3 lần),
           src/backend/CulinaryBlog.Infrastructure/WelcomeEmail.cs (RetryDelays 0/1/5/30 phút),
           tests/CulinaryBlog.Tests/ApiExceptionHandlerDbUnavailableTests.cs,
           tests/CulinaryBlog.Tests/BackgroundJobRetryContractTests.cs
Lệnh chạy: powershell -NoProfile -ExecutionPolicy Bypass -File deploy/outage-drill.ps1
           dotnet test --filter "FullyQualifiedName~ApiExceptionHandlerDbUnavailableTests|FullyQualifiedName~BackgroundJobRetryContractTests"
Kết quả:  ✅ C2 — Redis tắt: đọc VẪN 200 (fallback cache in-process, log "Redis unavailable,
           falling back to in-process cache"), /health/ready 503. KHÔNG có 500.
           ✅ C1/S3 — S3 tắt: media.image đổi 404 → 503 (storage.unavailable), /health/ready 503.
           ✅ C1/DB — DB không truy cập được: /health/live 200, /health/ready 503,
           list(uncached) 503, detail 503 — tất cả 503 database.unavailable, KHÔNG 500.
           ✅ Phục hồi: Redis 0.2s · S3 0.5s · API 3.5s (tính từ lúc bật lại tới 200 đầu tiên).
           ✅ C1/worker — kill tiến trình API: /health/ready connection refused. Node đơn là
           SPOF, không có failover (xem Giới hạn).
           ✅ C1c — hợp đồng số lần retry khoá bằng test: sitemap 2, resize 3, xoá ảnh 3,
           welcome 3 lần theo lịch 0/1/5/30 phút. 10/10 test mới xanh.
           ✅ C1b — cache dùng chung giữa 2 tiến trình + sitemap distributed lock: xem TV4-K23
           (2 API thật 5080/5081 qua nginx, 10 request → api-1: 5 / api-2: 5) và TV4-K14
           (2 generator tranh lock → đúng 1 thắng).

            ⛔ HAI LỖI THẬT PHÁT HIỆN + ĐÃ SỬA TRONG LÚC DRILL, và LỖI THỨ BA DO CI BẮT (không phải chỉ ghi nhận):
           1. DB không truy cập được ⇒ 3 endpoint đọc trả **500 server.error**. Nguyên nhân:
              EfUnitOfWork chạy lệnh qua execution strategy của EF nên NpgsqlException gốc bị
              bọc thành InvalidOperationException("...likely due to a transient failure"), còn
              ApiExceptionHandler chỉ kiểm tra exception ngoài cùng nên không nhận ra. Sửa: dò
              CẢ chuỗi InnerException, map Npgsql/Socket/Timeout → 503 database.unavailable,
              và đặt nhánh này TRƯỚC nhánh 422 để DbUpdateException do mất kết nối không bị
              báo nhầm "dữ liệu đã thay đổi". 6 test hồi quy.
            2. DB không truy cập được lúc KHỞI ĐỘNG ⇒ AddOrUpdate của lịch sitemap ném
               NpgsqlException ra khỏi Main và **giết cả tiến trình**, mất luôn endpoint không
               cần DB. Sửa: bọc try/catch + log; /health/ready vẫn 503 nên orchestrator restart
               pod khi DB trở lại, lúc đó lịch được đăng ký lại.

            ⛔ LỖI THẬT THỨ BA — do **CI bắt được**, không phải do drill:
            Sau khi push, job `Backend week 1` đỏ 2 test (`RecipeAuthoringFlowTests`, và
            `TracingObservabilityTests` hỏng theo dây chuyền). Cả hai xanh ở máy dev.
            Nguyên nhân: CI **không có file `.env`** nên không có biến `Minio__*`, còn máy dev có
            trong `.env` — đúng loại lỗi mà môi trường dev che giấu.
            - `MinioClient.Build()` **ném** `MinioException: User Access Credentials not
              initialized` khi `AccessKey`/`SecretKey` rỗng, và `MinioStorageService` gọi nó ngay
              trong **constructor**.
            - B5 (issue #24) cho `GetRecipeBySlugHandler` tiêm `IRecipeImageDtoFactory` →
              `IObjectStorageUrlSigner` → `MinioStorageService`. Từ đó **mọi** lần đọc công thức
              công khai đều dựng service này, nên chỉ cần thiếu credential storage là
              `GET /api/v1/recipes/{slug>` trả **500 server.error** — hỏng cả endpoint không liên
              quan gì tới ảnh. Đây là lỗi **sản phẩm**, không phải lỗi test: đúng lúc deploy lên
              môi trường thiếu credential là mất sẵn trang công thức.
            - Sửa: `MinioClient` dựng **lazy** (`Lazy<IMinioClient>`). Constructor không còn ném,
              `Build()` chạy lần đầu khi thật sự gọi storage — tức nằm trong `GuardAsync`/catch
              của từng thao tác, nên `MinioException` bị `IsStorageFailure` bắt và thành **503
              storage.unavailable** đúng như tài liệu mô tả. Fail-fast lúc khởi động **không mất**:
              `MinioOptionsValidator` + `ValidateOnStart()` vẫn chạy ở mọi môi trường trừ Testing.
            - Khoá bằng **2 test hồi quy** trong `StorageFailureContractTests`:
              `Recipe_detail_still_readable_when_storage_credentials_are_missing` (thiếu credential
              thì đọc công thức vẫn 200) và `Upload_with_missing_storage_credentials_returns_503_not_500`
              (thiếu credential thì upload báo 503 chứ không 500). Đã kiểm chứng test có tác dụng:
              ép `Build()` chạy ở constructor thì **2 test đỏ**; bỏ ép thì **9/9 xanh**.
            - Sau khi sửa: **311/311** ở môi trường dev, và **311/311** khi dựng lại đúng điều kiện
              CI (không `Minio__*`, `Redis__Instance=ci`) — tức đã đóng được đúng chỗ CI đỏ.
            - **Đã xác nhận trên CI thật**: commit `8d9d62b` → run `37213966752` (`Backend week 1`)
              **thành công**, `311/311` + `ConcurrencySpike 5/5`, coverage gate 80% pass; Frontend run
              `37213966761` **thành công**. Cả hai job đã xanh trên branch
              `2312739_NHTSon_D5-D6-D7`.

           ⚠️ HAI HÀNH VI ĐÁNG GHI ĐỂ GIẢI THÍCH SỐ ĐO:
           - Khi DB chết mà Redis còn, list(page=1) vẫn **200** vì đọc cache. Đây là
             cache-aside đúng thiết kế, nhưng nghĩa là khi DB chết người dùng vẫn thấy DỮ LIỆU
             CŨ mà không có tín hiệu nào cho biết. Nếu không chấp nhận trả dữ liệu cũ thì phải
             thêm cờ hạn dữ liệu trong response — chưa làm.
           - App mất **~95–120 giây** mới bắt đầu LISTEN khi DB không truy cập được (Hangfire
             retry connection nhiều lần lúc khởi động). Readiness probe của orchestrator phải
             có startupProbe riêng, nếu chỉ dùng livenessProbe timeout ngắn sẽ giết app vô
             lý và tạo vòng lặp restart.
Giới hạn: ⚠️ KHÔNG dừng được PostgreSQL thật — máy này không chạy admin, `Stop-Service` và
          `pg_ctl stop` đều trả "Operation not permitted". Thay vào đó chạy instance thứ hai
          trỏ port DB không có gì lắng nghe (5499): lỗi Npgsql thật ở tầng app, chỉ khác là
          không cần quyền admin. Lệnh chuẩn để chạy thật: `Stop-Service postgresql-x64-18`
          (cần quyền Administrator).
          Đo trên một node đơn, không có load balancer/second node ⇒ số liệu failover là
          "mất dịch vụ", không phải "suy hao dịch vụ". Chưa đo RPO/RTO thật.
          S3/Redis dừng được bằng `docker stop` nên phần này là sự cố thật, không mô phỏng.
Reviewer + ngày: ⬜
```

---

## 5. Sổ kỹ năng lab (D6)

| Lab | Nhánh | Phase | Check | Kỹ thuật con | Sổ chi tiết | Trạng thái |
|---|---|---|---|---|---|---|
| L4 | `practice/TV4/L4` | `media` 25 · `email` 3 · `xml` 3 · `jobs` 8 | **39/39 PASS** (28/09) | K13, K14, K15 | `Tuan03/SOK_LAB_L4.md` ⚠️ *chỉ có trên nhánh `origin/practice/TV4/L4`, chưa có trong `main`* | ✅ Xong tuần 3 (chưa mở PR) |
| L4 mục 2 | `practice/TV4/L4` | Identity/Google/refresh/forms/FTS | ⬜ | K08, K09, K10, K11 | `Tuan04/SOK_LAB_L4_MUC2.md` | ⬜ Chưa làm (bù nợ tuần 3) |
| L5 | `practice/TV4/L5` | 7 phase: `search-ssr`, `isr-detail`, `query-rollback`, `image-opt`, `seo`, `observability`, `multi-instance` | **63 check** · 41 đạt · **3/7 PASS** (05/10) | K04, K05, K16, K17, K19, K20, K23 | `Tuan04/SOK_LAB_L5.md` + `logs/lab_l5_run.log` + `logs/lab_l5_db.txt` | 🟡 Đã chạy (commit `6c90ad8`, evidence `870d6e3`, PR #28 đã đóng). **4 phase lộ vấn đề thật**: ISR không hoạt động · ảnh không tối ưu 2 tầng · search `no-store` · RowVersion chưa kiểm chứng |

> Quy tắc: mock chỉ dùng cho unit/error test, **không** thay thế integration thật (DB/Redis/
> storage/provider). Thiếu Google credentials ⇒ ghi "integration ngoài còn chờ".

---

## 6. Nguyên tắc khi đóng ô kỹ năng

1. Ô có **nhiều kỹ thuật con** thì đánh dấu **từng kỹ thuật con**; thiếu một phần ⇒ ô chưa hoàn thành.
2. Một PR được tham chiếu nhiều ô khi nó thật sự chứa các kỹ năng đó.
3. "Đã đọc / đã họp / đã review code người khác / đã chạy lại demo nhóm" **không** tính.
4. Kết quả đo phải ghi **thiết bị, mạng, dữ liệu, số mẫu, công cụ, thời điểm, trạng thái cache**.
5. Không đưa secret hay tài khoản thật vào evidence; log đã redact.
6. Chỉ công nhận hoàn thành khi **reviewer Tâm xác nhận và ghi ngày**.
