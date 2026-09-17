using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConcurrencySpike;

/// <summary>
/// Chứng minh tiêu chí nghiệm thu D24/TV4:
///  - "đúng 1 primary cho mỗi recipe": two writers cùng PATCH set primary khác nhau
///    (Race A : image#1, Race B : image#2) -> writer chậm hơn bị DbUpdateException do
///    partial unique index ux_recipe_images_one_primary, KHÔNG xảy ra trạng thái 2 primary.
///  - Kết hợp RowVersion (D19): mọi update ảnh đều qua aggregate + unique index phòng thủ
///    lớp DB (defense in depth), không chỉ dựa vào logic ứng dụng.
/// </summary>
public sealed class RecipeImagePrimaryConcurrencyTests(SpikeDbFixture fixture) : IClassFixture<SpikeDbFixture>
{
    private async Task<Guid> SeedRecipeWithTwoImagesAsync()
    {
        await using var ctx = fixture.NewContext();
        var recipe = Recipe.CreateDraft(
            title: "Gỏi cuốn tôm thịt",
            slug: $"goi-cuon-{Guid.NewGuid():N}",
            description: "Gỏi cuốn thanh mát.",
            instructions: null,
            prepTimeMinutes: 15,
            cookTimeMinutes: 0,
            servings: 2,
            difficulty: RecipeDifficulty.Easy,
            categoryId: fixture.CategoryId,
            authorId: fixture.AuthorId);

        recipe.AddIngredient("Bánh tráng", 10, "cái", null);
        recipe.AddStep("Cuốn", "Cuốn bánh tráng với nhân.", null, null);

        recipe.AddImage("recipes/x/first.jpg", null);
        recipe.AddImage("recipes/x/second.jpg", null);
        ctx.Recipes.Add(recipe);
        await ctx.SaveChangesAsync();

        await using var setup = fixture.NewContext();
        var seeded = await setup.Recipes
            .Include(r => r.Images)
            .SingleAsync(r => r.Id == recipe.Id);
        Assert.Equal(2, seeded.Images.Count);
        Assert.Equal(1, seeded.Images.Count(i => i.IsPrimary));
        return recipe.Id;
    }

    [Fact]
    public async Task Concurrent_primary_setters_never_produce_two_primaries()
    {
        var recipeId = await SeedRecipeWithTwoImagesAsync();

        // Hai writer độc lập đọc cùng trạng thái.
        await using var writerA = fixture.NewContext();
        await using var writerB = fixture.NewContext();
        var recipeA = await writerA.Recipes.Include(r => r.Images).SingleAsync(r => r.Id == recipeId);
        var recipeB = await writerB.Recipes.Include(r => r.Images).SingleAsync(r => r.Id == recipeId);

        var imageA = recipeA.Images.First(i => i.IsPrimary);
        var imageA2 = recipeA.Images.First(i => !i.IsPrimary);

        // A giữ primary cũ, B chuyển primary sang ảnh kia. Cả hai chạy "đồng thời".
        var taskA = Task.Run(() =>
        {
            recipeA.SetPrimaryImage(imageA2.Id);
            return writerA.SaveChangesAsync();
        });
        var taskB = Task.Run(() =>
        {
            recipeB.SetPrimaryImage(imageA.Id);
            return writerB.SaveChangesAsync();
        });

        int failures = 0;
        try { await taskA; } catch (DbUpdateException) { failures++; }
        try { await taskB; } catch (DbUpdateException) { failures++; }

        // Ít nhất 1 writer bị từ chối => không ai ghi đè im lặng thành 2 primary.
        Assert.True(failures >= 1, "Unique partial index phải chặn ít nhất 1 writer tạo primary thứ 2.");

        // Trạng thái cuối: đúng 1 primary, 2 ảnh nguyên vẹn.
        await using var verify = fixture.NewContext();
        var current = await verify.Recipes.Include(r => r.Images).SingleAsync(r => r.Id == recipeId);
        Assert.Equal(2, current.Images.Count);
        Assert.Equal(1, current.Images.Count(i => i.IsPrimary));
    }
}
