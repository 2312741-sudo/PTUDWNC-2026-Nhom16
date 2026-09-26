using CulinaryBlog.Application;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Infrastructure;

// Các dòng đánh dấu "ĐỐI CHIẾU" là chỗ giả định tên DbContext / navigation.
// Nếu khác, lấy đúng tên từ RecipeDiscoveryRepository (projection của discovery đã chạy được).
public sealed class MyRecipesRepository(AuthDbContext db) : IMyRecipesRepository // ĐỐI CHIẾU: tên DbContext
{
    public async Task<PagedResult<MyRecipeSummaryDto>> GetByAuthorAsync(GetMyRecipesQuery query, CancellationToken ct)
    {
        // Global query filter IsDeleted (ADR-0001) tự loại bản ghi đã xoá mềm.
        var recipes = db.Recipes.AsNoTracking().Where(r => r.AuthorId == query.AuthorId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = Enum.Parse<RecipeStatus>(query.Status.Trim(), ignoreCase: true);
            recipes = recipes.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = "%" + EscapeLike(query.Q.Trim()) + "%";
            recipes = recipes.Where(r => EF.Functions.ILike(r.Title, pattern, "\\"));
        }

        var total = await recipes.CountAsync(ct);

        var desc = query.SortOrder.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
        recipes = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "title" => desc ? recipes.OrderByDescending(r => r.Title) : recipes.OrderBy(r => r.Title),
            "createdat" => desc ? recipes.OrderByDescending(r => r.CreatedAt) : recipes.OrderBy(r => r.CreatedAt),
            "publishedat" => desc ? recipes.OrderByDescending(r => r.PublishedAt) : recipes.OrderBy(r => r.PublishedAt),
            // UpdatedAt nullable (SRS 7.1): bản chưa sửa lần nào lấy CreatedAt để xếp.
            _ => desc
                ? recipes.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                : recipes.OrderBy(r => r.UpdatedAt ?? r.CreatedAt),
        };
        recipes = ((IOrderedQueryable<CulinaryBlog.Domain.Entities.Recipe>)recipes).ThenBy(r => r.Id); // phân trang ổn định

        // Chiếu enum/bytea thô trước, chuyển sang chuỗi sau khi materialize
        // để không phụ thuộc vào việc EF dịch được Enum.ToString() hay không.
        var rows = await recipes
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Slug,
                r.CategoryId,
                CategoryName = db.Categories.Where(c => c.Id == r.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "Khác",
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                r.Servings,
                r.Difficulty,
                r.Status,
                // ĐỐI CHIẾU: copy đúng biểu thức PrimaryImageUrl từ RecipeDiscoveryRepository
                PrimaryImageUrl = r.Images.Where(i => i.IsPrimary).Select(i => i.OriginalUrl).FirstOrDefault(),
                IngredientCount = r.Ingredients.Count(),
                StepCount = r.Steps.Count(),
                r.PublishedAt,
                r.CreatedAt,
                r.UpdatedAt,
                r.RowVersion,
            })
            .ToListAsync(ct);

        var items = rows.Select(x => new MyRecipeSummaryDto(
            x.Id, x.Title, x.Slug, x.CategoryId, x.CategoryName,
            x.PrepTimeMinutes, x.CookTimeMinutes, x.Servings,
            x.Difficulty.ToString(), x.Status.ToString(),
            x.PrimaryImageUrl, x.IngredientCount, x.StepCount,
            ToUtc(x.PublishedAt), ToUtc(x.CreatedAt), ToUtc(x.UpdatedAt),
            Convert.ToBase64String(x.RowVersion))).ToList();

        return new PagedResult<MyRecipeSummaryDto>(items, PaginationMeta.Create(query.Page, query.PageSize, total)); // ĐỐI CHIẾU: ctor PagedResult
    }

    public async Task<MyRecipeCountsDto> CountByAuthorAsync(string authorId, CancellationToken ct)
    {
        var grouped = await db.Recipes.AsNoTracking()
            .Where(r => r.AuthorId == authorId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Trả đủ mọi trạng thái (kể cả 0) để frontend không phải tự điền.
        var byStatus = Enum.GetValues<RecipeStatus>()
            .ToDictionary(
                s => s.ToString(),
                s => grouped.FirstOrDefault(g => g.Status == s)?.Count ?? 0);

        return new MyRecipeCountsDto(byStatus.Values.Sum(), byStatus);
    }

private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static DateTimeOffset? ToUtc(DateTime? value) => value.HasValue ? ToUtc(value.Value) : null;

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}