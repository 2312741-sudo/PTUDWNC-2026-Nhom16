# ADR 0002: Thiết kế Module Category, Phân trang D11 và Đăng nhập Google D04

- **Trạng thái:** Đã chấp thuận (Accepted)
- **Ngày:** 14/09/2026
- **Tác giả:** Ngô Quốc Trường Vĩ (2312796 — TV2)
- **Người duyệt:** Nguyễn Thanh Tâm (Nhóm trưởng — TV1)

---

## 1. Bối cảnh
Trong tài liệu SRS Culinary Blog v1.0.0, có các điểm mâu thuẫn cần chốt:
1. **D04:** Google OAuth Flow (Chương 3 nêu Authorization Code Flow với PKCE trên Auth.js, trong khi Chương 8 lại nhận ID token từ client).
2. **D09:** Xóa Category (Chương 3 nói xóa entity, Chương 8 nói soft delete, chưa rõ cách xử lý công thức còn lại).
3. **D11:** Cấu trúc phân trang (`items/totalCount` vs `data/meta`, `pageSize` 10 vs 12).
4. **D15:** Độ dài tên Category (2–50 vs 100 trong DB).
5. **D29:** Thứ tự hiển thị Category (Sắp theo Name vs OrderIndex).

---

## 2. Quyết định kiến trúc

### 2.1. Quy ước Category Entity & Xóa an toàn (D09 & D15)
* Tên danh mục hỗ trợ độ dài **từ 2 đến 100 ký tự** (D15).
* Slug danh mục được tự động sinh không dấu, chuyển chữ thường, thay khoảng trắng/ký tự đặc biệt bằng dấu gạch ngang `-`, độ dài tối đa 120 ký tự.
* **Cập nhật danh mục giữ nguyên slug:** Khi cập nhật danh mục, slug ban đầu không thay đổi nhằm bảo vệ tính toàn vẹn của các URL SEO và tránh gãy liên kết bên ngoài.
* **Cơ chế xóa (Hard Delete & Conflict Guard theo SRS v1.1.1 - C07):** Áp dụng xóa cứng (Hard Delete — xóa entity khỏi database) theo đúng chuẩn hóa của SRS v1.1.1 (FR-CAT-005). Chặn xóa và trả mã lỗi **`409 Conflict` (`category.delete_has_recipes`)** nếu danh mục vẫn còn bất kỳ công thức nào (kể cả món `Draft` hay `Archived`) để bảo vệ tính toàn vẹn dữ liệu.

### 2.2. Thứ tự hiển thị danh mục (D29)
* API `GET /api/v1/categories` mặc định sắp xếp theo **`OrderIndex` tăng dần**, sau đó sắp theo **`Name` tăng dần** để phục vụ việc điều hướng thanh menu (Navigation) của ban quản trị linh hoạt.

### 2.3. Cấu trúc phân trang thống nhất (D11)
* Mọi API phân trang trong hệ thống sử dụng định dạng chung:
  ```json
  {
    "data": [],
    "meta": {
      "page": 1,
      "pageSize": 12,
      "total": 0,
      "totalPages": 0,
      "hasNextPage": false,
      "hasPreviousPage": false
    }
  }
  ```
* Giá trị mặc định: `page = 1`, `pageSize = 12`, `pageSize` tối đa 50.

### 2.4. Đăng nhập Google (D04)
* Frontend (Next.js App Router) sử dụng Auth.js v5 với Code Flow + PKCE để tương tác với máy chủ Google, đảm bảo client secret không bị lộ ở trình duyệt.
* Backend cung cấp endpoint `POST /api/v1/auth/google` nhận `idToken` và xác minh chữ ký độc lập với Google RS256 Public Keys. Tuyệt đối không chấp nhận thông tin profile do client tự gửi lên mà không có chữ ký xác thực.

---

## 3. Hệ quả
* **Ưu điểm:**
  * Thống nhất mô hình dữ liệu giữa 4 thành viên ngay từ tuần 1.
  * TV3 có thể an tâm sử dụng `CategoryId` mà không lo danh mục bị xóa mất dữ liệu khi đang tạo công thức.
  * Tránh xung đột API DTOs khi kết nối Frontend.
* **Nhược điểm:**
  * Cần thêm logic kiểm tra `CountRecipesAsync` trong `DeleteCategoryHandler`.
