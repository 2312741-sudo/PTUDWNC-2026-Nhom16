# Tuần 6 (W6) — Bàn giao test & DB docs (TV3, C1–C7)

## 1. Test — cách chạy lại toàn bộ

### Backend (`tests/CulinaryBlog.Tests`, `tests/concurrency-spike`)
```powershell
$env:MSBUILDDISABLENODEREUSE=1
$env:TEST_DATABASE="Host=127.0.0.1;Port=5432;Database=<DB_RONG_MOI>;Username=postgres;Password=postgres"
# BẮT API trước khi test: file bị khoá nếu đang chạy.
dotnet test CulinaryBlog.sln
dotnet build-server shutdown
```
Số thật gần nhất (07–08/10/2026, nhánh `C8/C9-tuan6`, DB rỗng): **421/421 + ConcurrencySpike 5/5**. Coverage `CulinaryBlog.Application` **94,23%** (≥ 80% theo G4).

### Frontend Jest/RTL (`src/frontend`)
```powershell
npm ci; npx tsc --noEmit; npm test
```
**85/85** (14 suite) — gồm `WizardA11y.test.tsx`, `DashboardA11y.test.tsx` (khoá 3 lỗi NVDA K18), `recipe-jsonld.test.tsx`, `metadata.test.ts`, các test bước wizard.

### Playwright E2E (`src/frontend/e2e`, của TV3)
```powershell
npm run build; npm start   # (hoặc API riêng --no-auto-migrate + DB đã --seed)
npx playwright test create-recipe wizard-week4 k18-a11y-names k18-reflow-screenshots
```
Số thật 08/10: **11/11** (`create-recipe.spec.ts` + `wizard-week4.spec.ts`), **5/5** (`k18-a11y-names.spec.ts`), **1/1** (`k18-reflow-screenshots.spec.ts`, 17/18 mốc đo OK — xem mục 3). 4 file spec này đều của TV3; **không** gồm `recipe-publish.spec.ts` (TV4) — xem `handoff_TV4_recipe-publish-spec_strict-mode.md`.

### Lab (`labs/TV3`, worktree `D:\CulinaryBlog-lab`, nhánh `practice/TV3/labs`)
```powershell
dotnet test labs/TV3/Lab.TV3.Tests --filter "Infra!=docker"
```
Số thật Tuần 4: 82 test (Redis tắt) + 8 `L3SearchTests` (cần Redis `:6399`). **Không merge lab vào main.**

## 2. DB docs

- **Schema đầy đủ** (ERD, chỉ mục, ON DELETE, RowVersion, migration kèm mục đích, đối chiếu SRS): `docs/evidence/TV3/Tuan05/SCHEMA_RECIPE.md`.
- **ADR**: `docs/adr/ADR-0001-recipe-schema-va-concurrency.md` (schema + concurrency, đã chấp nhận), `ADR-0002-recipe-version-schema-va-nap-du-lieu.md` (RowVersion/migration/nạp aggregate, đang chờ Tâm review).
- **Giải thích demo nhanh**: `docs/evidence/TV3/Tuan06/GIAI_THICH_AGGREGATE_DEMO.md` (W5).
- **Tính nhất quán dữ liệu** (kết quả SELECT-only trên DB sạch + `culinary_blog` chỉ đọc): `docs/evidence/TV3/Tuan05/NHAT_QUAN_DU_LIEU.md`.
- **8 migration** (`src/backend/CulinaryBlog.Infrastructure/Migrations/`): `InitialIdentity` (TV1) → `AddCategoryModule` (TV2) → `AddRecipeAggregate` → `RecipeChildIdsValueGeneratedNever` → `AddFtsAndGinIndex` (TV2) → `RecipeStepNumberUniqueIgnoresSoftDeleted` → `RecipeImageIdValueGeneratedNever` → `RecipeImageOnePrimaryIgnoresSoftDeleted`.
- **Lệnh chuẩn** (không chạy nhầm vào `culinary_blog`):
  ```powershell
  $env:ConnectionStrings__Database="Host=127.0.0.1;Port=5432;Database=<DB>;Username=postgres;Password=postgres"
  $env:Jwt__SigningKey="<tu sinh bang 'openssl rand -base64 48', khong commit>"
  dotnet run --project src/backend/CulinaryBlog.API -- --migrate
  dotnet run --project src/backend/CulinaryBlog.API -- --seed
  ```

## 3. Hạn chế/handoff đang mở (chưa ai sửa, không phải việc của TV3)

| Handoff | Tóm tắt | Người nhận |
|---|---|---|
| `handoff_TV1_layout_flex_scroll_320px.md` | Dashboard cuộn ngang thật ở 320px, nghi do `layout.tsx` flex ở `<body>`/`<main>` | TV1 |
| `handoff_TV1_migrate_bao_thanh_cong_khi_loi.md` | (đã có từ trước; TV4 đã sửa trên `main` qua `18ca04c`, nhánh TV3 cần merge | TV1/đã sửa trên main |
| `handoff_TV4_ImagesStep_blob_preview_bi_mat.md` | Đã có kết luận (B5 presigned URL có chủ đích) — giữ hồ sơ, không cần trả lời thêm | — (đã xử lý) |
| `handoff_TV4_backup_postgres.md` | CI "Backup PostgreSQL" thiếu secret `DATABASE_URL` | TV4/người quản lý CI |
| `handoff_TV4_recipe-publish-spec_strict-mode.md` | `recipe-publish.spec.ts` (TV4) lỗi `getByLabel` strict mode với aria-label "(dòng N)" của TV3 | TV4 |
| `handoff_TV4_resize_xoa_anh_422.md` | (đã có từ trước, xem nội dung file) | TV4 |

**Lưu ý vận hành**: Redis (container `culinaryblog-redis`) dùng chung giữa các lần tạo DB tạm khác nhau trong một phiên làm việc — `GET /categories` có thể trả ID của DB CŨ nếu chưa `FLUSHALL`. Luôn `docker exec culinaryblog-redis redis-cli FLUSHALL` sau khi tạo DB tạm mới trước khi test qua UI/API.

## 4. Việc chỉ Trung làm được (không lặp lại ở đây, xem `BAO_CAO_TUAN6.md` mục "VIỆC CỦA TRUNG")
Nghe lại NVDA (K18), xin Tâm review (K24), gửi tin nhóm, mở PR, demo trước thầy.
