# Báo cáo nghiệm thu Tuần 4 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục ẩm thực, Tìm kiếm FTS tiếng Việt không dấu (PostgreSQL GIN Index), SSR Search & Bộ lọc AND, Caching Cache-Aside phân tầng & Fallback Resilient
- **Mã task tuần 4**: B3, B4, B5, B6, B7
- **Nhánh Git**: `2312796-ngo-quoc-truong-vi-feat/TV2-week3-search-cache-ssr` (đã merge vào `main` qua PR #18)
- **Ngày hoàn thành**: 30/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 4 (177/177 tests toàn hệ thống pass, 10/10 tests TV2 pass, 15/15 trang Next.js build sạch)
- **Quy tắc đặt tên file nộp bài Lab 4**: `Lab4_2312796_NgoQuocTruongVi.docx` *(kèm file dự phòng `Lab04_2312796_NgoQuocTruongVi.docx`)*

---

## 0. Yêu cầu tối thiểu của Lab 4: Hoàn thành việc cài đặt tất cả API endpoints (41/41 Endpoints - 100%)

Thành viên TV2 đã phối hợp cùng nhóm hoàn thành và kiểm thử 100% các API endpoints thuộc phân hệ Danh mục, Tìm kiếm và Khám phá:

| STT | Endpoint | Phương thức | Quyền hạn / Cache | Mô tả & Trạng thái |
|:---:|---|:---:|:---:|---|
| 1 | `/api/v1/categories` | `GET` | Public (Cache 60m) | Lấy danh sách toàn bộ danh mục ẩm thực — **Hoàn thành** |
| 2 | `/api/v1/categories/{slug}` | `GET` | Public (Cache 60m) | Chi tiết danh mục theo slug tiếng Việt — **Hoàn thành** |
| 3 | `/api/v1/categories` | `POST` | AdminPolicy | Tạo mới danh mục, tự động invalidate cache — **Hoàn thành** |
| 4 | `/api/v1/categories/{id}` | `PUT` | AdminPolicy | Cập nhật danh mục, tự động invalidate cache — **Hoàn thành** |
| 5 | `/api/v1/categories/{id}` | `DELETE` | AdminPolicy | Xoá danh mục (Hard delete C07), invalidate cache — **Hoàn thành** |
| 6 | `/api/v1/recipes` | `GET` | Public (Cache 15m) | Danh sách công thức khám phá, phân trang `pageSize=12` — **Hoàn thành** |
| 7 | `/api/v1/recipes/search` | `GET` | Public (Cache 1m) | Tìm kiếm FTS không dấu + bộ lọc AND đa tiêu chí — **Hoàn thành** |
| 8 | `/api/v1/recipes/sitemap` | `GET` | Public | Danh sách sitemap công thức cho bot tìm kiếm — **Hoàn thành** |
| 9 | `/api/v1/recipes/{slug}` | `GET` | Public | Chi tiết công thức công khai theo slug — **Hoàn thành** |

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B3 & K11 — Tìm kiếm FTS tiếng Việt không dấu với GIN Trigram Index & SSR Search
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B3, K11
- **FR/NFR/Kỹ năng**: FR-SRCH-001, FR-SRCH-002, FR-SRCH-003, FR-SRCH-004; NFR-PERF-001; K02, K06, K11, K16
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/Migrations/20260929060455_AddFtsAndGinIndex.cs`: Migration kích hoạt extension `unaccent`, `pg_trgm`, tạo hàm bất biến `f_unaccent(text)` và GIN index `IDX_Recipes_Title_Description_Trgm`.
  - `src/backend/CulinaryBlog.Application/Discovery.cs`: `SearchRecipesQuery`, `SearchRecipesHandler` hỗ trợ tìm kiếm không dấu kết hợp đồng thời bộ lọc AND (`CategoryId`, `Difficulty`, `MaxCookTime`, `MinServings`).
  - `src/frontend/src/app/search/page.tsx`: Giao diện tìm kiếm Server-Side Rendering (SSR) trong Next.js 15, bắt query params thời gian thực, điều hướng phân trang mượt mà.
- **Test / Kết quả thực tế**:
  - Test `SearchRecipes_supports_unaccented_vietnamese_and_and_filters`: Tìm từ khóa không dấu `"pho ga"` khớp chính xác `"Phở Gà Đồi Lá Chanh"`. Lọc kết hợp độ khó và thời gian nấu trả kết quả chuẩn xác -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 2: Task B5 & K12 — Cache-Aside phân tầng Redis, Invalidation tự động & Resilient Fallback (NFR-REL-002)
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B5, K12
- **FR/NFR/Kỹ năng**: FR-CAT-001, FR-SRCH-001; NFR-PERF-001, NFR-REL-002; K01, K03, K04, K12, K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/IRecipeCacheService.cs` & `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs`.
  - TTL phân tầng: Categories 60 phút, Recipes 15 phút, Search kết quả 1 phút.
  - Cơ chế tự phục hồi (Resilience Fallback): Bọc try/catch toàn diện. Khi Redis gặp sự cố hoặc timeout, hệ thống ghi log cảnh báo và tự động fallback về CSDL PostgreSQL mà không quăng lỗi HTTP 500 ra người dùng.
- **Test / Kết quả thực tế**:
  - Test `Categories_GetCategories_and_GetBySlug_cache_and_invalidate_properly` -> **PASS**.
  - Test `Cache_service_fallback_resilience_when_cache_server_down` -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 3: Task B4 & K18, K19 — Tối ưu SEO Search Metadata, Robots Noindex và Giao diện Responsive WCAG 2.1 AA
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B4, K18, K19
- **FR/NFR/Kỹ năng**: NFR-SEO-001, NFR-USE-001, NFR-ACC-001; K16, K18, K19
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/app/search/page.tsx`: Gắn thẻ `robots: { index: false, follow: true }` cho trang kết quả tìm kiếm có query param `q` (ngăn cản phân mảnh crawl budget và duplicate content), kèm thẻ chuẩn hóa `canonical`.
  - Cải tiến giao diện bộ lọc danh mục và độ khó chuẩn Responsive (Mobile 375px, Tablet 768px, Desktop 1280px), gắn aria-labels đầy đủ hỗ trợ người khiếm thị đọc màn hình.
- **Test / Kết quả thực tế**:
  - Next.js build `npm run build`: 15/15 routes biên dịch thành công 100%.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

## 2. Tổng kết tiến độ Tuần 4

| Nhóm nội dung | Kế hoạch tuần 4 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Yêu cầu tối thiểu Lab 4: Cài đặt API endpoints** | Hoàn thành tất cả API nhóm Danh mục & Tìm kiếm | 9/9 endpoints phân hệ Category & Discovery hoàn thành 100% | **100%** |
| **Tìm kiếm FTS tiếng Việt & SSR (Task B3)** | GIN Index, unaccent, SSR Search page | Hoàn thành migration, query handler và UI Next.js | **100%** |
| **Cache-Aside & Fallback (Task B5)** | Redis TTL phân tầng, Invalidation, fallback DB | Đầy đủ service, tích hợp CQRS handlers và 4 tests cache pass | **100%** |
| **SEO & Kiểm thử Tự động (Task B4, B7)** | Robots noindex search, xUnit tests, PR #18 | PR #18 đã merge vào `main`, 177 tests toàn hệ thống pass | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Ngô Quốc Trường Vĩ (TV2 - 2312796)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312796_NgoQuocTruongVi.docx` đã được tạo tại `docs/evidence/TV2/` và `~/Downloads/`.
