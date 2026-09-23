using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

public sealed class FakeAuthoringRecipeRepository : IRecipeRepository
{
    public readonly List<Recipe> Recipes = [];
    public readonly List<Guid> Categories = [];

    public Task<Recipe?> FindForWriteAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Recipes.FirstOrDefault(r => r.Id == id));

    public Task<Recipe?> FindBySlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Recipes.FirstOrDefault(r => r.Slug == slug));

    public Task<IReadOnlyList<string>> FindUsedSlugsAsync(string baseSlug, Guid? excludeRecipeId, CancellationToken ct)
    {
        var slugs = Recipes
            .Where(r => (r.Slug == baseSlug || r.Slug.StartsWith(baseSlug + "-")) && (excludeRecipeId == null || r.Id != excludeRecipeId))
            .Select(r => r.Slug)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(slugs);
    }

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct) =>
        Task.FromResult(Categories.Contains(categoryId));

    public void Add(Recipe recipe) => Recipes.Add(recipe);

    public void RemoveIngredient(RecipeIngredient ingredient) { }

    public void RemoveStep(RecipeStep step) { }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}

public sealed class FakeTestCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "author-123";
    public bool IsInRole(string role) => role == "Author";
}

public sealed class RecipeAuthoringAndCrudTests
{
    private readonly FakeAuthoringRecipeRepository _repo = new();
    private readonly FakeTestCurrentUser _user = new();
    private readonly Guid _catId = Guid.NewGuid();

    public RecipeAuthoringAndCrudTests()
    {
        _repo.Categories.Add(_catId);
    }

    [Fact]
    public async Task CreateRecipe_creates_draft_with_unique_slug_and_authenticated_author()
    {
        var handler = new CreateRecipeHandler(_repo, _user);
        var cmd = new CreateRecipeCommand(
            Title: "Bún bò Huế gia truyền",
            Description: "Hương vị cay nồng chuẩn cố đô",
            Instructions: "Hầm xương, nêm mắm ruốc",
            PrepTimeMinutes: 30,
            CookTimeMinutes: 120,
            Servings: 6,
            Difficulty: RecipeDifficulty.Hard,
            CategoryId: _catId,
            Nutrition: new NutritionDto(550, 35, 45, 18, 4, 1200));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bún bò Huế gia truyền", result.Title);
        Assert.Equal("bun-bo-hue-gia-truyen", result.Slug);
        Assert.Equal(_user.UserId, result.AuthorId);
        Assert.Equal(RecipeStatus.Draft, result.Status);
        Assert.Equal(RecipeDifficulty.Hard, result.Difficulty);
        Assert.Single(_repo.Recipes);
    }

    [Fact]
    public async Task CreateRecipe_unauthorized_user_throws_401()
    {
        _user.UserId = null;
        var handler = new CreateRecipeHandler(_repo, _user);
        var cmd = new CreateRecipeCommand(
            Title: "Bún bò Huế",
            Description: "Mô tả",
            Instructions: null,
            PrepTimeMinutes: 15,
            CookTimeMinutes: 30,
            Servings: 2,
            Difficulty: RecipeDifficulty.Medium,
            CategoryId: _catId,
            Nutrition: null);

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(cmd, CancellationToken.None));
        Assert.Equal(401, ex.Status);
    }

    [Fact]
    public async Task UpdateRecipe_forbidden_when_not_author_throws_403()
    {
        var recipe = Recipe.CreateDraft(
            "Phở gà ta", "pho-ga-ta", "Mô tả", null,
            15, 45, 4, RecipeDifficulty.Medium, _catId, "another-author");
        _repo.Add(recipe);

        var handler = new UpdateRecipeHandler(_repo, _user); // Current user is "author-123" != "another-author"
        var cmd = new UpdateRecipeCommand(
            recipe.Id, "Phở gà ta đổi tên", "Mô tả mới", null,
            20, 50, 4, RecipeDifficulty.Medium, _catId, null, null);

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(cmd, CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
    }

    [Fact]
    public async Task AddIngredient_and_DeleteIngredient_reindexes_order()
    {
        var recipe = Recipe.CreateDraft(
            "Bánh mì pate", "banh-mi-pate", "Bánh mì giòn", null,
            10, 5, 1, RecipeDifficulty.Easy, _catId, _user.UserId!);
        _repo.Add(recipe);

        var addHandler = new AddIngredientHandler(_repo, _user);
        var ing1 = await addHandler.Handle(new AddIngredientCommand(recipe.Id, "Bánh mì", 1, "ổ", null), CancellationToken.None);
        var ing2 = await addHandler.Handle(new AddIngredientCommand(recipe.Id, "Pate", 50, "g", null), CancellationToken.None);
        var ing3 = await addHandler.Handle(new AddIngredientCommand(recipe.Id, "Dưa leo", 1, "trái", null), CancellationToken.None);

        Assert.Equal(3, recipe.Ingredients.Count);
        Assert.Equal(0, ing1.OrderIndex);
        Assert.Equal(1, ing2.OrderIndex);
        Assert.Equal(2, ing3.OrderIndex);

        var deleteHandler = new DeleteIngredientHandler(_repo, _user);
        await deleteHandler.Handle(new DeleteIngredientCommand(recipe.Id, ing2.Id), CancellationToken.None);

        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Equal(0, recipe.Ingredients[0].OrderIndex);
        Assert.Equal(1, recipe.Ingredients[1].OrderIndex);
        Assert.Equal("Dưa leo", recipe.Ingredients[1].Name);
    }

    [Fact]
    public async Task AddStep_and_ReorderSteps_updates_step_numbers()
    {
        var recipe = Recipe.CreateDraft(
            "Trà đào cam sả", "tra-dao-cam-sa", "Thức uống giải nhiệt", null,
            10, 10, 2, RecipeDifficulty.Easy, _catId, _user.UserId!);
        _repo.Add(recipe);

        var addStepHandler = new AddStepHandler(_repo, _user);
        var s1 = await addStepHandler.Handle(new AddStepCommand(recipe.Id, "Đun sả", "Đun sôi nước với sả", 5, null), CancellationToken.None);
        var s2 = await addStepHandler.Handle(new AddStepCommand(recipe.Id, "Ủ trà", "Ủ trà đen với nước sả", 10, null), CancellationToken.None);
        var s3 = await addStepHandler.Handle(new AddStepCommand(recipe.Id, "Pha chế", "Thêm đào ngâm và cam", null, null), CancellationToken.None);

        Assert.Equal(3, recipe.Steps.Count);
        Assert.Equal(1, s1.StepNumber);
        Assert.Equal(2, s2.StepNumber);
        Assert.Equal(3, s3.StepNumber);

        var uow = new FakeUnitOfWork();
        var reorderHandler = new ReorderStepsHandler(_repo, _user, uow);
        // Đảo ngược thứ tự: s3, s2, s1
        var reordered = await reorderHandler.Handle(new ReorderStepsCommand(recipe.Id, [s3.Id, s2.Id, s1.Id]), CancellationToken.None);

        Assert.Equal(s3.Id, reordered[0].Id);
        Assert.Equal(1, reordered[0].StepNumber);
        Assert.Equal(s2.Id, reordered[1].Id);
        Assert.Equal(2, reordered[1].StepNumber);
        Assert.Equal(s1.Id, reordered[2].Id);
        Assert.Equal(3, reordered[2].StepNumber);
    }

    [Fact]
    public void Publish_recipe_requires_at_least_one_ingredient_and_one_step_C02()
    {
        var recipe = Recipe.CreateDraft(
            "Cơm chiên dương châu", "com-chien-duong-chau", "Mô tả", null,
            10, 15, 2, RecipeDifficulty.Medium, _catId, _user.UserId!);

        // Chưa có nguyên liệu và bước: Publish phải ném DomainException RECIPE_PUBLISH_INCOMPLETE (C02)
        var ex = Assert.Throws<DomainException>(() => recipe.Publish());
        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", ex.Code);

        // Thêm 1 nguyên liệu nhưng chưa có bước: vẫn lỗi
        recipe.AddIngredient("Cơm nguội", 2, "chén", null);
        var ex2 = Assert.Throws<DomainException>(() => recipe.Publish());
        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", ex2.Code);

        // Thêm 1 bước: đủ điều kiện publish
        recipe.AddStep("Chiên cơm", "Xào nhân rồi đảo đều cơm", 10, null);
        recipe.Publish();

        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
    }
}
