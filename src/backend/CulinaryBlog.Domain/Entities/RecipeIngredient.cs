using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Nguyên liệu (D15). Name &lt;=200; Quantity decimal(10,3) nullable, nếu có phải &gt;0;
/// Unit &lt;=50 nullable; Notes &lt;=500 nullable; OrderIndex giữ thứ tự ổn định.
/// Soft delete đồng nhất qua BaseEntity (D08 / SRS 7.1).
/// </summary>
public sealed class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public string? Notes { get; private set; }
    public int OrderIndex { get; private set; }

    private RecipeIngredient() { }

    internal RecipeIngredient(Guid recipeId, string name, decimal? quantity, string? unit, string? notes, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new DomainException("INGREDIENT_NAME_INVALID", "Tên nguyên liệu phải từ 1 đến 200 ký tự.");
        if (quantity is <= 0)
            throw new DomainException("INGREDIENT_QUANTITY_INVALID", "Số lượng nếu có phải lớn hơn 0.");
        if (unit is { Length: > 50 })
            throw new DomainException("INGREDIENT_UNIT_INVALID", "Đơn vị tối đa 50 ký tự.");
        if (notes is { Length: > 500 })
            throw new DomainException("INGREDIENT_NOTES_INVALID", "Ghi chú tối đa 500 ký tự.");

        RecipeId = recipeId;
        Name = name.Trim();
        Quantity = quantity;
        Unit = unit?.Trim();
        Notes = notes?.Trim();
        OrderIndex = orderIndex;
    }

    internal void SetOrder(int orderIndex) => OrderIndex = orderIndex;

    internal void Update(string name, decimal? quantity, string? unit, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new DomainException("INGREDIENT_NAME_INVALID", "Tên nguyên liệu phải từ 1 đến 200 ký tự.");
        if (quantity is <= 0)
            throw new DomainException("INGREDIENT_QUANTITY_INVALID", "Số lượng nếu có phải lớn hơn 0.");
        if (unit is { Length: > 50 })
            throw new DomainException("INGREDIENT_UNIT_INVALID", "Đơn vị tối đa 50 ký tự.");
        if (notes is { Length: > 500 })
            throw new DomainException("INGREDIENT_NOTES_INVALID", "Ghi chú tối đa 500 ký tự.");

        Name = name.Trim();
        Quantity = quantity;
        Unit = unit?.Trim();
        Notes = notes?.Trim();
    }
}
