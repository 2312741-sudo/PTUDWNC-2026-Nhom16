# Search & Discovery API Contract — Tuần 2 (TV2: Ngô Quốc Trường Vĩ)

Trạng thái: **Đã triển khai & kiểm thử hoàn tất**  
Chủ trì: **Ngô Quốc Trường Vĩ (2312796 — TV2)**  
Reviewer: **Nguyễn Thanh Tâm (Nhóm trưởng — TV1)**  
Prefix API: `/api/v1/recipes`  
OpenAPI tag: `Recipes`

---

## 1. Danh sách Endpoints

| Method | Route | Quyền truy cập | Thành công | Lỗi chính | Mô tả |
|---|---|---|---|---|---|
| `GET` | `/api/v1/recipes` | Guest / Public | `200 OK` | `400 Bad Request` | Lấy danh sách công thức đã xuất bản (`Published`), hỗ trợ lọc đa tiêu chí, sắp xếp theo allowlist và phân trang chuẩn `data/meta`. |
| `GET` | `/api/v1/recipes/search` | Guest / Public | `200 OK` | `400 Bad Request` | Tìm kiếm toàn văn FTS tiếng Việt không dấu (`q >= 2`), xếp hạng tương đồng `ts_rank`, kết hợp bộ lọc và phân trang. |

---

## 2. Tham số Query Parameters & Ràng buộc Validation

### `GET /api/v1/recipes`
* `page`: int $\ge 1$ (mặc định `1`).
* `pageSize`: int từ `1` đến `50` (mặc định `12`).
* `sortBy`: string, bắt buộc thuộc allowlist: `createdAt`, `title`, `cookTimeMinutes`, `prepTimeMinutes` (mặc định `createdAt`).
* `sortOrder`: string, chỉ chấp nhận `asc` hoặc `desc` (mặc định `desc`).
* `categoryId`: UUID tùy chọn.
* `difficulty`: string tùy chọn, thuộc `Easy`, `Medium`, `Hard`, `Expert`.
* `maxCookTime`: int $\ge 0$ tùy chọn (lọc thời gian nấu tối đa bằng phút).
* `minServings`: int $> 0$ tùy chọn (lọc số lượng khẩu phần tối thiểu).

### `GET /api/v1/recipes/search`
* `q`: string, **bắt buộc**, độ dài tối thiểu $2$ ký tự, tối đa $200$ ký tự.
* Các tham số phân trang, sắp xếp và bộ lọc: Tương tự như `GET /api/v1/recipes`.

---

## 3. Schema DTO & Cấu trúc Phản hồi (`data/meta`)

```json
{
  "data": [
    {
      "id": "e0b8e7c1-8bb1-4c6e-821f-8292834bfa91",
      "title": "Phở Bò Gia Truyền Nam Định",
      "slug": "pho-bo-gia-truyen-nam-dinh",
      "description": "Hương vị phở truyền thống nước dùng trong vắt, đậm đà từ xương ống bò.",
      "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "categoryName": "Món Chính",
      "authorId": "user-tv1-uuid",
      "authorDisplayName": "Nguyễn Thanh Tâm",
      "prepTimeMinutes": 30,
      "cookTimeMinutes": 180,
      "servings": 4,
      "difficulty": "Medium",
      "status": "Published",
      "primaryImageUrl": "https://storage.culinaryblog.local/recipes/pho-bo-primary.webp",
      "publishedAt": "2026-09-14T02:00:00Z",
      "createdAt": "2026-09-13T10:00:00Z"
    }
  ],
  "meta": {
    "page": 1,
    "pageSize": 12,
    "total": 1,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

---

## 4. Ràng buộc Bảo mật & Toàn vẹn Dữ liệu
1. **Chỉ hiển thị công thức `Published`:** Endpoint công cộng tuyệt đối không bao giờ trả về các công thức ở trạng thái `Draft` hoặc `Archived`, kể cả khi tìm kiếm toàn văn.
2. **Kháng SQL / Script Injection:** Mọi câu lệnh truy vấn đều được parameterize qua EF Core LINQ và kiểm tra allowlist sắp xếp nghiêm ngặt.
3. **Tìm kiếm tiếng Việt không dấu:** Từ khóa tìm kiếm được chuẩn hóa qua `SlugHelper` và PostgreSQL `unaccent` để người dùng gõ `"pho bo"` vẫn tìm thấy chính xác món `"Phở Bò"`.
