# C1 — Hướng dẫn hoàn thiện & nghiệm thu (TV3, solo)

> Trạng thái: **code C1 đã đủ để chạy độc lập**. Vì môi trường soạn thảo không có .NET SDK/nuget,
> bạn chạy 4 bước dưới ở máy mình để tạo DB + evidence. Mọi quyết định schema đã chốt trong `docs/adr/ADR-0001`.

## 0. Yêu cầu công cụ
- .NET 10 SDK, Docker, `dotnet-ef` (`dotnet tool install --global dotnet-ef`).
- Khóa version EF/Npgsql thật trong csproj (hiện để `10.0.0` theo D25 — sửa cho khớp bản phát hành).

## 1. Khởi động PostgreSQL 16
```bash
docker compose -f docker-compose.dev.yml up -d
# kiểm tra: docker ps  → thấy culinaryblog-pg, cổng 5432
```

## 2. Tạo migration từ model (Code First)
Migration KHÔNG viết tay — sinh từ entity + config bằng lệnh:
```bash
cd src/backend
dotnet ef migrations add InitialCreate \
  --project CulinaryBlog.Infrastructure \
  --startup-project CulinaryBlog.Infrastructure \
  --output-dir Persistence/Migrations
```
(`ApplicationDbContextFactory` cho phép chạy `dotnet ef` mà không cần project API.)

Kỳ vọng: sinh file trong `Persistence/Migrations/` tạo các bảng AspNet* + Categories + Recipes (+ 6 cột Nutrition_*) +
RecipeIngredients/RecipeSteps/RecipeImages + RefreshTokens, kèm FK/CHECK/partial index.

## 3. Áp lên DB sạch
```bash
export CONNECTIONSTRINGS__DEFAULT="Host=localhost;Port=5432;Database=culinaryblog;Username=postgres;Password=postgres"
dotnet ef database update \
  --project CulinaryBlog.Infrastructure --startup-project CulinaryBlog.Infrastructure
```
Kiểm tra nhanh: `docker exec -it culinaryblog-pg psql -U postgres -d culinaryblog -c "\dt"` → thấy đủ bảng.
→ **Evidence `TV3-K06`**: ảnh lệnh chạy thành công + `\d "Recipes"` cho thấy cột Nutrition_*, FK, CHECK.

## 4. Chạy concurrency spike (tiêu chí C1 quan trọng nhất)
```bash
export SPIKE_DB="Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres"
dotnet test tests/concurrency-spike/ConcurrencySpike.csproj
```
Kỳ vọng 4/4 PASS:
- `Two_writers_second_save_is_rejected_no_lost_update` → 2 writer không lost update.
- `Conflict_resolution_reload_and_reapply_succeeds` → mẫu xử lý 422 (D02) cho C2.
- `Nested_create_failure_rolls_back_whole_aggregate` → rollback transaction.
- `Publish_requires_at_least_one_ingredient_and_one_step` → bất biến publish (D07).
→ **Evidence `TV3-K07`**: log test xanh.

## 5. (Tùy chọn) Seed dữ liệu mẫu
Gọi `DbSeeder.SeedAsync(db)` từ host của bạn (hoặc một console nhỏ) để có 2 category + 2 recipe (1 published, 1 draft)
làm dữ liệu cho C2. Seed Bogus đầy đủ (≥50 recipe) là việc chung, làm sau khi tích hợp.

## Đối chiếu tiêu chí nghiệm thu C1 (đề mục 7)
| Tiêu chí | Bằng chứng |
|---|---|
| FK đúng | Bước 3: FK Recipe→Categories (RESTRICT), Recipe→AspNetUsers, RefreshToken→AspNetUsers (CASCADE) |
| Migration chạy DB sạch | Bước 2–3 chạy thành công trên DB rỗng |
| Rollback transaction | Test `Nested_create_failure_rolls_back_whole_aggregate` |
| 2 writer không lost update | Test `Two_writers_second_save_is_rejected_no_lost_update` |

## Lưu ý kỹ thuật
- EF có thể log cảnh báo `PossibleIncorrectRequiredNavigationWithQueryFilterInteraction` do child có cùng filter `!IsDeleted`
  với Recipe — đây là **cảnh báo, không phải lỗi**, và đúng ý đồ (soft delete đồng nhất). Không cần xử lý.
- `Category`/`ApplicationUser`/`RefreshToken` là bản tối thiểu của TV3 để chạy solo. Khi nhóm họp:
  TV2 thay Category, TV1 mở rộng ApplicationUser + sở hữu RefreshToken. Giữ **một** ApplicationDbContext.
- Sau khi tích hợp, nếu tên bảng/khóa của TV1/TV2 khác, chỉ cần sinh migration mới — không sửa tay DB.

## Việc tiếp theo
Xong C1 → sang **C2** (Create/Update/Detail CQRS + validators + ownership + slug + map 422). Xem `KE_HOACH_TV3.md`.
