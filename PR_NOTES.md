# Ghi chú PR — nhánh 2312786_HuynhQuocTrung_C7-frontend-tests (TV3)

Người viết: Huỳnh Quốc Trung (TV3). Reviewer: Nguyễn Thành Tâm (TV1). Cần TV4 (Nguyễn Hữu Trung Sơn) xem mục 1 và 2.

## 1. Chỉ mục `ux_recipe_images_one_primary` (IMAGE_CONTRACT §4, phần của TV4) — đổi bộ lọc cho khớp xoá mềm

- Cũ: `WHERE "IsPrimary" = true`. Mới: `WHERE "IsPrimary" = true AND "IsDeleted" = false`.
- Lý do: `AuditableEntityInterceptor` (D08) đổi Delete thành UPDATE `IsDeleted = true`; dòng ảnh xoá mềm vẫn giữ
  `IsPrimary = true` nên vẫn chiếm chỗ trong chỉ mục. Hệ quả: xoá ảnh duy nhất rồi tải ảnh mới -> 23505 -> 422
  `recipe.version_conflict`. Log Postgres: `duplicate key value violates unique constraint "ux_recipe_images_one_primary"`
  ở `INSERT INTO "RecipeImages"`. Cùng loại lỗi đã sửa cho bước ở 784459c.
- Tương thích ngược: bất biến "mỗi công thức tối đa 1 ảnh chính" vẫn giữ cho ảnh chưa xoá; ảnh đã xoá không hiện ở đâu
  (query filter `IsDeleted`). Không đổi cột, không đổi dữ liệu, API không đổi.
- Dữ liệu cũ: `culinary_blog` đang có 3 dòng xoá mềm vẫn `IsPrimary = true`; bộ lọc mới gỡ chúng mà không phải sửa dữ liệu.
  Test `Legacy_soft_deleted_row_still_primary_does_not_block_new_primary_upload` giữ điều này (đột biến về bộ lọc cũ -> đỏ).

## 2. Migration mới `20261004155213_RecipeImageOnePrimaryIgnoresSoftDeleted`

- Chỉ `DROP INDEX` / `CREATE INDEX` (cập nhật `RecipeImageConfiguration` và `AuthDbContextModelSnapshot`, snapshot chỉ đổi 1 dòng).
- `Down` sẽ lỗi nếu lúc đó có công thức mang 2 dòng `IsPrimary = true` (1 dòng xoá mềm + 1 dòng còn) — giống migration của bước.
- Lưu ý môi trường: trên DB dev `culinary_blog`, lịch sử migration lệch (chỉ có `20260914061426_InitialCreate`) nên
  `--migrate` in "Database migrations applied successfully." nhưng thực tế thất bại (`relation "AspNetRoles" already exists`)
  và migration này KHÔNG được áp. DB mới (CI, `culinary_test`) áp bình thường.

## 3. Xoá ảnh chính khi còn ảnh khác (cùng PR)

- Postgres kiểm unique ngay sau từng UPDATE; EF không biết bộ lọc của chỉ mục nên có thể đôn ảnh còn lại lên chính trước khi
  ảnh cũ được xoá mềm -> 23505 -> 422 (log: `UPDATE "RecipeImages" SET "IsPrimary"`).
- `Recipe.RemoveImage(id, promoteNext = true)` bỏ cờ chính của ảnh bị gỡ; thêm `Recipe.EnsurePrimaryImage()`.
- `DeleteRecipeImageHandler` (TV4) lưu 2 pha trong 1 transaction qua `IUnitOfWork` (như `ReorderStepsHandler`):
  xoá mềm trước, đôn ảnh chính sau. `IUnitOfWork` là tham số tuỳ chọn nên các test đơn vị dựng handler bằng tay không đổi.

## 4. `GET /api/v1/recipes/{key}` nhận cả mã công thức (Guid) ngoài slug

- Key là Guid -> tra theo id trước, không thấy mới tra theo slug (slug sinh từ tiêu đề có thể trông như Guid).
- Phân quyền giữ nguyên: Draft chỉ chủ sở hữu/Admin xem được, người khác nhận 404 như tra theo slug.
- Lý do: slug bản nháp đổi khi sửa tiêu đề; wizard ở tab thứ hai nạp lại theo slug cũ nhận 404. Wizard và trang edit giờ nạp theo id.
- `IRecipeRepository.FindByIdAsync` là default interface member (trả null) để các repository giả trong test của thành viên khác
  không phải sửa.

## 5. Kiểm tra đã chạy (05/10/2026)

| Lệnh | Kết quả |
|---|---|
| `dotnet restore --locked-mode` | exit 0 |
| `dotnet format --verify-no-changes --severity error` | exit 0 |
| `dotnet test` (DB rỗng `culinary_ci_check`, API tắt) | 281/281 + ConcurrencySpike 5/5; coverage Application 95,45% |
| `npx tsc --noEmit` / `npm test` / `npm run build` | exit 0 / 65/65 / thành công |
| `npx playwright test` | 11/11 (gồm ca mới (d2) tải ảnh, xoá, tải lại) |

## 6. Còn mở (đã báo TV4, không sửa trong PR này)

Xoá ảnh ngay khi job resize vừa ghi `ThumbnailUrl` có thể nhận 422: job đổi RowVersion dòng ảnh, lệnh xoá đã nạp trước
đó vài ms. Ca E2E (d2) chờ resize xong rồi mới xoá.
