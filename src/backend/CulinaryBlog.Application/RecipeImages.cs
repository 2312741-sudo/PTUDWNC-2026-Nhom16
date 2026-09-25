using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using FluentValidation;
using MediatR;
// main có thêm class CulinaryBlog.Domain.Recipe (discovery) nên phải chỉ định tường minh
// entity DDD mà file này dùng (AddImage/Images/SetPrimaryImage...).
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Application;

/// <summary>DTO ảnh công thức (contract docs/IMAGE_CONTRACT.md §3).</summary>
public sealed record RecipeImageDto(
    Guid Id,
    Guid RecipeId,
    string OriginalUrl,
    string? MediumUrl,
    string? ThumbnailUrl,
    string? AltText,
    bool IsPrimary,
    int OrderIndex,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static RecipeImageDto From(RecipeImage image) => new(
        image.Id,
        image.RecipeId,
        image.OriginalUrl,
        image.MediumUrl,
        image.ThumbnailUrl,
        image.AltText,
        image.IsPrimary,
        image.OrderIndex,
        new DateTimeOffset(DateTime.SpecifyKind(image.CreatedAt, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(image.UpdatedAt ?? image.CreatedAt, DateTimeKind.Utc)));
}

public sealed record UploadRecipeImageCommand(
    Guid RecipeId,
    string FileName,
    string ContentType,
    string? AltText,
    long Length,
    Stream Content) : IRequest<RecipeImageDto>;

public sealed record UpdateRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    bool? IsPrimary = null,
    string? AltText = null,
    int? OrderIndex = null) : IRequest<RecipeImageDto>;

public sealed record DeleteRecipeImageCommand(Guid RecipeId, Guid ImageId) : IRequest<Unit>;

/// <summary>
/// Cổng truy cập Recipe kèm collection Images đã load — D18 (Application không dùng EF Include).
/// </summary>
public interface IRecipeImageRepository
{
    Task<Recipe?> GetRecipeWithImagesAsync(Guid recipeId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class UploadRecipeImageHandler(
    IRecipeImageRepository repository,
    IFileStorageService storage,
    ICurrentUser currentUser)
    : IRequestHandler<UploadRecipeImageCommand, RecipeImageDto>
{
    public async Task<RecipeImageDto> Handle(UploadRecipeImageCommand request, CancellationToken ct)
    {
        var recipe = await repository.GetRecipeWithImagesAsync(request.RecipeId, ct)
            ?? throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");
        RecipeImageAccess.EnsureCanManage(recipe, currentUser);

        var (ok, code, message) = ImageUploadValidator.Validate(request.Content, request.Length, request.ContentType);
        if (!ok)
            throw new AppException(400, code!, message!);

        var detectedMime = DetectMime(request.Content);
        var extension = detectedMime is not null ? ImageFormats.ExtensionFor(detectedMime) : null;
        var fileName = $"image{extension}";
        var contentType = detectedMime ?? request.ContentType;

        var stored = await storage.UploadAsync(request.Content, fileName, contentType, $"recipes/{recipe.Id}", ct);

        var image = recipe.AddImage(stored.Key, request.AltText);
        await repository.SaveChangesAsync(ct);

        return RecipeImageDto.From(image);
    }

    private static string? DetectMime(Stream content)
    {
        var head = new byte[12];
        var read = content.Read(head, 0, head.Length);
        content.Position = 0;
        return ImageFormats.DetectMimeType(head.AsSpan(0, read));
    }
}

public sealed class UpdateRecipeImageHandler(
    IRecipeImageRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateRecipeImageCommand, RecipeImageDto>
{
    public async Task<RecipeImageDto> Handle(UpdateRecipeImageCommand request, CancellationToken ct)
    {
        var recipe = await repository.GetRecipeWithImagesAsync(request.RecipeId, ct)
            ?? throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");
        RecipeImageAccess.EnsureCanManage(recipe, currentUser);

        if (!recipe.Images.Any(i => i.Id == request.ImageId))
            throw new AppException(404, "image.not_found", "Không tìm thấy ảnh thuộc công thức này.");

        if (request.IsPrimary is true)
            recipe.SetPrimaryImage(request.ImageId);

        recipe.UpdateImageMetadata(request.ImageId, request.AltText, request.OrderIndex);
        await repository.SaveChangesAsync(ct);

        var image = recipe.Images.First(i => i.Id == request.ImageId);
        return RecipeImageDto.From(image);
    }
}

public sealed class DeleteRecipeImageHandler(
    IRecipeImageRepository repository,
    IFileStorageService storage,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteRecipeImageCommand, Unit>
{
    public async Task<Unit> Handle(DeleteRecipeImageCommand request, CancellationToken ct)
    {
        var recipe = await repository.GetRecipeWithImagesAsync(request.RecipeId, ct)
            ?? throw new AppException(404, "recipe.not_found", "Không tìm thấy công thức.");
        RecipeImageAccess.EnsureCanManage(recipe, currentUser);

        var image = recipe.Images.FirstOrDefault(i => i.Id == request.ImageId)
            ?? throw new AppException(404, "image.not_found", "Không tìm thấy ảnh thuộc công thức này.");

        var key = image.OriginalUrl;
        recipe.RemoveImage(request.ImageId);
        await repository.SaveChangesAsync(ct);
        await storage.DeleteAsync(key, ct);

        return Unit.Value;
    }
}

internal static class RecipeImageAccess
{
    public static void EnsureCanManage(Recipe recipe, ICurrentUser currentUser)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            throw new AppException(401, "auth.unauthorized", "Vui lòng đăng nhập.");
        if (!currentUser.IsInRole(Roles.Admin) && !string.Equals(userId, recipe.AuthorId, StringComparison.OrdinalIgnoreCase))
            throw new AppException(403, "recipe.forbidden", "Bạn không có quyền chỉnh sửa công thức này.");
    }
}

public sealed class UploadRecipeImageValidator : AbstractValidator<UploadRecipeImageCommand>
{
    public UploadRecipeImageValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AltText).MaximumLength(200).When(x => x.AltText is not null);
    }
}

public sealed class UpdateRecipeImageValidator : AbstractValidator<UpdateRecipeImageCommand>
{
    public UpdateRecipeImageValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
        RuleFor(x => x.AltText).MaximumLength(200).When(x => x.AltText is not null);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0).When(x => x.OrderIndex is not null);
    }
}

public sealed class DeleteRecipeImageValidator : AbstractValidator<DeleteRecipeImageCommand>
{
    public DeleteRecipeImageValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
