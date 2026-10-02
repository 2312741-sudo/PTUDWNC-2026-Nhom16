# Báo cáo nghiệm thu Tuần 4 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục ẩm thực, Tìm kiếm FTS tiếng Việt không dấu (PostgreSQL GIN Index) & Word Boundary Matching, SSR Search & Bộ lọc AND, Caching Cache-Aside phân tầng & Fallback Resilient, Đăng nhập Google & Liên kết Email
- **Mã task tuần 4**: B2, B3, B4, B5, B6, B7
- **Ngày hoàn thành**: 30/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 4 (178/178 tests toàn hệ thống pass, 15/15 routes Next.js build sạch)
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
| 10 | `/api/v1/auth/google` | `POST` | Public | Đăng nhập Google, xác thực ID Token & liên kết email — **Hoàn thành** |

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B3 & K11 — Tìm kiếm FTS tiếng Việt không dấu với GIN Trigram Index & Word Boundary Matching
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B3, K11
- **FR/NFR/Kỹ năng**: FR-SRCH-001, FR-SRCH-002, FR-SRCH-003, FR-SRCH-004; NFR-PERF-001; K02, K06, K11, K16
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/Migrations/20260929060455_AddFtsAndGinIndex.cs`: Migration kích hoạt extension `unaccent`, `pg_trgm`, tạo hàm bất biến `f_unaccent(text)` và GIN index `IDX_Recipes_Title_Description_Trgm`.
  - `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs`: Áp dụng ranh giới từ (word boundaries) cho tiếng Việt và phân tách token slug theo dấu gạch ngang (`-`), triệt tiêu hoàn toàn false positives (ví dụ tìm từ khóa ngắn như "gà" chỉ ra đúng các món có từ "gà", không còn lọt món "pancake" hay "flan").
  - `src/frontend/src/app/search/page.tsx`: Giao diện tìm kiếm Server-Side Rendering (SSR) trong Next.js 15.
- **Test / Kết quả thực tế**:
  - Test `Word_boundary_search_helper_prevents_false_positives` -> **PASS**.

---

### Bản ghi 2: Task B4 & K04 — Đăng nhập Google OAuth2 & Tự động liên kết tài khoản theo Email
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B4, K04
- **FR/NFR/Kỹ năng**: FR-AUTH-003; K04, K15
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/GoogleAuthService.cs`: Hỗ trợ xác thực token thật Google RS256 và giao thức token phát triển `dev_google:<email>:<name>`.
  - `src/frontend/src/components/GoogleSignInButton.tsx`: Modal đăng nhập Google một chạm, tự động điền email vừa đăng ký trên giao diện `/auth/login` và `/auth/register`.
- **Test / Kết quả thực tế**:
  - Test `GoogleAuthService_validates_dev_token_and_extracts_correct_payload` -> **PASS**.

---

### Bản ghi 3: Task B5 & K12 — Cache-Aside phân tầng Redis, Invalidation tự động & Resilient Fallback (NFR-REL-002)
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B5, K12
- **FR/NFR/Kỹ năng**: FR-CAT-001, FR-SRCH-001; NFR-PERF-001, NFR-REL-002; K01, K03, K04, K12, K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs`: TTL phân tầng: Categories 60 phút, Recipes 15 phút, Search kết quả 1 phút.
  - Cơ chế tự phục hồi (Resilience Fallback): Bọc try/catch toàn diện. Khi Redis gặp sự cố hoặc timeout, hệ thống ghi log cảnh báo và tự động fallback về CSDL PostgreSQL mà không quăng lỗi HTTP 500 ra người dùng.
- **Test / Kết quả thực tế**:
  - Test `Recipe_cache_keys_are_consistent_and_isolated` -> **PASS**.
  - Test `Category_cache_keys_are_consistent` -> **PASS**.

---

### Bản ghi 4: Task B2 & B4 — Tối ưu hóa Image Delivery & Bổ sung Thanh tìm kiếm tại trang Khám phá
- **Tuần / Người / Task**: 4 / Ngô Quốc Trường Vĩ (TV2) / B2, B4
- **FR/NFR/Kỹ năng**: NFR-USE-001, FR-RCP-001; K16, K17, K18
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/components/RecipeDetailImage.tsx`: Tự động nạp đúng ảnh độ nét cao theo từng món ăn, có cơ chế fallback tự động theo danh mục ẩm thực.
  - `src/frontend/src/components/RecipeSearchBar.tsx` & `src/frontend/src/app/recipes/page.tsx`: Tích hợp thanh tìm kiếm trực tiếp vào trang Khám phá công thức nấu ăn, kết hợp mượt mà với bộ lọc đa tiêu chí.
- **Test / Kết quả thực tế**:
  - Build Next.js 15: 15/15 routes biên dịch thành công 100%.

---

## 2. Tổng kết tiến độ Tuần 4

| Nhóm nội dung | Kế hoạch tuần 4 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Yêu cầu tối thiểu Lab 4: Cài đặt API endpoints** | Hoàn thành tất cả API nhóm Danh mục & Tìm kiếm | 10/10 endpoints phân hệ Category & Discovery & Auth Google | **100%** |
| **Tìm kiếm FTS & Ranh giới từ (Task B3)** | GIN Index, unaccent, Word Boundary Matching | Hoàn thành migration, query handler và loại bỏ false positives | **100%** |
| **Đăng nhập Google & Liên kết Email (Task B4)** | Hỗ trợ Dev token, Auto-linking theo email | Hoàn thành GoogleAuthService, IdentityService linking và modal UI | **100%** |
| **Cache-Aside & Fallback (Task B5)** | Redis TTL phân tầng, Invalidation, fallback DB | Đầy đủ service, tích hợp CQRS handlers và cache isolation tests | **100%** |
| **Chuẩn hóa công thức & Hình ảnh (Task B2)** | 100 món ăn chuẩn vị, RecipeDetailImage | Re-seed 100 món ăn >=10 NL, >=5 bước, component ảnh có fallback | **100%** |
| **UI Khám phá & Tìm kiếm (Task B2, B3)** | Thanh tìm kiếm RecipeSearchBar trên `/recipes` | Component tìm kiếm có clear button, URL query preservation | **100%** |
| **Kiểm thử Tự động (Task B7)** | Viết test Week4, 100% test suite Green | 178/178 tests toàn hệ thống pass | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Ngô Quốc Trường Vĩ (TV2 - 2312796)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312796_NgoQuocTruongVi.docx` đã được tạo tại `docs/evidence/TV2/` và `~/Downloads/`.
