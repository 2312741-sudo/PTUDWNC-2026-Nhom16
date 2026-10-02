# ADR-0002 — Recipe: phiên bản dữ liệu (RowVersion), phiên bản schema (migration) và cách nạp aggregate

- **Trạng thái:** Đề xuất — chờ Nguyễn Thanh Tâm (Nhóm trưởng) review. Ghi lại các quyết định **đang chạy trong code** sau tuần 4, bổ sung cho ADR-0001.
- **Ngày:** 2026-10-02 · **Người viết:** TV3 — Huỳnh Quốc Trung (2312786).
- **Liên quan:** ADR-0001 (schema Recipe & concurrency); SRS 7.1 (BaseEntity), D02/D07/D14/D16/D19; NFR-REL-003, NFR-PERF-004; FR-RCP-004/007/009/010.

## Bối cảnh
Công thức (aggregate `Recipe` + `RecipeIngredient`, `RecipeStep`, `RecipeImage`, owned `RecipeNutrition`) được nhiều tab/thiết bị sửa song song qua wizard.
Cần: (1) không mất cập nhật khi hai người sửa cùng lúc; (2) schema đổi được qua nhiều tuần mà DB CI rỗng lẫn DB dev đều nâng cấp được;
(3) trang chi tiết nạp đủ con mà không nhân dòng.

## Quyết định

### 1. Phiên bản dữ liệu = `RowVersion` bytea trên mọi `BaseEntity`
- Cột `RowVersion bytea NOT NULL`, `IsConcurrencyToken()`; giá trị là GUID 16 byte mới do `AuditableEntityInterceptor` gán khi Added / Modified / xoá mềm.
  Không dùng `xmin` của PostgreSQL: token nằm trong model, test được trên mọi provider, và đổi theo đúng thao tác của ứng dụng (kể cả xoá mềm).
- API trả `rowVersion` dạng base64 trong DTO. `PUT /recipes/{id}` gửi lại trong body, `DELETE /recipes/{id}` gửi qua `If-Match`.
- Kiểm hai tầng: `RecipeGuard.EnsureVersion` so trước khi ghi → **422 `recipe.concurrency_conflict`**; nếu hai request đọc cùng lúc,
  EF đưa token gốc vào `WHERE`, bên chậm nhận `DbUpdateConcurrencyException` → `ApiExceptionHandler` trả **422 `recipe.version_conflict`**.
  Client (wizard) coi cả hai là xung đột → banner "Tải dữ liệu mới nhất".
- Bằng chứng: `RecipeConcurrencyAndTransactionTests` (2 writer, đúng 1 thắng, không trộn trường), `RecipeIngredientHttpTests` (rowVersion sai → 422).

### 2. Endpoint con (nguyên liệu, bước, ảnh) không nhận rowVersion của recipe
- Thêm/sửa/xoá con **không đổi** `RowVersion` của recipe (recipe không ở trạng thái Modified) — test `CurrentBehavior_changing_ingredients_does_not_change_recipe_rowVersion...`.
  Nhờ vậy wizard sửa nguyên liệu xong vẫn lưu được bước 1 mà không phải tải lại.
- **Hệ quả chấp nhận:** hai tab sửa **cùng một nguyên liệu/bước** thì bản lưu sau thắng (last-write-wins ở mức dòng con).
- Phương án khác đã cân nhắc: (a) bắt client gửi `rowVersion` của dòng con (cột đã có token) — đổi hợp đồng API, để khi có yêu cầu thật;
  (b) "chạm" recipe mỗi lần sửa con để tăng version — mọi thao tác con sẽ làm hỏng rowVersion bước 1 đang mở, trải nghiệm wizard kém.
- Thứ tự con do server quản lý: `OrderIndex` 0..N-1 và `StepNumber` 1..N liên tục, đánh lại sau xoá; đổi thứ tự bước làm 2 pha trong transaction
  (unique `(RecipeId, StepNumber)` kiểm ngay sau từng UPDATE).

### 3. Phiên bản schema = EF Core migrations, không sửa tay DB
- Mọi thay đổi schema đi qua migration trong `src/backend/CulinaryBlog.Infrastructure/Migrations`, đặt tên theo ý nghĩa
  (`AddRecipeAggregate`, `RecipeChildIdsValueGeneratedNever`, `RecipeStepNumberUniqueIgnoresSoftDeleted`). Snapshot luôn commit cùng migration.
- Ràng buộc phải đúng với xoá mềm: unique `(RecipeId, StepNumber)` là **partial index `WHERE "IsDeleted" = false`** (lỗi 422 khi thêm bước sau khi xoá — đã sửa bằng migration).
- Migration phải chạy được trên DB rỗng (CI tạo DB mới mỗi lần; test `EnsureMigrated` gọi `Migrate()`), và chạy lặp không lỗi.
  Khởi động API với `--migrate` để áp migration rồi thoát. Không chạy `dotnet ef database update` lên DB dev `culinary_blog` (lịch sử migration lệch).
- Thêm trường vào DTO chỉ thêm ở cuối với giá trị mặc định (vd. `RecipeDetailDto.AuthorName/CategoryName` cho JSON-LD) để client cũ không vỡ.

### 4. Nạp aggregate
- **Đọc chi tiết** (`FindBySlugAsync`): `AsNoTracking` + `Include` 3 collection + **`AsSplitQuery()`** → 1 câu recipe + 1 câu mỗi collection (số câu cố định).
  Trước đó một câu JOIN trả 10 nguyên liệu × 6 bước = 60 dòng; sau: 18 dòng (`docs/evidence/TV3/Tuan04/K22_hieu_nang_recipe.md`).
- **Nạp để ghi** (`FindForWriteAsync`): có theo dõi thay đổi, `Include` nguyên liệu + bước trong một câu — hiện chưa tách (công thức nhỏ); xem lại nếu công thức lớn.
- Tên công khai cho trang chi tiết (tên hiển thị tác giả, tên danh mục) lấy bằng 1 câu neo vào dòng công thức (`RecipeDisplayNameReader`),
  danh mục đã xoá mềm chỉ làm mất tên danh mục.

## Hệ quả
- (+) Không lost update ở mức recipe; xung đột có mã lỗi ổn định cho client.
- (+) DB CI và DB mới nâng cấp bằng một lệnh; ràng buộc unique không chặn nhầm dòng đã xoá mềm.
- (−) Sửa trùng một dòng con là last-write-wins (đã ghi rõ, có test mô tả hành vi).
- (−) `ApiExceptionHandler` hiện gom mọi `DbUpdateException` (kể cả vi phạm unique) thành `recipe.version_conflict` — có thể làm thông báo sai loại lỗi;
  file đó thuộc phần dùng chung, cần thống nhất với nhóm trước khi đổi.

## Kiểm chứng
`dotnet test tests/CulinaryBlog.Tests` (TEST_DATABASE trỏ DB riêng) — các lớp `RecipeConcurrencyAndTransactionTests`, `RecipeIngredientHttpTests`,
`RecipeStepDeletionTests`, `RecipeQueryPerformanceTests`, `RecipeDetailSeoTests`; CI GitHub chạy trên DB Postgres 16 rỗng.
