namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Ranh giới transaction cho các use case ghi (C1: rollback transaction, đổi thứ tự step trong 1 transaction).
/// Cài đặt trong Infrastructure bọc ApplicationDbContext + Database.BeginTransactionAsync.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
