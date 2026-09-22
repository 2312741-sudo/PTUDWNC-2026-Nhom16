using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class FakeRecipeImageRepository : IRecipeImageRepository
{
    public readonly List<Recipe> Store = [];

    public Task<Recipe?> GetRecipeWithImagesAsync(Guid recipeId, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(r => r.Id == recipeId));

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeFileStorageService : IFileStorageService
{
    public readonly List<(string Key, string Folder, string ContentType)> Uploaded = [];
    public readonly List<string> Deleted = [];

    public Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var key = $"{folder.Trim('/')}/fake-key{extension}";
        Uploaded.Add((key, folder, contentType));
        return Task.FromResult(new StoredFile(key, key, contentType, content.Length));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        Deleted.Add(key);
        return Task.CompletedTask;
    }
}

public sealed class FakeCurrentUser(string userId, bool isAdmin = false) : ICurrentUser
{
    public string? UserId { get; } = userId;
    public bool IsInRole(string role) => isAdmin && role == Roles.Admin;
}

public sealed class RecipeImageApiTests
{
    private static Recipe NewDraftRecipe(string authorId = "author-1") =>
        Recipe.CreateDraft(
            title: "Phở bò Hà Nội",
            slug: $"pho-bo-{Guid.NewGuid():N}",
            description: "Phở bò truyền thống.",
            instructions: null,
            prepTimeMinutes: 30,
            cookTimeMinutes: 120,
            servings: 4,
            difficulty: RecipeDifficulty.Medium,
            categoryId: Guid.NewGuid(),
            authorId: authorId);

    private static MemoryStream JpegStream()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        var stream = new MemoryStream(bytes);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task Upload_adds_image_and_uploads_to_storage()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var storage = new FakeFileStorageService();
        var handler = new UploadRecipeImageHandler(repo, storage, new FakeCurrentUser("author-1"));

        var dto = await handler.Handle(new UploadRecipeImageCommand(
            recipe.Id, "anh.jpg", "image/jpeg", "Ảnh phở", 16, JpegStream()), CancellationToken.None);

        Assert.True(dto.IsPrimary, "Ảnh đầu tiên phải tự động thành primary.");
        Assert.Equal("Ảnh phở", dto.AltText);
        Assert.Equal(0, dto.OrderIndex);
        Assert.Single(repo.Store[0].Images);
        Assert.Equal(storage.Uploaded.Single().Key, dto.OriginalUrl);
        Assert.EndsWith(".jpg", dto.OriginalUrl);
        Assert.Equal("recipes/" + recipe.Id, storage.Uploaded.Single().Folder);
    }

    [Fact]
    public async Task Upload_rejects_non_owner()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-2"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.jpg", "image/jpeg", null, 16, JpegStream()), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
    }

    [Fact]
    public async Task Upload_allows_admin_regardless_of_owner()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("admin-1", isAdmin: true));

        var dto = await handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.jpg", "image/jpeg", null, 16, JpegStream()), CancellationToken.None);
        Assert.True(dto.IsPrimary);
    }

    [Fact]
    public async Task Upload_recipe_not_found_throws_404()
    {
        var repo = new FakeRecipeImageRepository(); // empty
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(Guid.NewGuid(), "a.jpg", "image/jpeg", null, 16, JpegStream()), CancellationToken.None));
        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }

    [Fact]
    public async Task Upload_rejects_invalid_file_type()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-1"));
        var gif = new MemoryStream([0x47, 0x49, 0x46, 0x38]); // GIF

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.gif", "image/gif", null, 4, gif), CancellationToken.None));
        Assert.Equal(400, ex.Status);
        Assert.Equal("file.invalid_type", ex.Code);
    }

    [Fact]
    public async Task Update_switches_primary_and_metadata()
    {
        var recipe = NewDraftRecipe();
        var first = recipe.AddImage("recipes/r1/a.jpg", "Ảnh cũ");
        var second = recipe.AddImage("recipes/r1/b.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UpdateRecipeImageHandler(repo, new FakeCurrentUser("author-1"));

        var dto = await handler.Handle(
            new UpdateRecipeImageCommand(recipe.Id, second.Id, IsPrimary: true, AltText: "Ảnh mới", OrderIndex: 0), CancellationToken.None);

        Assert.Equal(second.Id, dto.Id);
        Assert.True(dto.IsPrimary);
        Assert.Equal("Ảnh mới", dto.AltText);
        Assert.False(first.IsPrimary);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));

        // Chuyển primary sang ảnh còn lại -> ảnh trước bị bỏ primary
        await handler.Handle(new UpdateRecipeImageCommand(recipe.Id, first.Id, IsPrimary: true), CancellationToken.None);
        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public async Task Update_image_not_in_recipe_throws_404()
    {
        var recipe = NewDraftRecipe();
        recipe.AddImage("recipes/r1/a.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UpdateRecipeImageHandler(repo, new FakeCurrentUser("author-1"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UpdateRecipeImageCommand(recipe.Id, Guid.NewGuid(), IsPrimary: true), CancellationToken.None));
        Assert.Equal(404, ex.Status);
        Assert.Equal("image.not_found", ex.Code);
    }

    [Fact]
    public async Task Delete_removes_image_and_deletes_object()
    {
        var recipe = NewDraftRecipe();
        recipe.AddImage("recipes/r1/a.jpg", null);
        recipe.AddImage("recipes/r1/b.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var storage = new FakeFileStorageService();
        var handler = new DeleteRecipeImageHandler(repo, storage, new FakeCurrentUser("author-1"));

        var imageToDelete = recipe.Images.First(i => !i.IsPrimary);
        await handler.Handle(new DeleteRecipeImageCommand(recipe.Id, imageToDelete.Id), CancellationToken.None);

        Assert.Single(recipe.Images);
        Assert.Contains(imageToDelete.OriginalUrl, storage.Deleted);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public async Task Delete_non_owner_throws_403()
    {
        var recipe = NewDraftRecipe();
        var image = recipe.AddImage("recipes/r1/a.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new DeleteRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-9"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new DeleteRecipeImageCommand(recipe.Id, image.Id), CancellationToken.None));
        Assert.Equal(403, ex.Status);
    }

    [Fact]
    public async Task Upload_validator_rejects_oversized_alt_text()
    {
        var validator = new UploadRecipeImageValidator();
        var result = await validator.ValidateAsync(new UploadRecipeImageCommand(
            Guid.NewGuid(), "a.jpg", "image/jpeg", new string('a', 201), 16, JpegStream()));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadRecipeImageCommand.AltText));
    }

    [Fact]
    public async Task Update_validator_rejects_negative_order_index()
    {
        var validator = new UpdateRecipeImageValidator();
        var result = await validator.ValidateAsync(new UpdateRecipeImageCommand(
            Guid.NewGuid(), Guid.NewGuid(), OrderIndex: -1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRecipeImageCommand.OrderIndex));
    }

    [Fact]
    public void Update_dto_maps_updated_metadata()
    {
        var recipe = Recipe.CreateDraft("Bún đậu mắm tôm", "bun-dau-1", "Bún đậu.", null, 20, 0, 2, RecipeDifficulty.Easy, Guid.NewGuid(), "author-1");
        var image = recipe.AddImage("recipes/r/a.jpg", "Cũ");
        recipe.UpdateImageMetadata(image.Id, "Mới", 3);

        var dto = RecipeImageDto.From(image);
        Assert.Equal("Mới", dto.AltText);
        Assert.True(dto.IsPrimary);
        Assert.Equal(3, dto.OrderIndex);
    }
}
