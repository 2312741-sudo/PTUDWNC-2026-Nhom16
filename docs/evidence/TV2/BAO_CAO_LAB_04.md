# BÁO CÁO LAB - MÔN PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

- **Lab**: **04 (Tuần 4 — Cài đặt Toàn bộ API Endpoints, FTS Search GIN Index, Cache-Aside Redis & Fallback)**
- **Từ ngày**: **23/09/2026** **đến ngày**: **30/09/2026**
- **MSSV**: **2312796**
- **Họ và tên**: **Ngô Quốc Trường Vĩ**
- **Nhóm**: **16** (Thành viên — TV2)
- **Tên file nộp theo quy định**: `Lab4_2312796_NgoQuocTruongVi.docx` *(kèm file dự phòng `Lab04_2312796_NgoQuocTruongVi.docx`)*

---

### Bảng công việc thực hiện (Lab 04 / Tuần 4)

| STT | Công việc được giao | Liên kết đến github branch / PR / file | Tiến độ % |
|:---:|---|---|:---:|
| **1** | **[Yêu cầu trọng tâm Lab 4] Hoàn thành việc cài đặt TẤT CẢ các API endpoints theo hợp đồng và SRS (41 endpoints)**<br><br>_**Đã hoàn thành:**_<br>- Cùng nhóm hoàn thành và tích hợp 100% tất cả 41 API endpoints trên hệ thống, trong đó trực tiếp phụ trách các nhóm API Danh mục, Khám phá và Tìm kiếm:<br>  + `GET /api/v1/categories`, `GET /api/v1/categories/{slug}`, `POST /api/v1/categories`, `PUT /api/v1/categories/{id}`, `DELETE /api/v1/categories/{id}`.<br>  + `GET /api/v1/recipes`, `GET /api/v1/recipes/search`, `GET /api/v1/recipes/sitemap`, `GET /api/v1/recipes/{slug}`.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% tất cả các API endpoints). | [Discovery.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Application/Discovery.cs) | **100%** |
| **2** | **[Tìm kiếm Toàn văn FTS (Task B3)] Cài đặt FTS tiếng Việt không dấu qua PostgreSQL Trigram GIN Index & unaccent**<br><br>_**Đã hoàn thành:**_<br>- Tạo migration EF Core kích hoạt extension `unaccent`, `pg_trgm`, tạo hàm bất biến `f_unaccent(text)` và GIN index `IDX_Recipes_Title_Description_Trgm`.<br>- Hoàn thiện `SearchRecipesQuery`, `SearchRecipesHandler` tìm kiếm tiếng Việt không dấu kết hợp đồng thời bộ lọc AND (`CategoryId`, `Difficulty`, `MaxCookTime`, `MinServings`).<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Migration FTS](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Infrastructure/Migrations/20260929060455_AddFtsAndGinIndex.cs) | **100%** |
| **3** | **[Caching Cache-Aside & Invalidation (Task B5)] Phân tầng TTL Redis và cơ chế Resilient Fallback khi Redis down**<br><br>_**Đã hoàn thành:**_<br>- Xây dựng `RecipeCacheService` triển khai `IRecipeCacheService` với phân tầng TTL: Categories 60 phút, Recipes 15 phút, Search 1 phút.<br>- Tích hợp tự động Invalidate cache sau các mutation thêm/sửa/xoá danh mục hoặc đổi trạng thái công thức.<br>- Cơ chế Resilient Fallback: Tự động ghi log cảnh báo và fallback về CSDL PostgreSQL khi Redis gặp sự cố hoặc timeout, không quăng lỗi HTTP 500 ra người dùng.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [RecipeCacheService.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs) | **100%** |
| **4** | **[Giao diện Tìm kiếm SSR & SEO (Task B3, B4)] Xây dựng trang Search Next.js 15 với Robots Noindex & Canonical**<br><br>_**Đã hoàn thành:**_<br>- Xây dựng trang `/search` Server-Side Rendering (SSR) trong Next.js App Router, nhận searchParams trực tiếp từ URL.<br>- Tự động gắn thẻ `robots: { index: false, follow: true }` khi có từ khóa `q` để tránh phân mảnh crawl budget và duplicate content của search results, kèm thẻ `canonical` chuẩn hóa.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Search Page](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/app/search/page.tsx) | **100%** |
| **5** | **[Kiểm thử Tự động & Tích hợp PR #18 (Task B7)] 10 bài test tự động xUnit và merge thành công vào nhánh main**<br><br>_**Đã hoàn thành:**_<br>- Viết 10 bài test tự động bao quát tìm kiếm không dấu, bộ lọc AND, kiểm tra phân lập cache (chỉ trả công thức Published, không trả Draft/Archived) và test phục hồi khi Redis sập.<br>- PR #18 đã được review và merge vào nhánh `main`, toàn bộ 177 tests giải pháp đều pass 100%.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [PR #18](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/18) | **100%** |

---

### Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Ngô Quốc Trường Vĩ (TV2 - 2312796)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312796_NgoQuocTruongVi.docx` đã được tạo tại thư mục `~/Downloads/` và lưu trữ tại `docs/evidence/TV2/Lab4_2312796_NgoQuocTruongVi.docx`.
