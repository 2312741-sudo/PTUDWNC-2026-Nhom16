# Kết quả kiểm thử thực tế

Thời điểm: 2026-09-09T18:42:16.074827+07:00

API: http://localhost:5081; database PostgreSQL thật `ptudwnc_chuong01`.

**27 nhóm kiểm tra đạt.**

- PASS: Scalar trả HTML và tham chiếu OpenAPI
- PASS: OpenAPI có đủ 11 thao tác
- PASS: 11 thao tác có summary, description, response metadata
- PASS: Category.Create tạo slug tiếng Việt đúng
- PASS: Trùng slug trả 409
- PASS: Tên thiếu/rỗng/không tạo được slug trả 400
- PASS: Category search và pagination metadata
- PASS: Category.Update giữ CreatedAt và PUT lặp không đổi trạng thái
- PASS: Update trùng slug trả 409
- PASS: Update lỗi không lưu thay đổi
- PASS: POST Recipe A: 201, Location, Category DTO
- PASS: POST Recipe B: 201, Location, Category DTO
- PASS: POST Recipe C: 201, Location, Category DTO
- PASS: POST Recipe D: 201, Location, Category DTO
- PASS: GetRecipeById có Instructions và Category
- PASS: Kết hợp category/difficulty/time/search/sort/pagination
- PASS: Danh sách dùng RecipeDto gọn
- PASS: Trang 2 và hai biên thời gian inclusive
- PASS: Nested resource ưu tiên ID từ path
- PASS: Recipe.Update chuyển Category và PUT idempotent
- PASS: Validation Recipe: khẩu phần/thời gian/title/instructions/author/enum
- PASS: Recipe với Category không tồn tại trả 404
- PASS: Query không hợp lệ trả ProblemDetails 400
- PASS: GET/PUT/DELETE và nested ID không tồn tại trả 404
- PASS: Không xóa Category còn Recipe
- PASS: Trang vượt giới hạn trả items rỗng, totalCount thật
- PASS: DELETE 204, kiểm tra đã xóa và DELETE lặp 404

Dữ liệu kiểm thử đã được dọn sau khi chạy. Scalar kiểm tra ở mức HTTP HTML và OpenAPI; chưa tự động thao tác giao diện trình duyệt.

## Build và database

- `dotnet build --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1`: thành công, 0 cảnh báo, 0 lỗi.
- Migration `20260909114037_InitialCreate` đã được tạo bằng EF Core và áp dụng vào PostgreSQL `ptudwnc_chuong01`.
- .NET SDK 10.0.401; PostgreSQL 18.6; pgAdmin 4 có sẵn.
- EF CLI 10.0.3 có thông báo thấp hơn EF runtime 10.0.4 do dependency của Npgsql; tạo/apply migration vẫn thành công.
- Microsoft.OpenApi được ghim 2.7.5 để dùng bản vá cho [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).

## Đối chiếu đề bài

| Bài | Yêu cầu | Tệp / bằng chứng |
|---|---|---|
| 1 | Môi trường .NET/PostgreSQL/pgAdmin | Các công cụ đã có sẵn, PostgreSQL kết nối thành công |
| 1 | Bốn tầng, packages, Category.Create/Update | CulinaryBlog.slnx, src/, build thành công |
| 1 | Scalar /scalar/v1 | HTTP 200, HTML tham chiếu OpenAPI |
| 2 | Recipe đủ thuộc tính và enum | Domain/Entities/Recipe.cs, Domain/Enums/Difficulty.cs |
| 2 | CQRS, filtering/sorting/pagination, chi tiết chứa Category | Application/Features/Recipes và các kiểm tra đạt |
| 2 | Configuration và migration PostgreSQL | Infrastructure/Persistence/Configurations, Migrations |
| 3 | CRUD Category/Recipe và metadata | 11 thao tác được kiểm tra trong OpenAPI |
| 3 | Nested GET và lọc thời gian/độ khó | Kiểm tra HTTP kết hợp lọc và hai biên thời gian |
| 3 | HTTP file kiểm thử | CulinaryBlog.http, tests/integration_test.py |
