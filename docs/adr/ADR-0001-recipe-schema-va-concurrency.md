# ADR-0001 — Schema Recipe aggregate & concurrency (bản tự chốt cho C1 solo)

- **Trạng thái:** Chấp nhận cho C1 (tự chốt do nhóm chưa họp được). Đánh dấu lại là "đề xuất" nếu nhóm muốn xem lại điểm chạm API/auth.
- **Ngày:** 2026-09-13 · **Người chốt:** TV3 — Huỳnh Quốc Trung (2312786).
- **Nguyên tắc chọn:** **an toàn = bám sát SRS Chương 6.4 & 7 nguyên văn** để lúc nghiệm thu đối chiếu là khớp.
- **Liên quan:** D08, D15, D16, D18, D19, D24, D28 (schema); D02, D07, D14 (ghi nhận cho C2).

## Quyết định (đã hiện thực trong code)

1. **BaseEntity đồng nhất (SRS 7.1 / D24):** mọi entity nghiệp vụ (Recipe, Category, RecipeIngredient, RecipeStep, RecipeImage)
   kế thừa `BaseEntity` = Id (uuid), CreatedAt (NOT NULL), **UpdatedAt (NULL)**, IsDeleted, RowVersion (bytea).
   `RefreshToken` **không** kế thừa BaseEntity (cấu trúc riêng — D24). Identity dùng khóa `string`, FK UserId string.

2. **Soft delete đồng nhất (D08 / SRS 7.1):** interceptor chuyển mọi `Deleted` → `Modified(IsDeleted=true)`;
   global query filter `!IsDeleted` áp cho **mọi** BaseEntity (kể cả child). Đây là cách khớp SRS 7.1 nhất và đơn giản nhất.
   *Ghi chú:* hành vi "xóa cứng 1 ảnh + xóa file MinIO" (FR-RCP-008) là quyết định ở tầng handler/TV4, không đổi schema.

3. **RowVersion (D19):** `bytea` opaque, `IsConcurrencyToken`. `AuditableEntityInterceptor` gán token mới mỗi lần ghi;
   EF dùng giá trị GỐC trong WHERE → writer chậm nhận `DbUpdateConcurrencyException` (kiểm bằng spike 2 writer).
   Không dùng `xmin`/rowversion tự sinh; nếu đổi sẽ mở ADR mới.

4. **FK & ON DELETE (SRS 6.4/7.2/7.8):**
   - Recipe.CategoryId → Categories.Id **RESTRICT** (chặn xóa category còn recipe → FR-CAT-005/409).
   - Recipe.AuthorId → AspNetUsers.Id **RESTRICT**.
   - RefreshToken.UserId → AspNetUsers.Id **CASCADE**.
   - Recipe → child (Ingredient/Step/Image) **CASCADE**.

5. **Trường & ràng buộc (D15/D16/D28):**
   - Recipe: Title 5–200; Slug ≤220 unique; Description ≤2000; **Instructions NOT NULL default ""** (D28);
     PrepTime>0; CookTime≥0; Servings>0 (CHECK constraint); **Difficulty smallint DEFAULT 1 (Easy=1..Expert=4)**; Status DEFAULT 0 (Draft).
   - Nutrition: owned, 6 cột `Nutrition_*` numeric(8,2) nullable.
   - Ingredient: Name 1–200; Quantity numeric(10,3) nullable (>0 nếu có); Unit ≤50; Notes ≤500; OrderIndex.
   - Step: StepNumber liên tục 1..N, **unique (RecipeId, StepNumber)**; Title ≤200; Description 1–2000; TimerMinutes ≥0 nullable.
   - Image: OriginalUrl ≤500; Medium/Thumbnail nullable; AltText ≤200; **đúng 1 primary/recipe** (partial unique index); OrderIndex.

6. **Index (SRS 7.2):** IDX_Recipe_Slug (unique), _CategoryId, _AuthorId, _Status, _PublishedAt, _Difficulty,
   _IsDeleted (partial); IDX_Category_Slug (unique) + Name unique; IDX_RefreshToken_Hash (unique); unique (RecipeId, StepNumber);
   partial unique 1 primary image. SearchVector/GIN do TV2 thêm bằng migration FTS (D18).

7. **Ranh giới (D18):** Domain thuần BCL; `ApplicationUser` ở Infrastructure; SearchVector ở Infrastructure; FluentValidation ở Application.

## Ghi nhận cho C2 (chưa chốt ở ADR này)
- **D02:** validation/publish-thiếu → 400; concurrency → **422**; trùng → 409.
- **D07:** publish cần ≥1 ingredient và ≥1 step (không bắt buộc ảnh); lặp trạng thái → 200. (Đã enforce trong `Recipe.Publish()`.)
- **D14:** slug tự thêm suffix + unique constraint chống race; ổn định sau publish; đổi Draft có 301.

## Placeholder cần bàn giao lại khi nhóm họp
`Category`, `ApplicationUser`, `RefreshToken` hiện là **bản tối thiểu** do TV3 tạo để C1 chạy độc lập.
Khi tích hợp: TV2 thay `Category` (CRUD/policy/seed), TV1 mở rộng `ApplicationUser` (roles/PBKDF2/JWT/seed) và sở hữu RefreshToken schema.
Chỉ giữ **một** `ApplicationDbContext`.
