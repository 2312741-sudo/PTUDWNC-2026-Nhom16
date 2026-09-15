# Recipe List & Pagination Contract (D11) — TV2 (Ngô Quốc Trường Vĩ)

Trạng thái: **Đã thống nhất theo quyết định D11 & D29**  
Chủ trì: **Ngô Quốc Trường Vĩ (TV2)**  
Phối hợp: **Huỳnh Quốc Trung (TV3 — Recipe Aggregate) & Nguyễn Thanh Tâm (TV1 — Leader)**  
Áp dụng: Toàn bộ danh sách trả về có phân trang (`/api/v1/recipes`, `/api/v1/recipes/search`, `/api/v1/categories/{slug}/recipes`)

---

## 1. Cấu trúc Response chuẩn (`data/meta`)

Tuân theo quyết định **D11** trong kế hoạch tổng thể, loại bỏ định dạng cũ `items/totalCount` để dùng thống nhất một schema duy nhất:

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
    "total": 48,
    "totalPages": 4,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

## 2. Các tham số Query Parameters

| Tham số | Kiểu dữ liệu | Mặc định | Giới hạn / Giá trị hợp lệ | Ghi chú |
|---|---|---|---|---|
| `page` | `integer` | `1` | $\ge 1$ | Số trang hiện tại |
| `pageSize` | `integer` | `12` | Từ $1$ đến $50$ | Số bản ghi trên mỗi trang (theo D11) |
| `sortBy` | `string` | `createdAt` | Allowlist: `createdAt`, `title`, `cookTimeMinutes`, `views` | Chống SQL injection |
| `sortOrder` | `string` | `desc` | `asc` hoặc `desc` | Thứ tự sắp xếp |
| `categoryId` | `uuid` | `null` | UUID hợp lệ | Lọc theo danh mục |
| `difficulty` | `string` | `null` | `Easy`, `Medium`, `Hard`, `Expert` (theo D15) | Độ khó của món ăn |
| `maxCookTime` | `integer` | `null` | $\ge 0$ | Lọc thời gian nấu tối đa (phút) |
| `minServings` | `integer` | `null` | $> 0$ | Lọc số khẩu phần tối thiểu |

*Lưu ý kết hợp bộ lọc:* Tất cả các điều kiện lọc được kết hợp bằng phép logic **`AND`** (FR-SRCH-002).

---

## 3. Quy tắc Render & Cache (Public vs Private)
* **Public (`/recipes`, `/categories/[slug]`):** Chỉ trả về các bản ghi có `Status == "Published"` và `IsDeleted == false`. Được cache Redis và ISR.
* **Dashboard cá nhân (`/dashboard/recipes`):** Tải động (CSR / `no-store`), hiển thị theo phạm vi quyền: Author chỉ xem món của chính mình, Admin xem tất cả các trạng thái (`Draft`, `Published`, `Archived`). Tuyệt đối không cache lẫn dữ liệu draft vào cache public (D10, D12, D13).
