# BÁO CÁO GIẢI QUYẾT CÁC MÂU THUẪN NỘI TẠI TRONG TÀI LIỆU SRS

> **Dự án**: Culinary Blog — Blog Ẩm thực và Nấu ăn  
> **Học phần**: Phát Triển Ứng Dụng Web Nâng Cao (PTUDWNC) — Nhóm 16  
> **Văn bản cơ sở ban đầu**: `SRS_Culinary_Blog_v1.0.0.pdf` (04/06/2026)  
> **Văn bản chuẩn hóa chính thức**: `SRS_Culinary_Blog_v1.1.1.md` (Approved 16/09/2026)  
> **Người thực hiện rà soát & sửa đổi**: Nguyễn Thanh Tâm (Nhóm trưởng / Architect)

---

## 1. Bối cảnh & Lý do Cần Chuẩn hóa

Trong quá trình phân tích đặc tả yêu cầu phần mềm phiên bản v1.0.0, nhóm phát triển phát hiện **9 điểm mâu thuẫn nội tại (kí hiệu C01 đến C09)** giữa các chương (Chương 3 - Yêu cầu chức năng, Chương 7 - Mô hình dữ liệu, Chương 8 - Đặc tả API REST) và các phụ lục, kèm theo sự không nhất quán tại **Mục 8.1 (Auth API Summary)**.

Nếu không được chuẩn hóa kịp thời, các mâu thuẫn này sẽ dẫn đến xung đột kiến trúc nghiêm trọng giữa các thành viên:
- Backend và Frontend không đồng nhất kiểu dữ liệu và cấu trúc gói phản hồi (`response wrapper`).
- Nguy cơ mất mát dữ liệu hoặc vi phạm toàn vẹn tham chiếu khi xóa đối tượng.
- Kiểm thử tự động (Unit / Integration Tests) thất bại do khác biệt về mã lỗi HTTP và ràng buộc nghiệp vụ.

Tài liệu này ghi nhận chi tiết từng mâu thuẫn, quyết định kiến trúc chính thức của Kiến trúc sư hệ thống trong **SRS v1.1.1**, và toàn bộ các sửa đổi đã thực hiện trên Source Code.

---

## 2. Bảng Tổng Hợp 9 Mâu Thuẫn Nội Tại (C01–C09) & Chuẩn Hóa §8.1

| Mã | Tên mâu thuẫn | Vị trí mâu thuẫn trong SRS v1.0.0 | Quyết định chuẩn hóa trong SRS v1.1.1 | Trạng thái mã nguồn & Tests |
|:---:|---|---|---|:---:|
| **C01** | Xóa Recipe: Hard Delete vs Soft Delete | FR-RCP-007 (Hard delete) đối lập §8.3, NFR-REL-003, §7.1 BaseEntity (Soft delete) | **Soft Delete** (`IsDeleted = true`). Giữ nguyên các file ảnh trên MinIO. | ✅ Đã áp dụng Global Query Filter |
| **C02** | Điều kiện Publish Recipe | FR-RCP-005 (chỉ cần `Steps.Count > 0`) đối lập Phụ lục B (`phải có ít nhất 1 ingredient và 1 step`) | **≥ 1 Ingredient VÀ ≥ 1 Step**. Thiếu dữ liệu trả về HTTP `422 Unprocessable Entity` (`RECIPE_PUBLISH_INCOMPLETE`). | ✅ Đã kiểm thử Domain validation |
| **C03** | Category Cache TTL | FR-CAT-001 (IMemoryCache TTL = 60 phút) đối lập NFR-PERF-003 (Redis cache TTL = 30 phút) | **TTL = 60 phút** cho danh mục vì ít biến động. | ✅ Đã cấu hình Cache 60 phút |
| **C04** | Kích thước phân trang mặc định (Default Page Size) | FR-RCP-001 (`pageSize = 12`) đối lập FR-SRCH-001 (`pageSize = 10`) | **`pageSize = 12`** thống nhất cho toàn bộ hệ thống (phù hợp bố cục grid 3 cột, 4 dòng). | ✅ Đã đồng bộ DTOs & Queries |
| **C05** | Chuẩn hóa tên trường bước thực hiện | §7.3 & §8.5 (`TimerMinutes`) đối lập FR-RCP-010 (`DurationMinutes`) | Chuẩn hóa duy nhất **`TimerMinutes`**. | ✅ Đã cập nhật Entity, DTO, Database |
| **C06** | Chuẩn hóa tên trường nguyên liệu | §7.4 & §8.6 (`OrderIndex`) đối lập FR-RCP-009 (`SortOrder`) | Chuẩn hóa duy nhất **`OrderIndex`**. | ✅ Đã cập nhật Entity, DTO, Database |
| **C07** | Xóa Category: Hard Delete vs Soft Delete | FR-CAT-005 (xóa entity trực tiếp) đối lập §8.2 (ghi chú soft delete) | **Hard Delete** (xóa khỏi DB). Chặn xóa và trả `409 Conflict` nếu danh mục còn công thức. | ✅ Đã cập nhật `CategoryRepository` |
| **C08** | Cấu trúc Response wrapper | §5.2 (`{ data, meta }`) đối lập FR-AUTH-001 và FR-CAT-001 (trả raw object/mảng) | **Mọi response thành công LUÔN wrap trong `{ "data": ... }`**. | ✅ Đã cập nhật API & Frontend `api.ts` |
| **C09** | Ràng buộc duy nhất của Category | §7.6 (Name UNIQUE) đối lập FR-CAT-003 (logic slug thêm suffix "-2", "-3") | **`Name` UNIQUE trong DB**; `Slug` cũng UNIQUE, thêm suffix nếu 2 tên khác nhau trùng slug. | ✅ Đã cấu hình Index UNIQUE & SlugHelper |
| **§8.1** | Định dạng Auth API Summary | §8.1 dùng `displayName` và thiếu các trường profile đối lập FR-AUTH-001/006/007 | Register nhận `fullName`, `userName`, `email`, `password`. Response trả đủ `fullName`, `userName`, `emailConfirmed`, `createdAt`, `expiresAt`. | ✅ Đã cập nhật DTO, JWT, AuthTests |

---

## 3. Phân Tích Chi Tiết Từng Mâu Thuẫn & Giải Pháp Hiện Thực

### C01 — Hard Delete vs Soft Delete cho Recipe
- **Mâu thuẫn**:
  - *FR-RCP-007*: Mô tả *"Đây là hard delete (không dùng soft delete pattern cho recipe)"*, xóa vĩnh viễn và xóa ảnh trên MinIO.
  - *§8.3, NFR-REL-003, §7.1 BaseEntity*: Đều quy định Recipe áp dụng Soft Delete với cờ `IsDeleted = true`, có thể khôi phục dữ liệu.
- **Quyết định SRS v1.1.1**: Thống nhất **Soft Delete** cho Recipe. Đánh dấu `IsDeleted = true`, không xóa ảnh trên MinIO để đảm bảo tính toàn vẹn và khả năng phục hồi dữ liệu.
- **Hiện thực code**:
  - `BaseEntity.cs`: Giữ thuộc tính `IsDeleted` và Global Query Filter `.Where(r => !r.IsDeleted)`.
  - `DeleteRecipeHandler.cs`: Đánh dấu `recipe.IsDeleted = true`, không dispatch job xóa file MinIO.

---

### C02 — Điều kiện Publish Recipe
- **Mâu thuẫn**:
  - *FR-RCP-005*: Chỉ kiểm tra `Steps.Count > 0` là cho phép xuất bản.
  - *Phụ lục B (Mã lỗi)*: Đặc tả mã `RECIPE_PUBLISH_INCOMPLETE` ghi rõ *"phải có ít nhất 1 ingredient và 1 step"*.
- **Quyết định SRS v1.1.1**: Để một món ăn có thể xuất bản ra công chúng, bắt buộc phải có **ít nhất 1 nguyên liệu VÀ ít nhất 1 bước thực hiện**. Nếu không thỏa, trả về HTTP `422 Unprocessable Entity` kèm mã lỗi `RECIPE_PUBLISH_INCOMPLETE`.
- **Hiện thực code**:
  - `Recipe.Publish()` kiểm tra:
    ```csharp
    if (!Ingredients.Any() || !Steps.Any())
        throw new DomainException("Recipe phải có ít nhất 1 nguyên liệu và 1 bước thực hiện.");
    ```

---

### C03 — Category Cache TTL
- **Mâu thuẫn**:
  - *FR-CAT-001*: Sử dụng `IMemoryCache` TTL = 60 phút.
  - *NFR-PERF-003*: Ghi TTL danh mục = 30 phút.
- **Quyết định SRS v1.1.1**: Thống nhất **TTL = 60 phút**. Danh mục ẩm thực là dữ liệu ít khi thay đổi. Khi có thao tác Create/Update/Delete từ Admin, cơ chế `MemoryCache.Remove("categories:all")` sẽ tự động hủy cache ngay lập tức.
- **Hiện thực code**:
  - Cấu hình `AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)`.

---

### C04 — Default Page Size (12 vs 10)
- **Mâu thuẫn**:
  - *FR-RCP-001*: Mặc định `pageSize = 12`.
  - *FR-SRCH-001*: Mặc định `pageSize = 10`.
- **Quyết định SRS v1.1.1**: Thống nhất **`pageSize = 12`**. Con số 12 chia hết cho cả 2, 3, và 4, giúp hiển thị lưới card (Grid Layout) trên Frontend Next.js responsive đẹp mắt trên cả Mobile (1-2 cột), Tablet (3 cột), và Desktop (4 cột).
- **Hiện thực code**:
  - Giá trị mặc định trong `PaginationQuery.cs` và `RecipeListContract.md` là `PageSize = 12`.

---

### C05 & C06 — Chuẩn Hóa Tên Trường (`TimerMinutes` & `OrderIndex`)
- **Mâu thuẫn C05**: FR-RCP-010 dùng `DurationMinutes`, nhưng §7.3 và §8.5 dùng `TimerMinutes`.
- **Mâu thuẫn C06**: FR-RCP-009 dùng `SortOrder`, nhưng §7.4 và §8.6 dùng `OrderIndex`.
- **Quyết định SRS v1.1.1**:
  - Chuẩn hóa trường thời gian của bước là **`TimerMinutes`**.
  - Chuẩn hóa trường thứ tự của nguyên liệu là **`OrderIndex`**.
- **Hiện thực code**:
  - `RecipeStep.cs`: Thuộc tính `int? TimerMinutes { get; set; }`.
  - `RecipeIngredient.cs`: Thuộc tính `int OrderIndex { get; set; }`.

---

### C07 — Xóa Category: Hard Delete vs Soft Delete
- **Mâu thuẫn**:
  - *FR-CAT-005*: Mô tả xóa entity trực tiếp khỏi cơ sở dữ liệu (`_unitOfWork.Categories.Remove()`).
  - *§8.2*: Bảng API lại ghi chú `DELETE /categories/{id} (soft delete)`.
- **Quyết định SRS v1.1.1**: Category áp dụng **Hard Delete** (xóa vật lý khỏi database). Ràng buộc an toàn: Nếu danh mục còn bất kỳ công thức nào (kể cả món Draft), hệ thống từ chối xóa và trả về `409 Conflict` (`category.delete_has_recipes`).
- **Hiện thực code**:
  - `CategoryRepository.cs`: Phương thức `DeleteAsync(Category entity)` gọi `_context.Categories.Remove(entity)`.
  - `CategoryTests.cs`: Kiểm tra xóa thành công và assert `Assert.DoesNotContain(...)`.

---

### C08 — Response Wrapper `{ "data": ... }`
- **Mâu thuẫn**:
  - *§5.2*: Nêu mọi response thành công đều bọc trong `{ "data": ... }`.
  - *FR-AUTH-001 & FR-CAT-001*: Ví dụ trả về mảng trực tiếp hoặc object không bọc.
- **Quyết định SRS v1.1.1**: **100% endpoint trả về thành công đều wrap trong object `{ "data": ... }`**, bao gồm cả Auth Response, Single Entity, List và PagedResult.
- **Hiện thực code**:
  - Backend `Program.cs`: Các endpoint trả về `Results.Ok(new { data = result })` hoặc `Results.Created(..., new { data = result })`.
  - Frontend `api.ts`: Xử lý tự động bóc gói:
    ```typescript
    const body = (json.data !== undefined ? json.data : json) as T;
    ```

---

### C09 — Ràng Buộc Duy Nhất Category (Name UNIQUE & Slug Suffix)
- **Mâu thuẫn**:
  - *§7.6*: Cột `Name` có ràng buộc `UNIQUE`.
  - *FR-CAT-003*: Đặt giả thiết nếu slug bị trùng thì thêm hậu tố "-2", "-3"... (điều này chỉ xảy ra khi 2 Name khác nhau sinh ra cùng 1 slug không dấu, ví dụ: "Món Chính" và "mon chinh").
- **Quyết định SRS v1.1.1**: Cả `Name` và `Slug` đều có ràng buộc **`UNIQUE`** trong database. Thuật toán `SlugHelper` tự động dò và thêm suffix nếu phát hiện slug va chạm.
- **Hiện thực code**:
  - `CategoryConfiguration.cs`: `builder.HasIndex(c => c.Name).IsUnique();` và `builder.HasIndex(c => c.Slug).IsUnique();`.

---

### Chuẩn Hóa Mục 8.1 — Auth API Summary
- **Mâu thuẫn**:
  - Bảng tổng hợp §8.1 bản v1.0.0 nhận `displayName`, thiếu các trường quan trọng trong response như `fullName`, `userName`, `emailConfirmed`, `createdAt`, và dùng `expiresIn` thay vì mốc thời gian `expiresAt`.
- **Quyết định SRS v1.1.1**:
  - `POST /api/v1/auth/register`: Nhận `{ fullName, userName, email, password }`.
  - Phản hồi trả về:
    ```json
    {
      "data": {
        "accessToken": "...",
        "refreshToken": "...",
        "expiresAt": "2026-09-16T18:45:00.0000000+00:00",
        "user": {
          "id": "guid",
          "fullName": "...",
          "email": "...",
          "userName": "...",
          "avatarUrl": "...",
          "roles": ["Author"],
          "emailConfirmed": false,
          "createdAt": "..."
        }
      }
    }
    ```
- **Hiện thực code**:
  - `Auth.cs`: Cập nhật `RegisterCommand`, `UserDto`, `AuthResponse`.
  - `IdentityService.cs` & `JwtService.cs`: Ánh xạ đầy đủ thuộc tính và cấp `ExpiresAt` dạng ISO 8601.
  - `src/frontend/src/types/auth.ts`: Cập nhật interface `User`, `AuthResponse`, `ApiResponse<T>`.

---

## 4. Danh Sách Các File Đã Được Chỉnh Sửa & Cập Nhật

1. **Tài liệu đặc tả & Hợp đồng**:
   - `docs/root/SRS_Culinary_Blog_v1.1.1.md` *(Tạo mới bản full SRS v1.1.1)*
   - `docs/root/SRS_Contradictions_Report.md` *(Tạo mới báo cáo phân tích)*
   - `docs/AUTH_CONTRACT.md` *(Cập nhật theo §8.1)*
   - `docs/CATEGORY_CONTRACT.md` *(Cập nhật Hard Delete, TTL 60m, Name UNIQUE)*
   - `docs/ERD.md` *(Cập nhật đối chiếu SRS v1.1.1)*
   - `docs/adr/0002-category-and-google-auth-week1.md` *(Cập nhật quyết định C07 Hard Delete)*
   - `KE_HOACH_DU_AN.md` *(Cập nhật mục 2 và mã D09)*
   - `PHAN_CHIA_CONG_VIEC_6_TUAN.md` *(Cập nhật baseline tham chiếu SRS v1.1.1)*
   - `README.md` *(Cập nhật tiểu mục 3.5 và danh mục tài liệu)*

2. **Backend API (.NET 10)**:
   - `src/backend/CulinaryBlog.Application/Auth.cs`
   - `src/backend/CulinaryBlog.Application/Categories.cs`
   - `src/backend/CulinaryBlog.Infrastructure/CategoryRepository.cs`
   - `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs`
   - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs`
   - `src/backend/CulinaryBlog.Infrastructure/JwtService.cs`
   - `src/backend/CulinaryBlog.API/Program.cs`
   - `src/backend/CulinaryBlog.API/ApiExceptionHandler.cs`

3. **Frontend (Next.js 15)**:
   - `src/frontend/src/types/auth.ts`
   - `src/frontend/src/lib/api.ts`
   - `src/frontend/src/app/dashboard/profile/page.tsx`

4. **Kiểm thử tự động**:
   - `tests/CulinaryBlog.Tests/AuthTests.cs`
   - `tests/CulinaryBlog.Tests/CategoryTests.cs`
   - **Tỷ lệ kiểm thử**: 54 / 54 tests pass 100%.

---

## 5. Kết Luận

Việc chuẩn hóa theo **SRS v1.1.1** đã giải quyết triệt để tất cả các điểm mơ hồ và mâu thuẫn nội tại trong dự án. Toàn bộ mã nguồn backend, frontend, hợp đồng giao tiếp giữa các thành viên, cùng với bộ kiểm thử tự động hiện đã hoàn toàn đồng bộ, nhất quán và sẵn sàng cho việc mở rộng các tính năng của các tuần tiếp theo.
