using CulinaryBlog.Application;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// K19 (TV3): tên hiển thị tác giả + tên danh mục cho trang chi tiết / JSON-LD. Một câu SQL neo vào dòng công thức, hai subquery
/// độc lập: danh mục đã xoá mềm (query filter) chỉ làm mất tên danh mục, không làm mất tên tác giả. Chỉ đọc cột công khai (DisplayName).
/// </summary>
public sealed class RecipeDisplayNameReader(AuthDbContext db) : IRecipeDisplayNameReader
{
    public async Task<(string? AuthorName, string? CategoryName)> GetAsync(Guid recipeId, CancellationToken ct)
    {
        var row = await db.Recipes.AsNoTracking()
            .Where(r => r.Id == recipeId)
            .Select(r => new
            {
                Author = db.Users.Where(u => u.Id == r.AuthorId).Select(u => u.DisplayName).FirstOrDefault(),
                Category = db.Categories.Where(c => c.Id == r.CategoryId).Select(c => c.Name).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        return (string.IsNullOrWhiteSpace(row?.Author) ? null : row.Author, row?.Category);
    }
}
