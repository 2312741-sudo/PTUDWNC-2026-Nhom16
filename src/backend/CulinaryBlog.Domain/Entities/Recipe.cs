using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Aggregate root cho công thức. Sở hữu Nutrition (owned) và các collection Ingredients/Steps/Images.
/// Mọi thay đổi cấu trúc con đi qua aggregate để giữ bất biến (thứ tự bước liên tục, đúng 1 primary...).
///
/// Ràng buộc chính (nguồn tr.52-60 + D07/D08/D14/D15/D16/D28):
///  - Title 5..200; Slug &lt;=220 unique; Description &lt;=2000; Instructions NOT NULL mặc định "" (D28).
///  - PrepTimeMinutes &gt;0; CookTimeMinutes &gt;=0; Servings &gt;0.
///  - Tạo mới luôn ở trạng thái Draft.
///  - Publish cần &gt;=1 ingredient và &gt;=1 step (D07); không bắt buộc ảnh.
///  - AuthorId là string FK tới ApplicationUser (Identity, ở Infrastructure - D18/D24). Domain không tham chiếu IdentityUser.
///  - CategoryId là Guid FK tới Category (do TV2/B1 định nghĩa). Domain chỉ giữ khóa, không giữ navigation để tránh phụ thuộc ngược.
/// </summary>
public sealed class Recipe : BaseEntity, IAggregateRoot
{
    private readonly List<RecipeIngredient> _ingredients = [];
    private readonly List<RecipeStep> _steps = [];
    private readonly List<RecipeImage> _images = [];

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty; // D28: NOT NULL, mặc định ""
    public int PrepTimeMinutes { get; private set; }
    public int CookTimeMinutes { get; private set; }
    public int Servings { get; private set; }
    public RecipeDifficulty Difficulty { get; private set; }
    public RecipeStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public Guid CategoryId { get; private set; }
    public string AuthorId { get; private set; } = string.Empty;

    public RecipeNutrition Nutrition { get; private set; } = RecipeNutrition.Empty();

    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyList<RecipeStep> Steps => _steps.AsReadOnly();
    public IReadOnlyList<RecipeImage> Images => _images.AsReadOnly();

    private Recipe() { } // EF

    public static Recipe CreateDraft(
        string title, string slug, string description, string? instructions,
        int prepTimeMinutes, int cookTimeMinutes, int servings,
        RecipeDifficulty difficulty, Guid categoryId, string authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            throw new DomainException("RECIPE_AUTHOR_REQUIRED", "AuthorId bắt buộc và lấy từ token, không nhận từ client.");
        if (categoryId == Guid.Empty)
            throw new DomainException("RECIPE_CATEGORY_REQUIRED", "CategoryId bắt buộc.");

        var recipe = new Recipe
        {
            CategoryId = categoryId,
            AuthorId = authorId,
            Status = RecipeStatus.Draft,      // luôn tạo Draft
            PublishedAt = null,
            Instructions = instructions?.Trim() ?? string.Empty
        };
        recipe.SetCoreFields(title, slug, description, prepTimeMinutes, cookTimeMinutes, servings, difficulty);
        return recipe;
    }

    public void UpdateDetails(
        string title, string slug, string description, string? instructions,
        int prepTimeMinutes, int cookTimeMinutes, int servings,
        RecipeDifficulty difficulty, Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new DomainException("RECIPE_CATEGORY_REQUIRED", "CategoryId bắt buộc.");
        CategoryId = categoryId;
        Instructions = instructions?.Trim() ?? string.Empty;
        SetCoreFields(title, slug, description, prepTimeMinutes, cookTimeMinutes, servings, difficulty);
    }

    private void SetCoreFields(
        string title, string slug, string description,
        int prepTimeMinutes, int cookTimeMinutes, int servings, RecipeDifficulty difficulty)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length is < 5 or > 200)
            throw new DomainException("RECIPE_TITLE_INVALID", "Tiêu đề phải từ 5 đến 200 ký tự.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 220)
            throw new DomainException("RECIPE_SLUG_INVALID", "Slug phải từ 1 đến 220 ký tự.");
        if (description is { Length: > 2000 })
            throw new DomainException("RECIPE_DESCRIPTION_INVALID", "Mô tả tối đa 2000 ký tự.");
        if (prepTimeMinutes <= 0)
            throw new DomainException("RECIPE_PREPTIME_INVALID", "Thời gian chuẩn bị phải lớn hơn 0.");
        if (cookTimeMinutes < 0)
            throw new DomainException("RECIPE_COOKTIME_INVALID", "Thời gian nấu không được âm.");
        if (servings <= 0)
            throw new DomainException("RECIPE_SERVINGS_INVALID", "Khẩu phần phải lớn hơn 0.");

        Title = title.Trim();
        Slug = slug.Trim();
        Description = description?.Trim() ?? string.Empty;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
    }

    public void SetNutrition(RecipeNutrition nutrition) => Nutrition = nutrition;

    // ----- Ingredients -----
    public RecipeIngredient AddIngredient(string name, decimal? quantity, string? unit, string? notes)
    {
        var ingredient = new RecipeIngredient(Id, name, quantity, unit, notes, _ingredients.Count);
        _ingredients.Add(ingredient);
        return ingredient;
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        var ing = _ingredients.SingleOrDefault(i => i.Id == ingredientId)
                  ?? throw new DomainException("INGREDIENT_NOT_FOUND", "Nguyên liệu không thuộc công thức này.");
        _ingredients.Remove(ing);
        ReindexIngredients();
    }

    private void ReindexIngredients()
    {
        var ordered = _ingredients.OrderBy(i => i.OrderIndex).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].SetOrder(i);
    }

    // ----- Steps (StepNumber liên tục 1..N, D16) -----
    public RecipeStep AddStep(string title, string description, int? timerMinutes, string? imageUrl)
    {
        var nextNumber = _steps.Count + 1;
        var step = new RecipeStep(Id, nextNumber, title, description, timerMinutes, imageUrl);
        _steps.Add(step);
        return step;
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.SingleOrDefault(s => s.Id == stepId)
                   ?? throw new DomainException("STEP_NOT_FOUND", "Bước không thuộc công thức này.");
        _steps.Remove(step);
        RenumberSteps(); // giữ liên tục 1..N; thực hiện trong transaction ở Infrastructure
    }

    private void RenumberSteps()
    {
        var ordered = _steps.OrderBy(s => s.StepNumber).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].SetNumber(i + 1);
    }

    // ----- Images (đúng 1 primary khi có ảnh) -----
    public RecipeImage AddImage(string originalUrl, string? altText)
    {
        var isFirst = _images.Count == 0;
        var image = new RecipeImage(Id, originalUrl, altText, _images.Count, isPrimary: isFirst);
        _images.Add(image);
        return image;
    }

    public void SetPrimaryImage(Guid imageId)
    {
        var target = _images.SingleOrDefault(i => i.Id == imageId)
                     ?? throw new DomainException("IMAGE_NOT_FOUND", "Ảnh không thuộc công thức này.");
        foreach (var img in _images) img.SetPrimary(img.Id == target.Id);
    }

    public void RemoveImage(Guid imageId)
    {
        var img = _images.SingleOrDefault(i => i.Id == imageId)
                  ?? throw new DomainException("IMAGE_NOT_FOUND", "Ảnh không thuộc công thức này.");
        var wasPrimary = img.IsPrimary;
        _images.Remove(img);
        if (wasPrimary && _images.Count > 0)
            _images.OrderBy(i => i.OrderIndex).First().SetPrimary(true);
    }

    // ----- Vòng đời trạng thái -----
    public void Publish()
    {
        // D07: cần ít nhất 1 nguyên liệu và 1 bước; không bắt buộc ảnh.
        if (_ingredients.Count == 0 || _steps.Count == 0)
            throw new DomainException("RECIPE_PUBLISH_INCOMPLETE",
                "Cần ít nhất 1 nguyên liệu và 1 bước trước khi xuất bản.");

        if (Status == RecipeStatus.Published) return; // idempotent -> 200 (D07)
        Status = RecipeStatus.Published;
        PublishedAt ??= DateTime.UtcNow; // slug ổn định sau publish (D14)
    }

    public void Unpublish()
    {
        if (Status == RecipeStatus.Draft) return;
        Status = RecipeStatus.Draft;
    }

    public void Archive()
    {
        if (Status == RecipeStatus.Archived) return;
        Status = RecipeStatus.Archived;
    }
}
