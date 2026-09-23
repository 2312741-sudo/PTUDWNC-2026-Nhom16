namespace CulinaryBlog.Domain;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation property / counter (linked with Recipe in later phases)
    public int RecipesCount { get; set; }

    private Category() { }

    public Category(string name, string slug, string? description = null, string? imageUrl = null, int orderIndex = 0)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetSlug(slug);
        SetDescription(description);
        SetImageUrl(imageUrl);
        OrderIndex = orderIndex >= 0 ? orderIndex : 0;
        IsDeleted = false;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? description, string? imageUrl, int orderIndex)
    {
        SetName(name);
        SetDescription(description);
        SetImageUrl(imageUrl);
        OrderIndex = orderIndex >= 0 ? orderIndex : 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên danh mục không được để trống.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length is < 2 or > 100)
            throw new ArgumentException("Tên danh mục phải có từ 2 đến 100 ký tự.", nameof(name));

        if (trimmed.Any(char.IsControl) || trimmed.Contains('<') || trimmed.Contains('>'))
            throw new ArgumentException("Tên danh mục không được chứa ký tự điều khiển hoặc thẻ HTML.", nameof(name));

        Name = trimmed;
    }

    private void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug không được để trống.", nameof(slug));

        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length > 120)
            throw new ArgumentException("Slug không được vượt quá 120 ký tự.", nameof(slug));

        Slug = trimmed;
    }

    private void SetDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 500)
            throw new ArgumentException("Mô tả không được vượt quá 500 ký tự.", nameof(description));

        Description = trimmed;
    }

    private void SetImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = null;
            return;
        }

        var trimmed = imageUrl.Trim();
        if (trimmed.Length > 500)
            throw new ArgumentException("Đường dẫn ảnh không được vượt quá 500 ký tự.", nameof(imageUrl));

        ImageUrl = trimmed;
    }
}
