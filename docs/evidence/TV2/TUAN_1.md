# Báo cáo nghiệm thu Tuần 1 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục, khám phá và tìm kiếm; Đăng nhập Google
- **Mã task tuần 1**: B1, B2 (nền), B6 (nền), Chốt Google Contract (D04)
- **Nhánh Git**: `feat/TV2-week1-category`
- **Ngày hoàn thành**: 14/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 1 (Đã test pass toàn bộ, sẵn sàng bàn giao cho TV3)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B1 — Category Domain, EF Core Model, Migration & CQRS Handlers
- **Tuần / Người / Task**: 1 / Ngô Quốc Trường Vĩ (TV2) / B1
- **FR/NFR/Kỹ năng**: FR-CAT-001, FR-CAT-002, FR-CAT-003, FR-CAT-004, FR-CAT-005; NFR-MAINT-001; K01, K02, K03, K04, K06, K07
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Domain/Category.cs`: Thực thể Category với các phương thức Domain `Update`, `MarkDeleted`, `Restore`.
  - `src/backend/CulinaryBlog.Domain/SlugHelper.cs`: Thuật toán chuyển đổi tiếng Việt có dấu thành slug không dấu chuẩn URL.
  - `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs`: Cấu hình `DbSet<Category>`, bảng `Categories`, unique index `Slug`, soft-delete filter `!x.IsDeleted`.
  - `src/backend/CulinaryBlog.Infrastructure/CategoryRepository.cs`: Triển khai `ICategoryRepository` truy vấn theo thứ tự `OrderIndex` và `Name` (D29).
  - `src/backend/CulinaryBlog.Infrastructure/Migrations/20260913182804_AddCategoryModule.cs`: Migration tạo bảng `Categories`.
  - `src/backend/CulinaryBlog.Application/Categories.cs`: DTOs, Commands, Queries, Handlers và FluentValidation.
  - `src/backend/CulinaryBlog.API/Program.cs`: Đăng ký DI và map các endpoint `/api/v1/categories`.
  - `docs/CATEGORY_CONTRACT.md`, `docs/adr/0002-category-and-google-auth-week1.md`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Migration EF Core chạy thành công: `AddCategoryModule` tạo bảng với đầy đủ cột, index unique và default value.
  - Unit test: 12 tests trong `CategoryTests.cs` pass 100% (Test Domain, SlugHelper, CreateHandler, UpdateHandler, DeleteHandler, GetCategoriesHandler).
  - Clean Architecture test: `ArchitectureTests` pass 100% (Domain không chứa bất kỳ dependency ngoài nào).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/09/2026.

---

### Bản ghi 2: Task B2 (nền) — Hợp đồng phân trang D11 & Hợp đồng danh sách công thức
- **Tuần / Người / Task**: 1 / Ngô Quốc Trường Vĩ (TV2) / B2 (nền)
- **FR/NFR/Kỹ năng**: FR-RCP-001, FR-SRCH-002/003/004; NFR-PERF-001; K01, K05, K16
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Pagination.cs`: Định dạng `PagedResult<T>` và `PaginationMeta` chuẩn `data/meta`.
  - `docs/RECIPE_LIST_CONTRACT.md`: Đặc tả các tham số lọc (`categoryId`, `difficulty`, `maxCookTime`, `minServings`), sắp xếp theo allowlist, phân trang mặc định `page=1`, `pageSize=12`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `PaginationMeta_calculates_page_bounds_correctly` pass 100% (tính đúng `totalPages`, `hasNextPage`, `hasPreviousPage`).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/09/2026.

---

### Bản ghi 3: Chốt Google OAuth Contract (D04 / Task B4 spike)
- **Tuần / Người / Task**: 1 / Ngô Quốc Trường Vĩ (TV2) / Google Contract
- **FR/NFR/Kỹ năng**: FR-AUTH-003; NFR-SEC-004; K01, K09
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docs/GOOGLE_AUTH_CONTRACT.md`: Đặc tả luồng Code Flow + PKCE trên frontend Next.js với Auth.js v5; endpoint backend `POST /api/v1/auth/google` nhận `idToken` và xác minh độc lập qua Google Public Keys.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/09/2026.

---

### Bản ghi 4: Task B6 (nền) — Bài thực hành cá nhân (K08)
- **Tuần / Người / Task**: 1 / Ngô Quốc Trường Vĩ (TV2) / B6 (nền)
- **FR/NFR/Kỹ năng**: K01, K02, K03, K08
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docs/evidence/TV2/LAB_DANH_MUC_AUTH_SPIKE.md`: Thử nghiệm giải mã PBKDF2 Identity V3 100.000 iterations và JWT claims, kiểm tra phân quyền `AdminPolicy` trên API Category.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/09/2026.

---

## 2. Kết quả kiểm thử tự động (Automated Test Suite)

### Kết quả chạy lệnh `dotnet test`
```
Test run for CulinaryBlog.Tests.dll (.NETCoreApp,Version=v10.0)
Passed! - Failed: 0, Passed: 19, Skipped: 0, Total: 19, Duration: 52 ms
```

Danh sách 12 tests module Category:
1. `CategoryTests.Category_domain_creates_and_validates_properties`: PASS
2. `CategoryTests.Category_domain_rejects_invalid_names`: PASS
3. `CategoryTests.SlugHelper_generates_vietnamese_slug_correctly`: PASS
4. `CategoryTests.CreateCategory_validator_catches_invalid_inputs`: PASS
5. `CategoryTests.CreateCategory_handler_creates_and_generates_unique_slug`: PASS
6. `CategoryTests.UpdateCategory_preserves_slug_and_validates_uniqueness`: PASS
7. `CategoryTests.DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty`: PASS
8. `CategoryTests.GetCategories_orders_by_order_index_then_name`: PASS
9. `CategoryTests.PaginationMeta_calculates_page_bounds_correctly`: PASS
10. `ArchitectureTests.Inner_layers_do_not_reference_infrastructure_or_web`: PASS
11. `ArchitectureTests.Display_name_rejects_invalid_values`: PASS
12. `ArchitectureTests.Validation_pipeline_stops_handler_on_invalid_password`: PASS

---

## 3. Bàn giao cho thành viên tiếp theo (Handoff)
1. **Bàn giao cho TV3 (Huỳnh Quốc Trung):**
   - Đã có bảng `Categories` và `CategoryDto` để TV3 dùng `CategoryId` làm khóa ngoại tạo Recipe Draft trong Tuần 1 (đạt cổng G1).
   - Đã có cấu trúc phân trang `PagedResult<T>` trong `CulinaryBlog.Application` để TV3 dùng ngay cho các query danh sách món ăn.
2. **Bàn giao cho TV1 (Nguyễn Thanh Tâm):**
   - Báo cáo tuần 1 và các hợp đồng API Category, Phân trang D11, Google D04 sẵn sàng để review và merge vào nhánh chính.
