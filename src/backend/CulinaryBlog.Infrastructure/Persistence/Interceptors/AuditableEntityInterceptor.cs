using CulinaryBlog.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CulinaryBlog.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Chạy trước khi SaveChanges gửi lệnh xuống DB, gộp 3 trách nhiệm cho MỌI BaseEntity:
///  1) Audit: CreatedAt (Added), UpdatedAt (Modified) theo UTC. UpdatedAt để NULL khi Added (SRS 7.1).
///  2) RowVersion (D19): gán token opaque bytea mới khi Added/Modified. EF dùng giá trị GỐC trong WHERE
///     (IsConcurrencyToken) nên writer chậm hơn nhận DbUpdateConcurrencyException — không lost update.
///  3) Soft delete đồng nhất (D08 / SRS 7.1): Deleted -> Modified(IsDeleted=true) cho mọi BaseEntity.
///     (RefreshToken KHÔNG phải BaseEntity nên hard delete như thường - D24.)
/// </summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void ApplyRules(DbContext? context)
    {
        if (context is null) return;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (EntityEntry<BaseEntity> entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            // D19: EF DetectChanges đánh giá entity con MỚI (vừa add qua aggregate: ingredient/step/image)
            // thành Modified vì BaseEntity.Id = Guid.NewGuid() non-empty đã set key. Nguyên gốc RowVersion
            // rỗng (chưa từng nạp từ DB) chứng minh nó là entity mới trong bộ nhớ -> phải Added để INSERT
            // (nếu không, UPDATE WHERE RowVersion=empty => 0 rows => DbUpdateConcurrencyException).
            if (entry.State == EntityState.Modified
                && entry.Property(nameof(BaseEntity.RowVersion)).OriginalValue is byte[] { Length: 0 })
            {
                entry.State = EntityState.Added;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = null;              // SRS 7.1: UpdatedAt NULL tới lần sửa đầu
                    entry.Entity.RowVersion = NewToken();
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.RowVersion = NewToken();
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;         // soft delete (D08)
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.RowVersion = NewToken();
                    break;
            }
        }
    }

    private static byte[] NewToken() => Guid.NewGuid().ToByteArray();
}
