# Tuần 6 (W5) — Tự giải thích: Aggregate / UoW / Version / Owned Data (Recipe)

Tài liệu ngắn để Trung tự trả lời khi Tâm/thầy hỏi lúc demo. Chi tiết đầy đủ đã có ở `docs/adr/ADR-0001-recipe-schema-va-concurrency.md` và `ADR-0002-recipe-version-schema-va-nap-du-lieu.md`, `docs/evidence/TV3/Tuan05/SCHEMA_RECIPE.md` — tài liệu này chỉ gạn lại phần hay bị hỏi.

## 1. Aggregate là gì trong hệ thống này?

`Recipe` là **aggregate root**: một đơn vị nhất quán nghiệp vụ, gồm `Recipe` + 3 collection con (`RecipeIngredient`, `RecipeStep`, `RecipeImage`) + 1 owned value object (`RecipeNutrition`). Quy tắc: mọi thay đổi vào con (thêm/sửa/xoá nguyên liệu, bước, ảnh) đều đi qua các method trên `Recipe` (`AddIngredient`, `RemoveIngredient`, `Publish`…) — không có repository riêng cho `RecipeIngredient`, không ai tạo nguyên liệu "tự do" không thuộc công thức nào. Đây là lý do `RecipeGuard.LoadOwnedAsync` luôn nạp cả `Recipe` trước khi cho sửa con: đảm bảo kiểm tra quyền sở hữu và quy tắc nghiệp vụ (ví dụ `childId` phải thuộc đúng `recipe.Id` đang sửa, không cho sửa nhầm sang recipe khác) chạy đúng một chỗ.

## 2. `RecipeNutrition` là Owned Entity — khác gì Aggregate con?

`RecipeNutrition` **không có bảng riêng** — 6 cột `Nutrition_*` nằm thẳng trong bảng `Recipes` (EF Core `OwnsOne`). Khác với `RecipeIngredient`/`RecipeStep`/`RecipeImage` (có `Id` riêng, bảng riêng, có thể tồn tại độc lập về mặt lưu trữ dù luôn thuộc một Recipe) — `RecipeNutrition` không có khoá riêng, không có vòng đời riêng, chỉ là một "cục giá trị" đi kèm Recipe. Lý do chọn Owned: dinh dưỡng không cần soft-delete riêng, không cần truy vấn độc lập, và tránh JOIN thêm một bảng chỉ để lấy 6 số.

## 3. Unit of Work (UoW) hoạt động thế nào?

`IUnitOfWork`/`EfUnitOfWork` bọc một `DbContext` dùng chung cho toàn bộ request — mọi thay đổi (sửa Recipe, thêm Ingredient, xoá Image…) gom vào MỘT `SaveChangesAsync()` ở cuối handler, chạy trong MỘT transaction của PostgreSQL. Ví dụ rõ nhất: xoá một bước làm đánh lại `StepNumber` của các bước sau — nếu giữa đường có lỗi, EF rollback toàn bộ, không để lại trạng thái nửa vời (đã kiểm bằng `RecipeConcurrencyAndTransactionTests`, kể cả transaction lồng nhau).

## 4. RowVersion (version dữ liệu) — vì sao sửa con không đổi version cha?

- `RowVersion` là `bytea` 16-byte do `AuditableEntityInterceptor` gán **mới mỗi lần** một entity ở trạng thái `Added`/`Modified`/xoá mềm — không dùng `xmin` của Postgres, để test được trên mọi provider và đổi đúng theo thao tác ứng dụng.
- **Chủ đích**: thêm/sửa/xoá một nguyên liệu/bước/ảnh KHÔNG đổi `RowVersion` của `Recipe` cha (vì EF không đánh `Recipe` là `Modified`, chỉ đánh dòng con). Nhờ vậy wizard nhiều bước không phải tải lại `rowVersion` của Recipe sau mỗi lần sửa nguyên liệu.
- Đánh đổi đã ghi trong ADR-0002: hai tab sửa **cùng một dòng con** thì bản lưu sau thắng (last-write-wins ở mức dòng con) — vì endpoint con không nhận `rowVersion` riêng của con.
- Nếu bị hỏi "vậy concurrency kiểm ở đâu?": kiểm ở mức Recipe (sửa tiêu đề/mô tả/trạng thái qua `PUT /recipes/{id}` phải gửi đúng `rowVersion`, sai thì `422 recipe.concurrency_conflict`) và ở mức dòng con qua `IsConcurrencyToken` của EF (hai request cùng sửa đúng 1 dòng con cùng lúc thì bên chậm nhận `DbUpdateConcurrencyException` → `422`).

## 5. Phiên bản schema (migration) khác phiên bản dữ liệu (RowVersion) thế nào?

- **Phiên bản dữ liệu** (RowVersion): theo dõi một DÒNG dữ liệu cụ thể đổi bao nhiêu lần — dùng cho optimistic concurrency.
- **Phiên bản schema** (migration, 8 file trong `Infrastructure/Migrations`): theo dõi CẤU TRÚC bảng đổi qua thời gian (thêm cột, đổi index…) — dùng cho triển khai/nâng cấp DB. Hai thứ độc lập: đổi schema không ảnh hưởng RowVersion của dữ liệu đã có; đổi RowVersion (ghi dữ liệu) không cần migration.

## 4 câu thầy có thể hỏi (kèm đáp án ngắn)

1. **"Vì sao không cho endpoint con nhận `rowVersion` riêng để tránh last-write-wins?"**
   → Có thể làm, cột `RowVersion` của dòng con đã có sẵn trong DB — nhưng cần đổi hợp đồng API (client phải gửi kèm), và đã đánh giá không cần ngay vì tần suất 2 người sửa đúng 1 dòng con cùng lúc thấp hơn nhiều so với sửa cùng Recipe cha. Đã ghi là hạn chế biết trước trong ADR-0002, không phải thiếu sót chưa nghĩ tới.

2. **"Transaction lồng nhau (nested transaction) nghĩa là gì trong hệ thống này, Postgres có hỗ trợ transaction lồng thật không?"**
   → PostgreSQL không có nested transaction thật, chỉ có `SAVEPOINT`. Trong code, "transaction lồng" là EF mở một `IDbContextTransaction` ngoài rồi gọi `SaveChangesAsync` nhiều lần/scope nhỏ bên trong — đã test rollback đúng khi lỗi xảy ra ở bước giữa (`RecipeConcurrencyAndTransactionTests`), không phải SAVEPOINT thủ công.

3. **"Nếu publish một Recipe mà đúng lúc đó có người xoá nguyên liệu cuối cùng, hệ thống xử lý sao?"**
   → `Recipe.Publish()` kiểm điều kiện "≥1 ingredient, ≥1 step" ngay trong cùng transaction nạp để ghi (`FindForWriteAsync`), nên đọc và ghi cùng một snapshot nhất quán ở mức transaction DB — không thể publish "lọt" một công thức vừa mất hết nguyên liệu, vì nếu giao dịch xoá nguyên liệu commit trước, publish sẽ đọc thấy 0 nguyên liệu và từ chối; nếu publish đọc trước và xoá nguyên liệu xảy ra song song, RowVersion của dòng ingredient đổi khiến phía nào SaveChanges sau gặp xung đột (tuỳ thứ tự thật, đã có test 2 writer nhưng chưa có test đúng kịch bản publish-đua-với-xoá-nguyên-liệu-cuối — ghi suy đoán, chưa có test riêng).

4. **"`childId` thuộc recipe khác bị từ chối ở đâu, bằng cách nào?"**
   → `RecipeGuard.LoadOwnedAsync` nạp Recipe theo `recipeId` từ URL, rồi handler tìm `childId` TRONG collection con của CHÍNH Recipe đó (ví dụ `recipe.Ingredients.SingleOrDefault(i => i.Id == ingredientId)`) — nếu `childId` thuộc recipe khác, nó không có trong collection này nên trả lỗi "không tìm thấy" (`ingredient.not_found`), không phải tìm `childId` toàn cục rồi so `RecipeId`. Cách này tự nhiên chặn truy cập chéo mà không cần thêm điều kiện kiểm tra riêng — đã có test xác nhận (`RecipeIngredientHttpTests`).
