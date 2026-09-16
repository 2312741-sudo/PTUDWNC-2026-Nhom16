namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Dinh dưỡng của công thức (D28). Là owned entity: 6 cột Nutrition_* nằm ngay trong bảng Recipes,
/// không có bảng/khóa riêng. Tất cả decimal(8,2), nullable, không âm (validate ở Application).
/// Đơn vị theo tr.56: Calories kcal; Protein/Carbohydrates/Fat/Fiber gram; Sodium mg.
/// </summary>
public sealed class RecipeNutrition
{
    public decimal? Calories { get; private set; }
    public decimal? Protein { get; private set; }
    public decimal? Carbohydrates { get; private set; }
    public decimal? Fat { get; private set; }
    public decimal? Fiber { get; private set; }
    public decimal? Sodium { get; private set; }

    // EF cần ctor không tham số cho owned type.
    private RecipeNutrition() { }

    public static RecipeNutrition Empty() => new();

    public static RecipeNutrition Create(
        decimal? calories, decimal? protein, decimal? carbohydrates,
        decimal? fat, decimal? fiber, decimal? sodium)
        => new()
        {
            Calories = calories,
            Protein = protein,
            Carbohydrates = carbohydrates,
            Fat = fat,
            Fiber = fiber,
            Sodium = sodium
        };
}
