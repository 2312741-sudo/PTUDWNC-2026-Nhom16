using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CulinaryBlog.Domain.Exceptions;
namespace CulinaryBlog.Domain.Entities;
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    protected Category() { }
    public static Category Create(string name, string? description = null)
    {
        var entity = new Category { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
        entity.SetValues(name, description);
        return entity;
    }
    public void Update(string name, string? description)
    {
        if (Name == name?.Trim() && Description == description?.Trim()) return;
        SetValues(name!, description);
        UpdatedAt = DateTime.UtcNow;
    }
    private void SetValues(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new DomainException("Tên danh mục phải có từ 1 đến 200 ký tự.");
        if (description?.Trim().Length > 2000) throw new DomainException("Mô tả tối đa 2000 ký tự.");
        var normalized = name.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var plain = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var slug = Regex.Replace(plain, "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0) throw new DomainException("Tên phải chứa ký tự có thể tạo slug.");
        Name = name.Trim(); Slug = slug; Description = description?.Trim();
    }
}
