# Culinary Blog — Nguyễn Thanh Tâm, tuần 1

MSSV **2312741**. Backend nền cho A1, A2 cơ bản, A5 và A6 kiến trúc/API. Phân công gốc: [kế hoạch 6 tuần](PHAN_CHIA_CONG_VIEC_6_TUAN.md).

## Chạy trên máy

Điều kiện: .NET SDK 10, PostgreSQL **16**, database riêng đã tạo và NuGet truy cập được. Từ thư mục PTUDWNC:

```sh
dotnet restore CulinaryBlog.sln --locked-mode
export ConnectionStrings__Database='Host=localhost;Port=5432;Database=culinary_blog;Username=culinary;Password=YOUR_LOCAL_PASSWORD'
# Tạo key ngẫu nhiên riêng, không dùng key của test cho app.
export Jwt__SigningKey="$(openssl rand -base64 64)"
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/backend/CulinaryBlog.API -- --migrate
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```

Mở **http://localhost:5080/scalar/v1** để thử API. `--migrate` chỉ cập nhật schema rồi thoát; chạy lại không tạo trùng. Không tự migrate trong startup production. `.env.example` là mẫu, .NET không tự nạp `.env`; dùng biến môi trường hoặc `dotnet user-secrets` với project API. Dùng cùng key giữa các lần chạy nếu muốn token cũ còn hiệu lực.

## Demo tuần 1

1. `POST /api/v1/auth/register` với `email`, `password`, `displayName`. Mật khẩu ≥8 ký tự, có hoa/thường/số/đặc biệt. Nhận 201, user Author và access token.
2. `POST /api/v1/auth/login` với email/password, nhận 200 và JWT.
3. `GET /api/v1/auth/me` với header `Authorization: Bearer <accessToken>` để kiểm tra danh tính.
4. Thử email trùng (409), mật khẩu yếu (400 + errors), login sai (401), thiếu/sai JWT (401). Không gửi field `role`; client không được tự cấp quyền.

Token chỉ có thời hạn 15 phút, chưa có refresh. Không lưu token/password vào log hoặc commit. Scalar chỉ mở ở Development. UI Next.js/RHF/Zod của Tâm nằm tuần 2 (A3); tuần 1 thao tác qua Scalar. Không tạo UI shell của TV2 hoặc tính năng Category/Draft của TV2–TV3 trong nhánh này.

## Kiểm thử

Tạo database PostgreSQL 16 **riêng cho test**; tests tự apply migration và thêm tài khoản ngẫu nhiên, không xóa dữ liệu:

```sh
export TEST_DATABASE='Host=localhost;Port=5432;Database=culinary_test;Username=culinary;Password=YOUR_LOCAL_PASSWORD'
dotnet build CulinaryBlog.sln --no-restore
dotnet test CulinaryBlog.sln --no-build --collect:"XPlat Code Coverage" --logger trx
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore
```

`TEST_DATABASE` bắt buộc để tránh dùng nhầm DB sản phẩm. CI dùng service PostgreSQL 16 tạm thời. [Báo cáo tuần 1](docs/evidence/TV1/TUAN_1.md) ghi kết quả đã chạy và giới hạn nghiệm thu.

## Kiến trúc và bàn giao

- Domain thuần BCL: `DisplayName`, `Roles`.
- Application: commands/query, handlers, interfaces, FluentValidation và MediatR logging/validation behaviors.
- Infrastructure: ASP.NET Identity, EF Core/Npgsql, transaction đăng ký + role, JWT service.
- API: Minimal APIs, JWT validation, policies, ICurrentUser adapter, Problem Details, correlation, Scalar.
- [Auth contract](docs/AUTH_CONTRACT.md), [ADR tuần 1](docs/adr/0001-auth-week1.md), [lab kiến trúc/API](docs/evidence/TV1/LAB_KIEN_TRUC_API.md).

Chưa tích hợp Google/refresh/logout, email job, lockout/rate limit, profile cập nhật, frontend hoặc deployment chung; các phần này theo lịch/owner trong tài liệu gốc. `GetMe` tối thiểu dùng xác minh token và bàn giao `ICurrentUser`, chưa phải toàn bộ A3. Seed chỉ hai **role**, không tạo tài khoản Admin/mật khẩu mặc định. Tests tạo tài khoản Author tạm thời.
