# Runbook — Module soạn thảo công thức (Recipe, nguyên liệu, bước, wizard) — TV3

> Phạm vi: API `/api/v1/recipes/*`, `/api/v1/me/recipes*`, wizard `/dashboard/recipes/new|{id}/edit`, trang chi tiết `/recipes/{slug}`.
> Môi trường mẫu: Windows 11 + PowerShell 5.1 (dùng `;`, không `&&`), PostgreSQL 16 cài trên máy (không chạy trong Docker), Node 20+, .NET 10.
> **Không ghi mật khẩu vào file hay lệnh lưu lại**: đặt trong `.secrets.local.ps1` (đã bị git bỏ qua) rồi `. .\.secrets.local.ps1`.

## 1. Chạy hệ thống
```powershell
cd D:\CulinaryBlog; . .\.secrets.local.ps1                       # nạp $env:LAB_PG_PASSWORD
docker compose -f docker-compose.dev.yml up -d --no-deps redis mailhog minio minio-init   # chỉ cần khi dùng ảnh/email/cache
dotnet run --project src/backend/CulinaryBlog.API --launch-profile http -- --migrate      # áp migration rồi thoát
dotnet run --project src/backend/CulinaryBlog.API --launch-profile http -- --urls http://localhost:5080
cd src\frontend; npm ci; npm run dev                              # http://localhost:3000 (NEXT_PUBLIC_API_URL trong .env.local)
```
Kiểm nhanh: `GET http://localhost:5080/api/v1/categories` trả `{ data: [...] }`; mở `/dashboard/recipes/new` sau khi đăng nhập Author.

## 2. Migrate / seed
| Việc | Lệnh | Ghi chú |
|---|---|---|
| Áp migration | `dotnet run --project src/backend/CulinaryBlog.API -- --migrate` | Chạy được lặp lại; DB mới thì tạo đủ bảng |
| Seed dữ liệu mẫu | `dotnet run --project src/backend/CulinaryBlog.API -- --seed` | 25 danh mục, 100 công thức (mỗi công thức ≥ 10 nguyên liệu, ≥ 5 bước) |
| Thêm migration | `dotnet ef migrations add <TenCoNghia> --project src/backend/CulinaryBlog.Infrastructure --startup-project src/backend/CulinaryBlog.API` | Commit cả file migration + snapshot; chạy `dotnet format` |
| **Không làm** | `dotnet ef database update` lên `culinary_blog` | Lịch sử migration của DB dev lệch → lệnh lỗi (xem mục 4) |

## 3. Kiểm thử
```powershell
# Backend — LUÔN đặt TEST_DATABASE trỏ DB test riêng trước khi chạy
$env:TEST_DATABASE = "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet format CulinaryBlog.sln --verify-no-changes --severity error
dotnet test tests/CulinaryBlog.Tests
dotnet test tests/CulinaryBlog.Tests --collect:"XPlat Code Coverage" --results-directory TestResults\cov   # đọc coverage.cobertura.xml, gói CulinaryBlog.Application
# Đếm SQL + EXPLAIN (ghi báo cáo)
$env:K22_REPORT = "$PWD\TestResults\k22.md"; dotnet test tests/CulinaryBlog.Tests --filter "FullyQualifiedName~RecipeQueryPerformanceTests"
# Frontend
cd src\frontend; npx tsc --noEmit; npx jest
npx playwright install chromium; npx playwright test          # cần API 5080 + npm run dev; test tự đăng ký user, không cần tài khoản seed
# Tải (API + trang đang chạy)
Get-Content -Raw tests/k6/recipe-detail.js | docker run --rm -i grafana/k6 run --quiet -e API=http://host.docker.internal:5080/api/v1 -e WEB=http://host.docker.internal:3000 -e SLUG=<slug> -
```
Giống CI: tạo DB rỗng riêng, đặt TEST_DATABASE vào đó, `dotnet build CulinaryBlog.sln -c Release; dotnet test CulinaryBlog.sln --no-build -c Release`, xoá DB sau khi chạy.

## 4. Lỗi thường gặp
| Triệu chứng | Nguyên nhân | Xử lý |
|---|---|---|
| `dotnet build/test` báo file `.dll` đang bị dùng | API đang chạy giữ DLL | Tắt API (Ctrl+C hoặc dừng tiến trình cổng 5080) rồi build/test |
| Test xanh nhưng dữ liệu test xuất hiện trong `culinary_blog` | Quên đặt `TEST_DATABASE` (hoặc nhánh thiếu `UseSetting` trong `ApiFactory`) | Đặt `TEST_DATABASE`; kiểm DB test có bảng sau khi chạy |
| CI đỏ `28P01 password authentication failed` | Test dùng chuỗi trong `appsettings.json` thay vì TEST_DATABASE | Nhánh phải có `builder.UseSetting("ConnectionStrings:Database", TEST_DATABASE)` (commit `4bf775b`) |
| `dotnet ef database update` lỗi trên `culinary_blog` | Lịch sử `__EFMigrationsHistory` lệch | Dùng `--migrate` của API; tạo DB mới nếu cần |
| `400` khi POST/PUT dù dữ liệu đúng | JSON strict: body thừa/thiếu field so với record (`UnmappedMemberHandling.Disallow`) | Gửi đúng field: nguyên liệu `name, quantity, unit, notes`; bước `title, description, timerMinutes, imageUrl`; `difficulty` là số 1..4 |
| `422 recipe.concurrency_conflict` / `recipe.version_conflict` | `rowVersion` cũ (sửa ở tab khác) | Bấm "Tải dữ liệu mới nhất" rồi sửa lại (ADR-0002) |
| `403 recipe.forbidden` | Không phải tác giả và không phải Admin | Đúng thiết kế (NFR-SEC-006) |
| Thêm bước sau khi xoá bị 422 | DB còn unique `(RecipeId, StepNumber)` cũ không bỏ dòng xoá mềm | Áp migration `RecipeStepNumberUniqueIgnoresSoftDeleted` |
| JSON-LD trang chi tiết chưa đổi sau khi sửa | Layout cache API 60 s, trang ISR 300 s | Đợi hoặc tải lại 2 lần; wizard đã gọi `/api/revalidate` khi lưu |
| Wizard tự nhảy về bước 1 sau "Lưu & tiếp" | Lỗi cũ: `router.refresh()` sau `replaceState` sang `/edit` | Đã sửa (`64b29c1`); nếu gặp lại kiểm `RecipeWizard.refreshPublic` |
| Log `SLOW_SQL … ms (> 100 ms)` | Câu lệnh chậm hơn `Perf:SlowQueryMs` | Thường gặp ở lần tải đồng thời đầu tiên; nếu lặp lại: lấy SQL trong log, `EXPLAIN (ANALYZE, BUFFERS)` |
| Playwright `Executable doesn't exist` | Chưa cài trình duyệt | `npx playwright install chromium` |
| Lệnh PowerShell hỏng dấu `"` khi gửi JSON | PS 5.1 xử lý tham số native | Ghi JSON/SQL ra file rồi truyền file (`psql -f`, `curl --data @file`) |
| Máy chậm, Docker/next dev/test cùng lúc treo | RAM thấp (~0.5–1.4 GB trống) | Chạy từng việc nặng một; tắt API/next dev sau khi dùng; `dotnet build-server shutdown` |

## 5. Liên hệ / tài liệu
ADR-0001, ADR-0002 (`docs/adr/`); hợp đồng `docs/RECIPE_LIST_CONTRACT.md`, `docs/IMAGE_CONTRACT.md`; bằng chứng `docs/evidence/TV3/`.
Người phụ trách module: TV3 Huỳnh Quốc Trung; reviewer: Nguyễn Thanh Tâm.
