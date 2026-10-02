# BÁO CÁO LAB - MÔN PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

- **Lab**: **04 (Tuần 4 — Cài đặt Toàn bộ API Endpoints, Wizard Soạn thảo 5 bước, Concurrency RowVersion & Xoá mềm)**
- **Từ ngày**: **23/09/2026** **đến ngày**: **30/09/2026**
- **MSSV**: **2312786**
- **Họ và tên**: **Huỳnh Quốc Trung**
- **Nhóm**: **16** (Thành viên — TV3)
- **Tên file nộp theo quy định**: `Lab4_2312786_HuynhQuocTrung.docx` *(kèm file dự phòng `Lab04_2312786_HuynhQuocTrung.docx`)*

---

### Bảng công việc thực hiện (Lab 04 / Tuần 4)

| STT | Công việc được giao | Liên kết đến github branch / PR / file | Tiến độ % |
|:---:|---|---|:---:|
| **1** | **[Yêu cầu trọng tâm Lab 4] Hoàn thành việc cài đặt TẤT CẢ các API endpoints theo hợp đồng và SRS (41 endpoints)**<br><br>_**Đã hoàn thành:**_<br>- Cùng nhóm hoàn thành và tích hợp 100% tất cả 41 API endpoints trên hệ thống, trong đó trực tiếp phụ trách 12 endpoints phân hệ Soạn thảo & Quản lý Công thức:<br>  + `POST /api/v1/recipes`, `PUT /api/v1/recipes/{id}`, `DELETE /api/v1/recipes/{id}`.<br>  + `POST /api/v1/recipes/{id}/ingredients`, `PUT /api/v1/recipes/{id}/ingredients/{iid}`, `DELETE /api/v1/recipes/{id}/ingredients/{iid}`.<br>  + `POST /api/v1/recipes/{id}/steps`, `PUT /api/v1/recipes/{id}/steps/{sid}`, `DELETE /api/v1/recipes/{id}/steps/{sid}`, `PATCH /api/v1/recipes/{id}/steps/reorder`.<br>  + `GET /api/v1/me/recipes`, `GET /api/v1/me/recipes/counts`.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% tất cả các API endpoints). | [Recipes.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Application/Recipes.cs) | **100%** |
| **2** | **[Giao diện Wizard Soạn thảo 5 bước (Task C4)] Xây dựng form soạn thảo đa bước & Dashboard Quản lý Công thức**<br><br>_**Đã hoàn thành:**_<br>- Xây dựng giao diện Wizard 5 bước tại `/dashboard/recipes/new` và `/[id]/edit`: Thông tin cơ bản/Dinh dưỡng, Nguyên liệu, Các bước (đổi thứ tự), Upload ảnh bìa, Xem lại & Xuất bản.<br>- Xây dựng trang Dashboard `/dashboard/recipes` thống kê số lượng bài theo trạng thái, hỗ trợ lọc và xoá mềm (Soft delete).<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Recipe Wizard](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/app/dashboard/recipes/new/page.tsx) | **100%** |
| **3** | **[Kiểm soát Tương tranh Lạc quan OCC (Task C5)] Cấu hình RowVersion / xmin chống Lost Update trên PostgreSQL**<br><br>_**Đã hoàn thành:**_<br>- Cấu hình token tương tranh `RowVersion` / `xmin` trên thực thể `Recipe`.<br>- Bắt lỗi `DbUpdateConcurrencyException` khi phát hiện 2 người dùng ghi đè đồng thời và chuyển đổi thành HTTP 422 `recipe.version_conflict`.<br>- Giao diện Frontend hiển thị cảnh báo xung đột và cung cấp tính năng nạp lại dữ liệu mới nhất.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Concurrency Spike](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/concurrency-spike) | **100%** |
| **4** | **[Đổi Thứ tự Các bước Thực hiện (Task C4)] Thuật toán đổi thứ tự 2 pha giải quyết triệt để lỗi Unique Constraint**<br><br>_**Đã hoàn thành:**_<br>- Giải quyết triệt để lỗi ràng buộc duy nhất `Unique(RecipeId, StepNumber)` trong PostgreSQL khi đổi thứ tự các bước bằng kỹ thuật cập nhật 2 pha (temporary negative indices) trong một Transaction duy nhất.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Recipes.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Application/Recipes.cs) | **100%** |
| **5** | **[Kiểm thử Tự động Luồng Soạn thảo (Task C7)] Bộ test tích hợp RecipeAuthoringFlowTests trên PostgreSQL Thật**<br><br>_**Đã hoàn thành:**_<br>- Viết bài test tích hợp luồng trọn vẹn từ tạo bản nháp -> thêm nguyên liệu -> thêm bước -> đổi thứ tự -> kiểm tra điều kiện xuất bản -> xuất bản thành công.<br>- Kiểm tra các trường hợp chặn: HTTP 422 khi thiếu thành phần bắt buộc, HTTP 401 khi chưa xác thực.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Recipe Tests](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/RecipeAuthoringFlowTests.cs) | **100%** |

---

### Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Huỳnh Quốc Trung (TV3 - 2312786)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312786_HuynhQuocTrung.docx` đã được tạo tại thư mục `~/Downloads/` và lưu trữ tại `docs/evidence/TV3/Lab4_2312786_HuynhQuocTrung.docx`.
