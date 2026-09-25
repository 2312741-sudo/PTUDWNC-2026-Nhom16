using CulinaryBlog.Application;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class FakeOwnedRecipeRepository : IRecipeRepository
{
    public readonly List<Recipe> Store = [];

    public Task<Recipe?> FindForWriteAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(r => r.Id == id));

    public Task<Recipe?> FindBySlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(r => r.Slug == slug));

    public Task<IReadOnlyList<string>> FindUsedSlugsAsync(string baseSlug, Guid? excludeRecipeId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(true);

    public void Add(Recipe recipe) => Store.Add(recipe);

    public void RemoveIngredient(RecipeIngredient ingredient) { }

    public void RemoveStep(RecipeStep step) { }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class RecipeLifecycleHandlerTests
{
    private static Recipe NewDraftRecipe(string authorId = "author-1", bool withIngredientsAndSteps = false)
    {
        var recipe = Recipe.CreateDraft(
            title: "Bánh xèo miền Tây",
            slug: $"banh-xeo-{Guid.NewGuid():N}",
            description: "Bánh xèo giòn rụm.",
            instructions: null,
            prepTimeMinutes: 20,
            cookTimeMinutes: 30,
            servings: 2,
            difficulty: RecipeDifficulty.Easy,
            categoryId: Guid.NewGuid(),
            authorId: authorId);
        if (withIngredientsAndSteps)
        {
            recipe.AddIngredient("Bột bánh xèo", 200, "g", null);
            recipe.AddStep("Trộn bột", "Trộn bột với nước cho đều.", null, null);
        }
        return recipe;
    }

    // ----- Publish (D3.1, FR-RCP-005, C02) -----

    [Fact]
    public async Task Publish_recipe_with_ingredient_and_step_succeeds()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var dto = await handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(nameof(RecipeStatus.Published), dto.Status);
        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
        Assert.Equal(recipe.PublishedAt, dto.PublishedAt);
    }

    [Fact]
    public async Task Publish_missing_ingredient_throws_422_incomplete()
    {
        var recipe = NewDraftRecipe();
        recipe.AddStep("Trộn bột", "Trộn bột với nước.", null, null);   // có step, thiếu ingredient
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new PublishRecipeCommand(recipe.Id), CancellationToken.None));
        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", ex.Code);
    }

    [Fact]
    public async Task Publish_missing_step_throws_422_incomplete()
    {
        var recipe = NewDraftRecipe();
        recipe.AddIngredient("Bột bánh xèo", 200, "g", null);   // có ingredient, thiếu step
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new PublishRecipeCommand(recipe.Id), CancellationToken.None));
        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", ex.Code);
    }

    [Fact]
    public async Task Publish_is_idempotent_when_already_published()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var first = await handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None);
        var publishedAt = recipe.PublishedAt;
        var second = await handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(nameof(RecipeStatus.Published), second.Status);
        Assert.Equal(publishedAt, recipe.PublishedAt);   // PublishedAt giữ nguyên sau publish lần 2
        Assert.Equal(first.PublishedAt, second.PublishedAt);
    }

    [Fact]
    public async Task Publish_non_owner_throws_403()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-9"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new PublishRecipeCommand(recipe.Id), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
    }

    [Fact]
    public async Task Publish_admin_can_override_ownership()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-9", isAdmin: true));

        var dto = await handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(nameof(RecipeStatus.Published), dto.Status);
    }

    [Fact]
    public async Task Publish_recipe_not_found_throws_404()
    {
        var repo = new FakeOwnedRecipeRepository();   // empty
        var handler = new PublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new PublishRecipeCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }

    [Fact]
    public async Task Publish_validator_rejects_empty_recipe_id()
    {
        var validator = new PublishRecipeValidator();
        var result = await validator.ValidateAsync(new PublishRecipeCommand(Guid.Empty));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PublishRecipeCommand.RecipeId));
    }

    // ----- Unpublish (D3.2, FR-RCP-006) -----

    [Fact]
    public async Task Unpublish_changes_published_to_draft()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        recipe.Publish();
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new UnpublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var dto = await handler.Handle(new UnpublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(nameof(RecipeStatus.Draft), dto.Status);
        Assert.Equal(RecipeStatus.Draft, recipe.Status);
    }

    [Fact]
    public async Task Unpublish_is_idempotent_when_already_draft()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new UnpublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var dto = await handler.Handle(new UnpublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(nameof(RecipeStatus.Draft), dto.Status);
    }

    [Fact]
    public async Task Unpublish_non_owner_throws_403()
    {
        var recipe = NewDraftRecipe(withIngredientsAndSteps: true);
        recipe.Publish();
        var repo = new FakeOwnedRecipeRepository();
        repo.Store.Add(recipe);
        var handler = new UnpublishRecipeHandler(repo, new FakeCurrentUser("author-9"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UnpublishRecipeCommand(recipe.Id), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
    }

    [Fact]
    public async Task Unpublish_recipe_not_found_throws_404()
    {
        var repo = new FakeOwnedRecipeRepository();   // empty
        var handler = new UnpublishRecipeHandler(repo, new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UnpublishRecipeCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task Unpublish_validator_rejects_empty_recipe_id()
    {
        var validator = new UnpublishRecipeValidator();
        var result = await validator.ValidateAsync(new UnpublishRecipeCommand(Guid.Empty));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UnpublishRecipeCommand.RecipeId));
    }
}
