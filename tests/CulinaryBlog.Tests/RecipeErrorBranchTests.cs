using CulinaryBlog.Application;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// Task 3 / nhóm D7-D11: nhánh lỗi và quyền của Recipes.cs.
/// Dùng lại FakeAuthoringRecipeRepository, FakeUnitOfWork, FakeTestCurrentUser (RecipeAuthoringAndCrudTests.cs)
/// và FakeCurrentUser (RecipeImageTests.cs).
///
/// LƯU Ý về id con thuộc recipe khác: hiện Update* ném DomainException (API map 400) còn Delete* ném
/// AppException 404. Đây là hành vi ĐANG CÓ, chưa phải thiết kế đã chốt (xem PLAN_TUAN4.local.md, mục cần hỏi Tâm).
/// Điểm khẳng định chính: recipe kia KHÔNG bị thay đổi sau khi bị từ chối.
/// </summary>
public sealed class RecipeErrorBranchTests
{
    private const string Owner = "author-1";
    private const string Other = "author-2";

    private readonly FakeAuthoringRecipeRepository _repo = new();

    private Recipe Seed(string authorId, string slug, int steps = 3)
    {
        var recipe = Recipe.CreateDraft(
            "Công thức " + slug, slug, "Mô tả", null,
            10, 20, 2, RecipeDifficulty.Easy, Guid.NewGuid(), authorId);
        recipe.AddIngredient("Hành", 1, "củ", "ghi chú 1");
        recipe.AddIngredient("Tỏi", 2, "tép", null);
        for (var i = 1; i <= steps; i++)
            recipe.AddStep($"Bước {i}", $"Mô tả bước {i}", 5, null);
        _repo.Add(recipe);
        return recipe;
    }

    private static UpdateIngredientCommand UpdIng(Guid recipeId, Guid ingredientId) =>
        new(recipeId, ingredientId, "Tên mới", 9, "kg", "note mới");

    private static UpdateStepCommand UpdStep(Guid recipeId, Guid stepId) =>
        new(recipeId, stepId, "Tiêu đề mới", "Mô tả mới", 99, null);

    // ----- D7: UpdateIngredient -----

    [Fact]
    public async Task UpdateIngredient_owner_succeeds_and_keeps_order()
    {
        var recipe = Seed(Owner, "d7-ok");
        var target = recipe.Ingredients[1];
        var handler = new UpdateIngredientHandler(_repo, new FakeCurrentUser(Owner));

        var dto = await handler.Handle(UpdIng(recipe.Id, target.Id), CancellationToken.None);

        Assert.Equal("Tên mới", dto.Name);
        Assert.Equal(9m, dto.Quantity);
        Assert.Equal("kg", dto.Unit);
        Assert.Equal("note mới", dto.Notes);
        Assert.Equal(1, dto.OrderIndex);
        Assert.Equal(recipe.Id, dto.RecipeId);
    }

    [Fact]
    public async Task UpdateIngredient_id_of_other_recipe_is_rejected_and_other_recipe_unchanged()
    {
        var mine = Seed(Owner, "d7-mine");
        var theirs = Seed(Other, "d7-theirs");
        var foreignIngredient = theirs.Ingredients[0];
        var before = (foreignIngredient.Name, foreignIngredient.Quantity, foreignIngredient.Unit, foreignIngredient.Notes);
        var handler = new UpdateIngredientHandler(_repo, new FakeCurrentUser(Owner));

        // Hành vi hiện tại: DomainException (API map 400), chưa phải thiết kế đã chốt.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(UpdIng(mine.Id, foreignIngredient.Id), CancellationToken.None));

        Assert.Equal("INGREDIENT_NOT_FOUND", ex.Code);
        Assert.Equal(before, (foreignIngredient.Name, foreignIngredient.Quantity, foreignIngredient.Unit, foreignIngredient.Notes));
        Assert.All(mine.Ingredients, i => Assert.NotEqual("Tên mới", i.Name));
    }

    [Fact]
    public async Task UpdateIngredient_unknown_id_throws_ingredient_not_found()
    {
        var recipe = Seed(Owner, "d7-unknown");
        var handler = new UpdateIngredientHandler(_repo, new FakeCurrentUser(Owner));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(UpdIng(recipe.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("INGREDIENT_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task UpdateIngredient_non_owner_throws_403_and_data_unchanged()
    {
        var recipe = Seed(Owner, "d7-403");
        var target = recipe.Ingredients[0];
        var handler = new UpdateIngredientHandler(_repo, new FakeCurrentUser(Other));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(UpdIng(recipe.Id, target.Id), CancellationToken.None));

        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
        Assert.Equal("Hành", target.Name);
    }

    [Fact]
    public async Task UpdateIngredient_admin_not_owner_succeeds()
    {
        var recipe = Seed(Owner, "d7-admin");
        var handler = new UpdateIngredientHandler(_repo, new FakeCurrentUser("admin-1", isAdmin: true));

        var dto = await handler.Handle(UpdIng(recipe.Id, recipe.Ingredients[0].Id), CancellationToken.None);

        Assert.Equal("Tên mới", dto.Name);
    }

    [Fact]
    public async Task UpdateIngredient_missing_recipe_404_and_anonymous_401()
    {
        var notFound = new UpdateIngredientHandler(_repo, new FakeCurrentUser(Owner));
        var ex404 = await Assert.ThrowsAsync<AppException>(() =>
            notFound.Handle(UpdIng(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(404, ex404.Status);
        Assert.Equal("recipe.not_found", ex404.Code);

        var anonymous = new UpdateIngredientHandler(_repo, new FakeTestCurrentUser { UserId = null });
        var ex401 = await Assert.ThrowsAsync<AppException>(() =>
            anonymous.Handle(UpdIng(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(401, ex401.Status);
    }

    [Theory]
    [InlineData("", 1, "g", "n", "Name")]
    [InlineData("   ", 1, "g", "n", "Name")]
    [InlineData("x", 0, "g", "n", "Quantity")]
    [InlineData("x", -1, "g", "n", "Quantity")]
    public async Task UpdateIngredient_validator_rejects_invalid_fields(
        string name, double quantity, string unit, string notes, string property)
    {
        var cmd = new UpdateIngredientCommand(Guid.NewGuid(), Guid.NewGuid(), name, (decimal)quantity, unit, notes);
        var result = await new UpdateIngredientValidator().ValidateAsync(cmd);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public async Task UpdateIngredient_validator_rejects_too_long_and_accepts_null_quantity()
    {
        var validator = new UpdateIngredientValidator();

        var tooLong = new UpdateIngredientCommand(
            Guid.NewGuid(), Guid.NewGuid(), new string('a', 201), 1, new string('u', 51), new string('n', 501));
        var bad = await validator.ValidateAsync(tooLong);
        Assert.Contains(bad.Errors, e => e.PropertyName == "Name");
        Assert.Contains(bad.Errors, e => e.PropertyName == "Unit");
        Assert.Contains(bad.Errors, e => e.PropertyName == "Notes");

        var ok = await validator.ValidateAsync(
            new UpdateIngredientCommand(Guid.NewGuid(), Guid.NewGuid(), "Hành", null, null, null));
        Assert.True(ok.IsValid);
    }

    // ----- D8: UpdateStep -----

    [Fact]
    public async Task UpdateStep_owner_succeeds_and_keeps_step_number()
    {
        var recipe = Seed(Owner, "d8-ok");
        var target = recipe.Steps[1];
        var handler = new UpdateStepHandler(_repo, new FakeCurrentUser(Owner));

        var dto = await handler.Handle(UpdStep(recipe.Id, target.Id), CancellationToken.None);

        Assert.Equal("Tiêu đề mới", dto.Title);
        Assert.Equal("Mô tả mới", dto.Description);
        Assert.Equal(99, dto.TimerMinutes);
        Assert.Equal(2, dto.StepNumber);
        Assert.Equal(recipe.Id, dto.RecipeId);
    }

    [Fact]
    public async Task UpdateStep_id_of_other_recipe_is_rejected_and_other_recipe_unchanged()
    {
        var mine = Seed(Owner, "d8-mine");
        var theirs = Seed(Other, "d8-theirs");
        var foreignStep = theirs.Steps[0];
        var before = (foreignStep.Title, foreignStep.Description, foreignStep.TimerMinutes, foreignStep.StepNumber);
        var handler = new UpdateStepHandler(_repo, new FakeCurrentUser(Owner));

        // Hành vi hiện tại: DomainException (API map 400), chưa phải thiết kế đã chốt.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(UpdStep(mine.Id, foreignStep.Id), CancellationToken.None));

        Assert.Equal("STEP_NOT_FOUND", ex.Code);
        Assert.Equal(before, (foreignStep.Title, foreignStep.Description, foreignStep.TimerMinutes, foreignStep.StepNumber));
        Assert.All(mine.Steps, s => Assert.NotEqual("Tiêu đề mới", s.Title));
    }

    [Fact]
    public async Task UpdateStep_non_owner_403_and_admin_succeeds()
    {
        var recipe = Seed(Owner, "d8-auth");
        var target = recipe.Steps[0];

        var forbidden = new UpdateStepHandler(_repo, new FakeCurrentUser(Other));
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            forbidden.Handle(UpdStep(recipe.Id, target.Id), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
        Assert.Equal("Bước 1", target.Title);

        var admin = new UpdateStepHandler(_repo, new FakeCurrentUser("admin-1", isAdmin: true));
        var dto = await admin.Handle(UpdStep(recipe.Id, target.Id), CancellationToken.None);
        Assert.Equal("Tiêu đề mới", dto.Title);
    }

    [Fact]
    public async Task UpdateStep_validator_rules()
    {
        var validator = new UpdateStepValidator();
        var id = Guid.NewGuid();

        var bad = await validator.ValidateAsync(new UpdateStepCommand(id, id, "", "", -1, new string('u', 501)));
        Assert.Contains(bad.Errors, e => e.PropertyName == "Title");
        Assert.Contains(bad.Errors, e => e.PropertyName == "Description");
        Assert.Contains(bad.Errors, e => e.PropertyName == "TimerMinutes");
        Assert.Contains(bad.Errors, e => e.PropertyName == "ImageUrl");

        var zeroTimer = await validator.ValidateAsync(new UpdateStepCommand(id, id, "Tiêu đề", "Mô tả", 0, null));
        Assert.True(zeroTimer.IsValid);
    }

    // ----- D9: DeleteStep / DeleteIngredient -----

    [Fact]
    public async Task DeleteStep_middle_step_renumbers_remaining_steps_1_to_N()
    {
        var recipe = Seed(Owner, "d9-ok");
        var first = recipe.Steps[0];
        var middle = recipe.Steps[1];
        var last = recipe.Steps[2];
        var handler = new DeleteStepHandler(_repo, new FakeCurrentUser(Owner), new FakeUnitOfWork());

        await handler.Handle(new DeleteStepCommand(recipe.Id, middle.Id), CancellationToken.None);

        var remaining = recipe.Steps.OrderBy(s => s.StepNumber).ToList();
        Assert.Equal([first.Id, last.Id], remaining.Select(s => s.Id));
        Assert.Equal([1, 2], remaining.Select(s => s.StepNumber));
    }

    [Fact]
    public async Task DeleteStep_id_of_other_recipe_throws_404_and_other_recipe_unchanged()
    {
        var mine = Seed(Owner, "d9-mine");
        var theirs = Seed(Other, "d9-theirs");
        var foreignStep = theirs.Steps[0];
        var handler = new DeleteStepHandler(_repo, new FakeCurrentUser(Owner), new FakeUnitOfWork());

        // Hành vi hiện tại: AppException 404 (khác Update là DomainException 400), chưa phải thiết kế đã chốt.
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new DeleteStepCommand(mine.Id, foreignStep.Id), CancellationToken.None));

        Assert.Equal(404, ex.Status);
        Assert.Equal("step.not_found", ex.Code);
        Assert.Equal(3, theirs.Steps.Count);
        Assert.Contains(theirs.Steps, s => s.Id == foreignStep.Id);
        Assert.Equal([1, 2, 3], theirs.Steps.Select(s => s.StepNumber));
        Assert.Equal(3, mine.Steps.Count);
    }

    [Fact]
    public async Task DeleteStep_unknown_id_404_and_non_owner_403_keeps_step()
    {
        var recipe = Seed(Owner, "d9-errors");

        var owner = new DeleteStepHandler(_repo, new FakeCurrentUser(Owner), new FakeUnitOfWork());
        var ex404 = await Assert.ThrowsAsync<AppException>(() =>
            owner.Handle(new DeleteStepCommand(recipe.Id, Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(404, ex404.Status);

        var stranger = new DeleteStepHandler(_repo, new FakeCurrentUser(Other), new FakeUnitOfWork());
        var ex403 = await Assert.ThrowsAsync<AppException>(() =>
            stranger.Handle(new DeleteStepCommand(recipe.Id, recipe.Steps[0].Id), CancellationToken.None));
        Assert.Equal(403, ex403.Status);
        Assert.Equal("recipe.forbidden", ex403.Code);
        Assert.Equal(3, recipe.Steps.Count);
    }

    [Fact]
    public async Task DeleteIngredient_id_of_other_recipe_throws_404_and_other_recipe_unchanged()
    {
        var mine = Seed(Owner, "d9i-mine");
        var theirs = Seed(Other, "d9i-theirs");
        var foreignIngredient = theirs.Ingredients[0];
        var handler = new DeleteIngredientHandler(_repo, new FakeCurrentUser(Owner));

        // Hành vi hiện tại: AppException 404, chưa phải thiết kế đã chốt.
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new DeleteIngredientCommand(mine.Id, foreignIngredient.Id), CancellationToken.None));

        Assert.Equal(404, ex.Status);
        Assert.Equal("ingredient.not_found", ex.Code);
        Assert.Equal(2, theirs.Ingredients.Count);
        Assert.Contains(theirs.Ingredients, i => i.Id == foreignIngredient.Id);
        Assert.Equal(2, mine.Ingredients.Count);
    }

    // ----- D10: DeleteRecipe -----

    [Fact]
    public async Task DeleteRecipe_owner_soft_deletes_without_row_version_or_with_empty_one()
    {
        var a = Seed(Owner, "d10-a");
        var b = Seed(Owner, "d10-b");
        var handler = new DeleteRecipeHandler(_repo, new FakeCurrentUser(Owner));

        await handler.Handle(new DeleteRecipeCommand(a.Id, null), CancellationToken.None);
        await handler.Handle(new DeleteRecipeCommand(b.Id, ""), CancellationToken.None);

        Assert.True(a.IsDeleted);
        Assert.True(b.IsDeleted);
    }

    [Fact]
    public async Task DeleteRecipe_matching_row_version_succeeds_but_stale_one_throws_422()
    {
        var recipe = Seed(Owner, "d10-rv");
        recipe.RowVersion = [1, 2, 3, 4];
        var handler = new DeleteRecipeHandler(_repo, new FakeCurrentUser(Owner));

        var stale = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new DeleteRecipeCommand(recipe.Id, Convert.ToBase64String([9, 9, 9, 9])), CancellationToken.None));
        Assert.Equal(422, stale.Status);
        Assert.Equal("recipe.concurrency_conflict", stale.Code);
        Assert.False(recipe.IsDeleted);

        await handler.Handle(
            new DeleteRecipeCommand(recipe.Id, Convert.ToBase64String(recipe.RowVersion)), CancellationToken.None);
        Assert.True(recipe.IsDeleted);
    }

    [Fact]
    public async Task DeleteRecipe_non_owner_403_keeps_recipe_and_admin_can_delete()
    {
        var recipe = Seed(Owner, "d10-403");

        var stranger = new DeleteRecipeHandler(_repo, new FakeCurrentUser(Other));
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            stranger.Handle(new DeleteRecipeCommand(recipe.Id, null), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
        Assert.False(recipe.IsDeleted);

        var admin = new DeleteRecipeHandler(_repo, new FakeCurrentUser("admin-1", isAdmin: true));
        await admin.Handle(new DeleteRecipeCommand(recipe.Id, null), CancellationToken.None);
        Assert.True(recipe.IsDeleted);
    }

    [Fact]
    public async Task DeleteRecipe_missing_recipe_throws_404()
    {
        var handler = new DeleteRecipeHandler(_repo, new FakeCurrentUser(Owner));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new DeleteRecipeCommand(Guid.NewGuid(), null), CancellationToken.None));

        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }

    [Fact]
    public async Task DeleteRecipe_validator_rejects_empty_id()
    {
        var result = await new DeleteRecipeValidator().ValidateAsync(new DeleteRecipeCommand(Guid.Empty, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteRecipeCommand.Id));
    }

    // ----- D11: GetRecipeBySlug -----

    [Fact]
    public async Task GetBySlug_published_is_visible_to_anonymous_and_strangers()
    {
        var recipe = Seed(Owner, "d11-pub");
        recipe.Publish();

        var anonymous = new GetRecipeBySlugHandler(_repo, new FakeTestCurrentUser { UserId = null });
        var stranger = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Other));

        Assert.Equal(recipe.Id, (await anonymous.Handle(new GetRecipeBySlugQuery("d11-pub"), CancellationToken.None)).Id);
        Assert.Equal(recipe.Id, (await stranger.Handle(new GetRecipeBySlugQuery("d11-pub"), CancellationToken.None)).Id);
    }

    [Fact]
    public async Task GetBySlug_draft_of_other_user_returns_404_not_403()
    {
        Seed(Owner, "d11-draft");
        var handler = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Other));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new GetRecipeBySlugQuery("d11-draft"), CancellationToken.None));

        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }

    [Fact]
    public async Task GetBySlug_draft_when_anonymous_returns_404()
    {
        Seed(Owner, "d11-anon");
        var handler = new GetRecipeBySlugHandler(_repo, new FakeTestCurrentUser { UserId = null });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new GetRecipeBySlugQuery("d11-anon"), CancellationToken.None));

        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task GetBySlug_draft_is_visible_to_owner_and_admin()
    {
        var recipe = Seed(Owner, "d11-own");

        var owner = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Owner));
        var admin = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser("admin-1", isAdmin: true));

        var dtoOwner = await owner.Handle(new GetRecipeBySlugQuery("d11-own"), CancellationToken.None);
        var dtoAdmin = await admin.Handle(new GetRecipeBySlugQuery("d11-own"), CancellationToken.None);

        Assert.Equal(recipe.Id, dtoOwner.Id);
        Assert.Equal(nameof(RecipeStatus.Draft), dtoOwner.Status);
        Assert.Equal(recipe.Id, dtoAdmin.Id);
    }

    [Fact]
    public async Task GetBySlug_archived_of_other_user_returns_404_but_owner_can_see()
    {
        var recipe = Seed(Owner, "d11-arch");
        recipe.Archive();

        var stranger = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Other));
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            stranger.Handle(new GetRecipeBySlugQuery("d11-arch"), CancellationToken.None));
        Assert.Equal(404, ex.Status);

        var owner = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Owner));
        var dto = await owner.Handle(new GetRecipeBySlugQuery("d11-arch"), CancellationToken.None);
        Assert.Equal(nameof(RecipeStatus.Archived), dto.Status);
    }

    [Fact]
    public async Task GetBySlug_unknown_slug_returns_404()
    {
        var handler = new GetRecipeBySlugHandler(_repo, new FakeCurrentUser(Owner));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new GetRecipeBySlugQuery("khong-ton-tai"), CancellationToken.None));

        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }
}
