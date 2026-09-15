using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Danh mục công thức (SRS 7.6). Name ≤100 unique; Slug ≤120 unique; Description/ImageUrl nullable; OrderIndex.
///
/// LƯU Ý VAI TRÒ: đây là bản tối thiểu để C1 chạy độc lập và để FK Recipe→Category là thật (RESTRICT).
/// TV2 (task B1) sở hữu Category đầy đủ (CRUD/policy/seed). Khi tích hợp, thay bản này bằng bản của TV2.
/// </summary>
public sealed class Category : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public int OrderIndex { get; private set; }

    private Category() { }

    public static Category Create(string name, string slug, string? description = null, string? imageUrl = null, int orderIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 100)
            throw new DomainException("CATEGORY_NAME_INVALID", "Tên danh mục phải từ 2 đến 100 ký tự (D15).");
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 120)
            throw new DomainException("CATEGORY_SLUG_INVALID", "Slug danh mục tối đa 120 ký tự.");
        return new Category
        {
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description?.Trim(),
            ImageUrl = imageUrl?.Trim(),
            OrderIndex = orderIndex
        };
    }
}
