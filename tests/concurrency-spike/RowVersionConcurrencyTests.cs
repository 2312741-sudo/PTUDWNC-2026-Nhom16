using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace ConcurrencySpike;

/// <summary>
/// Chứng minh tiêu chí nghiệm thu C1:
///  - "2 writer không lost update": writer chậm hơn nhận DbUpdateConcurrencyException, không ghi đè im lặng.
///  - "rollback transaction": lỗi giữa chừng trong transaction -> không dữ liệu nào được commit.
/// Đồng thời minh họa mẫu giải quyết xung đột (reload + reapply) mà C2 sẽ ánh xạ sang HTTP 422 (D02).
/// </summary>
public sealed class RowVersionConcurrencyTests(SpikeDbFixture fixture) : IClassFixture<SpikeDbFixture>
{
    private Recipe NewSampleRecipe() =>
        Recipe.CreateDraft(
            title: "Phở bò truyền thống",
            slug: $"pho-bo-{Guid.NewGuid():N}",
            description: "Món phở bò chuẩn vị.",
            instructions: null,
            prepTimeMinutes: 30,
            cookTimeMinutes: 180,
            servings: 4,
            difficulty: RecipeDifficulty.Medium,
            categoryId: fixture.CategoryId,
            authorId: fixture.AuthorId);

    private async Task<Guid> SeedRecipeAsync()
    {
        await using var ctx = fixture.NewContext();
        var recipe = NewSampleRecipe();
        ctx.Recipes.Add(recipe);
        await ctx.SaveChangesAsync();
        return recipe.Id;
    }

    [Fact]
    public async Task Two_writers_second_save_is_rejected_no_lost_update()
    {
        var id = await SeedRecipeAsync();

        // Hai context độc lập cùng đọc một bản ghi (RowVersion giống nhau).
        await using var ctxA = fixture.NewContext();
        await using var ctxB = fixture.NewContext();

        var a = await ctxA.Recipes.SingleAsync(r => r.Id == id);
        var b = await ctxB.Recipes.SingleAsync(r => r.Id == id);

        // Writer A ghi trước -> thành công, interceptor bump RowVersion.
        a.UpdateDetails("Phở bò đặc biệt A", a.Slug, a.Description, a.Instructions,
            a.PrepTimeMinutes, a.CookTimeMinutes, a.Servings, a.Difficulty, a.CategoryId);
        await ctxA.SaveChangesAsync();

        // Writer B ghi trên RowVersion cũ -> EF UPDATE khớp 0 dòng -> ném concurrency.
        b.UpdateDetails("Phở bò đặc biệt B", b.Slug, b.Description, b.Instructions,
            b.PrepTimeMinutes, b.CookTimeMinutes, b.Servings, b.Difficulty, b.CategoryId);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());

        // Xác nhận giá trị của A còn nguyên, không bị B ghi đè.
        await using var verify = fixture.NewContext();
        var current = await verify.Recipes.SingleAsync(r => r.Id == id);
        Assert.Equal("Phở bò đặc biệt A", current.Title);
    }

    [Fact]
    public async Task Conflict_resolution_reload_and_reapply_succeeds()
    {
        var id = await SeedRecipeAsync();

        await using var ctxA = fixture.NewContext();
        await using var ctxB = fixture.NewContext();
        var a = await ctxA.Recipes.SingleAsync(r => r.Id == id);
        var b = await ctxB.Recipes.SingleAsync(r => r.Id == id);

        a.UpdateDetails("A wins", a.Slug, a.Description, a.Instructions,
            a.PrepTimeMinutes, a.CookTimeMinutes, a.Servings, a.Difficulty, a.CategoryId);
        await ctxA.SaveChangesAsync();

        b.UpdateDetails("B attempt", b.Slug, b.Description, b.Instructions,
            b.PrepTimeMinutes, b.CookTimeMinutes, b.Servings, b.Difficulty, b.CategoryId);

        try
        {
            await ctxB.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Mẫu C2 sẽ dùng: trả 422 kèm bản mới nhất để client reload, hoặc reapply có chủ đích.
            await using var fresh = fixture.NewContext();
            var latest = await fresh.Recipes.SingleAsync(r => r.Id == id);
            latest.UpdateDetails("B retried after reload", latest.Slug, latest.Description, latest.Instructions,
                latest.PrepTimeMinutes, latest.CookTimeMinutes, latest.Servings, latest.Difficulty, latest.CategoryId);
            await fresh.SaveChangesAsync();
        }

        await using var verify = fixture.NewContext();
        var current = await verify.Recipes.SingleAsync(r => r.Id == id);
        Assert.Equal("B retried after reload", current.Title);
    }

    [Fact]
    public async Task Nested_create_failure_rolls_back_whole_aggregate()
    {
        Guid recipeId;
        await using (var ctx = fixture.NewContext())
        {
            var recipe = NewSampleRecipe();
            recipe.AddIngredient("Bánh phở", 500, "g", null);
            recipe.AddStep("Trần bánh", "Trần bánh phở qua nước sôi.", 2, null);
            recipeId = recipe.Id;

            ctx.Recipes.Add(recipe);

            // Mô phỏng lỗi giữa chừng (ví dụ vi phạm ràng buộc sau đó): buộc rollback.
            await using var tx = await ctx.Database.BeginTransactionAsync();
            await ctx.SaveChangesAsync();
            await tx.RollbackAsync(); // KHÔNG commit
        }

        await using var verify = fixture.NewContext();
        var exists = await verify.Recipes.AnyAsync(r => r.Id == recipeId);
        Assert.False(exists); // rollback -> không có recipe lẫn ingredient/step nào tồn tại
    }

    [Fact]
    public void Publish_requires_at_least_one_ingredient_and_one_step()
    {
        // D07: kiểm tra bất biến publish ở Domain (không cần DB).
        var recipe = NewSampleRecipe();
        var ex = Assert.Throws<CulinaryBlog.Domain.Common.DomainException>(() => recipe.Publish());
        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", ex.Code);

        recipe.AddIngredient("Bánh phở", 500, "g", null);
        recipe.AddStep("Trần bánh", "Trần bánh phở.", null, null);
        recipe.Publish();
        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
    }
}
