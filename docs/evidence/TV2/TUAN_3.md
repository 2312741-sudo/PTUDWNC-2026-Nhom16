# Báo cáo nghiệm thu Tuần 3 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục, khám phá, tìm kiếm, tối ưu Caching, FTS Index và SEO
- **Mã task tuần 3**: B3 (hoàn thiện FTS/SSR/SEO), B5 (Cache-Aside/Invalidation/Fallback), B6 (Lab cá nhân K07/K08/K13), B7 (Kiểm thử tự động & ADR)
- **Nhánh Git**: `2312796-ngo-quoc-truong-vi-feat/TV2-week3-search-cache-ssr`
- **Ngày hoàn thành**: 29/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 3 (10/10 automated tests pass, 18/18 Clean Architecture tests pass, 15/15 Next.js SSR pages build pass)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B3 — Tìm kiếm toàn văn FTS PostgreSQL với GIN Trigram Index, SSR Search & SEO Robots/Canonical
- **Tuần / Người / Task**: 3 / Ngô Quốc Trường Vĩ (TV2) / B3
- **FR/NFR/Kỹ năng**: FR-SRCH-001, FR-SRCH-002, FR-SRCH-003, FR-SRCH-004; NFR-PERF-001, NFR-SEO-001; K02, K06, K11, K16, K18, K19
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/Migrations/20260929060455_AddFtsAndGinIndex.cs`: Migration EF Core kích hoạt extension `unaccent`, `pg_trgm`, tạo hàm bất biến `f_unaccent(text)` và chỉ mục GIN `IDX_Recipes_Title_Description_Trgm` trên tiêu đề và mô tả công thức.
  - `src/backend/CulinaryBlog.Application/Discovery.cs`: Hoàn thiện `SearchRecipesQuery`, `SearchRecipesHandler` hỗ trợ tìm kiếm tiếng Việt không dấu kết hợp đồng thời bộ lọc AND (`CategoryId`, `Difficulty`, `MaxCookTime`, `MinServings`) và sắp xếp.
  - `src/frontend/src/app/search/page.tsx`: Giao diện tìm kiếm Server-Side Rendering (SSR) trong Next.js App Router:
    - Render động theo URL searchParams: `q`, `categoryId`, `difficulty`, `maxCookTime`, `minServings`, `sortBy`, `sortOrder`, `page`.
    - Tích hợp bộ lọc dropdown trực quan cho Category và Difficulty, hỗ trợ xoá lọc nhanh.
    - Cấu hình SEO qua `generateMetadata`: Tự động gắn thẻ `robots: { index: false, follow: true }` khi có query param `q` (tránh phân mảnh crawl budget và duplicate content của search results), đồng thời gắn link chuẩn hóa `canonical` trỏ về trang search gốc.
  - `src/frontend/src/app/categories/page.tsx` & `src/frontend/src/app/categories/[slug]/page.tsx`: Bổ sung OpenGraph metadata và canonical links.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test xUnit:
    - `SearchRecipes_supports_unaccented_vietnamese_and_and_filters`: Tìm từ khóa không dấu `"pho ga"` khớp chính xác món `"Phở Gà Đồi Lá Chanh"`. Kết hợp bộ lọc AND (`pho` + `Hard`) trả về đúng món `"Phở Bò Sốt Vang"`. Lọc thời gian nấu tối đa 50 phút lọc bỏ món ninh hầm 120 phút -> **PASS**.
    - `SearchRecipes_validator_rejects_query_shorter_than_2_chars`: Chặn từ khóa tìm kiếm rỗng hoặc dưới 2 ký tự -> **PASS**.
  - Kiểm tra build Frontend: `npm --prefix src/frontend run build` biên dịch thành công 15/15 routes tĩnh và động không phát sinh cảnh báo.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 29/09/2026.

---

### Bản ghi 2: Task B5 — Caching Cache-Aside cho Category & Search, Invalidation và Resilient Fallback (NFR-REL-002)
- **Tuần / Người / Task**: 3 / Ngô Quốc Trường Vĩ (TV2) / B5
- **FR/NFR/Kỹ năng**: FR-CAT-001, FR-SRCH-001; NFR-PERF-001, NFR-REL-002; K01, K03, K04, K10, K12, K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/IRecipeCacheService.cs`: Interface trừu tượng hoá caching tại tầng Application, đảm bảo tính độc lập kiến trúc sạch (Clean Architecture CONS-001).
  - `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs`: Triển khai caching Cache-Aside với Redis và In-Memory fallback:
    - Phân tầng thời gian sống (TTL): Danh mục 60 phút (`categories:all`, `category:slug:*`), Danh sách công thức 15 phút (`recipes:list:*`), Kết quả tìm kiếm 1 phút (`recipes:search:*`).
    - Cơ chế phục hồi sự cố (Resilience Fallback): Bọc an toàn mọi thao tác Redis bằng `try/catch`. Khi Redis gặp sự cố hoặc timeout, service ghi log cảnh báo Serilog và trả về `null` để handler tự động truy vấn CSDL PostgreSQL mà không quăng lỗi HTTP 500 ra người dùng.
  - `src/backend/CulinaryBlog.Application/Categories.cs`: Tích hợp Cache-Aside vào `GetCategoriesHandler`, `GetCategoryBySlugHandler`; kích hoạt Cache Invalidation tự động trong `CreateCategoryHandler`, `UpdateCategoryHandler`, `DeleteCategoryHandler`.
  - `src/backend/CulinaryBlog.Application/Discovery.cs`: Tích hợp Cache-Aside vào `GetRecipesHandler` và `SearchRecipesHandler`.
  - `src/backend/CulinaryBlog.Application/Recipes.cs`: Kích hoạt Cache Invalidation (`recipes:list:`, `recipes:search:`) khi công thức thay đổi trạng thái trong `PublishRecipeHandler`, `UnpublishRecipeHandler`, `ArchiveRecipeHandler`, `DeleteRecipeHandler`.
  - `src/backend/CulinaryBlog.API/Program.cs`: Đăng ký `IRecipeCacheService` Singleton vào DI Container.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `Categories_GetCategories_and_GetBySlug_cache_and_invalidate_properly`: Cache MISS ở lần đầu, Cache HIT ở lần 2 (không gọi DB repo), tự động làm mới sau khi thêm/sửa/xoá danh mục -> **PASS**.
  - Test `GetRecipes_and_SearchRecipes_cache_and_invalidate_properly`: Cache HIT trên danh sách và tìm kiếm, tự động vô hiệu hóa cache khi có thay đổi recipe -> **PASS**.
  - Test `SearchRecipes_cache_isolation_never_returns_draft_or_archived_recipes`: Bản nháp (`Draft`) và món lưu trữ (`Archived`) tuyệt đối không xuất hiện trong cache tìm kiếm công khai -> **PASS**.
  - Test `Cache_service_fallback_resilience_when_cache_server_down`: Khi giả lập dịch vụ cache bị sập (down), hệ thống tự động fallback trực tiếp về DB mà không làm gián đoạn request của người dùng (NFR-REL-002) -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 29/09/2026.

---

### Bản ghi 3: Task B6 — Lab cá nhân TV2: Optimistic Concurrency Conflict (K07), PBKDF2 Security (K08) & Media Boundary (K13)
- **Tuần / Người / Task**: 3 / Ngô Quốc Trường Vĩ (TV2) / B6
- **FR/NFR/Kỹ năng**: FR-RCP-003; NFR-SEC-001, NFR-SEC-004; K07, K08, K13
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/CulinaryBlog.Tests/Week3DiscoverySearchCacheTests.cs` (Khu vực Task B6):
    - **K07 Concurrency Conflict:** Mô phỏng kịch bản hai tác giả (Writer 1 và Writer 2) cùng tải một công thức `Recipe` với `RowVersion` ban đầu. Writer 1 cập nhật thành công làm xoay vòng `RowVersion`. Writer 2 gửi lệnh cập nhật kèm `RowVersion` cũ. Hệ thống kích hoạt kiểm tra `RecipeGuard.EnsureVersion` và chặn đứng giao dịch, trả về ngoại lệ nghiệp vụ `AppException(422, "recipe.concurrency_conflict")`.
    - **K08 Security Hash & Salt:** Kiểm chứng cơ chế băm mật khẩu chuẩn NIST qua PBKDF2 với thuật toán SHA-512, 100.000 vòng lặp và muối ngẫu nhiên bằng mật mã (`RandomNumberGenerator`). Chứng minh cùng mật khẩu nhưng với salt khác nhau sinh ra chuỗi băm hoàn toàn khác biệt (chống tấn công Rainbow Table).
    - **K13 Media Boundary:** Kiểm tra ngưỡng biên tải lên tệp đa phương tiện theo CONS-007 và FR-MED-001: Cho phép các định dạng hiện đại tối ưu web (`image/jpeg`, `image/png`, `image/webp`, `image/avif`), kiên quyết từ chối định dạng cấm (`image/gif`, `application/pdf`) và từ chối kích thước vượt quá 5 MiB ($5 \times 1024 \times 1024 + 1$ byte).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test `PersonalLab_Recipe_optimistic_concurrency_two_writers_conflict`: Bắt chính xác mã lỗi 422 và mã `recipe.concurrency_conflict` khi xung đột phiên bản -> **PASS**.
  - Test `PersonalLab_Identity_password_hash_and_token_security`: Sinh hash 64-byte an toàn, chống rainbow table thành công -> **PASS**.
  - Test `PersonalLab_Media_mime_and_size_boundary_validation`: Kiểm tra giới hạn định dạng và dung lượng chính xác 100% -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 29/09/2026.

---

### Bản ghi 4: Task B7 — Bộ kiểm thử tự động, Kiểm chứng kiến trúc sạch & Tài liệu kiến trúc ADR 0003
- **Tuần / Người / Task**: 3 / Ngô Quốc Trường Vĩ (TV2) / B7
- **FR/NFR/Kỹ năng**: NFR-MAINT-001, NFR-MAINT-002; K01, K03, K21, K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docs/adr/0003-category-and-search-caching-week3.md`: Ban hành tài liệu quyết định kiến trúc số 0003 chuẩn hóa phân tầng TTL, Cache-Aside, Invalidation mutation-driven, Resilience Fallback và chỉ mục GIN trigram.
  - `tests/CulinaryBlog.Tests/Week3DiscoverySearchCacheTests.cs`: Toàn bộ bộ kiểm thử tự động cho các tính năng của TV2 trong Tuần 3.
  - Kiểm chứng Clean Architecture: Chạy 18 tests trong `ArchitectureTests.cs` xác nhận không có bất kỳ vi phạm nào giữa tầng Application và Infrastructure.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Lệnh: `dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~Week3DiscoverySearchCacheTests"`
    ```
    Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 248 ms (CulinaryBlog.Tests.dll)
    ```
  - Lệnh: `dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests"`
    ```
    Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18, Duration: 82 ms (CulinaryBlog.Tests.dll)
    ```
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 29/09/2026.

---

## 2. Kết quả kiểm thử tự động (Automated Test Suite)

### 2.1. Chi tiết 10/10 Tests Tuần 3 của TV2
```
Test run for C:\Users\admin\.gemini\antigravity\scratch\PTUDWNC-2026-Nhom16\tests\CulinaryBlog.Tests\bin\Debug\net10.0\CulinaryBlog.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 248 ms - CulinaryBlog.Tests.dll (net10.0)
```

| STT | Tên Test Case | Mục đích kiểm tra | Kết quả |
|:---:|---|---|:---:|
| 1 | `Categories_GetCategories_and_GetBySlug_cache_and_invalidate_properly` | Cache-Aside 60m cho danh mục và Invalidation khi thêm danh mục | **PASS** |
| 2 | `Categories_Update_and_Delete_invalidate_cache` | Invalidation khi sửa/xoá danh mục | **PASS** |
| 3 | `GetRecipes_and_SearchRecipes_cache_and_invalidate_properly` | Cache-Aside 15m danh sách, 1m tìm kiếm, Invalidation khi thay đổi | **PASS** |
| 4 | `SearchRecipes_cache_isolation_never_returns_draft_or_archived_recipes` | Cô lập cache, không bao giờ lộ món Draft hoặc Archived | **PASS** |
| 5 | `Cache_service_fallback_resilience_when_cache_server_down` | Tự động fallback về DB khi cache server down (NFR-REL-002) | **PASS** |
| 6 | `SearchRecipes_supports_unaccented_vietnamese_and_and_filters` | FTS tiếng Việt không dấu và kết hợp bộ lọc AND phức hợp | **PASS** |
| 7 | `SearchRecipes_validator_rejects_query_shorter_than_2_chars` | Validation chặn từ khóa ngắn dưới 2 ký tự | **PASS** |
| 8 | `PersonalLab_Recipe_optimistic_concurrency_two_writers_conflict` | Lab K07: Xử lý xung đột ghi đồng thời qua RowVersion (422) | **PASS** |
| 9 | `PersonalLab_Identity_password_hash_and_token_security` | Lab K08: Băm mật khẩu PBKDF2/SHA-512 với Salt ngẫu nhiên | **PASS** |
| 10 | `PersonalLab_Media_mime_and_size_boundary_validation` | Lab K13: Kiểm tra định dạng WebP/AVIF và ngưỡng dung lượng 5MiB | **PASS** |

---

## 3. Tổng kết tiến độ Tuần 3 của TV2

| Hạng mục công việc | Kế hoạch tuần 3 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Task B3: FTS & Search SSR** | Migration GIN index unaccent/trgm, SSR Search page, bộ lọc kết hợp, SEO tags | Hoàn thành migration EF, trang `/search` SSR, metadata robots noindex | **100%** |
| **Task B5: Caching & Fallback** | Abstraction Clean Arch, Cache-Aside TTL 60m/15m/1m, Invalidation, Resilient Fallback | Hoàn thành `IRecipeCacheService`, wired vào Categories & Recipes, test fallback | **100%** |
| **Task B6: Personal Lab** | 3 bài lab cá nhân: Concurrency conflict (K07), PBKDF2 (K08), Media boundary (K13) | Hoàn thành và tích hợp test suite tự động kiểm chứng | **100%** |
| **Task B7: Tests & Docs** | Automated test suite, ADR 0003, cập nhật ma trận kỹ năng | Hoàn thành 10/10 tests pass, ADR 0003 ban hành, ma trận đạt 20/24 skills | **100%** |
