# TV3 — Huỳnh Quốc Trung · Tuần 1 (Task C1) — Bàn giao

Phạm vi tuần 1 của bạn theo hồ sơ (mục 4.2 + task C1):
**ERD/migrations, Draft CRUD nền/Nutrition, concurrency spike** — trọng tâm là *Recipe aggregate + persistence + RowVersion*.
Cổng G1 cuối tuần: đăng ký → đăng nhập → category → **Draft** chạy được xuyên suốt.

## 1. Đã dựng trong lần này

```
docs/adr/ADR-0001-recipe-schema-va-concurrency.md      # artifact "cổng" tuần 1 (D08,D15,D16,D18,D19,D24,D28)
src/backend/CulinaryBlog.Domain/                        # thuần BCL (D18)
  Common/            BaseEntity, ISoftDeletable, IAggregateRoot, DomainException
  Enums/             RecipeStatus, RecipeDifficulty (đủ Expert - D15)
  Entities/          Recipe (aggregate root), RecipeNutrition (owned), RecipeIngredient, RecipeStep, RecipeImage
src/backend/CulinaryBlog.Application/Common/Interfaces/  # IApplicationDbContext, IUnitOfWork, ICurrentUser
src/backend/CulinaryBlog.Infrastructure/Persistence/
  ApplicationDbContext, EfUnitOfWork
  Interceptors/      AuditableEntityInterceptor  (audit + RowVersion bytea + soft delete)
  Configurations/    Recipe/Ingredient/Step/Image (index, unique, partial-unique primary image, owned nutrition)
tests/concurrency-spike/                                 # spike chứng minh "2 writer không lost update"
```

Đã bao phủ tiêu chí nghiệm thu C1:
- **FK đúng**: cột + index `CategoryId`/`AuthorId`; FK vật lý bật ở migration tích hợp (xem seam bên dưới).
- **Migration chạy trên DB sạch**: model sẵn sàng cho `dotnet ef migrations add`.
- **Rollback transaction**: `EfUnitOfWork.ExecuteInTransactionAsync` + test `Nested_create_failure_rolls_back_whole_aggregate`.
- **2 writer không lost update**: test `Two_writers_second_save_is_rejected_no_lost_update`.

## 2. Chạy concurrency spike (làm được NGAY tuần 1, không cần chờ TV1/TV2)

Spike dùng đúng `ApplicationDbContext` + interceptor thật, trên một Postgres nháp. **Không dùng InMemory** vì
provider đó bỏ qua concurrency token.

```bash
# 1) Có Postgres 16 (dùng luôn service trong docker-compose dev, hoặc container riêng):
docker run --rm -d --name pg-spike -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16

# 2) Trỏ connection string tới DB nháp:
export SPIKE_DB="Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres"

# 3) Chạy (cần .NET 10 SDK + mạng nuget ở máy bạn):
dotnet test tests/concurrency-spike/ConcurrencySpike.csproj
```

Kỳ vọng: 4 test PASS. Lưu output làm minh chứng cho **K07** (concurrency) và **C1** trong sổ evidence `TV3-K07`.

> Môi trường tạo hồ sơ này không có .NET SDK và chặn nuget nên mình chưa build/chạy hộ được.
> Bạn chạy ở máy để bắt lỗi biên dịch (nếu có) và khóa phiên bản package theo D25.

## 3. Seam tích hợp (phụ thuộc A1 của TV1 và B1 của TV2)

- **ApplicationDbContext là DUY NHẤT** cho cả hệ thống. File hiện tại chỉ chứa phần Recipe của bạn; khi tích hợp,
  DbSet `ApplicationUser`/`RefreshToken` (TV1) và `Category` + SearchVector (TV2) gộp vào cùng lớp. Đừng tạo bản thứ hai.
- FK vật lý tới `categories`/`AspNetUsers`: mở khối đã ghi chú trong `RecipeConfiguration.cs` khi 2 entity đó có trong model,
  rồi tạo migration tích hợp theo thứ tự **category → user → recipe**. `OnDelete(Restrict)` cho category để đỡ FR-CAT-005 (409).
- Bạn (TV3) **điều phối thứ tự/rebase migration** cho nhóm, nhưng mỗi người tự viết migration của mình (mục 12.1).

## 4. Việc còn lại của bạn để đóng G1 tuần 1

1. Khóa phiên bản EF10/Npgsql10/PG16 vào `global.json`/lockfile cùng cả nhóm (D25) — sửa các `Version="10.0.0"` cho đúng bản phát hành thật.
2. Sau khi B1 (Category) + A1 (User) tối thiểu merge: bật FK, tạo migration `AddRecipeAggregate`, chạy trên DB sạch.
3. Viết seed Bogus tối thiểu cho recipe (nối vào seed chung của nhóm, không tạo seeder riêng trùng lặp).
4. Ghi evidence: `TV3-K03` (aggregate/UoW), `TV3-K06` (migration/config), `TV3-K07` (RowVersion/soft delete/audit).

## 5. Bắt đầu tuần 2 (C2/C3/C5) — không làm trong lần này
- **C2**: Create/Update/Detail CQRS + validators + ownership + slug (D14) + map 422 (D02).
- **C3**: Ingredient/Step CRUD + renumber trong transaction.
- **C5**: RefreshToken rotation/family reuse (D05) — phối hợp contract auth với TV1.

## 6. Lưu ý đúng quy tắc hồ sơ
- Không nhận vơ phần của TV1/TV2/TV4; các lab K0x còn lại (Google, media, jobs...) bạn tự làm trên nhánh `practice/TV3/Lx`.
- Chưa đánh dấu "hoàn thành" mục nào tới khi có test/PR/review của nhóm trưởng (Nguyễn Thanh Tâm).
