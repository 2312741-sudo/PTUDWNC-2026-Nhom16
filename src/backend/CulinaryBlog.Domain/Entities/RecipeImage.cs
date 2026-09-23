using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Ảnh công thức. OriginalUrl &lt;=500 bắt buộc; Medium/Thumbnail nullable (do resize job cập nhật sau).
/// AltText &lt;=200 nullable. Ràng buộc: có ảnh thì đúng 1 primary; không có ảnh thì 0 primary
/// (enforce bằng partial unique index + logic aggregate). Hard delete; xóa file vật lý do job của TV4.
/// </summary>
public sealed class RecipeImage : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public string OriginalUrl { get; private set; } = string.Empty;
    public string? MediumUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int OrderIndex { get; private set; }

    private RecipeImage() { }

    internal RecipeImage(Guid recipeId, string originalUrl, string? altText, int orderIndex, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(originalUrl) || originalUrl.Length > 500)
            throw new DomainException("IMAGE_URL_INVALID", "URL ảnh phải từ 1 đến 500 ký tự.");
        if (altText is { Length: > 200 })
            throw new DomainException("IMAGE_ALT_INVALID", "AltText tối đa 200 ký tự.");

        RecipeId = recipeId;
        OriginalUrl = originalUrl.Trim();
        AltText = altText?.Trim();
        OrderIndex = orderIndex;
        IsPrimary = isPrimary;
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    public void SetOriginalUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 500)
            throw new DomainException("IMAGE_URL_INVALID", "URL ảnh phải từ 1 đến 500 ký tự.");
        OriginalUrl = url.Trim();
    }

    internal void SetAltText(string altText)
    {
        if (altText.Length > 200)
            throw new DomainException("IMAGE_ALT_INVALID", "AltText tối đa 200 ký tự.");
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
    }

    internal void SetOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
            throw new DomainException("IMAGE_ORDER_INVALID", "OrderIndex không được âm.");
        OrderIndex = orderIndex;
    }

    internal void SetResizedUrls(string? mediumUrl, string? thumbnailUrl)
    {
        MediumUrl = mediumUrl;
        ThumbnailUrl = thumbnailUrl;
    }
}
