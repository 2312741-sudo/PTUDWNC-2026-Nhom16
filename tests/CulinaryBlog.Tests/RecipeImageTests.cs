using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;
// main có thêm class CulinaryBlog.Domain.Recipe (discovery) — chỉ định tường minh entity DDD.
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

public sealed class FakeRecipeImageRepository : IRecipeImageRepository
{
    public readonly List<Recipe> Store = [];

    public Task<Recipe?> GetRecipeWithImagesAsync(Guid recipeId, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(r => r.Id == recipeId));

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>N2-E4: xoá tường minh — fake không có DbContext nên chỉ ghi nhận.</summary>
    public readonly List<Domain.Entities.RecipeImage> MarkedDeleted = [];

    public void MarkImageDeleted(Domain.Entities.RecipeImage image) => MarkedDeleted.Add(image);
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

/// <summary>D23: unit test không chạy job thật — chỉ ghi lại để assert handler đã enqueue.</summary>
public sealed class FakeImageResizeQueue : IImageResizeQueue
{
    public readonly List<(Guid RecipeId, Guid ImageId, string OriginalKey)> Enqueued = [];

    public Task EnqueueAsync(Guid recipeId, Guid imageId, string originalKey, CancellationToken ct)
    {
        Enqueued.Add((recipeId, imageId, originalKey));
        return Task.CompletedTask;
    }
}

/// <summary>B5: signer giả — ghi lại key được ký để assert thứ tự ưu tiên variant.</summary>
public sealed class FakeObjectStorageUrlSigner : IObjectStorageUrlSigner
{
    public readonly List<string> SignedKeys = [];

    /// <summary>Đặt null để mô phỏng RustFS/MinIO hỏng lúc ký (B5 yêu cầu fail-soft).</summary>
    public string? Result { get; set; } = "https://storage.local/presigned";

    public Task<string?> CreatePresignedUrlAsync(string key, CancellationToken ct = default)
    {
        SignedKeys.Add(key);
        return Task.FromResult(Result);
    }
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
        // Đệm tới `ImageFormats.MinBytes`: validator chặn `file.too_small` trước khi đối chiếu
        // magic bytes, nên fixture 16 byte sẽ bị chặn ở ngưỡng kích thước thay vì tới bước JPEG.
        var bytes = new byte[Math.Max(16, (int)ImageFormats.MinBytes)];
        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.CopyTo(bytes, 0);
        var stream = new MemoryStream(bytes);
        stream.Position = 0;
        return stream;
    }

    /// <summary>B5: factory thật + signer giả để handler đi đúng luồng production.</summary>
    private static RecipeImageDtoFactory DtoFactory(IObjectStorageUrlSigner? signer = null)
        => new(signer ?? new FakeObjectStorageUrlSigner());

    [Fact]
    public async Task Upload_adds_image_and_uploads_to_storage()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var storage = new FakeFileStorageService();
        var queue = new FakeImageResizeQueue();
        var handler = new UploadRecipeImageHandler(repo, storage, new FakeCurrentUser("author-1"), queue, DtoFactory());

        var dto = await handler.Handle(new UploadRecipeImageCommand(
            recipe.Id, "anh.jpg", "image/jpeg", "Ảnh phở", ImageFormats.MinBytes, JpegStream()), CancellationToken.None);

        Assert.True(dto.IsPrimary, "Ảnh đầu tiên phải tự động thành primary.");
        Assert.Equal("Ảnh phở", dto.AltText);
        Assert.Equal(0, dto.OrderIndex);
        Assert.Single(repo.Store[0].Images);
        Assert.Equal(storage.Uploaded.Single().Key, dto.OriginalUrl);
        Assert.EndsWith(".jpg", dto.OriginalUrl);
        Assert.Equal("recipes/" + recipe.Id, storage.Uploaded.Single().Folder);

        // D23: upload xong phải enqueue resize với đúng key original đã lưu.
        var enqueued = Assert.Single(queue.Enqueued);
        Assert.Equal(recipe.Id, enqueued.RecipeId);
        Assert.Equal(repo.Store[0].Images[0].Id, enqueued.ImageId);
        Assert.Equal(storage.Uploaded.Single().Key, enqueued.OriginalKey);
    }

    [Fact]
    public async Task Upload_does_not_enqueue_resize_when_invalid_file()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var queue = new FakeImageResizeQueue();
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-1"), queue, DtoFactory());
        // GIF không thuộc danh sách MIME cho phép. Đệm tới `MinBytes` để bị chặn đúng ở nhánh
        // "sai MIME" chứ không phải nhánh `file.too_small` (test chỉ assert là throw + không enqueue).
        var gifBytes = new byte[(int)ImageFormats.MinBytes];
        new byte[] { 0x47, 0x49, 0x46, 0x38 }.CopyTo(gifBytes, 0);
        var gif = new MemoryStream(gifBytes);

        await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.gif", "image/gif", null, gif.Length, gif), CancellationToken.None));

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public void ResizedKeys_derives_300x300_and_800x600_keys()
    {
        var keys = RecipeImageKeys.ResizedKeys("recipes/abc/1f2e.jpg");
        Assert.NotNull(keys);
        Assert.Equal("recipes/abc/1f2e_300x300.jpg", keys!.Value.ThumbnailKey);
        Assert.Equal("recipes/abc/1f2e_800x600.jpg", keys.Value.MediumKey);
    }

    [Theory]
    [InlineData("recipes/abc/1f2e.avif")]
    [InlineData("recipes/abc/noext")]
    public void ResizedKeys_returns_null_for_unsupported(string originalKey)
    {
        // AVIF: ImageSharp 3.1 không decode → giữ original (FE fallback imageSrc), không tạo object phái sinh.
        Assert.Null(RecipeImageKeys.ResizedKeys(originalKey));
    }

    [Fact]
    public async Task Upload_rejects_non_owner()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-2"), new FakeImageResizeQueue(), DtoFactory());

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.jpg", "image/jpeg", null, ImageFormats.MinBytes, JpegStream()), CancellationToken.None));
        Assert.Equal(403, ex.Status);
        Assert.Equal("recipe.forbidden", ex.Code);
    }

    [Fact]
    public async Task Upload_allows_admin_regardless_of_owner()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("admin-1", isAdmin: true), new FakeImageResizeQueue(), DtoFactory());

        var dto = await handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.jpg", "image/jpeg", null, ImageFormats.MinBytes, JpegStream()), CancellationToken.None);
        Assert.True(dto.IsPrimary);
    }

    [Fact]
    public async Task Upload_recipe_not_found_throws_404()
    {
        var repo = new FakeRecipeImageRepository(); // empty
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-1"), new FakeImageResizeQueue(), DtoFactory());

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(Guid.NewGuid(), "a.jpg", "image/jpeg", null, ImageFormats.MinBytes, JpegStream()), CancellationToken.None));
        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
    }

    [Fact]
    public async Task Upload_rejects_invalid_file_type()
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new UploadRecipeImageHandler(repo, new FakeFileStorageService(), new FakeCurrentUser("author-1"), new FakeImageResizeQueue(), DtoFactory());
        // GIF không nằm trong danh sách MIME cho phép. Đệm tới `MinBytes` để lỗi đến tay đúng
        // nhánh "sai MIME" thay vì bị chặn sớm bởi nhánh `file.too_small`.
        var gifBytes = new byte[(int)ImageFormats.MinBytes];
        new byte[] { 0x47, 0x49, 0x46, 0x38 }.CopyTo(gifBytes, 0);
        var gif = new MemoryStream(gifBytes);
        gif.Position = 0;

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, "a.gif", "image/gif", null, gif.Length, gif), CancellationToken.None));
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
        var handler = new UpdateRecipeImageHandler(repo, new FakeUnitOfWork(), new FakeCurrentUser("author-1"), DtoFactory());

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
        var handler = new UpdateRecipeImageHandler(repo, new FakeUnitOfWork(), new FakeCurrentUser("author-1"), DtoFactory());

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
        var handler = new DeleteRecipeImageHandler(repo, new FakeUnitOfWork(), storage, new FakeCurrentUser("author-1"));

        var imageToDelete = recipe.Images.First(i => !i.IsPrimary);
        await handler.Handle(new DeleteRecipeImageCommand(recipe.Id, imageToDelete.Id), CancellationToken.None);

        Assert.Single(recipe.Images);
        Assert.Contains(imageToDelete.OriginalUrl, storage.Deleted);
        Assert.Equal(1, recipe.Images.Count(i => i.IsPrimary));
    }

    [Fact]
    public async Task Delete_also_deletes_derived_resize_objects()
    {
        var recipe = NewDraftRecipe();
        var primary = recipe.AddImage("recipes/r1/a.jpg", null);
        var derived = recipe.AddImage("recipes/r1/b.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var storage = new FakeFileStorageService();
        var handler = new DeleteRecipeImageHandler(repo, new FakeUnitOfWork(), storage, new FakeCurrentUser("author-1"));

        await handler.Handle(new DeleteRecipeImageCommand(recipe.Id, derived.Id), CancellationToken.None);

        // D2: xoá ảnh phải dọn luôn object resize 300x300/800x600 để không rò rỉ storage.
        Assert.Contains("recipes/r1/b.jpg", storage.Deleted);
        Assert.Contains("recipes/r1/b_300x300.jpg", storage.Deleted);
        Assert.Contains("recipes/r1/b_800x600.jpg", storage.Deleted);
        Assert.Single(recipe.Images);
        Assert.Equal(primary.Id, recipe.Images[0].Id);
    }

    [Fact]
    public async Task Delete_non_owner_throws_403()
    {
        var recipe = NewDraftRecipe();
        var image = recipe.AddImage("recipes/r1/a.jpg", null);
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var handler = new DeleteRecipeImageHandler(repo, new FakeUnitOfWork(), new FakeFileStorageService(), new FakeCurrentUser("author-9"));

        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(
            new DeleteRecipeImageCommand(recipe.Id, image.Id), CancellationToken.None));
        Assert.Equal(403, ex.Status);
    }

    [Fact]
    public async Task Upload_validator_rejects_oversized_alt_text()
    {
        var validator = new UploadRecipeImageValidator();
        var result = await validator.ValidateAsync(new UploadRecipeImageCommand(
            Guid.NewGuid(), "a.jpg", "image/jpeg", new string('a', 201), ImageFormats.MinBytes, JpegStream()));
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

