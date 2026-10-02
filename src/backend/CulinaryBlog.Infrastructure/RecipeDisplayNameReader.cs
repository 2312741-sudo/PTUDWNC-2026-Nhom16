using CulinaryBlog.Application;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// K19 (TV3): tên hiển thị tác giả + tên danh mục cho trang chi tiết / JSON-LD. Một câu SQL (subquery tác giả trong
/// projection danh mục) để không thêm round-trip; chỉ đọc cột công khai (DisplayName), không lộ email.
/// </summary>
public sealed class RecipeDisplayNameReader(AuthDbContext db) : IRecipeDisplayNameReader
{
    public async Task<(string? AuthorName, string? CategoryName)> GetAsync(string authorId, Guid categoryId, CancellationToken ct)
    {
        var row = await db.Categories.AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new
            {
                c.Name,
                Author = db.Users.Where(u => u.Id == authorId).Select(u => u.DisplayName).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        return (string.IsNullOrWhiteSpace(row?.Author) ? null : row.Author, row?.Name);
    }
}
