using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Contracts.Persistence;
// Theo abstraction DbSet trong giáo trình: phụ thuộc EF Core, không phụ thuộc Infrastructure/Npgsql.
public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Recipe> Recipes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
