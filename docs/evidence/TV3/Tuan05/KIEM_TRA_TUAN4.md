# Phần 0 — Kiểm tra lại Tuần 4 (TV3, trước khi làm Tuần 5)

Ngày chạy: 07/10/2026. Nhánh làm việc: `2312786_HuynhQuocTrung_C7-frontend-tests` (HEAD `66cd33e`).
`origin/main` tại thời điểm fetch: `262201b`. Người kiểm: Claude Code (tự động, không hỏi lại theo yêu cầu).

## 1. Đối chiếu tài liệu phân công trên origin/main mới nhất

Đọc `git show origin/main:docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` và `origin/main:README.md`.

### 1.1. Tuần 4 / Tuần 5 theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md` (mục 2, dòng lịch 6 tuần)

| Tuần | Cổng hoàn thành |
|---|---|
| 4 | **G4**: 24/24 kỹ năng mỗi người có minh chứng; **G5**: coverage đạt, sửa lỗi chặn luồng/bảo mật |
| 5 | **G6**: staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; 5 E2E pass |

Mục 3.3 (TV3), dòng "5 — C7": *Tự deploy/restore/test 2 API; điều phối migration sạch/nâng cấp; Nguyễn Thanh Tâm (Nhóm trưởng) review; số đo query/CWV/E2E* → đầu ra: *Migrations chạy lặp trên môi trường đã quy định; 5 critical E2E tích hợp; docs schema/version đầy đủ*.
Mục 3: *"Tất cả các PR đều do Nguyễn Thanh Tâm (Nhóm trưởng) review và phê duyệt."*

### 1.2. Tuần 4 / Tuần 5 theo `README.md` (khác tên cổng — ghi theo yêu cầu "nêu cả hai")

- README gọi cổng tuần 5 là **G5** ("Staging độc lập khởi động từ checkout sạch; restore dữ liệu thành công; 5 E2E flows pass"), trong khi `PHAN_CHIA_CONG_VIEC_6_TUAN.md` gọi đúng nội dung đó là **G6** và dùng "G5" cho tiêu chí coverage/bảo mật của tuần 4. **Hai file đặt tên cổng lệch nhau một số** (G4/G5 ở PHAN_CHIA ứng với tuần 4; G5/G6 ở README ứng với tuần 4/5). Theo yêu cầu đề bài, dùng file `PHAN_CHIA_CONG_VIEC_6_TUAN.md` làm chuẩn: tuần 4 = G4+G5, tuần 5 = G6.
- README bảng tuần 5, cột TV3: *"Tự deploy & restore DB độc lập; Điều phối migration sạch trên staging; Rà soát tính nhất quán dữ liệu; **Review kiểm thử TV2 (Trường Vĩ)**; Hoàn thiện tài liệu Schema."*
  → Đây là phân công review **chéo kiểm thử** (TV3 đọc/chạy thử test của TV2), **khác** với "Nguyễn Thanh Tâm review PR" ở PHAN_CHIA (review PR chính thức luôn là Tâm). Hai việc không mâu thuẫn nhau (một là review PR, một là review kiểm thử đồng nghiệp) — đã làm theo README ở phần T7.

## 2. Kiểm từng ô K01–K24 trong `BANG_MINH_CHUNG_K01_K24.md`

Phương pháp: trích toàn bộ hash commit dạng `` `xxxxxxx` ``, đường dẫn file bằng chứng, tên lớp test nêu trong bảng; đối chiếu bằng `git cat-file -e`, kiểm file tồn tại trên đĩa, và `git grep "class <Tên>"` trên cả hai repo (SP + lab).

- **66/66 hash commit** được trích ra đều tồn tại thật trong lịch sử git (không có hash bịa).
- **12/12 file bằng chứng** dẫn chiếu dạng `Tuan04/...` (SP) đều tồn tại dưới `docs/evidence/TV3/`.
- **13/13 file bằng chứng** dẫn chiếu dạng `lab:...` đều tồn tại dưới `D:\CulinaryBlog-lab\docs\evidence\TV3\`.
- **13/13 tên lớp test** nêu trong bảng (`RecipeAuthoringFlowTests`, `RecipeErrorBranchTests`, `RecipeIngredientHttpTests`, `RecipeConcurrencyAndTransactionTests`, `RecipeAuthoringAndCrudTests`, `LayerDependencyRulesTests`, `MyRecipesValidatorTests`, `Week3AuthAndPersonalLabTests`, `RecipeQueryPerformanceTests`, `SlowQueryInterceptor`, `RecipeMetricsExportTests`, `RecipeMetricsTests`, `RecipeDetailSeoTests`) có thật trong `tests/CulinaryBlog.Tests` (hoặc `Infrastructure`). `L1AuthTests`, `L3SearchTests` có thật trong `labs/TV3/Lab.TV3.Tests`.

**Không tìm thấy ô nào ghi XONG mà bằng chứng là bịa** — sổ minh chứng hiện tại trung thực về mặt "file/commit/tên test có tồn tại". Giữ nguyên bảng tổng hợp cũ (XONG 22 / CHƯA 2: K18, K24), **trừ K24 cần cập nhật theo phát hiện ở mục 4 dưới đây** (PR đã mở và đã merge, không còn "chưa mở").

| Trạng thái | Số ô | Danh sách |
|---|---|---|
| XONG, có bằng chứng xác nhận lại | 21 | K01–K17, K19–K23 |
| CHƯA (cần người, không đổi) | 1 | K18 — cần Trung nghe lại NVDA + điền 320/768/1200 tay |
| Cần sửa lại mô tả (không phải CHƯA đơn thuần) | 1 | K24 — xem mục 4 |

## 3. Chạy lại độc lập (từng bước, DB rỗng `cb_w5_seed`, API tắt khi build/test)

Lệnh dùng đúng theo quy ước (`$env:ConnectionStrings__Database=...` cho app chạy thật; `$env:TEST_DATABASE=...` cho `dotnet test`). Mọi số dưới đây **chạy thật**, không suy đoán.

| Bước | Lệnh | Kết quả thật |
|---|---|---|
| 1. Restore khoá | `dotnet restore CulinaryBlog.sln --locked-mode` | OK, khớp `packages.lock.json`, không có cảnh báo |
| 2. Format verify | `dotnet format CulinaryBlog.sln --verify-no-changes --severity error` | Exit `0` — không có thay đổi cần sửa |
| 3. Test + coverage (DB rỗng `cb_w5_seed`, API tắt) | `dotnet test CulinaryBlog.sln --collect:"XPlat Code Coverage" --results-directory TestResults\tuan5` | **CulinaryBlog.Tests 281/281**, **ConcurrencySpike 5/5**, 0 Failed/0 Skipped, 4m58s. Coverage `CulinaryBlog.Application` thật (đọc `coverage.cobertura.xml`): **line-rate 0.9423 = 2320/2462 dòng = 94,23%** (≥ 80% theo G4/G5). `CulinaryBlog.Domain` 84,3%; `CulinaryBlog.Infrastructure` 53,67%; `CulinaryBlog.API` 59,98%; tổng toàn solution 58,94%. |
| 4. npm ci | `npm ci` (src/frontend) | 484 packages, exit 0 (chỉ cảnh báo deprecated bình thường của npm, không liên quan code TV3) |
| 5. Typecheck | `npx tsc --noEmit` | Exit 0, không lỗi |
| 6. Jest | `npm test` | **14 suite / 85 test — 85/85 PASS** (có vài `console.error` "not wrapped in act()" vô hại, không phải test fail, đã có từ trước) |
| 7. Build | `npm run build` | Exit 0, **15/15 trang** build sạch; log có `[TypeError: fetch failed] ECONNREFUSED` trong lúc prerender vì API chưa chạy ở bước build — bình thường, các route liên quan là `ƒ` (dynamic/SSR), không phải lỗi build |
| 8. Playwright (API `--no-auto-migrate` + frontend bản build) | `npx playwright test` (DB seed 100 recipes/24 categories, cổng 5080/3000) | **8/11 pass**, 3 fail (`wizard-week4.spec.ts` dòng 99, 162, 280) — xem nguyên nhân ở mục 5 |

**Số liệu khác báo cáo cũ (05/10)**: báo cáo cũ ghi coverage `Application` 95,45%, backend 281/281 + 5/5, Jest 85/85. Lần chạy độc lập hôm nay (07/10, DB rỗng mới, không kế thừa trạng thái cũ) cho backend/Jest **khớp đúng số test**, nhưng coverage **thật là 94,23%**, không phải 95,45% — **chênh lệch khoảng 1,2 điểm %**, nguyên nhân chưa xác định (có thể do đo trước/sau một commit khác, hoặc coverage tool đếm khác khi có nhiều assembly). Ghi nhận số thật hôm nay (94,23%) làm số chính thức của báo cáo Tuần 5, không dùng lại số 95,45% cũ.

## 4. Phát hiện quan trọng — vượt ngoài dự đoán ban đầu, cần Trung biết ngay

### 4.1. PR #27 của TV3 **đã được mở và đã merge vào main** từ 05/10/2026 — khác hoàn toàn giả định "chưa mở PR nào"

Qua GitHub API (`api.github.com/repos/2312741-sudo/PTUDWNC-2026-Nhom16/pulls/27`):

- PR #27 *"feat(TV3): wizard soạn công thức, test C7, JSON-LD/SEO, sửa lỗi ảnh, a11y"*, nhánh `2312786_HuynhQuocTrung_C7-frontend-tests` → `main`.
- Tạo và merge **cùng ngày 05/10/2026** (06:02 → 12:56 UTC), **người merge là `Tson-dev` (TV4 — Trung Sơn)**, không phải Tâm.
- **0 review chính thức** (`GET /pulls/27/reviews` trả `[]`); 3 comment đều là bot (Vercel). Khớp với ghi nhận cũ trong `BANG_MINH_CHUNG_K01_K24.md` ("0 review trên PR") — phần đó đúng.
- **Nhưng dòng "PR C4/C7 chưa mở" trong K24 là SAI** — PR đã mở (05/10) và đã merge (05/10), chỉ là merge **không qua review của Tâm** như quy định ở `PHAN_CHIA_CONG_VIEC_6_TUAN.md` ("Tất cả các PR đều do Nguyễn Thanh Tâm review và phê duyệt"). Đây là một sai lệch quy trình thật (ai đó — TV4 — tự merge PR của TV3 mà không đợi Tâm), **không phải lỗi của Trung**, nhưng cần đưa vào báo cáo và hỏi Tâm/nhóm xem có cần review hồi tố hay không.
- Merge commit `2961a22` **đã nằm trong `origin/main` hiện tại** (`262201b` là ancestor chứa nó). Nghĩa là **toàn bộ nội dung nhánh làm việc hiện tại của TV3 đã vào main**; Tuần 5 nên tính từ `origin/main`, không phải chỉ từ nhánh riêng.

**Cập nhật K24**: đổi mô tả "Còn thiếu: ... PR C4/C7 chưa mở" → "PR #27 đã mở và đã merge 05/10 bởi TV4, **chưa có review chính thức của Tâm** (0 review ghi nhận trên GitHub) — vẫn giữ **CHƯA** cho đến khi có review, nhưng lý do là thiếu review, không phải thiếu PR."

### 4.2. `--migrate` "báo thành công giả" — tái hiện được thật trên máy, xác nhận TV4 đã sửa trên `origin/main` nhưng **chưa có trên nhánh TV3**

Đọc code `src/backend/CulinaryBlog.API/Program.cs` trên nhánh hiện tại (dòng 211–217):
```csharp
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    try { await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync(); } catch { }
    Console.WriteLine("Database migrations applied successfully.");
    return;
}
```
`catch { }` nuốt mọi lỗi rồi luôn in "applied successfully" — **đúng như TV3 đã báo trong `TV3_BAN_GIAO_TUAN4.md` §C.2**.

Trên `origin/main` (`git show origin/main:src/backend/CulinaryBlog.API/Program.cs`), đoạn này **đã được TV4 sửa** (commit `18ca04c`, PR #29, merge `262201b`): in lỗi thật ra `Console.Error` + `Environment.ExitCode = 1` khi migrate thất bại. **Đã đọc code xác nhận, không chỉ tin README.**

**Tái hiện lỗi thật** (vô tình, xem mục 4.3): khi chạy `--migrate` trên DB đã có sẵn schema một phần (không phải DB trống), lệnh in log `[ERR] Failed executing DbCommand ... CREATE TABLE "AspNetRoles"` (vì bảng đã tồn tại) **nhưng vẫn in "Database migrations applied successfully."** — đúng kịch bản lỗi đã mô tả, chạy thật không phải suy đoán.

**Việc tương tự `DbSeeder` ghi đè URL ảnh người dùng**: đọc code `src/backend/CulinaryBlog.Infrastructure/Persistence/DbSeeder.cs` trên nhánh TV3 — điều kiện cũ vẫn là so khớp theo kiểu cũ (chưa có đoạn lọc `photo-1546069901-ba9599a7e63c` / chỉ đụng `/images/` không phải `/images/recipes/` như trên `main`). Trên `origin/main`, DbSeeder **đã được TV4 sửa** (cùng commit `18ca04c`) để chỉ ghi đè ảnh seed, không đụng ảnh người dùng; có test hồi quy `DbSeederUserImageUrlTests` (có thật trên `origin/main`, **không có trên nhánh TV3 hiện tại** — 0 kết quả khi `git grep` trên HEAD).

**Kết luận mục này** (theo đúng yêu cầu "việc đầu tiên" của đề bài): **cả hai lỗi đã được TV4 sửa đúng trên `origin/main`** (không phải "chưa ai xác nhận bằng code" như giả định ban đầu — nay đã xác nhận bằng code thật), nhưng **nhánh làm việc hiện tại của TV3 chưa có hai bản sửa này** vì chưa merge `origin/main` mới nhất (nhánh TV3 dừng ở `66cd33e`, trước PR #29). Cập nhật mục "đã biết" trong `TV3_BAN_GIAO_TUAN4.md`: chuyển hai dòng DbSeeder/--migrate từ "vấn đề của người khác, không sửa" sang "đã được TV4 sửa trên main (`18ca04c`), nhánh TV3 cần merge `origin/main` để có bản sửa khi làm việc tiếp".

### 4.3. Sự cố do chính Claude Code gây ra trong phiên này — không gây hại thật, ghi lại trung thực

Khi chạy `--migrate` lần đầu, tôi gõ sai tên biến môi trường (`ConnectionStrings__DefaultConnection` thay vì `ConnectionStrings__Database`), khiến lệnh **thực chạy nhắm vào `culinary_blog`** (DB dev, cấu hình mặc định trong `appsettings.Development.json`) thay vì DB tạm `cb_w5_test` đã tạo. Hậu quả: in log lỗi `CREATE TABLE "AspNetRoles"` (vì migration chưa khớp lịch sử lệch đã biết) rồi vẫn báo "applied successfully" — đúng là tái hiện lỗi thật, nhưng **xảy ra trên DB dev, vi phạm quy tắc "KHÔNG chạy dotnet ef database update lên culinary_blog"**.

**Đã kiểm tra ngay sau đó và xác nhận không có thiệt hại**: `culinary_blog` sau sự cố vẫn **217 Recipes, 25 Categories, 487 AspNetUsers**, lịch sử migration vẫn chỉ 1 dòng `20260914061426_InitialCreate` (không đổi) — khớp đúng mô tả "lịch sử migration lệch" đã biết từ trước, không có bảng mới, không có dữ liệu mất. Nguyên nhân không hại: lệnh thất bại ngay ở bước đầu (bảng đã tồn tại) nên không có gì được áp dụng thêm.

Sau phát hiện, đã sửa bằng tên biến đúng, tạo DB tạm mới `cb_w5_seed` (xoá `cb_w5_test` cũ vì đã lẫn dữ liệu test), và toàn bộ các bước còn lại của Tuần 5 đều dùng `cb_w5_seed`/DB tạm khác, không còn đụng `culinary_blog`. Ghi nhận để Trung biết: **không có dữ liệu dev nào bị mất**, nhưng đây là lỗi thao tác cần cẩn thận hơn ở các lệnh `--migrate` sau.

### 4.4. Mâu thuẫn số liệu nhỏ trong seed: log nói "25 categories" nhưng DB thật chỉ có 24

`DbSeeder.SeedAsync` in: *"Database seeded and synchronized successfully: 25 categories, 100 recipes"*, nhưng đếm thật trên `cb_w5_seed` sau khi seed: **`Categories` = 24** (không phải 25), `Recipes` = 100 (khớp), `RecipeIngredients` = 1200, `RecipeSteps` = 600. Chưa rõ nguyên nhân (một category trùng slug bị bỏ qua, hoặc log hard-code sai số) — **không sửa vì `DbSeeder.cs`/`Categories` là phần của TV1/TV2, không phải TV3**; ghi handoff riêng.

## 5. Nguyên nhân 3 Playwright fail ở bước 3.8 — môi trường, không phải lỗi code

Cả 3 ca thất bại (`wizard-week4.spec.ts:99`, `:162`, `:280`) đều fail ở bước **upload ảnh** với lỗi `500` từ API; log API cho thấy nguyên nhân thật:
```
[ERR] Request failed with HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:9000)
```
→ MinIO/RustFS (cổng 9000) **chưa chạy** lúc test (Docker Desktop đang tắt ở đầu phiên). Không phải lỗi code TV3. Ghi chú thêm: API trả `500` thô cho lỗi storage-down thay vì `503 storage.unavailable` có kiểm soát — đây **là hành vi đã có sẵn trên nhánh TV3 hiện tại** (bản sửa B1 "503 storage.unavailable" là việc của TV4, nằm trong `origin/main` sau PR #29, **chưa có trên nhánh TV3**). Đã bật lại Docker Desktop (3 container `culinaryblog-redis/minio/mailhog` lên `healthy`), nhưng RAM máy tụt xuống **0,11–0,17 GB** trống — dưới mức an toàn đã quy định (0,3–0,8 GB) — nên **chưa chạy lại được 3 ca này ngay**; đã tắt API/frontend để giải phóng RAM (hồi về 0,32 GB), dự định chạy lại khi làm T6 (E2E luồng TV3) với ít tiến trình song song hơn.

## 6. GitHub Actions — kiểm được qua REST API (không cần `gh`)

- Nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`, commit `66cd33e`: **Frontend CI thành công**; check-runs của chính commit này cho thấy job `build-test` (workflow "Backend week 1") **cũng thành công** (2 lần, do cả event push và pull_request) — Backend CI **có chạy và xanh** cho PR #27, không phải "chưa từng chạy backend CI" như có thể hiểu nhầm ban đầu.
- Trên `main` sau khi merge PR #27 (`2961a22`): **"Backend week 1" THẤT BẠI** (khác với lúc chạy trên nhánh riêng) — phù hợp với mục đã ghi trong README (CI đỏ 4 lần liên tiếp sau merge, do 3 nguyên nhân của commit khác: khoá JWT lộ trong `.md` của TV4, `ApiFactory.EnsureMigrated()` không seed, race trong `TracingObservabilityTests`) — đã được TV4 sửa ở các commit sau, main hiện tại (`262201b`) **Backend week 1 + Frontend CI đều thành công**, riêng workflow **"Backup PostgreSQL" đang FAIL** ở `262201b` (chưa rõ nguyên nhân, không phải việc của TV3, ghi handoff nếu cần).

## 7. Việc Tuần 4 còn thiếu mà tự làm ngay được — đã làm

- Cập nhật ngay trong file này (không sửa `BANG_MINH_CHUNG_K01_K24.md` gốc ở bước này để tránh xung đột — sẽ cập nhật K24 ở bước commit riêng cuối Tuần 5 theo mục 9 của đề bài).

## 8. Việc cần người (đưa vào "VIỆC CỦA TRUNG" cuối báo cáo Tuần 5)

- K18: nghe lại NVDA sau 3 bản sửa + kiểm tay 320/768/1200 + zoom 200%.
- K24: xin Tâm review chính thức PR #27 (đã merge, review hồi tố) — hoặc nhóm quyết định không cần review hồi tố (cần Trung/Tâm quyết).
- Hỏi nhóm: ai cho phép TV4 merge PR #27 của TV3 mà không qua review của Tâm? Có cần quy trình chặt hơn cho Tuần 5?
- Lighthouse 3 lần lấy trung vị (việc của Trung theo phân công gốc).
