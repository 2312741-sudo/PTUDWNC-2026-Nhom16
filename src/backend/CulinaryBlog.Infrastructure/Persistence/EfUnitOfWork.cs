using CulinaryBlog.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using CulinaryBlog.Infrastructure;
namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork bọc ApplicationDbContext. ExecuteInTransactionAsync dùng execution strategy để an toàn với retry (Npgsql).
/// Phục vụ C1: rollback transaction khi nested create thất bại; renumber step nguyên tử.
/// </summary>
public sealed class EfUnitOfWork(AuthDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            try
            {
                await action(ct);
                await context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
