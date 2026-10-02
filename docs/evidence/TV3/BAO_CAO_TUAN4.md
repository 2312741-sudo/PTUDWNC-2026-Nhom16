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
