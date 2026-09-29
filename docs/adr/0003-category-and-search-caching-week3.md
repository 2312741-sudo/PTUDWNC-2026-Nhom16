# ADR 0003: Chiến lược Cache-Aside, Invalidation và Tối ưu Full-Text Search với GIN Trigram Index (Tuần 3)

- **Trạng thái:** Đã chấp thuận (Accepted)
- **Ngày:** 28/09/2026
- **Tác giả:** Ngô Quốc Trường Vĩ (2312796 — TV2)
- **Người duyệt:** Nguyễn Thanh Tâm (Nhóm trưởng — TV1)

---

## 1. Bối cảnh
Trong hệ thống Culinary Blog, các truy vấn Khám phá danh mục (Category Discovery) và Tìm kiếm công thức (Recipe Search) chiếm hơn 80% lưu lượng truy cập công khai từ người dùng cuối và bot công cụ tìm kiếm.
Nếu tất cả các yêu cầu đọc này đều truy vấn trực tiếp vào PostgreSQL, hệ thống sẽ gặp các vấn đề:
1. Tải CPU và I/O của database tăng cao khi có đột biến lưu lượng (Spike).
2. Thời gian phản hồi của trang tìm kiếm và trang chủ bị kéo dài, ảnh hưởng tiêu cực đến Core Web Vitals (LCP, TTFB) và trải nghiệm người dùng (UX).
3. Tìm kiếm tiếng Việt có dấu/không dấu trên các cột text dài nếu dùng `LIKE '%...%'` thông thường sẽ gây Full Table Scan, không tận dụng được chỉ mục B-tree truyền thống.
4. Rủi ro lỗi gián đoạn dịch vụ (Single Point of Failure): nếu cụm Cache (Redis) gặp sự cố mạng hoặc sập tiến trình, ứng dụng không được phép sập theo (trả lỗi 500) mà phải tiếp tục phục vụ người dùng ổn định (NFR-REL-002).

---

## 2. Quyết định kiến trúc

### 2.1. Kiến trúc Cache-Aside và Phân cấp TTL (Task B5)
Áp dụng mẫu hình **Cache-Aside Pattern** thông qua abstraction `IRecipeCacheService` nằm ở tầng `CulinaryBlog.Application` (tuân thủ nghiêm ngặt Clean Architecture CONS-001):
* **Danh mục món ăn (`categories:all`, `category:slug:*`):** Dữ liệu danh mục rất ít khi thay đổi (chỉ do Admin cập nhật), do đó thiết lập thời gian sống **TTL = 60 phút**.
* **Danh sách công thức phân trang (`recipes:list:*`):** Dữ liệu trang chủ và duyệt công thức theo trang, thiết lập **TTL = 15 phút**.
* **Kết quả tìm kiếm công thức (`recipes:search:*`):** Tần suất tìm kiếm theo từ khóa đa dạng, thiết lập **TTL = 1 phút** để cân bằng giữa việc giảm tải DB và tính kịp thời của kết quả.
* **Nguyên tắc cô lập quyền riêng tư (Privacy & Cache Isolation):** Cache công khai chỉ lưu trữ các công thức đã được xuất bản (`RecipeStatus = Published` và `IsDeleted = false`). Bản nháp (`Draft`) hoặc công thức lưu trữ (`Archived`) tuyệt đối không bao giờ được cache vào vùng public search/list.

### 2.2. Chiến lược Cache Invalidation chủ động (Event/Mutation Driven)
Không phụ thuộc hoàn toàn vào TTL tự nhiên. Mỗi khi có thao tác làm biến đổi dữ liệu (Mutation), hệ thống chủ động xoá bỏ cache liên quan ngay lập tức:
* Thêm/sửa/xoá Category (`CreateCategoryHandler`, `UpdateCategoryHandler`, `DeleteCategoryHandler`): Invalidate prefix `categories:all` và `category:slug:*`.
* Thay đổi trạng thái/nội dung Recipe (`PublishRecipeHandler`, `UnpublishRecipeHandler`, `ArchiveRecipeHandler`, `DeleteRecipeHandler`): Invalidate prefix `recipes:list:` và `recipes:search:`.

### 2.3. Khả năng chịu lỗi và Fallback linh hoạt (Resilience & Graceful Degradation - NFR-REL-002)
Trong `RecipeCacheService`:
* Mọi thao tác đọc/ghi/xóa cache đều được bọc trong khối bảo vệ `try/catch`.
* Khi Redis hoặc cụm Cache gặp sự cố (mất kết nối, timeout, connection reset), service ghi nhận cảnh báo (`ILogger.LogWarning`) và trả về `default(T)` mà không quăng lỗi ra ngoài.
* Các Use Case Handler (như `SearchRecipesHandler`, `GetRecipesHandler`, `GetCategoriesHandler`) khi thấy cache trả về `null` sẽ tự động truy vấn trực tiếp từ PostgreSQL Database Repository. Toàn bộ tiến trình diễn ra minh bạch, người dùng không nhận mã lỗi 500.

### 2.4. Tối ưu Full-Text Search với pg_trgm, unaccent và GIN Index (Task B3)
* Tạo Migration `AddFtsAndGinIndex` trong PostgreSQL:
  * Kích hoạt tiện ích mở rộng `unaccent` (hỗ trợ chuẩn hóa tiếng Việt bỏ dấu) và `pg_trgm` (hỗ trợ n-gram matching).
  * Định nghĩa hàm bất biến `f_unaccent(text)` để có thể lập chỉ mục biểu thức (IMMUTABLE Expression Index).
  * Tạo chỉ mục `IDX_Recipes_Title_Description_Trgm` theo cơ chế **GIN (Generalized Inverted Index)** trên biểu thức `f_unaccent(Title) gin_trgm_ops` và `f_unaccent(Description) gin_trgm_ops`.
* Nhờ chỉ mục GIN trigram, các câu truy vấn tìm kiếm tiếng Việt không dấu (ví dụ gõ `"pho bo"` tìm thấy `"Phở Bò Sốt Vang"`) đạt tốc độ cực nhanh ngay cả khi tập dữ liệu phát triển lên hàng trăm nghìn bản ghi.

### 2.5. Tối ưu Frontend Search SSR và SEO Tagging (Task B3)
* Màn hình `/search` được triển khai dưới dạng **Server Component (SSR)** của Next.js App Router:
  * URL phản ánh toàn diện trạng thái bộ lọc: `q`, `categoryId`, `difficulty`, `maxCookTime`, `minServings`, `sortBy`, `sortOrder`, `page`.
  * Hỗ trợ hàm `generateMetadata` tự động:
    * Khi có tham số tìm kiếm `q`: Thiết lập `robots: { index: false, follow: true }` để tránh lỗi nội dung trùng lặp và lãng phí crawl budget của Googlebot.
    * Gắn thẻ liên kết chuẩn hóa `canonical` trỏ về `https://culinaryblog.vn/search`.

---

## 3. Hệ quả
* **Ưu điểm:**
  * Giảm hơn 75% số lượng truy vấn đọc trực tiếp vào database trong điều kiện tải bình thường.
  * Tốc độ tìm kiếm tiếng Việt được tăng tốc đáng kể nhờ chỉ mục GIN, đồng thời hỗ trợ người dùng gõ không dấu tự nhiên.
  * Đảm bảo tính khả dụng của hệ thống theo tiêu chuẩn NFR-REL-002: hỏng cache không gây hỏng ứng dụng.
  * Tuân thủ kiến trúc sạch (Clean Architecture) với 100% bài kiểm tra Architecture Tests hợp lệ.
* **Nhược điểm:**
  * Việc xoá cache theo prefix trong môi trường Redis phân tán quy mô lớn cần quản lý kích thước keyspace cẩn thận để tránh nghẽn thread đơn của Redis.
