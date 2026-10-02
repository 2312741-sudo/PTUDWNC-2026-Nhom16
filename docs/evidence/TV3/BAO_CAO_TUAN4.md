# BÁO CÁO TUẦN 4 — TV3 Huỳnh Quốc Trung (2312786) — 02/10/2026

> Mọi số liệu dưới đây lấy từ lần chạy thật; file bằng chứng nằm cùng thư mục `docs/evidence/TV3/`.
> **Chưa push** nhánh nào: CLAUDE.local.md quy định không tự push → Trung kiểm tra rồi tự push (lệnh ở mục 4).

## 1. Bảng việc

| # | Việc | Trạng thái | Commit | Bằng chứng |
|---|---|---|---|---|
| 0 | Recon + kế hoạch | XONG | `fc0c99f` | `KE_HOACH_TUAN4.md` |
| 0 | File bí mật `.secrets.local.ps1` (2 thư mục, đã exclude, không có trong `git status`) | XONG | — (không commit) | — |
| 1 | Hồi quy lab K04/K10/K23 (Redis tắt, bỏ L3SearchTests) | XONG — 55/55 | — | chạy đầu phiên |
| 2a | LAB K16 SSR search | XONG — 5/5 | đỏ `a567013`, xanh `192e635` | `LAB_K16.md`, `LAB_K16_do.txt`, `LAB_K16_xanh.txt` |
| 2b | LAB K19 sitemap/robots/301 | XONG — 9/9 | đỏ `32db275`, xanh `34220c5` | `LAB_K19.md`, `_do.txt`, `_xanh.txt` |
| 2c | LAB K20 log sink/correlation/trace/metric/health | XONG — 8/8 (+ chạy thật) | đỏ `37985e4`, xanh `ac33ac8`, fix `a3e26a8` | `LAB_K20.md`, `_do.txt`, `_xanh.txt`, `LAB_K20_chay_that.txt` |
| 2d | LAB K22 k6/EXPLAIN/N+1/SLOW_SQL | XONG — 5/5 + k6 đạt | đỏ `60728f7`, xanh `c4688b7` | `LAB_K22.md`, `_do.txt`, `_xanh.txt`, `LAB_K22_explain.txt`, `LAB_K22_k6.txt` |
| 2e | Hồi quy cuối bộ lab | XONG — 82/82 (Redis tắt) + 8/8 L3SearchTests (Redis 6399) | — | `LAB_HOIQUY_tuan4.txt` |
| 3 | C7 Playwright (D:\CulinaryBlog) | **BLOCKED** — đã viết config + test, **chưa chạy luồng thật** | `ce2a057` (nhánh C7-frontend-tests) | `C7_PLAYWRIGHT_chua_chay.txt` |
| 4a | K05 (RHF + Zod wizard) trên SP | ĐÃ CÓ từ trước (commit `22654aa`, `6c5f3da`, `abe2352`; Jest 16/16) | — | — |
| 4b | K17 TanStack Query + optimistic rollback | **CHƯA LÀM** phần TanStack Query (optimistic/rollback tự viết bằng reducer đã có) | — | mục 3 |
| 4c | K18 a11y 320/768/1200 + NVDA | **CHƯA LÀM** (phải làm tay, cần mở trình duyệt + NVDA) | — | — |
| 4d | K20 metric recipe trên SP | XONG phần đếm (`culinary.recipes.created/updated`), **chưa AddMeter ở API** | `af534c1` (nhánh C7-frontend-tests) | backend 205/205 |
| 5 | Báo cáo + giải thích + handoff | XONG | (commit docs cuối) | file này, `GIAI_THICH_TUAN4.md`, `handoff/` (không commit) |

## 2. Kết quả kiểm tra trước push

| Kiểm tra | Kết quả thật |
|---|---|
| `dotnet format` Lab.TV3.Api / Lab.TV3.Tests `--verify-no-changes --severity error` | exit 0 / exit 0 |
| `dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests" -m:1` | 82/82 Passed |
| L3SearchTests với Redis `localhost:6399` | 8/8 Passed (container đã `docker rm -f`) |
| SP: `dotnet format CulinaryBlog.sln --verify-no-changes --severity error` | exit 0 |
| SP: `dotnet test CulinaryBlog.sln` (TEST_DATABASE = culinary_test) | CulinaryBlog.Tests 205/205, ConcurrencySpike 5/5 |
| SP: `npx jest` (src/frontend) | 16/16 |
| SP: `npx tsc --noEmit` | exit 0 |
| SP: `npx playwright test` | 1 skipped (thiếu E2E_PASSWORD) |
| **Chưa làm**: test "giống CI" trên DB rỗng `culinary_ci_check`; đo lại coverage Application sau commit metric | — |

## 3. Việc BLOCKED / CHƯA LÀM và lý do

- **Playwright (C7)**: `DbSeeder.cs` tạo user seed **không có mật khẩu** → không có `E2E_PASSWORD` → không đăng nhập được. Theo quy tắc không đoán mật khẩu.
  Test viết xong, `--list` thấy 1 test, chạy thì `skipped`. Gỡ chặn: TV1 seed mật khẩu từ biến môi trường (xem handoff), sau đó
  `npx playwright install chromium` rồi `$env:E2E_PASSWORD="..."; npm run e2e` (cần API 5080 + frontend 3000).
- **K17 TanStack Query**: chưa thêm `@tanstack/react-query`. Hiện wizard đã có optimistic + rollback qua `run()`/reducer (`RecipeWizard.tsx:134`) và test RTL.
  Đề xuất: bọc `QueryClientProvider` ngay trong `RecipeWizard` (không đụng `app/layout.tsx` dùng chung), chuyển `addIngredient`/`addStep` sang `useMutation` với
  `onMutate` (snapshot + setQueryData), `onError` (rollback), `onSettled` (invalidate `['recipe', id]`), giữ test RTL hiện có + thêm test rollback.
- **K18**: checklist 320/768/1200 + NVDA là việc tay — chưa làm. Đã ghi lại một lỗi a11y ở form đăng nhập (của TV2/TV1) trong handoff.
- **K20 SP export**: `RecipeMetrics` chỉ dùng BCL; cần thêm `.AddMeter(RecipeMetrics.MeterName)` vào cấu hình OTel của API — OTel (TV4, D5) đang ở `origin/main`, nhánh C7 chưa rebase.
- **Lighthouse/CWV**: máy không có `lighthouse` → chưa đo.
- **Seq**: không có server Seq → sink Seq mới cấu hình được, chưa kiểm thật.

## 4. Lệnh chạy lại (PowerShell 5.1)

```powershell
# Lab
cd D:\CulinaryBlog-lab; . .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests" -m:1
docker run -d --name lab-redis-6399 -p 6399:6379 redis:7-alpine; $env:LAB_REDIS="localhost:6399"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L3SearchTests" -m:1; docker rm -f lab-redis-6399; Remove-Item Env:LAB_REDIS
# k6 (app lab đang chạy ở 5090)
Get-Content -Raw labs/TV3/k6/search.js | docker run --rm -i grafana/k6 run --quiet -e BASE=http://host.docker.internal:5090 -

# SP
cd D:\CulinaryBlog; . .\.secrets.local.ps1
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet format CulinaryBlog.sln --verify-no-changes --severity error; dotnet test CulinaryBlog.sln
cd src\frontend; npx jest; npx playwright test

# Push (Trung tự chạy sau khi xem lại; chỉ nhánh của mình)
git -C D:\CulinaryBlog-lab push origin practice/TV3/labs
git -C D:\CulinaryBlog push origin 2312786_HuynhQuocTrung_C7-frontend-tests
```

## 5. Giả định (của TV3, không phải yêu cầu giảng viên)

Chi tiết ở mục "Giả định" đầu mỗi `LAB_Kxx.md`. Tóm tắt: K16 = trang SSR trong Lab.TV3.Api (không dựng Next.js trong lab); K19 = sitemap/robots/canonical/301
theo D14; K20 = Serilog Console/File/Seq theo cấu hình + OTel exporter Console + health live/ready; K22 = k6 20 VU/30 s/p95 < 500 ms, đếm SQL để chứng minh không N+1,
ngưỡng chậm 100 ms. Metric SP: tên `culinary.recipes.created/updated` do TV3 tự đặt.

## 6. Thông tin môi trường (không nhạy cảm)

App lab `http://localhost:5090`, DB `lab_tv3` (đã seed thêm 200 công thức Published để đo k6, user `tv3-k22-seed-*@lab.test`), DB test `lab_tv3_test`;
SP API `http://localhost:5080`, frontend `http://localhost:3000`, DB test `culinary_test`; email seed E2E `trung.huynh@culinary.local`.
Không có `k6`, `lighthouse`, `psql` trong PATH; k6 chạy bằng image `grafana/k6`. RAM trống lúc chạy ~0.5 GB.

## 7. Handoff (KHÔNG commit, ở `D:\CulinaryBlog-lab\handoff\`)

| File | Gửi | Nội dung |
|---|---|---|
| `TV2_TV1_google_login_demo_token.md` | TV2, TV1 | Nút Google gửi token cứng `...demo_token`; frontend không có `/api/auth/callback/google` (404 suy ra từ cấu trúc thư mục, chưa chạy trình duyệt) |
| `TV1_seed_user_khong_co_mat_khau.md` | TV1 | User seed không có PasswordHash → không đăng nhập được, chặn Playwright |
| `TV2_TV1_login_label_khong_gan_input.md` | TV2, TV1 | Label form đăng nhập không gắn input (a11y) |
