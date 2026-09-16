# Báo cáo nghiệm thu Tuần 2 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục, khám phá và tìm kiếm; Đăng nhập Google
- **Mã task tuần 2**: B2, B3, B4, B7 (nền tuần 2)
- **Nhánh Git**: `feat/TV2-week2-discovery-search-google`
- **Ngày hoàn thành**: 16/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 2 (Đã test pass 22/22 tests, đạt mốc G2)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B2 — Danh sách công thức duyệt công khai (List Recipes with Scope/Filter/Sort/Page)
- **Tuần / Người / Task**: 2 / Ngô Quốc Trường Vĩ (TV2) / B2
- **FR/NFR/Kỹ năng**: FR-RCP-001, FR-SRCH-002/003/004; NFR-PERF-001, NFR-SEC-006; K01, K02, K04, K05, K06
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Domain/Recipe.cs`: Thực thể Recipe phục vụ duyệt và phân loại công thức.
  - `src/backend/CulinaryBlog.Application/Discovery.cs`: `RecipeSummaryDto`, `GetRecipesQuery`, `GetRecipesValidator`, `GetRecipesHandler`.
  - `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs`: Truy vấn LINQ parameterized kết hợp bộ lọc AND (`CategoryId`, `Difficulty`, `MaxCookTime`, `MinServings`), sắp xếp theo allowlist an toàn.
  - `src/backend/CulinaryBlog.API/Program.cs`: Endpoint `GET /api/v1/recipes`.
  - `src/frontend/src/app/recipes/page.tsx` & `src/frontend/src/components/RecipeCard.tsx`: Giao diện duyệt món ăn kèm sidebar bộ lọc và điều hướng phân trang.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `GetRecipes_only_returns_published_and_applies_and_filters` -> PASS.
  - Bảo mật dữ liệu: các món `Draft` và `Archived` tuyệt đối không bị lọt vào response public.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 2: Task B3 — Tìm kiếm toàn văn FTS tiếng Việt không dấu (PostgreSQL Full-Text Search)
- **Tuần / Người / Task**: 2 / Ngô Quốc Trường Vĩ (TV2) / B3
- **FR/NFR/Kỹ năng**: FR-SRCH-001; NFR-PERF-001; K02, K06, K11
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Discovery.cs`: `SearchRecipesQuery`, `SearchRecipesValidator`, `SearchRecipesHandler`.
  - `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs`: Triển khai tìm kiếm không dấu qua `SlugHelper` và EF `ILike`, hỗ trợ xếp hạng tương đồng `ts_rank`.
  - `src/backend/CulinaryBlog.API/Program.cs`: Endpoint `GET /api/v1/recipes/search`.
  - `src/frontend/src/app/search/page.tsx`: Giao diện tìm kiếm trực quan với ô tìm kiếm lớn, highlight kết quả và trạng thái rỗng thân thiện.
  - `docs/SEARCH_AND_DISCOVERY_CONTRACT.md`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `SearchRecipes_finds_by_unaccented_keyword_and_rejects_short_queries` -> PASS.
  - Gõ từ khóa `"pho"` tìm thấy chính xác món `"Phở Bò Gia Truyền"`.
  - Từ khóa ngắn $q < 2$ bị chặn và trả về lỗi `400 Bad Request` kèm Problem Details.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 3: Task B4 — Tích hợp Đăng nhập Google OAuth2 (Google Sign-In & Verification)
- **Tuần / Người / Task**: 2 / Ngô Quốc Trường Vĩ (TV2) / B4
- **FR/NFR/Kỹ năng**: FR-AUTH-003; NFR-SEC-002/004; K02, K04, K08, K09
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/GoogleAuth.cs`: `GoogleLoginCommand`, `IGoogleAuthService`, `GoogleLoginValidator`, `GoogleLoginHandler`.
  - `src/backend/CulinaryBlog.Infrastructure/GoogleAuthService.cs`: Xác minh chữ ký token với Google SDK (`GoogleJsonWebSignature`).
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs`: Xử lý `LoginWithGoogleAsync` (tự động tạo tài khoản role `Author` hoặc liên kết tài khoản cũ, cấp token JWT `AuthResponse`).
  - `src/backend/CulinaryBlog.API/Program.cs`: Endpoint `POST /api/v1/auth/google`.
  - `src/frontend/src/components/GoogleSignInButton.tsx` & `src/frontend/src/app/auth/login/page.tsx`: Nút đăng nhập Google và giao diện đăng nhập đầy đủ.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `GoogleLogin_creates_author_or_links_account` -> PASS (Token rỗng bị từ chối, user mới được tạo role Author, email unverified bị từ chối 401).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 4: Task B7 (nền tuần 2) — Bộ kiểm thử tự động & Giao diện Responsive
- **Tuần / Người / Task**: 2 / Ngô Quốc Trường Vĩ (TV2) / B7
- **FR/NFR/Kỹ năng**: NFR-USE-001; NFR-MAINT-002; K16, K17, K18, K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/CulinaryBlog.Tests/DiscoveryAndSearchTests.cs`: Toàn bộ test suite kiểm tra tìm kiếm, lọc, phân trang và Google Login.
  - Giao diện các trang `/recipes`, `/search`, `/auth/login` được thiết kế responsive theo Tailwind CSS cho 3 mốc: 320px (Mobile), 768px (Tablet), 1200px (Desktop).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `dotnet test`: 22/22 tests pass 100% (gồm 7 tests Kiến trúc, 12 tests Category, 3 tests Discovery & Search).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

## 2. Kết quả kiểm thử tự động (Automated Test Suite)

### Kết quả chạy lệnh `dotnet test`
```
Test run for CulinaryBlog.Tests.dll (.NETCoreApp,Version=v10.0)
Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 53 ms
```

Danh sách 22 tests đã đạt:
1. `DiscoveryAndSearchTests.GetRecipes_only_returns_published_and_applies_and_filters`: PASS
2. `DiscoveryAndSearchTests.SearchRecipes_finds_by_unaccented_keyword_and_rejects_short_queries`: PASS
3. `DiscoveryAndSearchTests.GoogleLogin_creates_author_or_links_account`: PASS
4. `CategoryTests.Category_domain_creates_and_validates_properties`: PASS
5. `CategoryTests.Category_domain_rejects_invalid_names`: PASS
6. `CategoryTests.SlugHelper_generates_vietnamese_slug_correctly`: PASS
7. `CategoryTests.CreateCategory_validator_catches_invalid_inputs`: PASS
8. `CategoryTests.CreateCategory_handler_creates_and_generates_unique_slug`: PASS
9. `CategoryTests.UpdateCategory_preserves_slug_and_validates_uniqueness`: PASS
10. `CategoryTests.DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty`: PASS
11. `CategoryTests.GetCategories_orders_by_order_index_then_name`: PASS
12. `CategoryTests.PaginationMeta_calculates_page_bounds_correctly`: PASS
13. `ArchitectureTests.Inner_layers_do_not_reference_infrastructure_or_web`: PASS
14. `ArchitectureTests.Display_name_rejects_invalid_values`: PASS
15. `ArchitectureTests.Validation_pipeline_stops_handler_on_invalid_password`: PASS
... và các tests nền tảng khác.

---

## 3. Bàn giao & Sẵn sàng cho Mốc G2
* **Bàn giao cho TV3 (Huỳnh Quốc Trung):**
  - Thực thể `Recipe` và cấu trúc truy vấn đã sẵn sàng để TV3 gắn thêm các phần con: `Ingredients`, `Steps`, `Nutrition` và `RowVersion`.
* **Bàn giao cho TV4 (Nguyễn Hữu Trung Sơn):**
  - Cấu trúc `RecipeSummaryDto.PrimaryImageUrl` và `Status` đã sẵn sàng để TV4 kết nối module Upload ảnh và trigger trạng thái `Published`.
* **Bàn giao cho TV1 (Nguyễn Thanh Tâm - Leader):**
  - Nhánh `feat/TV2-week2-discovery-search-google` đầy đủ mã nguồn, test và tài liệu hợp đồng sẵn sàng để Nhóm trưởng review và merge vào `main`.
