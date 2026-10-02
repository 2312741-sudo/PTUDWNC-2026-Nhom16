# BÁO CÁO TUẦN 4 — TV3 Huỳnh Quốc Trung (2312786) — 02/10/2026 (cập nhật cuối ngày)

> Mọi số liệu lấy từ lần chạy thật. Bằng chứng lab ở `docs/evidence/TV3/` (nhánh `practice/TV3/labs`);
> bằng chứng SP ở `D:\CulinaryBlog\docs\evidence\TV3\Tuan04\` (nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`).
> Đã push: lab `practice/TV3/labs`, SP `2312786_HuynhQuocTrung_C7-frontend-tests`. **Không mở PR, không rebase.**

## 1. Bảng việc

### 1a. Lab (đã xong từ đầu ngày, giữ nguyên)

| Việc | Trạng thái | Commit | Bằng chứng |
|---|---|---|---|
| K04/K10/K23 hồi quy | XONG — 55/55 | — | chạy đầu phiên |
| K16 SSR search | XONG — 5/5 | đỏ `a567013`, xanh `192e635` | `LAB_K16*.md/txt` |
| K19 sitemap/robots/301 | XONG — 9/9 | đỏ `32db275`, xanh `34220c5` | `LAB_K19*` |
| K20 log/correlation/trace/metric/health | XONG — 8/8 + chạy thật | đỏ `37985e4`, xanh `ac33ac8`, fix `a3e26a8` | `LAB_K20*` |
| K22 k6/EXPLAIN/N+1/SLOW_SQL | XONG — 5/5 + k6 đạt | đỏ `60728f7`, xanh `c4688b7` | `LAB_K22*` |
| Hồi quy cuối bộ lab | XONG — 82/82 (Redis tắt) + 8/8 L3SearchTests | `14ab3f9` (đã push) | `LAB_HOIQUY_tuan4.txt` |

### 1b. Việc A–I (SP + docs)

| # | Việc | Trạng thái | Commit (SP trừ khi ghi "lab") | Bằng chứng / số thật |
|---|---|---|---|---|
| A | Rà backend nguyên liệu/bước + test tích hợp Postgres thật | **XONG** | `14022ef` | `RecipeIngredientHttpTests` 4/4; không thấy lỗi thật ở backend |
| B | Jest: sửa/xoá nguyên liệu, `confirm` true/false, mở Sửa chặn chuyển bước, hoàn tác; StepsStep thêm/sửa/xoá/sắp xếp | **XONG** | `b2c83f5`, `34324e0` | 7 + 5 test mới; 16 test cũ vẫn xanh (28/28) |
| C | Playwright E2E tự đăng ký user, thêm/sửa/xoá nguyên liệu + dialog, thêm bước, xem lại | **XONG** | `0d4c368`, `e01c5f8` | 3/3 (lặp 3 lần); mật khẩu không có trong report — `Tuan04/C7_E2E_va_loi_wizard.md` |
| C+ | **Lỗi thật của TV3 (C4)** tìm ra nhờ E2E: wizard nhảy về bước 1 sau "Lưu & tiếp" | **ĐÃ SỬA** | đỏ `fc34f87`, xanh `64b29c1` | `WizardRefresh.test.tsx` 1 đỏ → 2/2 xanh; log chẩn đoán trước/sau |
| D | K17 TanStack Query thay optimistic/rollback tự viết | **XONG** | `b8a480b`, `e82cf0e`, `8f76f06` | Jest 30/30 sau từng commit; E2E 2/2; thử đột biến bỏ hoàn tác → 4 test đỏ |
| E | K18 sửa a11y wizard + dashboard; checklist WCAG + NVDA | **XONG phần code + checklist**; NVDA **CHƯA** (việc tay của Trung) | `64e8f8b`; checklist (lab) | `WizardA11y.test.tsx` 5/5; Jest 35/35; E2E 2/2; `LAB_K18_checklist.md` |
| F | Chốt SP: test giống CI trên DB rỗng, coverage, git status | **XONG** | — | `culinary_ci_check` (tạo rồi xoá): 209/209 + ConcurrencySpike 5/5; coverage Application **90.9%** (989/1088); `git status` sạch |
| G | Bỏ mật khẩu viết thẳng (C1-HOAN-THIEN dòng 32, 41; LAB_K10), sửa che nhầm "PostgreSQL" ở LAB_K20_chay_that | **XONG** | lab `8ff015f`; SP `1d64d66` | dùng `$env:LAB_PG_PASSWORD`; K20: `***QL OK` → `PostgreSQL OK`, `***ql` → `postgresql` |
| H | Báo cáo + giải thích | **XONG** | lab (commit docs cuối) | file này, `GIAI_THICH_TUAN4.md` |
| I | Mô tả PR C4 và C7-frontend-tests (không mở PR) | **XONG** | lab (commit docs cuối) | `PR_NOTES.md` |
| — | K20 metric recipe trên SP | XONG phần đếm; **CHƯA `AddMeter`** ở API (chờ OTel của TV4 trên `main`, không rebase) | `af534c1` | 3 test `RecipeMetricsTests` |

## 2. Kết quả kiểm tra cuối (chạy thật)

| Kiểm tra | Kết quả |
|---|---|
| SP `dotnet format CulinaryBlog.sln --verify-no-changes --severity error` | exit 0 |
| SP `dotnet test tests/CulinaryBlog.Tests` (TEST_DATABASE = `culinary_test`) | **209/209 Passed** (205 cũ + 4 mới) |
| SP `dotnet test CulinaryBlog.sln` trên DB rỗng `culinary_ci_check` (+ `restore --locked-mode`) | CulinaryBlog.Tests **209/209**, ConcurrencySpike **5/5**; DB đã xoá sau khi chạy |
| SP coverage (`--collect:"XPlat Code Coverage"`, `TestResults\tuan4_cuoi`) | CulinaryBlog.Application line-rate **90.9%** (baseline 30/09: 77.1%) |
| SP `npx jest` (src/frontend) | **35/35 Passed** (6 suite) |
| SP `npx tsc --noEmit` | exit 0 |
| SP `npx playwright test` (API 5080 + next dev 3000, Postgres `culinary_blog`) | 3/3 (repeat 3) trước K17; 2/2 sau K17; 2/2 sau K18 |
| Quét `Aa1!` (tiền tố mật khẩu) trong `playwright-report` (giải nén dữ liệu nhúng) + `test-results` | không có |

## 3. BLOCKED / CHƯA LÀM

- **NVDA + kiểm tay 320/768/1200 (K18)**: CHƯA — phải mở trình duyệt + NVDA. Kịch bản và ô trống ở `LAB_K18_checklist.md`.
- **AddMeter cho `CulinaryBlog.Recipes`**: CHƯA — cấu hình OTel (TV4) đang ở `origin/main`, nhánh C7 không rebase theo yêu cầu.
- **Lighthouse/CWV, Seq thật**: như báo cáo đầu ngày (không có công cụ/server).
- **Workflow CI frontend**: chưa thêm (cần hỏi Tâm).

## 4. Lệnh chạy lại (PowerShell 5.1)

```powershell
# SP
cd D:\CulinaryBlog; . .\.secrets.local.ps1
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet format CulinaryBlog.sln --verify-no-changes --severity error; dotnet test tests/CulinaryBlog.Tests
dotnet test tests/CulinaryBlog.Tests --collect:"XPlat Code Coverage" --results-directory TestResults\tuan4_cuoi
cd src\frontend; npx tsc --noEmit; npx jest
# E2E (Docker không cần): migrate rồi chạy API, chạy frontend, rồi Playwright
cd D:\CulinaryBlog; dotnet run --project src/backend/CulinaryBlog.API --launch-profile http -- --migrate
dotnet run --no-build --project src/backend/CulinaryBlog.API --launch-profile http -- --urls http://localhost:5080   # cửa sổ 1
cd src\frontend; npm run dev                                                                                         # cửa sổ 2
npx playwright install chromium; npx playwright test                                                                  # cửa sổ 3
# Test giống CI trên DB rỗng
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h 127.0.0.1 -U postgres -c "CREATE DATABASE culinary_ci_check;"
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_ci_check;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet restore CulinaryBlog.sln --locked-mode; dotnet test CulinaryBlog.sln
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h 127.0.0.1 -U postgres -c "DROP DATABASE culinary_ci_check WITH (FORCE);"

# Lab
cd D:\CulinaryBlog-lab; . .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests" -m:1
```
Docker (redis/minio/mailhog) đã được **dừng** trong lúc chạy E2E để đỡ RAM; bật lại: `docker start culinaryblog-redis culinaryblog-minio culinaryblog-mailhog`.

## 5. Giả định (của TV3)

- E2E chạy trên DB dev `culinary_blog` (yêu cầu C ghi rõ); mỗi lần chạy để lại 1 user `e2e.*@culinary.local` + 1 Draft.
- Đổi nguyên liệu/bước **không** đổi RowVersion của recipe (RowVersion nằm trên từng entity) → test ghi rõ là "hành vi hiện tại", không phải đặc tả.
  Hệ quả: hai tab sửa cùng một nguyên liệu thì bản lưu sau thắng (endpoint con không nhận rowVersion).
- Sửa lỗi nhảy bước bằng cách bỏ `router.refresh()` khi URL đã đổi sang `/edit` trong phiên tạo mới: `/api/revalidate` (cache ISR phía server)
  vẫn gọi; chỉ Router Cache phía trình duyệt của phiên đó không bị xoá (trang công khai đúng lại sau ≤ 5 phút hoặc khi tải lại).
- K17: `QueryClient` riêng cho wizard, `staleTime: Infinity`, không refetch khi focus/reconnect, không retry — giữ đúng hành vi cũ.
- K18: tên nút có thêm chữ ẩn (`Sửa nguyên liệu …`) — người nhìn thấy không đổi.

## 6. Thông tin môi trường (không nhạy cảm)

SP API `http://localhost:5080`, frontend `http://localhost:3000`, DB dev `culinary_blog`, DB test `culinary_test`; Playwright 1.63.0 (Chromium headless);
`@tanstack/react-query` 5.104. App lab `http://localhost:5090`, DB `lab_tv3`. Mọi tiến trình dev (API, next dev) đã tắt sau khi chạy.

## 7. Handoff (KHÔNG commit, ở `D:\CulinaryBlog-lab\handoff\`)

| File | Gửi | Nội dung |
|---|---|---|
| `TV2_header_link_quan_tri_DM_hien_cho_moi_nguoi.md` **(mới)** | TV2 | Header luôn hiện "Quản trị DM" kể cả với Author/khách; backend vẫn chặn bằng AdminPolicy (mức thấp) |
| `TV1_seed_user_khong_co_mat_khau.md` (cập nhật) | TV1 | Không còn chặn Playwright (E2E tự đăng ký) |
| `TV2_TV1_google_login_demo_token.md` | TV2, TV1 | (đầu ngày) |
| `TV2_TV1_login_label_khong_gan_input.md` | TV2, TV1 | (đầu ngày) |

## 8. CI (cập nhật chiều 02/10) — chi tiết ở `CI_TAI_HIEN_tuan4.md`

**Trạng thái CI GitHub sau khi sửa: chưa xác nhận CI GitHub** (chờ Trung báo kết quả run của `2159787`).

**Nhánh đỏ là `practice/TV3/labs`, không phải `2312786_HuynhQuocTrung_C7-frontend-tests`** (GitHub REST API công khai: C7-frontend-tests xanh cả 6 lần push;
lab đỏ ở mọi lần push từ `9e6622a` tới `55b9a57`, cùng bước `dotnet test CulinaryBlog.sln --no-build --configuration Release ...`).

**Nguyên nhân:** nhánh lab thiếu commit `4bf775b` của chính TV3 (hqt7105) đã có trên `main` — dòng `builder.UseSetting("ConnectionStrings:Database", TEST_DATABASE)`
trong `ApiFactory`. Nhánh lab rẽ khỏi `main` tại `4515021` trước commit đó. Thiếu dòng này, test dùng chuỗi kết nối trong `appsettings.json` thay vì TEST_DATABASE:
máy dev có user/DB đó nên xanh (nhưng ghi vào `culinary_blog`), CI không có nên `28P01 password authentication failed`.
**Đã đồng bộ** bằng `2159787` (3 dòng trong `AuthTests.cs`, theo `4bf775b`).

| Tái hiện (Release, đúng lệnh CI, DB rỗng `culinary_ci_repro` + user riêng) | Spike | CulinaryBlog.Tests |
|---|---|---|
| SP `1d64d66` như CI / `-maxcpucount:1` | 5/5 / 5/5 | 209/209 / 209/209 |
| lab `55b9a57` như CI (máy dev) | 5/5 | 120/120 — DB TEST_DATABASE **0 bảng** (test ghi vào `culinary_blog`) |
| lab `55b9a57`, chuỗi mặc định hỏng giống CI | 5/5 | **102 pass / 18 fail** |
| **lab `2159787`**, chuỗi mặc định hỏng giống CI (restore/build Release/format exit 0) | **5/5** | **120/120**, DB TEST_DATABASE có 14 bảng |

**Đính chính:** con số "lab 120/120 trên DB rỗng `culinary_lab_check`" ở lần push lab `55b9a57` là sai về DB: khi đó test chạy vào DB dev `culinary_blog`.

**Còn là đoán / chưa kiểm:** CI thật đỏ đúng 18 test kể trên (chưa đọc được log job); khác biệt Linux (collation/TZ) chưa kiểm vì không chạy Docker
(RAM trống 0.8–0.9 GB). Loại trừ có bằng chứng: song song Spike/Tests, migration trên DB rỗng, cấu hình Release.

## 9. Cập nhật tối 02/10 — K19, K22, K24, kiểm lại L1/L3/L4 (SP: nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`)

| Việc | Trạng thái | Commit (SP trừ khi ghi lab) | Số thật / bằng chứng |
|---|---|---|---|
| K19 JSON-LD trang chi tiết (NFR-SEO-001) | **XONG** (validator ngoài: Trung chạy tay) | đỏ `5930a41` → xanh `f0cc239`, docs `6050356` | HTML thật trước: thiếu `image`, `author`; sau: 12/12 thuộc tính, không rating — `Tuan04/K19_jsonld_chi_tiet.md` |
| K19 — tên rỗng / danh mục xoá mềm | **XONG** — lỗi thật của TV3 (danh mục xoá làm mất tên tác giả) | đỏ `2f406f4` → xanh `e46f951`; Jest đỏ `4d1c4b5` → xanh `085a25a` | RecipeDetailSeoTests 3/3; Jest recipe-jsonld 11/11 |
| K22 SLOW_SQL > 100 ms (NFR-PERF-004) | **XONG** | đỏ `227fcb0` → xanh `cbfdc01` | 3 test; log API thật có `SLOW_SQL` lúc k6 bắt đầu |
| K22 không N+1 + EXPLAIN + k6 | **XONG** phần đo; Lighthouse **CHƯA** (tay) | `b70f114`, docs `1b5c837` | số lệnh SQL bằng nhau nhỏ/lớn; `Tuan04/K22_hieu_nang_recipe.md` |
| K22 AsSplitQuery trang chi tiết | **XONG** | đỏ `625b3fd` → xanh `b2789e4`, docs `78b824b` | 5 lệnh cố định; 18 dòng thay vì 60 (10 × 6); EXPLAIN 0.212 ms; k6 API p50 6.54 / p95 11.35 / p99 16.73 ms, 0% lỗi |
| Coverage Application | **XONG** | — (đo ở `085a25a`) | **92.58% (1012/1093 dòng)**, branch 68.69%; backend 217/217 |
| K24 ADR-0002 + runbook | **XONG** (chờ Tâm review) | `7ffd290` | `docs/adr/ADR-0002-recipe-version-schema-va-nap-du-lieu.md`, `docs/RUNBOOK_SOAN_THAO_CONG_THUC.md` |
| K24 secret scan cục bộ | **XONG** | `2b10cdd` | gitleaks 8.30.1: lịch sử git mọi nhánh 205 commit, 8 phát hiện đều dương tính giả; bí mật thật chỉ ở `.secrets.local.ps1`, `.env.local` (bị git bỏ qua) — `Tuan04/K24_secret_scan.md` |
| K24 sổ minh chứng K01–K24 | **XONG** | `2b32e8c`, cập nhật `68c9109` | `docs/evidence/TV3/BANG_MINH_CHUNG_K01_K24.md`: XONG 19 / CHƯA 5 (K18, K20, K22, K23, K24) |
| Kiểm lại L1/L3/L4 (K08, K09, K11–K15) | **XONG** | lab `a5425e5` | 42/42 không Docker + 2/2 Redis/MinIO thật; K09 Google thật, không chờ credentials — `LAB_L1_L3_L4_kiem_lai_tuan4.txt` |
| K23 build image / Nginx 2 instance / backup file | **BLOCKED (RAM)** | lab (ghi số đo vào `LAB_K23.md`) | RAM trống 1.04 → 1.40 GB sau khi dọn; 593 / 598 / 559 MB lúc 18:25 (ngưỡng 1.5 GB) |

Kiểm tra cuối (SP, HEAD `68c9109`): `dotnet format CulinaryBlog.sln --verify-no-changes --severity error` exit 0; `dotnet test tests/CulinaryBlog.Tests` 217/217
(lần chạy kèm coverage ở `085a25a`, các commit sau chỉ là tài liệu); `npx jest` 46/46; `npx tsc --noEmit` exit 0.

Ghi chú vận hành (trung thực): khi dọn RAM cho K23 đã dừng 4 tiến trình `node.exe` mà **chưa kiểm dòng lệnh từng cái** (đoán là worker Jest/Next còn sót);
nếu một công cụ của Trung chạy bằng node bị tắt theo thì mở lại. `FindForWriteAsync` (nạp để ghi) vẫn JOIN nguyên liệu × bước — đề xuất tách tiếp nếu công thức lớn.