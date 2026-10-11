# T4 — Schema Recipe: ERD, chỉ mục, xoá mềm, RowVersion, migration (từ DB sạch thật)

Nguồn: `information_schema` + `pg_indexes` chạy thật trên `cb_w5_seed` (DB tạm sạch, 8/8 migration mới nhất, đã seed 100 recipes) ngày 07/10/2026 (script `/tmp/schema_dump.sql`, không commit vì ngoài repo). Đối chiếu với `docs/root/SRS_Culinary_Blog_v1.1.1 Detail.md` chương 6.4 và 7, và `docs/adr/ADR-0001-recipe-schema-va-concurrency.md` / `ADR-0002-recipe-version-schema-va-nap-du-lieu.md`.

## 1. ERD (sinh từ DB sạch thật)

```mermaid
erDiagram
    Categories ||--o{ Recipes : "CategoryId (RESTRICT)"
    AspNetUsers ||--o{ Recipes : "AuthorId (RESTRICT)"
    AspNetUsers ||--o{ RefreshTokens : "UserId (CASCADE)"
    Recipes ||--o{ RecipeIngredients : "RecipeId (CASCADE)"
    Recipes ||--o{ RecipeSteps : "RecipeId (CASCADE)"
    Recipes ||--o{ RecipeImages : "RecipeId (CASCADE)"

    Recipes {
        uuid Id PK
        varchar200 Title
        varchar220 Slug "UNIQUE, khong loc IsDeleted"
        varchar2000 Description
        text Instructions
        int PrepTimeMinutes
        int CookTimeMinutes
        int Servings
        smallint Difficulty "1..4"
        smallint Status "0 Draft,1 Published,2 Archived"
        timestamptz PublishedAt
        uuid CategoryId FK
        varchar450 AuthorId FK
        numeric Nutrition_Calories "owned, 6 cot"
        timestamptz CreatedAt
        timestamptz UpdatedAt "null khi Added"
        bool IsDeleted
        bytea RowVersion
    }
    RecipeIngredients {
        uuid Id PK
        uuid RecipeId FK
        varchar200 Name
        numeric Quantity "nullable"
        varchar50 Unit
        varchar500 Notes
        int OrderIndex "0..N-1 lien tuc, song"
        bool IsDeleted
        bytea RowVersion
    }
    RecipeSteps {
        uuid Id PK
        uuid RecipeId FK
        int StepNumber "1..N, UNIQUE(RecipeId,StepNumber) WHERE IsDeleted=false"
        varchar200 Title
        varchar2000 Description
        int TimerMinutes "nullable"
        bool IsDeleted
        bytea RowVersion
    }
    RecipeImages {
        uuid Id PK
        uuid RecipeId FK
        varchar500 OriginalUrl
        varchar500 MediumUrl "nullable"
        varchar500 ThumbnailUrl "nullable"
        bool IsPrimary "1/recipe, partial unique"
        int OrderIndex
        bool IsDeleted
        bytea RowVersion
    }
    Categories {
        uuid Id PK
        varchar100 Name UNIQUE
        varchar120 Slug UNIQUE
        bool IsDeleted
    }
    RefreshTokens {
        uuid Id PK
        varchar450 UserId FK
        varchar64 TokenHash UNIQUE
        timestamptz ExpiresAt
        timestamptz RevokedAt
    }
```

## 2. Chỉ mục — nhấn mạnh partial/unique (lấy thật từ `pg_indexes`)

| Bảng | Tên chỉ mục | Định nghĩa thật | Ghi chú |
|---|---|---|---|
| RecipeImages | `ux_recipe_images_one_primary` | `UNIQUE btree ("RecipeId") WHERE ("IsPrimary"=true AND "IsDeleted"=false)` | **Partial unique có lọc `IsDeleted`** — đúng bản đã sửa ở migration `20261004155213` (xem mục 5). Ảnh xoá mềm không còn tranh "1 ảnh chính/recipe" với ảnh sống. |
| RecipeSteps | `IX_RecipeSteps_RecipeId_StepNumber` | `UNIQUE btree ("RecipeId","StepNumber") WHERE ("IsDeleted"=false)` | Partial unique — cho phép StepNumber trùng giữa bước **đã xoá mềm** và bước sống (đánh lại số không bị chặn bởi "xác" cũ). |
| Recipes | `IDX_Recipe_Slug` | `UNIQUE btree ("Slug")` — **không có `WHERE`** | **Không lọc `IsDeleted`** — khác 2 chỉ mục trên. Slug của công thức đã xoá mềm vẫn chiếm chỗ, công thức mới không thể tái dùng đúng slug đó (phải tự thêm số đuôi). Đã xác nhận đây là hạn chế thật (mục 6), không phải suy đoán. |
| Recipes | `IDX_Recipe_IsDeleted` | `btree ("IsDeleted") WHERE ("IsDeleted"=false)` | Partial — tối ưu query filter mặc định (EF Global Query Filter `!IsDeleted`), không phải unique. |
| Recipes | `IDX_Recipes_Title_Description_Trgm` | `gin (("Title" \|\| ' ' \|\| "Description") gin_trgm_ops)` | Trigram GIN (TV2, migration `AddFtsAndGinIndex`) — **không phải** `tsvector` + trigger như SRS 7.2 mô tả (xem mục 6). |
| Categories | `IX_Categories_Slug` | `UNIQUE btree ("Slug")` | Không lọc `IsDeleted` (giống Recipes.Slug — nhất quán trong cách thiết kế, cùng hạn chế). |
| RefreshTokens | `IDX_RefreshToken_Hash` | `UNIQUE btree ("TokenHash")` | — |
| RecipeIngredients | `IX_RecipeIngredients_RecipeId_OrderIndex` | `btree ("RecipeId","OrderIndex")` | Không unique (OrderIndex chỉ cần liên tục ở tầng ứng dụng, đã xác nhận SELECT-only không có hổng — xem `NHAT_QUAN_DU_LIEU.md`). |

## 3. Xoá mềm & query filter

- Mọi entity kế thừa `BaseEntity` (Recipe, RecipeIngredient, RecipeStep, RecipeImage, Category) có `IsDeleted boolean NOT NULL`.
- `AuditableEntityInterceptor.ApplyRules()`: khi EF đánh dấu entity `Deleted`, interceptor **chuyển thành `Modified(IsDeleted=true)`** (soft delete), gán `UpdatedAt` + `RowVersion` mới. `RefreshToken` **không** kế thừa `BaseEntity` → xoá cứng thật (D24, hợp lý vì token hết hạn không cần giữ lại).
- Global Query Filter (`!IsDeleted`) áp cho mọi `BaseEntity` ở tầng `DbContext` — mọi truy vấn qua EF (API) tự động ẩn dòng đã xoá mềm, không cần nhớ thêm `WHERE` ở từng handler.
- **3 chỉ mục partial** (`ux_recipe_images_one_primary`, `IX_RecipeSteps_RecipeId_StepNumber`) lọc theo `IsDeleted=false` ngay trong `WHERE` của index — ràng buộc unique chỉ áp cho dòng sống, đúng ý đồ thiết kế. **`IDX_Recipe_Slug` và `IX_Categories_Slug` thì KHÔNG** — đây là điểm không đồng nhất thật trong thiết kế (mục 6).

## 4. `ON DELETE` (ADR-0001 mục 4, đối chiếu DB thật — khớp)

| Quan hệ | `ON DELETE` thật | Khớp ADR-0001? |
|---|---|---|
| `Recipes.CategoryId` → `Categories.Id` | `RESTRICT` | Khớp — chặn xoá category còn recipe (FR-CAT-005). |
| `Recipes.AuthorId` → `AspNetUsers.Id` | `RESTRICT` | Khớp. |
| `RecipeIngredients/RecipeSteps/RecipeImages.RecipeId` → `Recipes.Id` | `CASCADE` | Khớp — xoá cứng Recipe (nếu có) sẽ cascade xoá cứng con; thực tế app luôn xoá mềm nên CASCADE chỉ chạy khi ai đó xoá cứng trực tiếp bằng SQL. |
| `RefreshTokens.UserId` → `AspNetUsers.Id` | `CASCADE` | Khớp. |

## 5. RowVersion — ngữ nghĩa thật (kiểm bằng code + test thật, không suy đoán)

- **Gán bởi `AuditableEntityInterceptor`** (chạy trước mọi `SaveChanges`): mỗi entity ở trạng thái `Added`/`Modified`/xoá mềm được gán `RowVersion = Guid.NewGuid().ToByteArray()` (16 byte, opaque, không phải `xmin` của Postgres). EF dùng giá trị **gốc** của `RowVersion` trong `WHERE` của UPDATE (`IsConcurrencyToken`) nên writer đến sau nhận `DbUpdateConcurrencyException` → `422 recipe.version_conflict`.
- **Sửa nguyên liệu/bước/ảnh con KHÔNG đổi `RowVersion` của Recipe cha** — đã kiểm bằng:
  1. Đọc code: `Recipe.AddIngredient()` (`src/backend/CulinaryBlog.Domain/Entities/Recipe.cs:111`) chỉ thêm vào collection `_ingredients`, không set thuộc tính nào trên `Recipe` → EF `ChangeTracker` không đánh `Recipe` là `Modified` (chỉ đánh dòng con mới là `Added`) → interceptor không gán `RowVersion` mới cho `Recipe`.
  2. Test thật **đã có và đang xanh** (nằm trong 281/281 vừa chạy ở `KIEM_TRA_TUAN4.md` mục 3): `RecipeIngredientHttpTests.CurrentBehavior_changing_ingredients_does_not_change_recipe_rowVersion_so_wizard_can_still_save_step_1` — thêm/sửa/xoá nguyên liệu qua HTTP thật trên Postgres thật, sau đó `Assert.Equal(rowVersion, await RecipeRowVersionInDb(recipeId))` (RowVersion đọc trực tiếp từ DB không đổi), rồi `PUT /recipes/{id}` với `rowVersion` CŨ vẫn trả `200 OK` (không bị 422).
  - **Đây là quyết định có chủ đích** (ghi trong `ADR-0002` mục 2, trạng thái "Đề xuất — chờ Tâm review"), không phải thiếu sót: giúp wizard nhiều bước không phải tải lại `rowVersion` của Recipe sau mỗi lần sửa nguyên liệu/bước. Đánh đổi: hai tab sửa **cùng một dòng con** thì bản lưu sau thắng (last-write-wins ở mức dòng con) — đã ghi rõ trong ADR-0002 bảng "Hạn chế đã biết", không lặp lại ở đây.
- Xem `docs/adr/ADR-0002-recipe-version-schema-va-nap-du-lieu.md` để biết đầy đủ phương án đã cân nhắc và hạn chế.

## 6. Danh sách migration (8 migration, kèm mục đích thật) + đối chiếu SRS 6.4/7

| # | MigrationId | Mục đích (đọc code `Up()`) |
|---|---|---|
| 1 | `20260909134304_InitialIdentity` | Tạo bảng ASP.NET Identity (`AspNetUsers/Roles/...`) — nền auth (TV1). |
| 2 | `20260913182804_AddCategoryModule` | Tạo `Categories` (TV2). |
| 3 | `20260919061954_AddRecipeAggregate` | Tạo `Recipes`, `RecipeIngredients`, `RecipeSteps`, `RecipeImages`, `RefreshTokens`, cột `Nutrition_*` owned, các FK + index ban đầu (TV3, C1). |
| 4 | `20260926145137_RecipeChildIdsValueGeneratedNever` | Đổi `Id` của 3 bảng con sang `ValueGeneratedNever` — ứng dụng tự sinh GUID trước khi `Add()` để `AuditableEntityInterceptor` phân biệt đúng Added/Modified (xem chú thích trong interceptor). |
| 5 | `20260929060455_AddFtsAndGinIndex` | Bật extension `unaccent`, `pg_trgm`; tạo `IDX_Recipes_Title_Description_Trgm` (GIN trigram trên `Title \|\| ' ' \|\| Description`) — tìm kiếm tiếng Việt không dấu (TV2). **Không tạo cột `tsvector`/trigger** như SRS 7.2 mô tả (lệch — mục dưới). |
| 6 | `20260930113035_RecipeStepNumberUniqueIgnoresSoftDeleted` | Đổi unique `(RecipeId, StepNumber)` thành **partial** (`WHERE IsDeleted=false`) — cho phép đánh lại `StepNumber` của bước mới trùng số với bước đã xoá mềm. |
| 7 | `20261001112029_RecipeImageIdValueGeneratedNever` | Đổi `Id` của `RecipeImages` sang `ValueGeneratedNever` (cùng lý do #4, áp dụng muộn hơn cho bảng ảnh). **`Up`/`Down` không đổi lược đồ cột/index nào khác** — đã xác nhận bằng cách migrate tới đúng mốc này trên DB rỗng và `\d` không thấy khác biệt ngoài kiểu sinh Id. |
| 8 | `20261004155213_RecipeImageOnePrimaryIgnoresSoftDeleted` | Đổi `ux_recipe_images_one_primary` từ `WHERE IsPrimary=true` sang `WHERE IsPrimary=true AND IsDeleted=false` — **nới** ràng buộc (ảnh xoá mềm không còn tranh chấp "1 ảnh chính" với ảnh sống). Đã kiểm chứng thật (mục T2 dưới): `Down()` của migration này **lỗi `23505` thật** nếu DB đang có cặp ảnh xoá mềm `IsPrimary=true` + ảnh sống `IsPrimary=true` cùng recipe (vì `Down` tạo lại chỉ mục CŨ không lọc `IsDeleted`); `Down` **chạy sạch** khi không có dữ liệu dạng đó. Hạn chế thật của migration, không sửa (không phải lỗi đang chặn luồng nào — `Down` chỉ dùng khi rollback thủ công).

### Kiểm tra nâng cấp/di chuyển (T2, chạy thật 07/10/2026 trên DB tạm `cb_w5_upgrade`/`cb_w5_down_clean`, đã xoá sau khi xong)

| Kịch bản | Lệnh | Kết quả thật |
|---|---|---|
| (a) Toàn bộ 8 migration trên DB rỗng | `dotnet ef database update` | Thành công, `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* — lược đồ khớp model 100%. |
| (b) Tạo dữ liệu bẩn ở mốc migration #7, rồi nâng lên #8 | Insert ảnh xoá mềm `IsPrimary=true` ở mốc #7 trước | **KHÔNG insert được** — mốc #7 vẫn dùng chỉ mục CŨ (không lọc `IsDeleted`) nên tự chặn ngay bằng `23505 duplicate key` khi cố tạo cặp ảnh xoá mềm + sống cùng `IsPrimary=true`. → **Kết luận khác giả thuyết ban đầu**: dữ liệu bẩn kiểu này **không thể phát sinh trước khi nâng cấp**; chỉ có thể phát sinh **sau** khi đã ở schema mới (#8) rồi mới gặp khi **hạ cấp** (xem (d)). |
| (c) `--migrate` (CLI) hai lần liên tiếp trên DB đã có schema | `dotnet run -- --migrate` ×2 | Lần 1: thành công (DB rỗng → đủ 8 migration). Lần 2: không có gì để áp, không lỗi — **idempotent**. |
| (d1) `Down` rồi `Up` migration #8 — dữ liệu sạch | `dotnet ef database update 20261001112029...` rồi update lại mới nhất | Cả hai chiều **thành công sạch**, không lỗi. |
| (d2) `Down` migration #8 — có dữ liệu bẩn (ảnh xoá mềm `IsPrimary=true` + ảnh sống `IsPrimary=true` cùng recipe, tạo **sau khi** đã lên #8) | `dotnet ef database update 20261001112029...` | **Lỗi thật**: `23505: could not create unique index "ux_recipe_images_one_primary"` — EF rollback transaction sạch (migration history, index đều giữ nguyên trạng thái #8, không có state nửa vời). **Xác nhận đúng giả thuyết ban đầu, nhưng chỉ đúng cho chiều hạ cấp (Down), không đúng cho chiều nâng cấp (Up/upgrade) như mô tả ban đầu trong đề bài.** |

## 7. Chỗ lệch giữa tài liệu (SRS) và DB thật

| # | SRS (`SRS_Culinary_Blog_v1.1.1 Detail.md`) | DB thật | Nhận xét |
|---|---|---|---|
| 1 | §7.2: Recipe có cột `SearchVector tsvector`, index `IDX_Recipe_Search (GIN)`, cập nhật bằng **TRIGGER** khi Title/Description đổi | Không có cột `SearchVector`; chỉ có `IDX_Recipes_Title_Description_Trgm` (GIN **trigram** `gin_trgm_ops` trên biểu thức `Title \|\| ' ' \|\| Description`, không trigger, tính runtime) | Thực hiện khác thiết kế SRS (việc của TV2 — FTS/search, không phải TV3) — không sửa SRS, chỉ ghi nhận. |
| 2 | §7.2: `IDX_Recipe_Slug (UNIQUE B-tree)` — SRS không nói rõ có lọc `IsDeleted` hay không | DB thật: **không lọc** `IsDeleted` | SRS và DB "khớp" theo nghĩa SRS không yêu cầu lọc, nhưng đây để lại hạn chế thật (mục 6 dưới) mà SRS không cảnh báo. |
| 3 | §7.3 RecipeStep: SRS ghi `UNIQUE cùng RecipeId (composite unique)` — không nói "partial" | DB thật: unique là **partial**, `WHERE IsDeleted=false` | DB làm đúng ý đồ nghiệp vụ hơn SRS diễn đạt (SRS không sai, chỉ chưa đủ chi tiết). |
| 4 | §7.5 RecipeImage: "Chỉ có 1 ảnh `IsPrimary=true` / Recipe" — không nói rõ tính trên dòng sống hay cả dòng xoá mềm | DB thật (sau migration #8): tính trên **dòng sống** (`WHERE IsPrimary=true AND IsDeleted=false`) | Hợp lý hơn diễn đạt gốc của SRS; nên bổ sung SRS câu "chỉ tính ảnh chưa xoá" nếu nhóm họp sửa SRS (không tự sửa SRS). |

## 8. Hạn chế thật đã xác nhận bằng dữ liệu/code (không sửa — không phải file của TV3 hoặc cần họp nhóm)

1. **`IDX_Recipe_Slug` không lọc `IsDeleted`**: slug của công thức đã xoá mềm vẫn chiếm chỗ vĩnh viễn, công thức mới cùng tên sinh slug đó phải tự thêm số đuôi (D14 đã xử lý ở tầng ứng dụng bằng auto-suffix, nên **không chặn luồng**, chỉ là slug "đẹp" bị mất). Cùng hạn chế ở `Categories.Slug`.
2. **4 dòng `RecipeImages` xoá mềm còn `IsPrimary=true` trên `culinary_blog`** (dữ liệu dev thật, xem `NHAT_QUAN_DU_LIEU.md`) — hệ quả của (a) chỉ mục cũ trên `culinary_blog` (migration #8 **chưa áp dụng** ở đó — lịch sử migration lệch đã biết) và (b) code xoá ảnh không tự chuyển `IsPrimary=false` khi xoá mềm ảnh đang là ảnh chính. Không sửa (DB dev chỉ đọc).
3. **`Down()` của migration #8 lỗi nếu có dữ liệu bẩn** (mục 6, kịch bản d2) — hạn chế thật của migration, chỉ lộ ra khi ai đó hạ cấp DB đã có dữ liệu mới; không ảnh hưởng luồng `--migrate`/`--seed`/chạy production bình thường (luôn đi lên, không đi xuống).
