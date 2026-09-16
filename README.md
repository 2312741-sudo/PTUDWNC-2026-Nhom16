# Culinary Blog — Nền tảng Chia sẻ & Khám phá Công thức Ẩm thực

> **Đồ án môn học**: Phát Triển Ứng Dụng Web Nâng Cao (PTUDWNC) — Học kỳ 1, Năm 2026  
> **Nhóm thực hiện**: Nhóm 16  
> **Kho lưu trữ GitHub**: [2312741-sudo/PTUDWNC-2026-Nhom16](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16)  
> **Nhóm trưởng & Reviewer toàn bộ**: Nguyễn Thanh Tâm (MSSV: 2312741)  

---

## 👥 1. Danh sách Thành viên & Phân công Trách nhiệm

Dự án được phân chia theo chiều dọc nghiệp vụ (mỗi thành viên phụ trách trọn vẹn từ Database, Backend CQRS/Minimal APIs, Frontend UI Next.js, Bảo mật, Kiểm thử cho đến Vận hành) theo [Kế hoạch phân chia 6 tuần](PHAN_CHIA_CONG_VIEC_6_TUAN.md):

| STT | Thành viên | MSSV | Vai trò | Phân hệ nghiệp vụ phụ trách | Task quy định |
|:---:|---|:---:|:---:|---|:---:|
| 1 | **Nguyễn Thanh Tâm** | **2312741** | **Nhóm trưởng (Leader)** | **Tài khoản, Hồ sơ, Nền tảng Xác thực (Auth), Bảo mật, Background Worker & CI/CD** | A1 – A7 |
| 2 | **Ngô Quốc Trường Vĩ** | **2312796** | Thành viên | **Danh mục ẩm thực, Tìm kiếm Full-Text Search tiếng Việt & Đăng nhập Google OAuth** | B1 – B7 |
| 3 | **Huỳnh Quốc Trung** | **2312786** | Thành viên | **Soạn thảo Công thức (Recipe Aggregate), Nguyên liệu/Các bước & Refresh Token Rotation** | C1 – C7 |
| 4 | **Nguyễn Hữu Trung Sơn** | **2312739** | Thành viên | **Hạ tầng Docker/Nginx, Storage MinIO, Xử lý ảnh/Resize, Xuất bản món ăn & Giám sát** | D1 – D7 |

---

## 📊 2. Đánh Giá Tiến Độ Hoàn Thành Của Nhóm

### 2.1. Đánh giá tổng quan theo các Cổng Hoàn Thành (Milestones)

- ✅ **Cổng G0 (Giữa Tuần 1 - Hạ tầng Dev & Skeletons)**: **ĐẠT 100%**. Đã khởi dựng thành công trọn bộ stack container hóa (PostgreSQL 16, Redis 7, MinIO, MailHog, Seq, Nginx) và khung mã nguồn Clean Architecture + Next.js App Router.
- ✅ **Cổng G1 (Cuối Tuần 1 - Tích hợp Đăng ký, Đăng nhập, Danh mục & Schema Recipe)**: **ĐẠT 100%**. Đã tích hợp thành công toàn bộ PR của 4 thành viên vào nhánh chính `main`.
- 🔄 **Cổng G2 (Tuần 2 - Hồ sơ người dùng, Soạn thảo công thức đa bước, Upload ảnh & Xuất bản)**: **ĐẠT TIẾN ĐỘ 60%**. TV1 đã hoàn thành xuất sắc 100% khối lượng Tuần 2; các thành viên TV2, TV3, TV4 đang hoàn tất các phần việc tiếp theo.

### 2.2. Bảng theo dõi tiến độ chi tiết từng thành viên (Cập nhật ngày 16/09/2026)

| Thành viên | Tiến độ Tuần 1 | Tiến độ Tuần 2 | Kỹ năng xác nhận | Trạng thái nghiệm thu |
|---|:---:|:---:|:---:|---|
| **TV1 — Nguyễn Thanh Tâm** *(Leader)* | **100%** (A1, A2, A5, CI) | **100%** (A2, A3, A4, A7) | **14 / 24** (K01, K02, K04, K05, K08, K10, K14, K15, K16, K17, K20, K21, K23, K24) | ✅ **Hoàn thành Tuần 1 & Tuần 2**. Đạt 50 tests tích hợp, build pass cả Backend & Frontend, có bằng chứng minh chứng đầy đủ. |
| **TV2 — Ngô Quốc Trường Vĩ** | **100%** (B1, B2 nền, B6) | **Đang thực hiện** (B2, B3, B4) | **9 / 24** (K01, K02, K03, K04, K06, K07, K08, K09, K16) | 🔄 **Hoàn thành Tuần 1**. Đã merge PR #5 (Category CRUD, UI Shell Next.js, Google Contract, phân trang D11). Đang làm Google Login PKCE & FTS. |
| **TV3 — Huỳnh Quốc Trung** | **100%** (C1, C6 nền) | **Đang thực hiện** (C2, C3, C5) | **7 / 24** (K01, K02, K03, K05, K06, K07, K21) | 🔄 **Hoàn thành Tuần 1**. Đã merge PR #3 (Recipe aggregate schema, Nutrition VO, Concurrency spike 4/4 pass). Đang làm Wizard soạn thảo & Refresh token. |
| **TV4 — Nguyễn Hữu Trung Sơn** | **100%** (D1, D3, D5, D6) | **Đang thực hiện** (D1, D2, D3) | **8 / 24** (K01, K05, K11, K12, K13, K20, K23, K24) | 🔄 **Hoàn thành Tuần 1**. Đã tích hợp Compose full stack, Nginx reverse proxy, Health check probes, Storage contract, Logout endpoint. Đang làm Media resize & Publish. |

---

## 🚀 3. Những Gì Nhóm Đã Hoàn Thành Thực Tế

### 3.1. Backend API & Kiến trúc hệ thống (.NET 10 Minimal APIs)

Hệ thống được thiết kế theo **Clean Architecture** kết hợp mô hình **CQRS** (MediatR), tuân thủ nghiêm ngặt nguyên tắc phân tầng và tiêu chuẩn RESTful:

1. **Phân hệ Xác thực & Phân quyền (TV1 - Nguyễn Thanh Tâm)**:
   - **Đăng ký tài khoản (`POST /api/v1/auth/register`)**: Chuẩn hóa theo SRS v1.1.1 §8.1 (`fullName`, `userName`, `email`, `password`), tự động cấp quyền `Author`, mã hóa mật khẩu theo tiêu chuẩn ASP.NET Core Identity (PBKDF2 100.000 iterations), kiểm tra trùng lặp email bất kể hoa thường, cấp ngay Access Token JWT (15 phút) và Refresh Token (7 ngày) kèm `expiresAt` (ISO 8601).
   - **Đăng nhập (`POST /api/v1/auth/login`)**: Xác thực tài khoản với `SignInManager`. Trả thông báo lỗi mờ chung khi sai thông tin để chống user enumeration. Toàn bộ response bọc chuẩn `{ "data": ... }`.
   - **Cơ chế Khóa tài khoản (Account Lockout)**: Tự động khóa tạm thời tài khoản 15 phút khi người dùng đăng nhập sai 5 lần liên tiếp. Trả về mã lỗi HTTP `423 Locked` kèm mã lỗi `auth.locked`.
   - **Giới hạn tần suất gọi API (Rate Limiting)**: Áp dụng thuật toán Fixed Window giới hạn **10 requests/phút/IP** trên các endpoint nhạy cảm (`register`, `login`). Khi vượt ngưỡng, hệ thống trả về HTTP `429 Too Many Requests` kèm header chuẩn `Retry-After: 60`.
   - **Quản lý Hồ sơ người dùng (`GET` & `PATCH /api/v1/auth/me`)**:
     - Xem thông tin cá nhân hiện tại của người dùng đang đăng nhập qua Bearer token: bao gồm `id`, `fullName`, `email`, `userName`, `avatarUrl`, `roles`, `emailConfirmed`, `createdAt`.
     - Cho phép cập nhật có chọn lọc (`fullName`, `avatarUrl`, `bio`).
     - Áp dụng FluentValidation nghiêm ngặt: chặn ký tự điều khiển, chặn injection thẻ HTML (`<`, `>`), xác thực định dạng URL ảnh đại diện (`http://` hoặc `https://`).
     - **Bảo mật tuyệt đối**: Ngăn chặn hoàn toàn việc can thiệp thay đổi `email` hoặc tự nâng cấp `roles` qua API hồ sơ.
   - **Đăng xuất an toàn (`POST /api/v1/auth/logout`)**: Yêu cầu Bearer Token hợp lệ, thu hồi Refresh Token và trả về `204 NoContent`.
   - **Xử lý Tác vụ nền (Background Worker - Welcome Email)**:
     - Sử dụng `System.Threading.Channels` (`UnboundedChannel`) để đẩy tác vụ gửi email chào mừng vào hàng đợi phi đồng bộ ngay sau khi giao dịch cơ sở dữ liệu commit thành công, không làm chậm response của người dùng.
     - Tự động mã hóa tên hiển thị qua `WebUtility.HtmlEncode` để phòng chống tấn công HTML Injection qua email.
     - Tích hợp chính sách thử lại (Retry Policy) 4 lần phân tầng (`0s`, `1 phút`, `5 phút`, `30 phút`).
     - Tuyệt đối không log thông tin nhạy cảm (secrets, tokens, passwords).

2. **Phân hệ Danh mục Ẩm thực (TV2 - Ngô Quốc Trường Vĩ)**:
   - Thực thể `Category` với định danh GUID, hỗ trợ thứ tự sắp xếp (`OrderIndex`), ràng buộc `Name` duy nhất (UNIQUE) và `Slug` duy nhất (C09).
   - Bộ chuyển đổi `SlugHelper` chuẩn hóa tiếng Việt có dấu thành URL slug thân thiện tự động, tự động thêm suffix số (e.g., `-2`, `-3`) nếu có va chạm slug.
   - Cơ chế xóa danh mục: **Hard Delete** xóa entity khỏi DB (C07), áp dụng ràng buộc bảo vệ toàn vẹn: chặn xóa và trả HTTP `409 Conflict` nếu danh mục còn bất kỳ công thức nào.
   - Bộ nhớ đệm danh mục: `IMemoryCache` với thời gian sống TTL **60 phút** (C03).
   - Hệ thống Endpoint CRUD `/api/v1/categories` được bảo vệ bằng chính sách phân quyền `AdminPolicy`, toàn bộ response bọc chuẩn `{ "data": ... }` (C08).
   - Đặc tả phân trang chuẩn với cấu trúc kết quả `PagedResult<T>` và kích thước trang mặc định `pageSize = 12` (C04).

3. **Phân hệ Recipe Aggregate & Kiểm thử Tương tranh (TV3 - Huỳnh Quốc Trung)**:
   - Mô hình hóa Domain Recipe Aggregate gồm: `Recipe`, Value Object `Nutrition`, `RecipeIngredient` (chuẩn hóa thuộc tính `OrderIndex` theo C06), `RecipeStep` (chuẩn hóa thuộc tính `TimerMinutes` theo C05), `RecipeImage`.
   - Chiến lược xóa công thức: **Soft Delete** (`IsDeleted = true`), kết hợp Global Query Filter và giữ nguyên các file ảnh trên MinIO (C01).
   - Điều kiện xuất bản công thức (Publish): Bắt buộc phải có **ít nhất 1 nguyên liệu VÀ ít nhất 1 bước thực hiện** (C02); nếu thiếu dữ liệu trả về HTTP `422 Unprocessable Entity` với mã lỗi `RECIPE_PUBLISH_INCOMPLETE`.
   - Cơ chế kiểm soát tương tranh lạc quan (Optimistic Concurrency Control) dựa trên cột `RowVersion` (PostgreSQL `xmin`), ngăn chặn hoàn toàn lỗi mất cập nhật (Lost Update) khi 2 tác giả chỉnh sửa cùng lúc.
   - Kiểm thử cô lập Concurrency Spike chứng minh khả năng rollback toàn phần của giao dịch khi có lỗi ở bảng con và đảm bảo bất biến khi xuất bản công thức.

4. **Hạ tầng Vận hành, Giám sát & Lưu trữ (TV4 - Nguyễn Hữu Trung Sơn)**:
   - Hệ thống kiểm tra sức khỏe hệ thống (Health Checks):
     - `/health`: Báo cáo chi tiết trạng thái của toàn bộ phụ thuộc (PostgreSQL, Redis, MinIO).
     - `/health/live`: Liveness probe phục vụ container orchestrator kiểm tra tiến trình đang chạy.
     - `/health/ready`: Readiness probe kiểm tra kết nối cơ sở dữ liệu và mạng trước khi tiếp nhận traffic.
   - Triển khai không phụ thuộc thư viện ngoài: Sử dụng TCP Socket Probe thuần giúp giữ nguyên `packages.lock.json` của CI.
   - Hợp đồng lưu trữ file `IFileStorageService` (Stream-based) và cấu hình `MinioOptions` sẵn sàng cho việc tích hợp bucket S3.
   - Reverse proxy Nginx (`nginx/nginx.dev.conf`) điều hướng thông suốt giữa frontend và backend API.

---

### 3.5. Đồng bộ Chuẩn hóa Toàn Diện theo SRS v1.1.1 (Giải quyết 9 Mâu thuẫn C01–C09)

Dự án đã giải quyết triệt để 9 mâu thuẫn nội tại được phát hiện trong tài liệu gốc theo **SRS v1.1.1** (tham chiếu [Báo cáo Mâu thuẫn](docs/root/SRS_Contradictions_Report.md)):

| Mã | Vấn đề mâu thuẫn ban đầu | Quyết định chuẩn hóa SRS v1.1.1 | Hiện thực trong Source Code & Tests |
|:---:|---|---|---|
| **C01** | Xóa Recipe: Hard Delete vs Soft Delete | **Soft Delete** (`IsDeleted = true`). Giữ file MinIO. | BaseEntity Global Query Filter, Recipe soft delete |
| **C02** | Điều kiện publish: chỉ Steps vs Steps + Ingredients | **≥ 1 Ingredient VÀ ≥ 1 Step**. Thiếu trả HTTP 422. | Domain validation `RECIPE_PUBLISH_INCOMPLETE` |
| **C03** | Category cache TTL: 60 phút vs 30 phút | **60 phút** cho IMemoryCache danh mục. | `CacheService` / In-memory TTL = 60 mins |
| **C04** | Default page size: 12 vs 10 | **pageSize = 12** mặc định cho tất cả endpoints. | Query pagination DTOs, Default PageSize = 12 |
| **C05** | Tên trường bước thực hiện: TimerMinutes vs DurationMinutes | Chuẩn hóa **`TimerMinutes`**. | `RecipeStep.TimerMinutes` (Entity, DTO, DB) |
| **C06** | Tên trường nguyên liệu: OrderIndex vs SortOrder | Chuẩn hóa **`OrderIndex`**. | `RecipeIngredient.OrderIndex` (Entity, DTO, DB) |
| **C07** | Xóa Category: Hard Delete vs Soft Delete | **Hard Delete** (xóa khỏi DB, chặn 409 nếu có món). | `CategoryRepository.DeleteAsync` xóa entity |
| **C08** | Response wrapper: có nơi thiếu `data` | **Toàn bộ response thành công wrap trong `{ data }`**. | API endpoints & Frontend `api.ts` tự unwrap |
| **C09** | Category uniqueness: Name UNIQUE vs Slug suffix | **`Name` UNIQUE trong DB**; Slug suffix nếu va chạm. | PostgreSQL Index UNIQUE Name & Slug generator |
| **§8.1** | Auth API format không khớp FR chi tiết | Bổ sung `fullName`, `userName`, `emailConfirmed`, `createdAt`, `expiresAt`. | DTOs, Handlers, Database mapping, Frontend types |


### 3.2. Giao diện Người dùng (Frontend Next.js 15 App Router & Tailwind CSS)

1. **Khung ứng dụng & Trang công khai**:
   - Sử dụng **Next.js 15 App Router**, React 19, TypeScript và **Tailwind CSS**.
   - **Header & Navigation**: Điều hướng responsive thông minh, hỗ trợ thanh tìm kiếm nhanh, menu mobile drawer, liên kết phân hệ quản trị và xác thực.
   - **Trang chủ (`/`) & Danh mục (`/categories`)**: Trình bày danh sách phân loại món ăn bắt mắt, card hiển thị hình ảnh, tên và số lượng công thức.

2. **Trang Quản trị Danh mục (`/dashboard/categories`)**:
   - Dành riêng cho Admin quản lý, tạo mới, chỉnh sửa và xóa danh mục trực quan.

3. **Trang Quản lý Hồ sơ Cá nhân (`/dashboard/profile`)** *(Mới hoàn thành ở Tuần 2 bởi TV1)*:
   - Biểu mẫu trực quan tích hợp **React Hook Form** và **Zod Validation**.
   - Kiểm tra dữ liệu tức thì (Real-time Inline Validation): cảnh báo nếu để trống tên, nhập quá 100 ký tự, chứa mã script độc hại, hoặc link ảnh avatar không hợp lệ.
   - Khóa cố định các trường bảo mật: hiển thị `Email` và `Vai trò (Roles)` dưới dạng badge bảo vệ kèm thông báo hướng dẫn người dùng.
   - Xem trước trực tiếp ảnh đại diện (Avatar Preview), hỗ trợ ảnh fallback khi URL lỗi.
   - Phản hồi trạng thái lưu mượt mà qua thông báo trạng thái (Toast Alert).

---

### 3.3. Môi trường Container hóa Đầy Đủ (Docker Dev Stack)

Toàn bộ dịch vụ phụ trợ được cấu hình tập trung trong file [`docker-compose.dev.yml`](docker-compose.dev.yml):

| Dịch vụ | Image Container | Cổng Host | Vai trò trong hệ thống |
|---|---|:---:|---|
| **PostgreSQL 16** | `postgres:16-alpine` | `5432` | Cơ sở dữ liệu quan hệ chính & test DB |
| **Redis 7** | `redis:7-alpine` | `6379` | Cache-aside, Rate Limiting & Blacklist |
| **MinIO Object Storage** | `minio/minio` | `9000` (API) / `9001` (Console) | Lưu trữ ảnh món ăn và avatar người dùng |
| **MailHog** | `mailhog/mailhog` | `1025` (SMTP) / `8025` (Web UI) | Máy chủ thử nghiệm gửi email chào mừng và thông báo |
| **Seq** | `datalust/seq:latest` | `5341` | Máy chủ thu thập log tập trung có cấu trúc (Structured Logging) |
| **Nginx** | `nginx:alpine` | `80` | Reverse proxy môi trường dev |

---

### 3.4. Chất lượng Mã nguồn & Báo cáo Kiểm thử Tự động (Testing Suite)

Dự án duy trì bộ kiểm thử tự động toàn diện đạt tỷ lệ vượt qua **100% (54 / 54 tests pass)**:

```text
Test run for ConcurrencySpike.dll (net10.0)
Passed!  - Failed: 0, Passed:  4, Skipped: 0, Total:  4, Duration: 274 ms

Test run for CulinaryBlog.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 50, Skipped: 0, Total: 50, Duration: 2 s
```

- **Kiểm định Kiến trúc (18 Architecture Tests)**: Bảo vệ ranh giới Clean Architecture, kiểm thử toàn bộ trường hợp biên của validator (XSS, ký tự điều khiển, độ dài chuỗi, URL scheme).
- **Kiểm thử Tích hợp Auth & Security (18 Auth Tests)**: Kiểm tra luồng đăng ký/đăng nhập, ngăn chặn đăng ký email trùng, chặn client tự cấp role Admin, kiểm tra khóa tài khoản HTTP 423, cập nhật hồ sơ HTTP 200/400/401, và đăng xuất HTTP 204.
- **Kiểm thử Phân hệ Danh mục (12 Category Tests)**: Kiểm tra trọn vẹn nghiệp vụ Domain, thuật toán sinh slug tiếng Việt, CRUD CQRS Handlers, và phân trang.
- **Kiểm thử Hạ tầng & Giám sát (2 Health Tests)**: Xác minh hoạt động của liveness và readiness probes.
- **Kiểm thử Concurrency Spike (4 Tests)**: Đảm bảo kiểm soát xung đột dữ liệu đồng thời và tính toàn vẹn của transaction.

---

## 💻 4. Hướng Dẫn Cài Đặt & Chạy Ứng Dụng

### 4.1. Yêu cầu môi trường
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+ LTS](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/)
- Hệ quản trị PostgreSQL 16 (có thể dùng qua Docker)

### 4.2. Khởi động hạ tầng Docker
```bash
# Khởi động toàn bộ 6 dịch vụ phụ trợ
docker compose -f docker-compose.dev.yml up -d

# Kiểm tra trạng thái các container
docker compose -f docker-compose.dev.yml ps
```

### 4.3. Khởi động Backend API (.NET 10)
```bash
# 1. Khôi phục dependencies theo locked-mode
dotnet restore CulinaryBlog.sln --locked-mode

# 2. Thiết lập biến môi trường kết nối
export ConnectionStrings__Database="Host=localhost;Port=5432;Database=culinary_blog;Username=culinary;Password=culinary_dev_secret"
export Jwt__SigningKey="super_secret_jwt_signing_key_for_culinary_blog_min_64_bytes_long_string_12345"
export ASPNETCORE_ENVIRONMENT=Development

# 3. Áp dụng migration cơ sở dữ liệu
dotnet run --project src/backend/CulinaryBlog.API -- --migrate

# 4. Chạy Backend API server
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```
> 📖 Truy cập tài liệu API trực quan tại: **http://localhost:5080/scalar/v1**

### 4.4. Khởi động Frontend (Next.js 15)
```bash
cd src/frontend

# Cài đặt thư viện phụ thuộc
npm install

# Chạy server phát triển
npm run dev
```
> 🌐 Mở trình duyệt truy cập:
> - Trang chủ ứng dụng: **http://localhost:3000**
> - Trang Quản lý Hồ sơ (Task A3): **http://localhost:3000/dashboard/profile**
> - Trang Quản trị Danh mục (Task B1): **http://localhost:3000/dashboard/categories**

### 4.5. Chạy bộ kiểm thử tự động (Automated Tests)
```bash
# Thiết lập chuỗi kết nối database test chuyên biệt
export TEST_DATABASE="Host=localhost;Port=5432;Database=culinary_test;Username=culinary;Password=culinary_dev_secret"

# Chạy toàn bộ 54 tests trong solution
dotnet test CulinaryBlog.sln --logger "console;verbosity=normal"
```

---

## 🧭 5. Kế Hoạch & Trọng Tâm Tiếp Theo (Tuần 3)

1. **TV2 (Trường Vĩ)**:
   - Tích hợp hoàn chỉnh luồng Đăng nhập Google OAuth (Code Flow + PKCE với Auth.js v5).
   - Thiết lập Full-Text Search (FTS) tiếng Việt không dấu với PostgreSQL `tsvector`, từ điển unaccent và chỉ mục GIN index.
2. **TV3 (Quốc Trung)**:
   - Chuyển giao mô hình Recipe Aggregate vào `AuthDbContext` chính của backend API.
   - Xây dựng giao diện Wizard soạn thảo công thức (Next.js RHF) với các bước và nguyên liệu động.
   - Triển khai cơ chế Refresh Token Rotation và Family Revocation.
3. **TV4 (Trung Sơn)**:
   - Triển khai cụ thể dịch vụ `MinIOStorageService` xử lý upload ảnh trực tiếp lên S3 bucket.
   - Tích hợp tính năng tự động resize ảnh công thức thành các kích thước chuẩn (300×300 và 800×600).
   - Hoàn thiện nghiệp vụ Chuyển trạng thái xuất bản (Publish / Unpublish / Archive Recipe).
4. **TV1 (Thanh Tâm - Leader)**:
   - Điều phối tích hợp toàn diện Cổng G2.
   - Bổ sung Log correlation với Serilog & Tracing, hỗ trợ các thành viên giải quyết xung đột mã nguồn.

---

## 📚 6. Danh Mục Tài Liệu Kỹ Thuật Tham Chiếu

- 📋 [Kế hoạch phân chia công việc 6 tuần](PHAN_CHIA_CONG_VIEC_6_TUAN.md)
- 📖 [Kế hoạch tổng thể & Giải quyết xung đột SRS](KE_HOACH_DU_AN.md)
- 📄 [Tài liệu Đặc tả Yêu cầu Phần mềm chính thức (SRS v1.1.1)](docs/root/SRS_Culinary_Blog_v1.1.1.md)
- 📑 [Báo cáo Mâu thuẫn Nội tại SRS (C01–C09)](docs/root/SRS_Contradictions_Report.md)
- 📄 [Tài liệu Đặc tả Yêu cầu Phần mềm (Bản gốc v1.0.0)](docs/root/SRS_Culinary_Blog_v1.0.0.md)
- 🔐 [Hợp đồng API Xác thực (Auth Contract)](docs/AUTH_CONTRACT.md)
- 🗂️ [Hợp đồng API Danh mục (Category Contract)](docs/CATEGORY_CONTRACT.md)
- 🍲 [Hợp đồng Danh sách Công thức (Recipe List Contract)](docs/RECIPE_LIST_CONTRACT.md)
- 🌐 [Hợp đồng Google OAuth (Google Auth Contract)](docs/GOOGLE_AUTH_CONTRACT.md)
- 📑 **Báo cáo nghiệm thu cá nhân**:
  - [Báo cáo Tuần 1 & Tuần 2 — TV1 (Nguyễn Thanh Tâm)](docs/evidence/TV1/TUAN_2.md)
  - [Báo cáo Tuần 1 — TV2 (Ngô Quốc Trường Vĩ)](docs/evidence/TV2/TUAN_1.md)
  - [Báo cáo Tuần 1 — TV3 (Huỳnh Quốc Trung)](docs/evidence/TV3/C1-HOAN-THIEN.md)
  - [Báo cáo Tuần 1 — TV4 (Nguyễn Hữu Trung Sơn)](docs/evidence/TV4/TRANG_THAI_THUC_HIEN.md)
