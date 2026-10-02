# Báo cáo nghiệm thu Tuần 4 — TV3 (Huỳnh Quốc Trung - 2312786)

- **Người thực hiện**: Huỳnh Quốc Trung (MSSV: 2312786) — TV3
- **Phần việc phụ trách**: Phân hệ Soạn thảo Công thức (Recipe Aggregate), Quản lý Nguyên liệu & Các bước thực hiện, Wizard 5 bước, Optimistic Concurrency Control (RowVersion) & Xoá mềm
- **Mã task tuần 4**: C4, C5, C6, C7
- **Nhánh Git**: `2312786_HuynhQuocTrung_C4-recipe-ui` (đã tích hợp vào `main`)
- **Ngày hoàn thành**: 30/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 4 (177/177 tests toàn hệ thống pass, 15/15 trang Next.js build sạch)
- **Quy tắc đặt tên file nộp bài Lab 4**: `Lab4_2312786_HuynhQuocTrung.docx` *(kèm file dự phòng `Lab04_2312786_HuynhQuocTrung.docx`)*

---

## 0. Yêu cầu tối thiểu của Lab 4: Hoàn thành việc cài đặt tất cả API endpoints (41/41 Endpoints - 100%)

Thành viên TV3 đã hoàn thành và tích hợp toàn bộ 12 API endpoints thuộc phân hệ Soạn thảo và Quản lý Công thức (Recipe Authoring):

| STT | Endpoint | Phương thức | Quyền hạn / Policy | Mô tả & Trạng thái |
|:---:|---|:---:|:---:|---|
| 1 | `/api/v1/recipes` | `POST` | AuthorPolicy | Tạo mới công thức nháp (Draft Recipe) — **Hoàn thành** |
| 2 | `/api/v1/recipes/{id}` | `PUT` | AuthorPolicy (Owner/Admin) | Cập nhật thông tin & dinh dưỡng (OCC qua RowVersion) — **Hoàn thành** |
| 3 | `/api/v1/recipes/{id}` | `DELETE` | AuthorPolicy (Owner/Admin) | Xoá mềm công thức (Soft delete C01) — **Hoàn thành** |
| 4 | `/api/v1/recipes/{id}/ingredients` | `POST` | AuthorPolicy (Owner/Admin) | Thêm nguyên liệu vào công thức — **Hoàn thành** |
| 5 | `/api/v1/recipes/{id}/ingredients/{iid}` | `PUT` | AuthorPolicy (Owner/Admin) | Cập nhật chi tiết nguyên liệu — **Hoàn thành** |
| 6 | `/api/v1/recipes/{id}/ingredients/{iid}` | `DELETE` | AuthorPolicy (Owner/Admin) | Xoá nguyên liệu khỏi công thức — **Hoàn thành** |
| 7 | `/api/v1/recipes/{id}/steps` | `POST` | AuthorPolicy (Owner/Admin) | Thêm bước thực hiện kèm `TimerMinutes` — **Hoàn thành** |
| 8 | `/api/v1/recipes/{id}/steps/{sid}` | `PUT` | AuthorPolicy (Owner/Admin) | Cập nhật nội dung bước thực hiện — **Hoàn thành** |
| 9 | `/api/v1/recipes/{id}/steps/{sid}` | `DELETE` | AuthorPolicy (Owner/Admin) | Xoá bước thực hiện — **Hoàn thành** |
| 10 | `/api/v1/recipes/{id}/steps/reorder` | `PATCH` | AuthorPolicy (Owner/Admin) | Đổi thứ tự các bước (đánh số 2 pha tránh conflict) — **Hoàn thành** |
| 11 | `/api/v1/me/recipes` | `GET` | AuthorPolicy (Owner) | Lấy danh sách công thức của tác giả đăng nhập — **Hoàn thành** |
| 12 | `/api/v1/me/recipes/counts` | `GET` | AuthorPolicy (Owner) | Thống kê số lượng công thức theo trạng thái — **Hoàn thành** |

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task C4 & K05, K16, K17 — Wizard Soạn thảo Công thức 5 bước & Quản lý Recipe Dashboard
- **Tuần / Người / Task**: 4 / Huỳnh Quốc Trung (TV3) / C4, K05, K16, K17
- **FR/NFR/Kỹ năng**: FR-RCP-001, FR-RCP-002; NFR-USE-001; K05, K16, K17
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Giao diện Wizard 5 bước tại `/dashboard/recipes/new` và `/[id]/edit`:
    - Bước 1: Thông tin cơ bản & Giá trị dinh dưỡng (Calories, Protein, Carbs, Fat).
    - Bước 2: Quản lý danh sách nguyên liệu động.
    - Bước 3: Quản lý các bước thực hiện, hỗ trợ kéo thả / đổi thứ tự `OrderIndex` và hẹn giờ `TimerMinutes`.
    - Bước 4: Tải lên và chọn ảnh đại diện công thức.
    - Bước 5: Xem lại toàn bộ và Xuất bản (kiểm tra điều kiện bắt buộc: ≥ 1 nguyên liệu, ≥ 1 bước, có ảnh).
  - Dashboard `/dashboard/recipes`: Bảng danh sách công thức cá nhân, lọc theo trạng thái (`Draft`, `Published`, `Archived`), đếm số lượng thời gian thực.
- **Test / Kết quả thực tế**:
  - Build frontend Next.js 15: Biên dịch thành công 15/15 trang sạch sẽ.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 2: Task C5 & K07 — Kiểm soát Tương tranh Lạc quan (Optimistic Concurrency Control) & RowVersion
- **Tuần / Người / Task**: 4 / Huỳnh Quốc Trung (TV3) / C5, K07
- **FR/NFR/Kỹ năng**: FR-RCP-003; NFR-DATA-001; K03, K06, K07
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Cấu hình thuộc tính `RowVersion` / `xmin` trên thực thể PostgreSQL của `Recipe`.
  - Cơ chế tự động phát hiện xung đột ghi đè đồng thời (Lost Update Problem): Khi phát hiện xung đột, EF Core quăng `DbUpdateConcurrencyException`, middleware API chuyển đổi thành HTTP 422 Unprocessable Entity kèm mã lỗi `recipe.version_conflict`.
  - Frontend xử lý thông báo xung đột và cung cấp nút reload dữ liệu mới nhất mà không mất thao tác người dùng.
- **Test / Kết quả thực tế**:
  - 5/5 tests trong `tests/concurrency-spike/` pass 100%.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 3: Task C7 & K21 — Kiểm thử Tự động Luồng Soạn thảo RecipeAuthoringFlowTests trên PostgreSQL Thật
- **Tuần / Người / Task**: 4 / Huỳnh Quốc Trung (TV3) / C7, K21
- **FR/NFR/Kỹ năng**: FR-RCP-001, FR-RCP-002; K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Xây dựng bài test xUnit tích hợp: Luồng trọn vẹn từ tạo bản nháp -> thêm nguyên liệu -> thêm bước -> đổi thứ tự bước -> xuất bản thành công.
  - Kiểm tra negative test: Chặn yêu cầu xuất bản khi thiếu nguyên liệu/bước với HTTP 422 `RECIPE_PUBLISH_INCOMPLETE`. Chặn truy cập khi chưa đăng nhập với HTTP 401 Unauthorized.
- **Test / Kết quả thực tế**:
  - Toàn bộ bài test luồng soạn thảo vượt qua trên CSDL PostgreSQL thật -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

## 2. Tổng kết tiến độ Tuần 4

| Nhóm nội dung | Kế hoạch tuần 4 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Yêu cầu tối thiểu Lab 4: Cài đặt API endpoints** | Hoàn thành tất cả API phân hệ Soạn thảo | 12/12 endpoints phân hệ Recipe Authoring hoàn thành 100% | **100%** |
| **Giao diện Wizard 5 bước (Task C4)** | Form soạn thảo đa bước, validation, nutrition | Hoàn thiện 5 bước wizard, UI Dashboard responsive | **100%** |
| **Optimistic Concurrency Control (Task C5)** | RowVersion, xmin, chống lost update | OCC hoạt động hoàn hảo, 5 tests concurrency spike pass | **100%** |
| **Kiểm thử Luồng Soạn thảo (Task C7)** | Test tích hợp tạo/sửa/xoá/reorder/publish | Toàn bộ tests pass, mã nguồn tích hợp vào `main` | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Huỳnh Quốc Trung (TV3 - 2312786)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312786_HuynhQuocTrung.docx` đã được tạo tại `docs/evidence/TV3/` và `~/Downloads/`.
