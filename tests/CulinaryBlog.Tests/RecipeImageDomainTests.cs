using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class RecipeImageDomainTests
{
    private static Recipe NewDraftRecipe() =>
        Recipe.CreateDraft(
            title: "Bánh xèo miền Tây",
            slug: $"banh-xeo-{Guid.NewGuid():N}",
            description: "Bánh xèo giòn rụm.",
            instructions: null,
            prepTimeMinutes: 20,
            cookTimeMinutes: 30,
            servings: 2,
            difficulty: RecipeDifficulty.Easy,
            categoryId: Guid.NewGuid(),
            authorId: "author-1");

    [Fact]
    public void First_image_becomes_primary_automatically()
    {
        var recipe = NewDraftRecipe();
        var img = recipe.AddImage("recipes/r1/abc.jpg", "Bánh xèo");
        Assert.True(img.IsPrimary);
        Assert.Equal(0, img.OrderIndex);
    }

    [Fact]
    public void Exactly_one_primary_is_always_kept_when_adding_images()
    {
        var recipe = NewDraftRecipe();
        var first = recipe.AddImage("recipes/r1/a.jpg", null);
        var second = recipe.AddImage("recipes/r1/b.jpg", null);
        var third = recipe.AddImage("recipes/r1/c.jpg", null);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
        Assert.False(third.IsPrimary);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public void SetPrimaryImage_switches_primary_and_clears_others()
    {
        var recipe = NewDraftRecipe();
        var first = recipe.AddImage("recipes/r1/a.jpg", null);
        var second = recipe.AddImage("recipes/r1/b.jpg", null);

        recipe.SetPrimaryImage(second.Id);

        Assert.False(first.IsPrimary);
        Assert.True(second.IsPrimary);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public void SetPrimaryImage_unknown_image_throws()
    {
        var recipe = NewDraftRecipe();
        recipe.AddImage("recipes/r1/a.jpg", null);
        var ex = Assert.Throws<DomainException>(() => recipe.SetPrimaryImage(Guid.NewGuid()));
        Assert.Equal("IMAGE_NOT_FOUND", ex.Code);
    }

    [Fact]
    public void RemoveImage_promotes_next_image_to_primary()
    {
        var recipe = NewDraftRecipe();
        var first = recipe.AddImage("recipes/r1/a.jpg", null);
        recipe.AddImage("recipes/r1/b.jpg", null);

        recipe.RemoveImage(first.Id);

        Assert.Single(recipe.Images);
        Assert.True(recipe.Images.Single().IsPrimary, "Sau khi xoá ảnh primary, ảnh còn lại được thăng cấp làm primary.");
    }

    [Fact]
    public void RemoveImage_keeps_exactly_one_primary_when_removing_non_primary()
    {
        var recipe = NewDraftRecipe();
        var first = recipe.AddImage("recipes/r1/a.jpg", null);
        var second = recipe.AddImage("recipes/r1/b.jpg", null);

        recipe.RemoveImage(second.Id);

        Assert.Single(recipe.Images);
        Assert.True(first.IsPrimary);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public void RemoveImage_unknown_image_throws()
    {
        var recipe = NewDraftRecipe();
        recipe.AddImage("recipes/r1/a.jpg", null);
        Assert.Throws<DomainException>(() => recipe.RemoveImage(Guid.NewGuid()));
    }

    [Fact]
    public void RecipeImage_validates_url_length_and_alt_text_length()
    {
        var recipe = NewDraftRecipe();
        Assert.Throws<DomainException>(() => recipe.AddImage("   ", null));
        Assert.Throws<DomainException>(() => recipe.AddImage(new string('x', 501), null));
        Assert.Throws<DomainException>(() => recipe.AddImage("recipes/r1/x.jpg", new string('a', 201)));
    }
}
