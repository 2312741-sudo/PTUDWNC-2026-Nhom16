# Thiết kế API v1

## Resource và thao tác

| Method | URI | Thành công | Lỗi nghiệp vụ |
|---|---|---|---|
| GET | /api/v1/categories | 200, phân trang CategoryDto | 400 query sai |
| GET | /api/v1/categories/{id} | 200 CategoryDto | 404 |
| POST | /api/v1/categories | 201 CategoryDto + Location | 400, 409 slug trùng |
| PUT | /api/v1/categories/{id} | 200 CategoryDto | 400, 404, 409 |
| DELETE | /api/v1/categories/{id} | 204 | 404, 409 còn recipe |
| GET | /api/v1/recipes | 200, phân trang RecipeDto | 400 query sai |
| GET | /api/v1/recipes/{id} | 200 RecipeDetailDto có Category | 404 |
| POST | /api/v1/recipes | 201 RecipeDetailDto + Location | 400, 404 category, 409 xung đột FK |
| PUT | /api/v1/recipes/{id} | 200 RecipeDetailDto | 400, 404, 409 |
| DELETE | /api/v1/recipes/{id} | 204 | 404 |
| GET | /api/v1/categories/{id}/recipes | 200, phân trang RecipeDto | 400, 404 category |

URI dùng danh từ số nhiều, chữ thường và version trong path. ID là UUID. PUT gửi toàn bộ trường được phép sửa, không nhận ID trong body; CreatedAt được giữ nguyên. Khi dữ liệu không đổi, UpdatedAt cũng giữ nguyên. DELETE lần hai trả 404 nhưng trạng thái cuối vẫn là đã xóa (idempotent).

## Request

Category:

```json
{"name":"Món Chính","description":"Các món ăn chính"}
```

Recipe:

```json
{
  "title":"Cơm chiên trứng",
  "description":"Bữa ăn nhanh",
  "instructions":"Đánh trứng, đảo cơm, nêm và phục vụ.",
  "prepTimeMinutes":10,
  "cookTimeMinutes":15,
  "servings":2,
  "difficulty":"Easy",
  "categoryId":"<UUID danh mục đã tạo>",
  "authorId":"11111111-1111-1111-1111-111111111111"
}
```

Name/Title: 1–200 ký tự sau trim. Description Category tối đa 2000, Recipe 4000 ký tự. Instructions: 1–20000 ký tự. Thời gian >= 0, Servings > 0. Difficulty: Easy/Medium/Hard. CategoryId phải tồn tại; AuthorId không rỗng. Slug tự sinh, xử lý dấu tiếng Việt và đ; unique index bảo vệ cả trường hợp request đồng thời. FK Restrict tránh xóa danh mục còn công thức.

## Query

- Chung: `page=1`, `pageSize=10` (1–100), `search`, `sortBy`, `descending=false`.
- Category sortBy: `name` (mặc định), `createdat`.
- Recipe sortBy: `createdat` (mặc định), `title`, `cooktimeminutes`, `preptimeminutes`, `difficulty`.
- Recipe filters: `categoryId`, `difficulty`, `minCookTime`, `maxCookTime` (phút, hai đầu bao gồm).
- Search tìm chuỗi con trong tên/tiêu đề hoặc mô tả, không phân biệt hoa thường; vẫn phân biệt dấu.
- Các bộ lọc kết hợp bằng AND. TotalCount tính sau lọc, trước phân trang. Sort thêm Id để ổn định khi giá trị chính trùng nhau.
- Trang vượt tổng trang trả items rỗng và metadata thật. Query sai trả 400; không âm thầm đổi tham số.
- Nested endpoint lấy CategoryId từ URL, ưu tiên hơn CategoryId trong query; danh mục tồn tại nhưng không có công thức trả danh sách rỗng.

Ví dụ: `/api/v1/recipes?difficulty=Easy&minCookTime=10&maxCookTime=30&sortBy=cooktimeminutes&descending=true&page=1&pageSize=5`.

## Response phân trang

```json
{"items":[],"totalCount":0,"page":1,"pageSize":10,"totalPages":0,"hasNextPage":false,"hasPreviousPage":false}
```

Lỗi dùng ProblemDetails với Content-Type application/problem+json và các trường type/title/status/detail/instance. Chi tiết lỗi bất ngờ được ghi log, không trả stack trace cho client. Các response body trả DTO, không trả entity EF trực tiếp.
