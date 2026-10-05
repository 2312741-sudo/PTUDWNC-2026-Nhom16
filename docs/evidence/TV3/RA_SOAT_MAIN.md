# Rà soát `origin/main` so với nhánh `2312786_HuynhQuocTrung_C7-frontend-tests` — TV3, 02/10/2026

Lệnh: `git fetch origin`, rồi chỉ dùng `git show`, `git ls-tree`, `git log`, `git grep`, `git diff`, `git merge-tree` (không giả định).

## 1. Trạng thái
| Mục | Kết quả thật |
|---|---|
| `origin/main` HEAD | `3d0695d` (2026-09-30) "Merge pull request #25 … TV2-week4-discovery-performance"; CI GitHub của commit này: **success** |
| HEAD nhánh C7 lúc rà | `68c9109` |
| Điểm rẽ (merge-base) | `0bc51fc` (2026-09-27, docs tuần 3 của TV3, vào main qua PR #15) |
| Commit chỉ có ở main | **45** (TV4 Nguyễn Hữu Trung Sơn 27 + Tson-dev 3; TV2 7 + tvi-158 1; TV1 Nguyễn Thanh Tâm 6 + 2312741-sudo 1) |
| Commit chỉ có ở C7 | **43** (đều của TV3) |
| `4bf775b` (UseSetting TEST_DATABASE) có trên main? | **CÓ** (`git merge-base --is-ancestor 4bf775b origin/main` → 0) |

## 2. OpenTelemetry / Meter trên main
- File: `src/backend/CulinaryBlog.API/Program.cs` dòng 155–167 (main), commit `2bbee0d` của TV4 (2026-09-24, "D5 OTEL trace/metrics"):
  `AddOpenTelemetry().ConfigureResource(AddService("CulinaryBlog.API"))` → tracing (AspNetCore, HttpClient, EF Core) + `AddOtlpExporter()`;
  metrics (AspNetCore, HttpClient, `AddMeter("Microsoft.EntityFrameworkCore")`) + `AddOtlpExporter()`.
- Gói (`CulinaryBlog.API.csproj` main): `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.19.1, `OpenTelemetry.Extensions.Hosting` 1.19.1,
  `OpenTelemetry.Instrumentation.AspNetCore` 1.19.0, `.EntityFrameworkCore` 1.19.0-beta.1, `.Http` 1.19.0.
- Exporter chỉ OTLP (mặc định `localhost:4317`); không có Console exporter; `appsettings.json` main không có mục OTel.
  `docker-compose.dev.yml` main có Seq (`datalust/seq:2026.1`) — Seq nhận log/trace, **không** phải nơi nhận metric.
- `RecipeMetrics.cs` (meter `CulinaryBlog.Recipes` của TV3) **chưa có** trên main → chưa ai `AddMeter` meter này.

## 3. Khác biệt main ↔ C7
- Main đổi 121 file kể từ điểm rẽ (nhiều nhất: `docs/evidence/TV4` 18, `src/frontend/src` 17, `Infrastructure` 15, `API` 7, `Application` 6).
- File **cả hai phía** cùng sửa: `src/backend/CulinaryBlog.API/Program.cs`, `src/backend/CulinaryBlog.Application/Recipes.cs`,
  `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs`, `tests/CulinaryBlog.Tests/packages.lock.json`.
  Phía main ở `Recipes.cs`: Publish/Unpublish nhận `IRecipeCacheService?` để xoá cache, thêm Archive (TV4); `Program.cs` +145 dòng (OTel, cache, health…).
- Workflow: main thêm `.github/workflows/frontend.yml` (Node 22: `npm ci`, `npx tsc --noEmit`, `npm test --if-present`, **`npm run build`**) và sửa `backend.yml`
  (concurrency, timeout 30', object storage RustFS thay MinIO, ghim SDK `10.0.401`).
- Thử merge không chạm cây làm việc: `git merge-tree --write-tree --name-only HEAD origin/main` → **exit 0, tree `6c74c5c`, không xung đột văn bản**.
  Rủi ro còn lại là ngữ nghĩa (code hai phía gặp nhau) → phải build + chạy toàn bộ test sau merge.

## 4. Quyết định
Merge `origin/main` vào nhánh C7 bằng `git merge --no-ff` (không rebase), build + test toàn bộ; sau đó thêm meter `CulinaryBlog.Recipes` vào MeterProvider
bằng một đăng ký riêng (`ConfigureOpenTelemetryMeterProvider(...AddMeter(RecipeMetrics.MeterName))`) để không sửa khối OTel của TV4. Kết quả ở `BAO_CAO_TUAN4.md` mục 10.
