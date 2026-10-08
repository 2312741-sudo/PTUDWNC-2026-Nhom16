# ADR-TV4-003 — Quy ước soft-delete: `Recipe` soft delete, `Category` hard delete

Ngày: 09/10/2026 · Người thực hiện: Nguyễn Hữu Trung Sơn (2312739) — TV4.
Phạm vi: quy ước vòng đời xoá dữ liệu được dùng trong domain + repository. Không thay đổi hành vi hiện hành.
Trạng thái: **Chốt quy ước + ghi tài liệu** (đóng phần "Phải đính chính" của `BUG-W4-06`).
Liên quan: `ADR-0001` (soft delete), `docs/adr/ADR-TV4-002` §D27, `docs/evidence/TV4/Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md` `BUG-W4-06`.

---

## 1. Tóm tắt (TL;DR)

Hai thực thể liên quan tới nút "Xoá" có **cách xoá khác nhau có chủ đích**:

| Thực thể | Cách xoá | Triển khai |
|---|---|---|
| `Recipe` | **Soft delete** (`IsDeleted = true`, giữ dòng DB) | `Entity.SoftDelete()` → interceptor chuyển `Deleted → Modified(IsDeleted=true, RowVersion++)` (ADR-0001) |
| `Category` | **Hard delete** (`DELETE` dòng) | `CategoryRepository.DeleteAsync` → `db.Categories.Remove(category)` (C07) |

Toàn bộ truy vấn `Recipe`/`Category` còn lọc `IsDeleted = false` qua **global query filter** nên phần
"ẩn" hoạt động cho cả hai — khác biệt nằm ở việc **dòng DB có còn hay không**.

> ⚠️ `BUG-W4-06` báo cáo "domain soft-delete còn sống nhưng repository hard delete" — bản thân lỗi là
> **sai tên test** + **thiếu tài liệu quy ước**, KHÔNG phải code sai. **Không được xoá** `MarkDeleted`/
> `SoftDelete`/`IsDeleted` của `Recipe`/`BaseEntity` — xoá sẽ phá tính năng đang chạy (ảnh, slug, sitemap dựa
> trên đó).

---

## 2. Hiện trạng

| Hạng mục | Vị trí | Trạng thái |
|---|---|---|
| `Recipe.SoftDelete()` | `src/backend/CulinaryBlog.Domain/Entities/Recipe.cs:362` | Soft delete — dùng thật trong `RecipeDeleteCommand` (`Application/Recipes.cs`) |
| `RecipeRepository.Remove` → `db.Recipes.Update(recipe)` | `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs:206` | Giữ dòng DB, ghi `IsDeleted = true` |
| Interceptor `EntityState.Deleted → Modified` | `Persistence/Interceptors/AuditableEntityInterceptor.cs:64-67` | Bảo đảm soft delete luôn là UPDATE |
| Global query filter `!IsDeleted` | `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs:93-103` | Ẩn bản ghi đã xoá ở mọi truy vấn |
| `CategoryRepository.DeleteAsync` → `db.Categories.Remove(category)` | `src/backend/CulinaryBlog.Infrastructure/CategoryRepository.cs:51` | **Hard delete** — `DELETE` dòng (C07) |
| `Category.MarkDeleted()` | `src/backend/CulinaryBlog.Domain/Category.cs:42` | **Code chết** — không được gọi ở production |
| Test `CategoryTests` nói "soft_deletes_when_empty" | `tests/CulinaryBlog.Tests/CategoryTests.cs:145` | Tên sai → đã đổi thành `...hard_deletes_when_empty` |

---

## 3. Quyết định

1. Giữ nguyên hai hành vi hiện hành: **`Recipe` soft delete**, **`Category` hard delete**.
2. `Category` **không dùng** `IsDeleted` làm cơ chế vòng đời — trường này giữ làm cờ dự phòng, không cam kết
   query filter sẽ che `Category` trong tương lai nếu repository chuyển sang hard delete tiếp.
3. Tên test phải khớp hành vi: `DeleteCategory_..._hard_deletes_when_empty` (đã đổi).
4. Không xoá `Category.MarkDeleted` lúc này (giữ API domain ổn định); ghi nhận là dead code trong báo cáo.

## 4. Hệ quả

- Nhà phát triển: với `Recipe`, muốn khôi phục/slug/ảnh cần nhớ dòng vẫn còn; với `Category` không thể khôi phục sau xoá.
- Slug của `Recipe` đã soft-delete vẫn chiếm unique index (comment tại `Recipes.cs`); `Category` thì không.
- Không cần migration — không đổi schema.

## 5. Các lựa chọn đã cân nhắc

| Lựa chọn | Kết quả |
|---|---|
| Soft delete cho cả `Category` | Bỏ — đụng schema (ĐK `Name`/`Slug` unique phải bỏ `WHERE IsDeleted`), không có yêu cầu phục hồi danh mục |
| Hard delete cho cả `Recipe` | Bỏ — phá ảnh/slug/sitemap và ADR-0001 đang chạy, mất khả năng điều tra nội dung đã xoá |
| Giữ nguyên + ghi tài liệu | **Chọn** — chi phí thấp nhất, không đổi hành vi, đóng `BUG-W4-06` |