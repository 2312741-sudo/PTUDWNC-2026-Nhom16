# Category API Contract — Tuần 1 (TV2: Ngô Quốc Trường Vĩ)

Trạng thái: **Đã triển khai & sẵn sàng bàn giao cho TV3 (Recipe)**  
Phụ trách: **Ngô Quốc Trường Vĩ (2312796 — TV2)**  
Reviewer: **Nguyễn Thanh Tâm (Nhóm trưởng — TV1)**  
Prefix API: `/api/v1/categories`  
OpenAPI tag: `Categories`

---

## 1. Danh sách Endpoints

| Method | Route | Quyền truy cập | Thành công | Lỗi chính | Mô tả |
|---|---|---|---|---|---|
| `GET` | `/api/v1/categories` | Guest / Public | `200 OK` | — | Lấy toàn bộ danh mục, sắp xếp theo `OrderIndex` tăng dần rồi đến `Name` tăng dần (D29). Kèm số lượng recipe `Published`. |
| `GET` | `/api/v1/categories/{slug}` | Guest / Public | `200 OK` | `404 Not Found` | Lấy chi tiết một danh mục theo slug URL-friendly. |
| `POST` | `/api/v1/categories` | `AdminPolicy` | `201 Created` + `Location` | `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `409 Conflict` | Admin tạo danh mục mới. Slug được tự động sinh không dấu, đảm bảo unique. |
| `PUT` | `/api/v1/categories/{id}` | `AdminPolicy` | `200 OK` | `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict` | Admin cập nhật danh mục. Slug được bảo toàn nguyên vẹn theo D15/ADR. |
| `DELETE` | `/api/v1/categories/{id}` | `AdminPolicy` | `204 NoContent` | `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict` | Admin xóa danh mục (**hard delete** theo SRS v1.1.1 C07). Bị chặn trả về `409` nếu còn công thức liên kết (D09, FR-CAT-005). |

> **Ghi chú SRS v1.1.1:**
> - Toàn bộ response thành công được wrap trong `{ "data": ... }` (C08).
> - Cache danh mục có TTL = 60 phút (C03).
> - Xóa danh mục là **Hard Delete** (xóa entity khỏi database) khi không còn recipes (C07).
> - Tên danh mục `Name` có ràng buộc UNIQUE trong cơ sở dữ liệu (C09).

---

## 2. Schema DTO & Payloads

### `CategoryDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Món Khai Vị",
  "slug": "mon-khai-vi",
  "description": "Các món ăn nhẹ kích thích vị giác trước bữa chính",
  "imageUrl": "https://storage.culinaryblog.local/categories/mon-khai-vi.webp",
  "orderIndex": 1,
  "recipesCount": 12,
  "createdAt": "2026-09-14T01:00:00Z",
  "updatedAt": "2026-09-14T01:00:00Z"
}
```

### `CreateCategoryCommand` (Request Body cho `POST`)
```json
{
  "name": "Món Tráng Miệng",
  "description": "Bánh ngọt, kem, chè và hoa quả tươi",
  "imageUrl": "https://storage.culinaryblog.local/categories/trang-mieng.webp",
  "orderIndex": 2
}
```
*Ràng buộc validation:*
* `name`: Bắt buộc, độ dài từ 2 đến 100 ký tự, không chứa ký tự điều khiển hoặc thẻ HTML `< >`.
* `description`: Tùy chọn, tối đa 500 ký tự.
* `imageUrl`: Tùy chọn, tối đa 500 ký tự.
* `orderIndex`: Số nguyên $\ge 0$ (mặc định 0).

### `UpdateCategoryCommand` (Request Body cho `PUT /api/v1/categories/{id}`)
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Món Khai Vị & Ăn Nhẹ",
  "description": "Cập nhật mô tả mới",
  "imageUrl": "https://storage.culinaryblog.local/categories/mon-khai-vi-v2.webp",
  "orderIndex": 1
}
```
*Lưu ý:* `id` trong body phải trùng khớp với `{id}` trên route path.

---

## 3. Quy ước Lỗi & Problem Details (RFC 7807)

Mọi phản hồi lỗi đều có header `Content-Type: application/problem+json` và mã `X-Correlation-ID`:

| HTTP Status | Extension `code` | Tình huống |
|---|---|---|
| `400 Bad Request` | `validation.failed` | Dữ liệu đầu vào vi phạm FluentValidation (tên ngắn < 2 ký tự, chứa thẻ HTML...). Trả về dictionary `errors`. |
| `400 Bad Request` | `request.invalid` | Body JSON sai cú pháp hoặc `id` trong URL không khớp với `id` trong body. |
| `401 Unauthorized` | `http.401` | Chưa đính kèm Bearer token JWT hoặc token không hợp lệ/hết hạn. |
| `403 Forbidden` | `http.403` | Người dùng đã đăng nhập nhưng không có role `Admin`. |
| `404 Not Found` | `category.not_found` | Không tìm thấy danh mục với `id` hoặc `slug` tương ứng. |
| `409 Conflict` | `category.name_exists` | Đã tồn tại danh mục khác có cùng tên (không phân biệt hoa/thường). |
| `409 Conflict` | `category.delete_has_recipes` | Danh mục vẫn còn công thức liên kết (kể cả món `Draft` hay `Archived`), không được phép xóa (FR-CAT-005). |

---

## 4. Tích hợp cho TV3 (Recipe) & TV4 (Publish)
* TV3 dùng `CategoryDto.Id` làm khóa ngoại `Recipe.CategoryId`.
* Khi TV4 thực hiện xóa vĩnh viễn hoặc chuyển trạng thái món ăn, số lượng `recipesCount` sẽ được cập nhật đồng bộ.
