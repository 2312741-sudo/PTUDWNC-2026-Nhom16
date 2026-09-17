# HƯỚNG DẪN KIỂM THỬ ỨNG DỤNG CULINARY BLOG (TESTING GUIDE)

> **Dự án**: Culinary Blog — Phát Triển Ứng Dụng Web Nâng Cao  
> **Nhóm thực hiện**: Nhóm 16  
> **Phiên bản tài liệu**: 1.1.1 (Đồng bộ theo SRS v1.1.1 & chuẩn hóa 9 mâu thuẫn C01–C09)  
> **Bộ kiểm thử tự động**: 54 / 54 tests pass 100%

---

## MỤC LỤC

1. [Tổng quan Chiến lược Kiểm thử](#1-tổng-quan-chiến-lược-kiểm-thử)
2. [Kiểm thử Tự động Backend (.NET 10 xUnit)](#2-kiểm-thử-tự-động-backend-net-10-xunit)
3. [Kiểm tra Chuẩn Quy trình CI (GitHub Actions)](#3-kiểm-tra-chuẩn-quy-trình-ci-github-actions)
4. [Kiểm thử Giao diện Frontend (Next.js 15 App Router)](#4-kiểm-thử-giao-diện-frontend-nextjs-15-app-router)
5. [Kiểm thử Tích hợp Môi trường Docker Dev Stack](#5-kiểm-thử-tích-hợp-môi-trường-docker-dev-stack)
6. [Kịch bản Kiểm thử API Chi tiết (cURL & Scalar UI)](#6-kịch-bản-kiểm-thử-api-chi-tiết-curl--scalar-ui)
7. [Xử lý Sự cố & Câu hỏi Thường gặp (Troubleshooting)](#7-xử-lý-sự-cố--câu-hỏi-thường-gặp-troubleshooting)

---

## 1. Tổng quan Chiến lược Kiểm thử

Dự án áp dụng mô hình Kim tự tháp kiểm thử (Testing Pyramid) với nhiều lớp bảo vệ toàn diện:

```
          / \
         /   \       E2E Tests (Playwright / Web flows)
        /-----\
       /       \     Integration API Tests (WebApplicationFactory + PostgreSQL)
      /---------\
     /           \   Architecture Tests & Spike Tests (ArchUnit, Concurrency)
    /-------------\
   /               \ Unit Tests (FluentValidation, Domain Entities, SlugHelper)
  /-----------------\
```

- **Architecture Tests (18 tests)**: Đảm bảo ranh giới Clean Architecture, kiểm tra nghiêm ngặt các validator (chặn XSS, ký tự điều khiển, giới hạn độ dài chuỗi, URL an toàn).
- **Auth & Security Tests (18 tests)**: Kiểm tra trọn vẹn luồng đăng ký, đăng nhập, kiểm tra hash PBKDF2, tự động gán role Author, bảo vệ chống Account Lockout (5 lần sai), Rate Limiting (10 req/phút/IP), cập nhật hồ sơ không thể leo quyền, và đăng xuất thu hồi token.
- **Category Module Tests (12 tests)**: Kiểm tra Domain Category, thuật toán sinh slug tiếng Việt tự động, CRUD Handlers, Hard Delete (C07), và bọc response trong `{ data }` (C08).
- **Health Probes Tests (2 tests)**: Xác minh liveness và readiness probes.
- **Concurrency Spike Tests (4 tests)**: Kiểm thử cô lập 2 writer đồng thời, kiểm soát xung đột qua `RowVersion` và rollback transaction toàn phần.

---

## 2. Kiểm thử Tự động Backend (.NET 10 xUnit)

### 2.1. Yêu cầu Tiên quyết
- .NET 10 SDK đã cài đặt (`dotnet --version` hiển thị `10.0.x`).
- PostgreSQL 16 đang hoạt động (có thể dùng qua Docker Compose hoặc Postgres local).

### 2.2. Thiết lập Biến Môi trường Test Database
```bash
# Thiết lập connection string tới database kiểm thử
export TEST_DATABASE="Host=localhost;Port=5432;Database=culinary_test;Username=culinary;Password=culinary_dev_secret"

# Nếu chạy PostgreSQL trên cổng khác hoặc tài khoản test riêng:
# export TEST_DATABASE="Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=postgres"
```

### 2.3. Chạy Toàn bộ 54 Tests
```bash
# Chạy toàn bộ 54 test cases trong solution
dotnet test CulinaryBlog.sln --logger "console;verbosity=normal"
```

**Kết quả mong đợi**:
```text
Passed!  - Failed: 0, Passed:  4, Skipped: 0, Total:  4, Duration: 255 ms - ConcurrencySpike.dll (net10.0)
Passed!  - Failed: 0, Passed: 50, Skipped: 0, Total: 50, Duration: 2 s - CulinaryBlog.Tests.dll (net10.0)
```

### 2.4. Chạy Từng Nhóm Test Chuyên Biệt

1. **Chỉ chạy nhóm kiểm thử Xác thực & Bảo mật (Auth & Security)**:
   ```bash
   dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~AuthTests"
   ```

2. **Chỉ chạy kiểm thử Khóa tài khoản (Account Lockout)**:
   ```bash
   dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~Lockout"
   ```

3. **Chỉ chạy nhóm kiểm thử Danh mục (Categories)**:
   ```bash
   dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~CategoryTests"
   ```

4. **Chỉ chạy kiểm thử Kiến trúc & Validation (Architecture & Validators)**:
   ```bash
   dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests"
   ```

5. **Chỉ chạy kiểm thử Tương tranh Concurrency Spike**:
   ```bash
   dotnet test tests/concurrency-spike/ConcurrencySpike.csproj
   ```

---

## 3. Kiểm tra Chuẩn Quy trình CI (GitHub Actions)

Quy trình CI trên GitHub (`.github/workflows/backend.yml`) yêu cầu nghiêm ngặt 4 bước liên hoàn. Bạn cần chạy kiểm tra các bước này tại máy local trước khi commit:

```bash
# Bước 1: Khôi phục gói phụ thuộc với locked-mode (chống trôi phiên bản)
dotnet restore CulinaryBlog.sln --locked-mode

# Bước 2: Biên dịch toàn bộ dự án ở chế độ Release
dotnet build CulinaryBlog.sln --no-restore --configuration Release

# Bước 3: Kiểm tra định dạng code & chuẩn khoảng trắng (tuyệt đối không có lỗi format)
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore

# Bước 4: Chạy kiểm thử tự động Release & thu thập Coverage
dotnet test CulinaryBlog.sln --no-build --configuration Release --collect:"XPlat Code Coverage"
```

> 💡 **Mẹo**: Nếu Bước 3 báo lỗi whitespace hoặc code formatting, bạn chỉ cần chạy lệnh sau để hệ thống tự căn chỉnh tự động:
> ```bash
> dotnet format CulinaryBlog.sln
> ```

---

## 4. Kiểm thử Giao diện Frontend (Next.js 15 App Router)

### 4.1. Kiểm tra Typecheck & Build Tĩnh
```bash
cd src/frontend

# Cài đặt thư viện phụ thuộc
npm install

# Kiểm tra kiểu TypeScript và build tối ưu hóa
npm run build
```
**Kết quả mong đợi**: Biên dịch thành công 100%, 7/7 routes được tạo (`/`, `/_not-found`, `/categories`, `/categories/[slug]`, `/dashboard/categories`, `/dashboard/profile`).

### 4.2. Khởi chạy Server Frontend
```bash
npm run dev
```
Mở trình duyệt truy cập: **http://localhost:3000**

### 4.3. Các Màn hình & Kịch bản Kiểm thử UI

1. **Trang Chủ (`http://localhost:3000`)**:
   - Kiểm tra Header responsive: logo, thanh tìm kiếm nhanh, menu điều hướng.
   - Kiểm tra hiển thị danh sách phân loại món ăn bắt mắt.
   - Kiểm tra Footer đầy đủ thông tin nhóm và bản quyền.

2. **Trang Quản trị Danh mục (`http://localhost:3000/dashboard/categories`)**:
   - Xem bảng danh mục món ăn (chỉ dành cho Admin).
   - Thử form thêm danh mục mới, kiểm tra trường Name không được để trống.

3. **Trang Quản lý Hồ sơ Cá nhân (`http://localhost:3000/dashboard/profile`)**:
   - **Xác thực dữ liệu tức thì (Inline Validation)**:
     - Thử xóa trắng ô *Họ và tên*: Form lập tức hiển thị cảnh báo đỏ *"Tên không được để trống."*
     - Nhập tên chứa mã độc: `<script>alert('hack')</script>` -> Bị chặn và cảnh báo.
   - **Bảo mật các trường nhạy cảm**:
     - Trường *Email* và *Vai trò (Role: Author)* ở chế độ Read-Only với biểu tượng khóa bảo vệ.
   - **Xem trước Avatar (Preview)**:
     - Nhập link ảnh HTTPS hợp lệ -> Avatar xem trước hiển thị ngay lập tức.
     - Nhập link ảnh hỏng -> Avatar tự động chuyển sang chế độ fallback an toàn.
   - **Lưu thay đổi**:
     - Nhấn nút *"Lưu thay đổi"* -> Nút hiển thị trạng thái loading spinner -> Toast thông báo thành công xanh lá xuất hiện.

---

## 5. Kiểm thử Tích hợp Môi trường Docker Dev Stack

Toàn bộ 6 dịch vụ phụ trợ được cấu hình trong `docker-compose.dev.yml`:

```bash
# 1. Khởi động 6 container phụ trợ
docker compose -f docker-compose.dev.yml up -d

# 2. Kiểm tra trạng thái hoạt động của các dịch vụ
docker compose -f docker-compose.dev.yml ps
```

| Dịch vụ | Cổng Host | Địa chỉ Kiểm tra / Console | Tài khoản mặc định |
|---|:---:|---|---|
| **PostgreSQL 16** | `5432` | `localhost:5432` | `culinary / culinary_dev_secret` |
| **Redis 7** | `6379` | `localhost:6379` | Không mật khẩu (dev) |
| **MinIO Storage** | `9000` / `9001` | Console: **http://localhost:9001** | `minioadmin / minioadmin` |
| **MailHog Web UI** | `8025` | Web: **http://localhost:8025** | Không cần mật khẩu |
| **Seq Log Server** | `5341` | Web: **http://localhost:5341** | Không cần mật khẩu |
| **Nginx Reverse Proxy**| `80` | Web: **http://localhost:80** | Điều hướng API & Web |

### 5.1. Khởi động Backend API
```bash
# Áp dụng migration cơ sở dữ liệu
dotnet run --project src/backend/CulinaryBlog.API -- --migrate

# Chạy Backend API trên cổng 5080 (hoặc 5000)
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```

---

## 6. Kịch bản Kiểm thử API Chi tiết (cURL & Scalar UI)

Tài liệu API tương tác trực quan tích hợp sẵn tại: **http://localhost:5080/scalar/v1**

### 6.1. Đăng ký Tài khoản Mới (FR-AUTH-001 / §8.1)
```bash
curl -X POST http://localhost:5080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Nguyen Van A",
    "userName": "nguyenvana",
    "email": "nguyenvana@example.com",
    "password": "Password123!"
  }'
```
**Phản hồi kỳ vọng (`HTTP 201 Created`)**:
```json
{
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "...",
    "expiresAt": "2026-09-16T18:45:00.0000000+00:00",
    "user": {
      "id": "guid-here",
      "fullName": "Nguyen Van A",
      "email": "nguyenvana@example.com",
      "userName": "nguyenvana",
      "avatarUrl": null,
      "roles": ["Author"],
      "emailConfirmed": false,
      "createdAt": "2026-09-16T18:30:00.0000000+00:00"
    }
  }
}
```

> 📧 **Kiểm tra MailHog**: Mở trình duyệt vào `http://localhost:8025` sẽ thấy ngay 1 email HTML chào mừng được gửi tới `nguyenvana@example.com` với tên `Nguyen Van A` đã được mã hóa HTML an toàn.

---

### 6.2. Đăng nhập (FR-AUTH-002)
```bash
curl -X POST http://localhost:5080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nguyenvana@example.com",
    "password": "Password123!"
  }'
```
**Phản hồi kỳ vọng (`HTTP 200 OK`)**: Nhận access token trong object `{ data: { accessToken, ... } }`.

---

### 6.3. Kiểm thử Khóa Tài khoản (Account Lockout - NFR-SEC-004)
Thực hiện chạy lệnh đăng nhập sai mật khẩu 5 lần liên tiếp:
```bash
for i in {1..5}; do
  curl -s -X POST http://localhost:5080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"nguyenvana@example.com","password":"WrongPassword!"}' \
    -w "\nLần $i - HTTP: %{http_code}\n"
done
```
**Phản hồi kỳ vọng**:
- Lần 1 đến 4: `HTTP 401 Unauthorized`.
- Lần 5: `HTTP 423 Locked` kèm response Problem Details:
  ```json
  {
    "type": "https://datatracker.ietf.org/doc/html/rfc7807",
    "title": "Account Locked",
    "status": 423,
    "detail": "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần.",
    "code": "auth.locked"
  }
  ```

---

### 6.4. Kiểm thử Giới hạn Tần suất (Rate Limiting - NFR-SEC-003)
Gửi 11 request liên tiếp trong vòng vài giây:
```bash
for i in {1..11}; do
  curl -s -X POST http://localhost:5080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"pwd"}' \
    -o /dev/null -w "Req $i: HTTP %{http_code}\n"
done
```
**Phản hồi kỳ vọng**: Từ request thứ 11 trở đi trả về `HTTP 429 Too Many Requests` kèm header `Retry-After: 60`.

---

### 6.5. Xem & Cập nhật Hồ sơ Cá nhân (FR-AUTH-006 & FR-AUTH-007)
```bash
# Thay thế {TOKEN} bằng accessToken nhận được từ bước đăng nhập
export TOKEN="eyJhbGciOi..."

# 1. Xem thông tin cá nhân
curl -X GET http://localhost:5080/api/v1/auth/me \
  -H "Authorization: Bearer $TOKEN"

# 2. Cập nhật tên và avatar
curl -X PATCH http://localhost:5080/api/v1/auth/me \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Nguyen Van A (Cap Nhat)",
    "avatarUrl": "https://images.unsplash.com/photo-1534528741775-53994a69daeb"
  }'
```

---

### 6.6. Kiểm thử Quản lý Danh mục (FR-CAT-001...005)
```bash
# 1. Lấy danh sách danh mục (Public, cache 60 phút - C03)
curl -X GET http://localhost:5080/api/v1/categories

# 2. Tạo danh mục mới (Cần quyền Admin)
curl -X POST http://localhost:5080/api/v1/categories \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Món Canh Truyền Thống",
    "description": "Các món canh thanh mát đậm đà hương vị quê hương"
  }'

# 3. Xóa danh mục (Hard Delete theo C07)
curl -X DELETE http://localhost:5080/api/v1/categories/{CATEGORY_ID} \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```
> 🛡️ **Lưu ý nghiệp vụ C07**: Nếu danh mục đang chứa công thức (kể cả món Draft), API sẽ từ chối xóa và trả về `HTTP 409 Conflict` kèm thông báo bảo vệ tính toàn vẹn dữ liệu.

---

### 6.7. Kiểm thử Giám sát & Sức khỏe Hệ thống (FR-OBS-001)
```bash
# 1. Liveness probe (tiến trình còn chạy)
curl -i http://localhost:5080/health/live

# 2. Readiness probe (kết nối DB và Redis sẵn sàng)
curl -i http://localhost:5080/health/ready

# 3. Báo cáo chi tiết tất cả dependencies
curl -i http://localhost:5080/health
```

---

## 7. Xử lý Sự cố & Câu hỏi Thường gặp (Troubleshooting)

### Q1: Lệnh `dotnet format --verify-no-changes` báo lỗi WHITESPACE?
**Nguyên nhân**: Mã nguồn chứa ký tự khoảng trắng thừa cuối dòng hoặc thụt lề chưa chuẩn.  
**Khắc phục**: Chạy lệnh `dotnet format CulinaryBlog.sln` để tự động chuẩn hóa lại định dạng theo file `.editorconfig`.

### Q2: Chạy test báo lỗi kết nối cơ sở dữ liệu `culinary_test`?
**Nguyên nhân**: Container PostgreSQL chưa khởi động hoặc chưa cấp quyền.  
**Khắc phục**:
1. Khởi động lại container PostgreSQL: `docker compose -f docker-compose.dev.yml up -d postgres`.
2. Kiểm tra biến môi trường: `echo $TEST_DATABASE`.

### Q3: Build Frontend báo lỗi `Error in getCategories`?
**Nguyên nhân**: Trong quá trình `npm run build`, Next.js thực hiện Server-Side Pre-rendering cho trang tĩnh `/categories`. Nếu Backend API chưa chạy, hàm bắt lỗi trong `api.ts` sẽ ghi nhận cảnh báo và tự động fallback về mảng rỗng một cách an toàn. Điều này hoàn toàn bình thường và không ảnh hưởng đến bản build sản phẩm.
