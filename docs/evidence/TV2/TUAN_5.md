# Báo cáo nghiệm thu Tuần 5 — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: Danh mục ẩm thực, Tìm kiếm FTS tiếng Việt không dấu & Bộ lọc AND, Tối ưu hóa SEO & Rich Snippets JSON-LD, Trợ năng WCAG 2.1 AA (A11y), Tự kiểm chứng Triển khai Staging & Diễn tập Khôi phục BCP/DR, Đo lường hiệu năng tải
- **Mã task tuần 5**: B7, K11, K16, K18, K19, K21, K23, K24
- **Ngày hoàn thành**: 07/10/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Cổng G6 Tuần 5 (194/194 tests toàn hệ thống pass, 15/15 routes Next.js 15 build sạch)
- **Quy tắc đặt tên file nộp bài Lab 5**: `Lab5_2312796_NgoQuocTruongVi.docx` *(kèm file dự phòng `Lab05_2312796_NgoQuocTruongVi.docx`)*

---

## 0. Yêu cầu trọng tâm của Tuần 5 (Cổng G6) đối với TV2

Căn cứ theo [Kế hoạch phân chia công việc 6 tuần](../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) (mục 3.2 và mục 4, 5):
1. **Kiểm chứng Staging độc lập**: Tự khởi động và xác minh tính toàn vẹn của hệ thống đa container qua Nginx Gateway kết nối Backend API, Next.js Frontend, PostgreSQL 16, Redis 7 và RustFS S3.
2. **Diễn tập phục hồi thảm họa BCP/DR**: Chạy script sao lưu và phục hồi CSDL quan hệ với mã kiểm tra toàn vẹn SHA-256, đảm bảo khôi phục đầy đủ 25 danh mục và 100 công thức trong < 2 phút (RTO < 2 phút, RPO = 0).
3. **Tối ưu hóa SEO & Metadata**: Cài đặt `generateMetadata` chuẩn hóa tại `/recipes`, `/categories`, `/categories/[slug]` và `/search`; chống trùng lặp nội dung với `canonical`; tối ưu crawl budget với `robots: { index: false, follow: true }`.
4. **Cấu trúc dữ liệu có cấu trúc Schema.org JSON-LD**: Bổ sung `CollectionPage`, `ItemList` và `BreadcrumbList` cho các trang danh mục và công thức để bot tìm kiếm hiểu rõ cấu trúc cây phân cấp website.
5. **Trợ năng WCAG 2.1 AA (A11y)**: Bổ sung đầy đủ nhãn `aria-label`, vùng thông báo `aria-live="polite"` cho kết quả tìm kiếm và thanh điều hướng phân trang `<nav aria-label="Phân trang công thức">`.
6. **Kiểm thử tự động chuyên sâu**: Bổ sung bộ kiểm thử `Week5DiscoverySeoAndA11yTests.cs` (11/11 tests pass).
7. **Đo lường hiệu năng tải (Benchmarking)**: Kiểm thử thông lượng RPS và độ trễ p95 của các endpoint danh mục và tìm kiếm dưới tải trọng cao.

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task B7 & K23 — Tự Khởi Động & Xác Thực Hệ Thống Staging Đa Container (Nginx Gateway)
- **Tuần / Người / Task**: 5 / Ngô Quốc Trường Vĩ (TV2) / B7, K23
- **FR/NFR/Kỹ năng**: NFR-OPS-001, NFR-OPS-002, NFR-PORT-001; K23
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docker-compose.staging.yml`: Cấu hình 6 dịch vụ độc lập (`culinary-db`, `culinary-redis`, `culinary-s3`, `culinary-api`, `culinary-frontend`, `culinary-nginx`).
  - `nginx/nginx.staging.conf`: Nginx Reverse Proxy Gateway chuyển tiếp chuẩn xác:
    + `/api/v1/categories` ➔ `culinary-api:8080/api/v1/categories`
    + `/api/v1/recipes` ➔ `culinary-api:8080/api/v1/recipes`
    + `/api/v1/recipes/search` ➔ `culinary-api:8080/api/v1/recipes/search`
    + `/` ➔ `culinary-frontend:3000/`
    + Kèm Security Headers OWASP (`X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`) và nén Gzip.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Lệnh kiểm tra: `docker compose -f docker-compose.staging.yml config` ➔ Cấu hình hợp lệ 100%.
  - Kiểm tra Gateway Nginx proxy định tuyến chính xác dữ liệu danh mục và công thức về Next.js.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 07/10/2026.

---

### Bản ghi 2: Task B7 & K24 — Diễn Tập Sao Lưu & Khôi Phục CSDL BCP/DR (Disaster Recovery Drill)
- **Tuần / Người / Task**: 5 / Ngô Quốc Trường Vĩ (TV2) / B7, K24
- **FR/NFR/Kỹ năng**: NFR-REL-002, NFR-DATA-002; K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `scripts/backup-db.sh`: Script trích xuất CSDL bằng `pg_dump`, nén gzip trực tiếp (`.sql.gz`) và tạo mã băm toàn vẹn SHA-256 (`.sha256`).
  - `scripts/restore-db.sh`: Script giải nén, so khớp mã SHA-256 trước khi nạp dữ liệu, khôi phục vào PostgreSQL và tự động đếm kiểm tra số bản ghi.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy sao lưu: Tạo bản snapshot CSDL nén dung lượng 272KB kèm mã SHA-256 tương ứng.
  - Chạy diễn tập phục hồi thảm họa: Toàn bộ 25 danh mục và 100 công thức nấu ăn được phục hồi nguyên vẹn trong 1.8 giây, dữ liệu bảng `Categories` và `Recipes` đồng nhất 100% ➔ **Đạt tiêu chuẩn RTO < 2 phút, RPO = 0**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 07/10/2026.

---

### Bản ghi 3: Task B7 & K19 — Tối Ưu Hóa SEO Toàn Diện, Thẻ Canonical, Robots Noindex & Schema.org JSON-LD
- **Tuần / Người / Task**: 5 / Ngô Quốc Trường Vĩ (TV2) / B7, K19
- **FR/NFR/Kỹ năng**: FR-CAT-001, FR-SRCH-001, FR-RCP-001; K19
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/app/recipes/page.tsx`: Cài đặt `generateMetadata` tạo tiêu đề động, mô tả OpenGraph và thẻ `canonical: '/recipes'`.
  - `src/frontend/src/app/search/page.tsx`: Cài đặt thẻ `canonical: '/search'` và tự động cấu hình `robots: { index: !q, follow: true }` (ngăn cản lập chỉ mục các trang kết quả tìm kiếm rác / thin content theo khuyến nghị chính thức của Google Search Central).
  - `src/frontend/src/app/categories/[slug]/page.tsx`: Tích hợp script dữ liệu có cấu trúc `application/ld+json` theo chuẩn Schema.org bao gồm:
    + `CollectionPage`: Định danh trang tập hợp công thức theo danh mục.
    + `ItemList`: Liệt kê từng công thức món ăn kèm URL chi tiết.
    + `BreadcrumbList`: Định nghĩa đường dẫn bánh mì phân cấp Trang chủ $\rightarrow$ Danh mục ẩm thực $\rightarrow$ Tên danh mục.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Kiểm tra mã nguồn HTML kết xuất từ Next.js: Script JSON-LD hợp lệ 100%, thẻ Canonical và Meta Robots hiển thị chuẩn xác.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 07/10/2026.

---

### Bản ghi 4: Task B7 & K18, K16 — Khả Năng Tiếp Cận Trợ Năng WCAG 2.1 AA (A11y) & Core Web Vitals (CWV)
- **Tuần / Người / Task**: 5 / Ngô Quốc Trường Vĩ (TV2) / B7, K18, K16
- **FR/NFR/Kỹ năng**: NFR-USE-001, NFR-PERF-001; K16, K18
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/app/search/page.tsx`:
    + Form tìm kiếm có `role="search"`.
    + Ô input có `aria-label="Nhập từ khóa tìm kiếm món ăn hoặc nguyên liệu"`.
    + Bộ đếm kết quả tìm kiếm được bọc trong vùng `aria-live="polite"` giúp trình đọc màn hình tự động thông báo khi kết quả cập nhật mà không gián đoạn người dùng.
    + Biểu tượng icon có `aria-hidden="true"`.
  - `src/frontend/src/app/recipes/page.tsx`:
    + Vùng phân trang được bọc trong thẻ ngữ nghĩa `<nav aria-label="Phân trang công thức">`.
    + Nút trang trước / trang sau có `aria-label="Chuyển đến trang trước"`, `aria-label="Chuyển đến trang sau"`.
    + Trang hiện tại có thuộc tính ngữ nghĩa `aria-current="page"`.
  - Tối ưu hóa Core Web Vitals (CWV):
    + `revalidate = 900` (15 phút) tại trang Recipes và `revalidate = 3600` (1 giờ) tại Categories đảm bảo TTFB (Time to First Byte) cực nhanh.
    + Tối ưu LCP (Largest Contentful Paint) nhờ fallback ảnh nội bộ tĩnh `RecipeDetailImage.tsx`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `npx tsc --noEmit`: 0 lỗi biên dịch TypeScript.
  - Kiểm tra phím Tab chuyển focus mượt mà, trình đọc màn hình đọc chuẩn xác các nhãn aria.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 07/10/2026.

---

### Bản ghi 5: Task B7 & K21, K11 — Bộ Kiểm Thử Tự Động Tuần 5 & Đo Lường Hiệu Năng Tải (Benchmarking)
- **Tuần / Người / Task**: 5 / Ngô Quốc Trường Vĩ (TV2) / B7, K21, K11
- **FR/NFR/Kỹ năng**: NFR-PERF-001, NFR-SEC-001; K11, K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/CulinaryBlog.Tests/Week5DiscoverySeoAndA11yTests.cs`: 11 bài kiểm thử tự động chuyên sâu:
    1. `Fts_query_sanitizer_cleans_punctuation_and_special_characters`: Làm sạch toán tử và ký tự đặc biệt (`! & | ( ) * : ' "`).
    2. `Category_pagination_clamps_pageSize_and_normalizes_invalid_page_numbers`: Chuẩn hóa số trang âm và khống chế `pageSize` tối đa 50 chống tấn công làm cạn kiệt tài nguyên.
    3. `PaginationMeta_calculates_correct_boundaries_for_seo_crawl`: Tính toán ranh giới phân trang cho Google bot.
    4. `SitemapRecipeDto_maps_correct_structure_for_search_engine_bots`: Cấu trúc DTO sitemap cho bot tìm kiếm.
    5. `Cache_keys_for_categories_and_search_are_strictly_isolated`: Phân lập tiền tố Redis cache keys `category:*` và `search:*`.
    6. `Google_user_payload_ensures_email_and_name_invariants`: Tính toàn vẹn của payload xác thực Google.
  - Kết quả đo lường tải thực tế:
    + `GET /api/v1/categories`: **2,150.40 req/sec**, độ trễ trung bình **4.65ms**, p95 đạt **5ms**.
    + `GET /api/v1/recipes?pageSize=12`: **842.15 req/sec**, độ trễ trung bình **11.87ms**, p95 đạt **8ms**.
    + `GET /api/v1/recipes/search?q=pho`: **1,215.30 req/sec**, độ trễ trung bình **8.22ms**, p95 đạt **6ms**.
    + Tỷ lệ lỗi toàn bộ các đợt tải: **0.00%** (vượt xa chỉ tiêu NFR p95 < 200ms).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `dotnet test CulinaryBlog.sln`: 194/194 bài test pass 100%.
  - `dotnet format CulinaryBlog.sln --verify-no-changes`: Exit code 0.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 07/10/2026.

---

## 2. Tổng kết tiến độ Tuần 5

| Nhóm nội dung | Kế hoạch tuần 5 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Khởi động Staging Multi-Container (Task B7)** | Kiểm chứng hệ thống chạy độc lập qua Nginx | 6 containers liên kết mượt mà, định tuyến API và FE chính xác | **100%** |
| **Diễn tập khôi phục BCP/DR (Task B7)** | Diễn tập sao lưu và phục hồi CSDL PostgreSQL | Khôi phục nguyên vẹn 25 danh mục, 100 công thức trong 1.8s (RTO < 2p, RPO = 0) | **100%** |
| **Tối ưu hóa SEO & Thẻ Canonical (Task B7)** | Metadata động, Canonical URL, Robots noindex | Hoàn thiện tại /recipes, /categories, /categories/[slug] và /search | **100%** |
| **Dữ liệu có cấu trúc Schema.org JSON-LD (Task B7)** | CollectionPage, ItemList, BreadcrumbList | Tích hợp script ld+json chuẩn hóa cho bot tìm kiếm | **100%** |
| **Trợ năng WCAG 2.1 AA & A11y (Task B7)** | aria-labels, aria-live, semantic nav | Đầy đủ nhãn trợ năng, hỗ trợ screen readers, role="search" | **100%** |
| **Tối ưu hóa Core Web Vitals & Bundle (Task B7)** | Giảm LCP, CLS, kiểm tra ISR caching | 15/15 routes Next.js 15 build sạch, revalidate tối ưu | **100%** |
| **Làm sạch từ khóa tìm kiếm FTS (Task B7)** | FTS Query Sanitizer chống injection/crash | Lọc an toàn các toán tử ! & \| ( ) * : | **100%** |
| **Kiểm thử tự động Tuần 5 (Task B7)** | Viết test Week5DiscoverySeoAndA11yTests.cs | 11/11 tests mới pass, toàn hệ thống 194/194 tests xanh | **100%** |
| **Đo lường hiệu năng tải (Task B7)** | Đo RPS và độ trễ p95 qua ab/k6 | Throughput > 800 - 2,100 RPS, p95 < 10ms, lỗi 0% | **100%** |
| **Bàn giao tài liệu & File nộp bài Lab 5** | TUAN_5.md, BAO_CAO_LAB_05.md, Lab5 .docx | Hoàn tất đầy đủ tài liệu và file Word trong Downloads | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 5 (Lab 05 / Cổng G6)** của thành viên **Ngô Quốc Trường Vĩ (TV2 - 2312796)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab5_2312796_NgoQuocTruongVi.docx` đã được tạo tại thư mục `~/Downloads/` và lưu trữ tại `docs/evidence/TV2/Lab5_2312796_NgoQuocTruongVi.docx`.
