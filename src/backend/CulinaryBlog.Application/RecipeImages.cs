using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
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
    DateTimeOffset UpdatedAt,
    string? PresignedUrl = null)
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

/// <summary>
/// B5 (issue #24): bổ sung <see cref="RecipeImageDto.PresignedUrl"/> khi ảnh còn PRIVATE.
/// Chỉ 2 endpoint ảnh trả DTO này (POST upload + PATCH update) — cả hai đều <c>RequireAuthorization</c>
/// + <c>EnsureCanManage</c> (owner hoặc Admin) nên không thể rò sang response công khai của
/// GET /recipes/{slug} (endpoint đó dùng DTO riêng, không có trường PresignedUrl).
/// </summary>
public interface IRecipeImageDtoFactory
{
    /// <param name="image">Ảnh vừa lưu.</param>
    /// <param name="includePresignedUrl">
    /// true khi recipe CHƯA Published (ảnh còn private). Recipe Published đã phục vụ công khai qua
    /// proxy D27 nên không ký — giữ response công khai sạch và tránh rải bearer token không cần thiết.
    /// </param>
    Task<RecipeImageDto> CreateAsync(RecipeImage image, bool includePresignedUrl, CancellationToken ct = default);

    /// <summary>
    /// B5: biến thể dùng cho detail (<c>RecipeDetailDto.Images</c>). Endpoint này phục vụ CẢ owner lẫn
    /// người xem công khai, nên <paramref name="includePresignedUrl"/> do handler quyết định
    /// (chỉ owner/Admin + recipe chưa Published) chứ không tự suy ra.
    /// </summary>
    Task<IReadOnlyList<RecipeImageSummaryDto>> CreateSummariesAsync(
        Recipe recipe, bool includePresignedUrl, CancellationToken ct = default);
}

/// <summary>
/// Chọn biến thể ảnh để ký và trả DTO kèm URL có chữ ký.
/// Ưu tiên thumbnail → medium → original, và CHỈ ký biến thể mà DB đã ghi (nghĩa là object resize đã tồn tại).
/// Không cần gọi ExistsAsync: <c>ThumbnailUrl</c>/<c>MediumUrl</c> chỉ được ghi sau khi job resize xong
/// (D23/D2) ⇒ có giá trị là bằng chứng object đã tồn tại, tránh ký cho object không có (ảnh vỡ).
/// </summary>
public sealed class RecipeImageDtoFactory(IObjectStorageUrlSigner signer) : IRecipeImageDtoFactory
{
    public async Task<RecipeImageDto> CreateAsync(RecipeImage image, bool includePresignedUrl, CancellationToken ct = default)
    {
        var dto = RecipeImageDto.From(image);
        if (!includePresignedUrl)
            return dto;

        var key = FirstExistingVariantKey(image);
        if (key is null)
            return dto;

        var presigned = await signer.CreatePresignedUrlAsync(key, ct).ConfigureAwait(false);
        return presigned is null ? dto : dto with { PresignedUrl = presigned };
    }

    private static string? FirstExistingVariantKey(RecipeImage image) =>
        FirstNonBlank(image.ThumbnailUrl) ?? FirstNonBlank(image.MediumUrl) ?? FirstNonBlank(image.OriginalUrl);

    private static string? FirstNonBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public async Task<IReadOnlyList<RecipeImageSummaryDto>> CreateSummariesAsync(
        Recipe recipe, bool includePresignedUrl, CancellationToken ct = default)
    {
        var summaries = new List<RecipeImageSummaryDto>(recipe.Images.Count);
        foreach (var image in recipe.Images.OrderBy(i => i.OrderIndex))
        {
            var summary = new RecipeImageSummaryDto(
                image.Id, image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl,
                image.AltText, image.IsPrimary, image.OrderIndex);
            summaries.Add(includePresignedUrl ? await WithPresignedUrlAsync(summary, image, ct) : summary);
        }
        return summaries;
    }

    private async Task<RecipeImageSummaryDto> WithPresignedUrlAsync(
        RecipeImageSummaryDto summary, RecipeImage image, CancellationToken ct)
    {
        var key = FirstExistingVariantKey(image);
        if (key is null)
            return summary;

        var presigned = await signer.CreatePresignedUrlAsync(key, ct).ConfigureAwait(false);
        return presigned is null ? summary : summary with { PresignedUrl = presigned };
    }
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

    /// <summary>
    /// N2-E4: đánh dấu xoá hẳn dòng ảnh. Không được dựa vào việc bỏ ảnh khỏi collection của
    /// aggregate: `Recipe.Images` chỉ expose `IReadOnlyList` qua backing field nên EF không
    /// nhận ra orphan trong cách đáng tin (bài kiểm chứng: entity vẫn ở state `Unchanged` sau
    /// SaveChanges, và `DeleteBehavior.Cascade` trên quan hệ bắt buộc khiến DB tự xoá chỉ khi
    /// xoá chính recipe — mà recipe là soft-delete). Vì vậy xoá phải được yêu cầu tường minh.
    /// </summary>
    void MarkImageDeleted(RecipeImage image);
}

/// <summary>
/// Queue resize ảnh (D23 PA-1 — Hangfire khi chạy thật; chạy inline trong môi trường Testing).
/// Application không phụ thuộc Hangfire: chỉ mô tả "enqueue job resize"; triển khai ở Infrastructure.
/// </summary>
public interface IImageResizeQueue
{
    Task EnqueueAsync(Guid recipeId, Guid imageId, string originalKey, CancellationToken ct);
}

/// <summary>
/// Tính key object phái sinh cho resize (D2): {base}_300x300.{ext} (thumbnail) và {base}_800x600.{ext} (medium).
/// AVIF trả null: bản 3.1.x của ImageSharp không decode AVIF → giữ original (fallback theo FE imageSrc()).
/// </summary>
public static class RecipeImageKeys
{
    public const string ThumbnailSuffix = "_300x300";
    public const string MediumSuffix = "_800x600";

    public static (string ThumbnailKey, string MediumKey)? ResizedKeys(string originalKey)
    {
        var ext = Path.GetExtension(originalKey);
        if (string.IsNullOrWhiteSpace(ext) || string.Equals(ext, ".avif", StringComparison.OrdinalIgnoreCase))
            return null;
        var baseKey = originalKey[..^ext.Length];
        return ($"{baseKey}{ThumbnailSuffix}{ext}", $"{baseKey}{MediumSuffix}{ext}");
    }
}

public sealed class UploadRecipeImageHandler(
    IRecipeImageRepository repository,
    IFileStorageService storage,
    ICurrentUser currentUser,
    IImageResizeQueue resizeQueue,
    IRecipeImageDtoFactory dtoFactory)
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

        // D23: enqueue resize original -> 300x300 + 800x600 (Hangfire ngoài request / inline trong Testing).
        await resizeQueue.EnqueueAsync(recipe.Id, image.Id, stored.Key, ct);

        // B5: ảnh vừa upload còn private (recipe Draft) -> kèm URL có chữ ký để thẻ <img> tải được.
        return await dtoFactory.CreateAsync(image, RecipeImageAccess.NeedsPresignedUrls(recipe), ct);
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
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IRecipeImageDtoFactory dtoFactory)
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
        {
            // N2-E4: hạ toàn bộ primary rồi mới bật ảnh mới, hai lần lưu trong một transaction.
            // Gộp làm một lần lưu thì EF có thể phát UPDATE(bật ảnh mới) trước UPDATE(hạ ảnh cũ)
            // → DB thấy hai primary cùng lúc → 23505 ux_recipe_images_one_primary → 422.
            await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            {
                recipe.ClearPrimaryImages();
                await repository.SaveChangesAsync(innerCt);   // lần 1: còn 0 primary
                recipe.PromotePrimaryImage(request.ImageId);  // lần 2: đúng 1 primary
                recipe.UpdateImageMetadata(request.ImageId, request.AltText, request.OrderIndex);
            }, ct);
        }
        else
        {
            recipe.UpdateImageMetadata(request.ImageId, request.AltText, request.OrderIndex);
            await repository.SaveChangesAsync(ct);
        }

        var image = recipe.Images.First(i => i.Id == request.ImageId);
        return await dtoFactory.CreateAsync(image, RecipeImageAccess.NeedsPresignedUrls(recipe), ct);
    }
}

public sealed class DeleteRecipeImageHandler(
    IRecipeImageRepository repository,
    IUnitOfWork unitOfWork,
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

        // N2-E4 — ba pha, mỗi pha đúng MỘT câu lệnh, tất cả trong một transaction.
        //
        // Lý do phải vậy: unique index partial `ux_recipe_images_one_primary` được Postgres kiểm
        // tra ngay TỪNG câu lệnh, và Postgres không cho unique index partial deferrable. Trong khi
        // đó EF KHÔNG bảo đảm thứ tự phát lệnh giữa các entity. Gộp "xoá ảnh primary" và "bật ảnh
        // thay thế" vào một lần SaveChanges thì EF có thể phát UPDATE(bật ảnh mới) khi dòng primary
        // cũ còn nằm trong bảng → 23505 → API trả 422 và ảnh cũ không bị xoá.
        //
        //   1) hạ tất cả primary      → DB còn 0 dòng IsPrimary = true  (hợp lệ với index)
        //   2) bật ảnh thay thế       → DB có đúng 1 dòng              (hợp lệ với index)
        //   3) xoá dòng ảnh cũ        → DELETE tường minh
        var replacementId = recipe.GetPrimaryReplacementCandidate(request.ImageId);

        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            // Chỉ khi ảnh bị xoá ĐANG là primary mới cần dựng lại primary. Xoá ảnh thường thì
            // primary hiện tại giữ nguyên — hạ cờ nó đi sẽ để lại công thức không có ảnh chính.
            if (replacementId is not null)
            {
                recipe.ClearPrimaryImages();
                await repository.SaveChangesAsync(innerCt);          // 1) còn 0 primary

                recipe.PromotePrimaryImage(replacementId.Value);
                await repository.SaveChangesAsync(innerCt);          // 2) đúng 1 primary
            }

            repository.MarkImageDeleted(image);
            // Bỏ khỏi aggregate SAU khi đã bật ảnh thay thế, để RemoveImage không promote lần nữa
            // (ảnh bị xoá lúc này đã IsPrimary = false).
            recipe.RemoveImage(request.ImageId);
            // EfUnitOfWork tự SaveChanges ở cuối action → phát DELETE.
        }, ct);

        await storage.DeleteAsync(key, ct);

        // D2: xoá luôn object resize phái sinh (nếu job đã chạy hoặc chạy trễ vẫn sạch — idempotent).
        var resized = RecipeImageKeys.ResizedKeys(key);
        if (resized is { } keys)
        {
            await storage.DeleteAsync(keys.ThumbnailKey, ct);
            await storage.DeleteAsync(keys.MediumKey, ct);
        }

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

    /// <summary>
    /// B5: recipe CHƯA Published thì ảnh còn private (proxy D27 chỉ owner/Admin) -> cần URL có chữ ký.
    /// Published đã phục vụ công khai, không ký.
    /// </summary>
    public static bool NeedsPresignedUrls(Recipe recipe) => recipe.Status != RecipeStatus.Published;
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
