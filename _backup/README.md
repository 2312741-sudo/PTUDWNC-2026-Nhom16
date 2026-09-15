# Culinary Blog — Phần TV3 (Huỳnh Quốc Trung, 2312786) — Tuần 1 / Task C1

Gói này chứa **công việc tuần 1** của TV3: Recipe aggregate, persistence (EF Core 10 / PostgreSQL 16),
audit + soft delete + optimistic concurrency, ERD chuẩn hóa theo SRS, ADR và concurrency spike.

## Bắt đầu nhanh (4 bước)

```bash
# 1. PostgreSQL 16
docker compose -f docker-compose.dev.yml up -d

# 2. Sinh migration từ model (Code First — không viết tay)
cd src/backend
dotnet ef migrations add InitialCreate \
  --project CulinaryBlog.Infrastructure \
  --startup-project CulinaryBlog.Infrastructure \
  --output-dir Persistence/Migrations

# 3. Áp lên DB sạch
export CONNECTIONSTRINGS__DEFAULT="Host=localhost;Port=5432;Database=culinaryblog;Username=postgres;Password=postgres"
dotnet ef database update \
  --project CulinaryBlog.Infrastructure --startup-project CulinaryBlog.Infrastructure

# 4. Concurrency spike (tiêu chí nghiệm thu C1)
cd ../..
export SPIKE_DB="Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres"
dotnet test tests/concurrency-spike/ConcurrencySpike.csproj
```

Yêu cầu: .NET 10 SDK, Docker, `dotnet tool install --global dotnet-ef`.

## Bản đồ tài liệu

| File | Nội dung |
|---|---|
| `docs/ERD.md` | **ERD chuẩn hóa theo SRS Ch.6.4 & 7** (sơ đồ Mermaid + data dictionary + index) |
| `docs/adr/ADR-0001-…md` | Quyết định schema & concurrency (D08/D15/D16/D18/D19/D24/D28) |
| `docs/evidence/TV3/C1-HOAN-THIEN.md` | Hướng dẫn chạy từng bước + bảng đối chiếu nghiệm thu C1 |
| `docs/evidence/TV3/KE_HOACH_TV3.md` | Kế hoạch C1–C7, ma trận K01–K24, sổ evidence |
| `TV3-TUAN-1-BAN-GIAO.md` | Tóm tắt bàn giao tuần 1 + seam tích hợp |

## Cấu trúc mã

```
src/backend/
  CulinaryBlog.Domain/          # thuần BCL (D18): Recipe aggregate, Nutrition owned, Category, RefreshToken
  CulinaryBlog.Application/     # interfaces: IApplicationDbContext, IUnitOfWork, ICurrentUser
  CulinaryBlog.Infrastructure/  # EF config, ApplicationDbContext (IdentityDbContext), interceptor, seeder, DI
tests/concurrency-spike/        # 4 test: lost update, reload+reapply, rollback, publish invariant
```

## Lưu ý tích hợp

`Category`, `ApplicationUser`, `RefreshToken` là **bản tối thiểu** do TV3 tạo để C1 chạy độc lập.
Khi nhóm tích hợp: TV2 thay `Category` (B1), TV1 mở rộng `ApplicationUser` + sở hữu RefreshToken schema (A1).
Chỉ giữ **một** `ApplicationDbContext`. Xem mục "Placeholder" trong ADR-0001.

## Tiếp theo
Task **C2** — Create/Update/Detail CQRS + validators + ownership + slug (D14) + map concurrency → 422 (D02).
