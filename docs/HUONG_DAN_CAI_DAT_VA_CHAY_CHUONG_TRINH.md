# HƯỚNG DẪN CÀI ĐẶT, CẤU HÌNH VÀ CHẠY CHƯƠNG TRÌNH
## Đồ án: Culinary Blog — Nền tảng Khám phá & Chia sẻ Công thức Ẩm thực (Nhóm 16)

> **Môn học**: Phát Triển Ứng Dụng Web Nâng Cao (PTUDWNC) — Học kỳ 1, Năm 2026  
> **Kho lưu trữ GitHub**: [2312741-sudo/PTUDWNC-2026-Nhom16](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16)  
> **Nhóm thực hiện**: Nhóm 16  
> **Tài liệu tham chiếu**: [SRS v1.1.1](root/SRS_Culinary_Blog_v1.1.1.md) · [README.md](../README.md) · [HUONG_DAN_TEST_APP.md](HUONG_DAN_TEST_APP.md)

---

## 📑 MỤC LỤC
1. [Kiến trúc Tổng quan Hệ thống](#1-kiến-trúc-tổng-quan-hệ-thống)
2. [Yêu cầu Môi trường & Phần mềm](#2-yêu-cầu-môi-trường--phần-mềm)
3. [Cấu hình Biến Môi trường (.env & .env.local)](#3-cấu-hình-biến-môi-trường-env--envlocal)
4. [Khởi động Hạ tầng Docker (6 Container)](#4-khởi-động-hạ-tầng-docker-6-container)
5. [Cài đặt, Migration & Chạy Backend API (.NET 10)](#5-cài-đặt-migration--chạy-backend-api-net-10)
6. [Cài đặt & Chạy Frontend Web (Next.js 15)](#6-cài-đặt--chạy-frontend-web-nextjs-15)
7. [Bảng Tra cứu Các Địa chỉ URL & Cổng Dịch vụ](#7-bảng-tra-cứu-các-địa-chỉ-url--cổng-dịch-vụ)
8. [Tài khoản Mặc định & Dữ liệu Mẫu](#8-tài-khoản-mặc-định--dữ-liệu-mẫu)
9. [Hướng dẫn Chạy Bộ Kiểm thử Tự động (Tests)](#9-hướng-dẫn-chạy-bộ-kiểm-thử-tự-động-tests)
10. [Xử lý Sự cố Thường gặp (Troubleshooting)](#10-xử-lý-sự-cố-thường-gặp-troubleshooting)
11. [Dừng và Dọn dẹp Môi trường](#11-dừng-và-dọn-dẹp-môi-trường)

---

## 1. Kiến trúc Tổng quan Hệ thống

Hệ thống được xây dựng theo kiến trúc **Clean Architecture** kết hợp mô hình **CQRS** (Command Query Responsibility Segregation) và phân tách rõ ràng giữa Frontend và Backend:

```mermaid
flowchart TD
    Client["Trình duyệt Người dùng (Desktop / Mobile)"]
    
    subgraph FrontendApp ["Frontend: Next.js 15 (Port 3000)"]
        Pages["App Router (SSR / CSR)"]
        UIComponents["Tailwind CSS + Lucide Icons"]
        ClientState["React Hook Form + Zod"]
    end
    
    subgraph ReverseProxy ["Nginx (Port 8080)"]
        NginxRoute["Reverse Proxy & Static Cache"]
    end

    subgraph BackendAPI ["Backend: .NET 10 Minimal APIs (Port 5080)"]
        API["Minimal API Endpoints + Problem Details (RFC 7807)"]
        MediatR["CQRS Pipeline Behaviors (Validation, Logging)"]
        Application["Application Core (Use Cases / DTOs / Interfaces)"]
        Infrastructure["Infrastructure (EF Core, Redis, S3, Identity)"]
        Hangfire["Hangfire Background Job Server"]
    end

    subgraph DevInfra ["Hạ tầng Docker Containers"]
        PG[("PostgreSQL 16\n(Port 5432)")]
        Redis[("Redis 7 Cache\n(Port 6379)")]
        S3[("RustFS S3 Storage\n(Port 9000 / 9001)")]
        Mail["MailHog SMTP\n(Port 1025 / 8025)")]
        Seq["Seq Log Sink\n(Port 5341)")]
    end

    Client -->|HTTP 3000| FrontendApp
    Client -->|HTTP 5080 / 8080| BackendAPI
    FrontendApp -->|API Calls| BackendAPI
    BackendAPI --> PG
    BackendAPI --> Redis
    BackendAPI --> S3
    BackendAPI --> Mail
    BackendAPI --> Seq
    Hangfire --> PG
```

---

## 2. Yêu cầu Môi trường & Phần mềm

Cần cài đặt các công cụ sau trước khi tiến hành khởi chạy dự án:

| Phần mềm / Công cụ | Phiên bản yêu cầu | Lệnh kiểm tra | Ghi chú |
|---|---|---|---|
| **.NET SDK** | `.NET 10.x` | `dotnet --version` | Dùng để build & chạy Backend |
| **Node.js & npm** | `Node.js >= 20.x LTS` | `node -v` && `npm -v` | Dùng để chạy Frontend Next.js |
| **Docker & Docker Compose** | Docker Desktop mới nhất | `docker compose version` | Bắt buộc Docker daemon đang chạy |
| **Git** | `2.x+` | `git --version` | Quản lý mã nguồn |

> [!IMPORTANT]
> **Đảm bảo Docker Desktop đang hoạt động:** Mở ứng dụng Docker Desktop trên Windows/macOS và đợi đến khi biểu tượng trạng thái hiển thị màu xanh (*Engine running*).

---

## 3. Cấu hình Biến Môi trường (.env & .env.local)

Dự án sử dụng cơ chế cấu hình 2 lớp:
1. **Giá trị chuẩn (Default)**: Nằm sẵn trong `appsettings.Development.json` và `docker-compose.dev.yml` (chuẩn PostgreSQL `Password=postgres`).
2. **Giá trị ghi đè (Override)**: Nằm trong file `.env` ở thư mục gốc (không commit lên Git).

### 3.1. Cấu hình Backend (.env ở thư mục gốc)
Tại thư mục gốc dự án (`PTUDWNC-2026-Nhom16`):

```powershell
# Copy file mẫu cấu hình sang .env — CHỈ KHI CHƯA CÓ .env
# (Copy-Item thường sẽ GHI ĐÈ .env đang có và làm mất cấu hình máy)
if (Test-Path .env) { ".env đã có — giữ nguyên" } else { Copy-Item .env.example .env }
```

Mở file `.env` vừa tạo và kiểm tra các giá trị quan trọng sau:
```ini
# --- Hạ tầng Docker Compose ---
POSTGRES_PASSWORD=postgres
RUSTFS_ACCESS_KEY=minioadmin
RUSTFS_SECRET_KEY=minioadmin
S3_TAG=1.0.0
S3_DIGEST=sha256:8cc9801755448b71a786705ce76692c77e14936cccd87cf2fc31842e58f4d1ff

# --- Kết nối CSDL PostgreSQL của Backend API ---
ConnectionStrings__Database=Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=postgres

# --- Khóa ký JWT (Bắt buộc tối thiểu 64 bytes để thỏa mãn HMAC-SHA256) ---
# KHÔNG dùng khóa có sẵn. Sinh khóa riêng cho máy này rồi dán kết quả 64 ký tự:
#   Git Bash / WSL : openssl rand -base64 48
#   PowerShell     : $b=New-Object byte[] 48; ([Security.Cryptography.RandomNumberGenerator]::Create()).GetBytes($b); [Convert]::ToBase64String($b)
# Khóa dev từng bị commit vào repo đã bị thu hồi và app sẽ từ chối dùng lại (QD3-3b).
Jwt__SigningKey=REPLACE_WITH_RANDOM_SECRET_AT_LEAST_64_BYTES
Jwt__Issuer=culinary-blog
Jwt__Audience=culinary-blog-client

ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5080

# --- Cấu hình Object Storage (RustFS S3) ---
Minio__Endpoint=localhost:9000
Minio__Bucket=culinary-blog
Minio__AccessKey=minioadmin
Minio__SecretKey=minioadmin
Minio__UseSsl=false

# --- CSDL dùng cho bài chạy kiểm thử (Automated Tests) ---
TEST_DATABASE=Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=postgres
```

### 3.2. Cấu hình Frontend (src/frontend/.env.local)
Next.js không tự động đọc file `.env` ở thư mục gốc của solution, do đó cần tạo file `.env.local` riêng cho frontend:

Tạo file `src/frontend/.env.local` với nội dung sau:
```ini
NEXT_PUBLIC_API_URL=http://localhost:5080/api/v1
NEXT_PUBLIC_MEDIA_URL=http://localhost:5080/api/v1/resources/images
NEXT_PUBLIC_SITE_URL=http://localhost:3000
```

---

## 4. Khởi động Hạ tầng Docker (6 Container)

Tại thư mục gốc dự án, chạy lệnh khởi động 6 container phụ trợ:

```powershell
docker compose -f docker-compose.dev.yml up -d
```

### Kiểm tra trạng thái các container:
```powershell
docker compose -f docker-compose.dev.yml ps
```

Khi chạy thành công, toàn bộ 6 container sẽ có trạng thái `Up` (hoặc `healthy`):
* `culinaryblog-pg`: PostgreSQL 16 (Cổng `5432`)
* `culinaryblog-redis`: Redis 7 Cache & Rate Limiting (Cổng `6379`)
* `culinaryblog-s3`: S3 RustFS Object Storage (Cổng `9000` cho API, `9001` cho Web Console)
* `culinaryblog-mailhog`: Máy chủ thử nghiệm gửi nhận email (Cổng `1025` SMTP, `8025` Web UI)
* `culinaryblog-seq`: Máy chủ thu thập log tập trung (Cổng `5341`)
* `culinaryblog-nginx`: Nginx Reverse Proxy (Cổng `8080`)

---

## 5. Cài đặt, Migration & Chạy Backend API (.NET 10)

Mở **PowerShell cửa sổ 1** tại thư mục gốc:

### Bước 5.1: Khôi phục dependencies theo locked-mode
```powershell
dotnet restore CulinaryBlog.sln --locked-mode
```

### Bước 5.2: Áp dụng Migration vào CSDL PostgreSQL
Lệnh này tự động tạo database `culinary_blog`, toàn bộ bảng, khóa ngoại, chỉ mục FTS unaccent/trgm/GIN:
```powershell
dotnet run --project src/backend/CulinaryBlog.API -- --migrate
```
*Kết quả hiển thị:* `Database migrations applied successfully.`

### Bước 5.3: Nạp dữ liệu mẫu (Seed Data)
Lệnh này nạp đầy đủ dữ liệu mẫu chuẩn đề bài (25 danh mục, 100 công thức, 1.099 nguyên liệu, 550 bước):
```powershell
dotnet run --project src/backend/CulinaryBlog.API -- --seed
```
*Kết quả hiển thị:* `Database seeded successfully: 25 categories, 100 recipes (each with >=10 ingredients, >=5 steps).`

### Bước 5.4: Khởi động máy chủ Backend API
```powershell
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```
> [!NOTE]
> Giữ nguyên cửa sổ PowerShell này để máy chủ Backend hoạt động liên tục.  
> Kiểm tra tài liệu API tương tác tại: [http://localhost:5080/scalar/v1](http://localhost:5080/scalar/v1)

---

## 6. Cài đặt & Chạy Frontend Web (Next.js 15)

Mở **PowerShell cửa sổ 2** (giữ cửa sổ 1 đang chạy Backend):

```powershell
# 1. Chuyển vào thư mục frontend
Set-Location src/frontend

# 2. Cài đặt các gói phụ thuộc npm
npm install

# 3. Khởi động server Next.js ở chế độ phát triển
npm run dev
```

*Kết quả hiển thị:*
```text
▲ Next.js 15.1.11
- Local:        http://localhost:3000
- Environments: .env.local

✓ Ready in 1.5s
```

> [!TIP]
> Bây giờ bạn có thể mở trình duyệt và truy cập ngay vào [**http://localhost:3000**](http://localhost:3000)!

---

## 7. Bảng Tra cứu Các Địa chỉ URL & Cổng Dịch vụ

| Thành phần / Dịch vụ | Địa chỉ URL | Cổng | Mô tả tính năng |
|---|---|:---:|---|
| **Website chính (Frontend)** | [http://localhost:3000](http://localhost:3000) | `3000` | Giao diện Next.js App Router |
| **Khám phá món ăn** | [http://localhost:3000/recipes](http://localhost:3000/recipes) | `3000` | Duyệt công thức, phân trang 12 món/trang, bộ lọc đa tiêu chí |
| **Tìm kiếm toàn văn (FTS)** | [http://localhost:3000/search](http://localhost:3000/search) | `3000` | Tìm kiếm tiếng Việt không dấu, lọc theo danh mục/độ khó |
| **Danh mục món ăn** | [http://localhost:3000/categories](http://localhost:3000/categories) | `3000` | 25 danh mục ẩm thực phong phú |
| **Đăng nhập & Đăng ký** | [http://localhost:3000/auth/login](http://localhost:3000/auth/login) | `3000` | Biểu mẫu xác thực và nút Google Sign-In |
| **Quản lý Hồ sơ** | [http://localhost:3000/dashboard/profile](http://localhost:3000/dashboard/profile) | `3000` | Cập nhật thông tin cá nhân, avatar preview |
| **Tài liệu API (Scalar UI)** | [http://localhost:5080/scalar/v1](http://localhost:5080/scalar/v1) | `5080` | Giao diện tương tác và chạy thử 33+ Minimal APIs |
| **Kiểm tra Sức khỏe (Health)** | [http://localhost:5080/health](http://localhost:5080/health) | `5080` | Health probes (Database, Redis, S3) |
| **Bảng điều khiển Hangfire** | [http://localhost:5080/hangfire](http://localhost:5080/hangfire) | `5080` | Giám sát hàng đợi tác vụ nền (Background Jobs) |
| **Bảng điều khiển S3 (RustFS)** | [http://localhost:9001](http://localhost:9001) | `9001` | Quản lý bucket và ảnh đã tải lên |
| **Hộp thư thử nghiệm (MailHog)**| [http://localhost:8025](http://localhost:8025) | `8025` | Xem email chào mừng và email xác thực tức thì |
| **Máy chủ Log tập trung (Seq)** | [http://localhost:5341](http://localhost:5341) | `5341` | Tìm kiếm log có cấu trúc Serilog, W3C TraceId |
| **Nginx Reverse Proxy** | [http://localhost:8080](http://localhost:8080) | `8080` | Reverse proxy dev stack |

---

## 8. Tài khoản Mặc định & Dữ liệu Mẫu

### 8.1. Tài khoản đăng nhập hệ thống (Seed Accounts)
Hệ thống cung cấp sẵn các tài khoản mẫu phục vụ kiểm thử theo các phân quyền:

| Vai trò (Role) | Email đăng nhập | Mật khẩu mặc định | Mô tả quyền hạn |
|---|---|---|---|
| **Admin** | `admin@culinaryblog.vn` | `Admin@123456` | Toàn quyền quản trị danh mục, xem Hangfire dashboard |
| **Author** | `author1@culinaryblog.vn` | `Author@123456` | Tác giả đăng bài, tạo và chỉnh sửa công thức cá nhân |
| **Author** | `author2@culinaryblog.vn` | `Author@123456` | Tác giả thứ 2 |
| **User** | `user1@culinaryblog.vn` | `User@123456` | Người dùng bình thường duyệt và đánh dấu yêu thích |

### 8.2. Tài khoản quản trị lưu trữ S3 (RustFS Console)
- **URL**: [http://localhost:9001](http://localhost:9001)
- **Access Key**: `minioadmin`
- **Secret Key**: `minioadmin`
- **Bucket mặc định**: `culinary-blog`

---

## 9. Hướng dẫn Chạy Bộ Kiểm thử Tự động (Tests)

### 9.1. Chạy toàn bộ test suite của Solution (172+ tests)
Mở cửa sổ PowerShell tại thư mục gốc:

```powershell
dotnet test CulinaryBlog.sln
```
> [!TIP]
> Tất cả các bài kiểm thử đơn vị (Unit Tests), kiểm tra kiến trúc (Architecture Tests), và kiểm tra tính năng Tuần 1–3 đều vượt qua 100% (Green).

### 9.2. Chạy riêng từng nhóm bài test cụ thể

- **Chỉ chạy test Tuần 3 (Khám phá, FTS, Caching, Concurrency RowVersion, PBKDF2):**
  ```powershell
  dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~Week3DiscoverySearchCacheTests"
  ```

- **Chỉ chạy bài test kiểm tra Kiến trúc sạch (Clean Architecture):**
  ```powershell
  dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests"
  ```

- **Kiểm tra định dạng mã nguồn:**
  ```powershell
  dotnet format CulinaryBlog.sln --verify-no-changes
  ```

- **Kiểm tra build Frontend:**
  ```powershell
  npm --prefix src/frontend run build
  ```

---

## 10. Xử lý Sự cố Thường gặp (Troubleshooting)

### 🔴 Sự cố 1: Lỗi `failed to connect to docker API / daemon is not running`
- **Nguyên nhân**: Ứng dụng Docker Desktop chưa được mở hoặc dịch vụ Docker daemon bị tắt.
- **Cách khắc phục**: Mở Docker Desktop từ Start Menu và đợi khoảng 30–60 giây cho đến khi trạng thái chuyển sang màu xanh. Sau đó chạy lại lệnh `docker compose -f docker-compose.dev.yml up -d`.

### 🔴 Sự cố 2: Lỗi `InvalidOperationException: Thieu/yeu Jwt:SigningKey (it nhat 64 byte UTF-8)`
- **Nguyên nhân**: `Jwt__SigningKey` thiếu, rỗng, hoặc ngắn hơn 64 byte UTF-8. Từ QD3-3b, khóa ký JWT
  **không còn nằm trong `appsettings*.json`** nên đây là trạng thái bắt buộc phải cấu hình, không phải lỗi cấu hình.
- **Cách khắc phục**: sinh khóa ngẫu nhiên rồi dán vào `.env` (không commit):
  ```bash
  openssl rand -base64 48          # Git Bash / WSL / CI
  ```
  ```powershell
  # PowerShell
  $b=New-Object byte[] 48; ([Security.Cryptography.RandomNumberGenerator]::Create()).GetBytes($b); [Convert]::ToBase64String($b)
  ```
  Dán chuỗi 64 ký tự nhận được vào `Jwt__SigningKey` trong `.env`.

### 🔴 Sự cố 2b: Lỗi `Jwt:SigningKey la khoa dev da bi commit vao repo (bi thu hoi)`
- **Nguyên nhân**: `.env` vẫn dùng khóa dev cũ từng bị commit vào lịch sử repo.
- **Cách khắc phục**: thay bằng khóa ngẫu nhiên mới (xem Sự cố 2). Lưu ý: mọi phiên đăng nhập đang dùng khóa cũ sẽ mất hiệu lực, cần đăng nhập lại.

### 🔴 Sự cố 3: Lỗi `28P01: password authentication failed for user "postgres"`
- **Nguyên nhân**: Mật khẩu trong `.env` không khớp với mật khẩu PostgreSQL đã khởi tạo trong volume container trước đó.
- **Cách khắc phục**:
  1. Kiểm tra biến `POSTGRES_PASSWORD` và `ConnectionStrings__Database` trong `.env` phải có mật khẩu giống nhau (mặc định là `postgres`).
  2. Nếu volume cũ bị lệch mật khẩu, đặt lại mật khẩu cho container PostgreSQL đang chạy bằng lệnh:
     ```powershell
     docker exec culinaryblog-pg psql -U postgres -c "ALTER USER postgres PASSWORD 'postgres';"
     ```

### 🔴 Sự cố 4: Trùng cổng (Port Conflict - 5432, 6379, 5080, 3000)
- **Nguyên nhân**: Máy tính đang chạy sẵn một dịch vụ cục bộ (ví dụ PostgreSQL cài trực tiếp trên Windows).
- **Cách khắc phục**:
  - Dừng dịch vụ PostgreSQL cục bộ trên Windows: `Stop-Service postgresql* -Force`
  - Hoặc đổi cổng host bên trái trong `docker-compose.dev.yml` và cập nhật lại chuỗi kết nối trong `.env`.

---

## 11. Dừng và Dọn dẹp Môi trường

### 11.1. Dừng các máy chủ ứng dụng
- Tại cửa sổ chạy Backend hoặc Frontend, nhấn tổ hợp phím `Ctrl + C` để dừng tiến trình.

### 11.2. Tạm dừng các container Docker (giữ nguyên dữ liệu)
```powershell
docker compose -f docker-compose.dev.yml stop
```

### 11.3. Khởi động lại các container đã dừng
```powershell
docker compose -f docker-compose.dev.yml start
```

### 11.4. Xóa toàn bộ container và reset dữ liệu dev sạch từ đầu (nếu cần)
> [!WARNING]
> Lệnh này sẽ xóa toàn bộ database và các ảnh upload trong môi trường dev để khởi tạo lại từ đầu.
```powershell
docker compose -f docker-compose.dev.yml down -v
```

---

*Tài liệu được biên soạn và kiểm chứng thực tế 100% trên môi trường Windows / PowerShell cho dự án Culinary Blog Nhóm 16.*
