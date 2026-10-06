using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;
using RecipeDifficulty = CulinaryBlog.Domain.Enums.RecipeDifficulty;
using RecipeStatus = CulinaryBlog.Domain.Enums.RecipeStatus;

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

    public void ResetIngredientsAndSteps()
    {
        _ingredients.Clear();
        _steps.Clear();
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

    /// <summary>N2-E4: bỏ cờ primary khỏi mọi ảnh — nửa đầu của <see cref="SetPrimaryImage"/>,
    /// tách riêng để use case lưu hai lần trong một transaction.</summary>
    /// <remarks>
    /// Unique index partial <c>ux_recipe_images_one_primary</c> được Postgres kiểm tra ngay từng
    /// statement, và EF không bảo đảm thứ tự phát lệnh UPDATE giữa các entity. Nên "hạ ảnh primary
    /// cũ" và "bật ảnh primary mới" phải là hai lần lưu riêng: lần này đưa DB về 0 primary (hợp lệ),
    /// lần sau mới bật ảnh mới. Trạng thái 0 primary chỉ tồn tại trong transaction.
    /// </remarks>
    public void ClearPrimaryImages()
    {
        foreach (var img in _images) img.SetPrimary(false);
    }

    /// <summary>
    /// N2-E4: ảnh sẽ thay thế ảnh <paramref name="imageId"/> khi ảnh đó là primary và bị xoá —
    /// cùng quy tắc chọn với <see cref="RemoveImage"/> (OrderIndex nhỏ nhất còn lại).
    /// Chỉ đọc, không thay đổi aggregate.
    /// </summary>
    public Guid? GetPrimaryReplacementCandidate(Guid imageId) =>
        _images.FirstOrDefault(i => i.Id == imageId)?.IsPrimary == true
            ? _images.Where(i => i.Id != imageId).OrderBy(i => i.OrderIndex).Select(i => (Guid?)i.Id).FirstOrDefault()
            : null;

    /// <summary>Cập nhật metadata ảnh (altText, orderIndex) qua aggregate — D17. isPrimary đi qua SetPrimaryImage.</summary>
    public void UpdateImageMetadata(Guid imageId, string? altText, int? orderIndex)
    {
        var target = _images.SingleOrDefault(i => i.Id == imageId)
                     ?? throw new DomainException("IMAGE_NOT_FOUND", "Ảnh không thuộc công thức này.");
        if (altText is not null) target.SetAltText(altText);
        if (orderIndex is not null) target.SetOrderIndex(orderIndex.Value);
    }

    /// <param name="promoteNext">
    /// false: chỉ gỡ ảnh, để người gọi lưu xong mới gọi <see cref="EnsurePrimaryImage"/>. Unique index một-ảnh-chính kiểm
    /// tra ngay sau từng UPDATE, và EF không biết bộ lọc của index nên có thể đôn ảnh mới lên trước khi ảnh cũ được
    /// đánh dấu xoá mềm -> 23505. Gỡ ảnh cũng bỏ cờ chính của nó để dòng xoá mềm không còn mang IsPrimary = true.
    /// </param>
    public void RemoveImage(Guid imageId, bool promoteNext = true)
    {
        var replacementId = RemoveImageCore(imageId);
        if (promoteNext) PromoteReplacement(replacementId);
    }

    /// <summary>
    /// N2-E4: xoá ảnh primary **không** promote ngay, trả về id ảnh sẽ được thay thế (null nếu
    /// không còn ảnh nào, hoặc ảnh bị xoá không phải primary).
    ///
    /// Vì sao tách ra: unique index partial <c>ux_recipe_images_one_primary</c> không deferrable
    /// trong Postgres, nên DB không được thấy hai dòng IsPrimary = true cùng lúc — kể cả tạm thời.
    /// EF ghi lệnh UPDATE (ảnh thay thế → true) **trước** lệnh DELETE (ảnh primary cũ), tạo ra
    /// trạng thái trung gian hai primary và sinh lỗi 23505. Use case xoá ảnh vì vậy lưu hai lần:
    /// (1) xoá, (2) bật primary mới — cùng một transaction.
    /// </summary>
    public Guid? RemoveImageDeferringPromotion(Guid imageId) => RemoveImageCore(imageId);

    /// <summary>Bật ảnh <paramref name="imageId"/> thành primary (dùng sau
    /// <see cref="RemoveImageDeferringPromotion"/>).</summary>
    public void PromotePrimaryImage(Guid imageId) => SetPrimaryImage(imageId);

    private Guid? RemoveImageCore(Guid imageId)
    {
        var img = _images.SingleOrDefault(i => i.Id == imageId)
                  ?? throw new DomainException("IMAGE_NOT_FOUND", "Ảnh không thuộc công thức này.");
        var wasPrimary = img.IsPrimary;
        img.SetPrimary(false);
        _images.Remove(img);
        return wasPrimary && _images.Count > 0
            ? _images.OrderBy(i => i.OrderIndex).First().Id
            : null;
    }

    private void PromoteReplacement(Guid? replacementId)
    {
        if (replacementId is { } id) SetPrimaryImage(id);
    }

    /// <summary>Còn ảnh mà chưa có ảnh chính thì ảnh có OrderIndex nhỏ nhất thành ảnh chính.</summary>
    public void EnsurePrimaryImage()
    {
        if (_images.Count > 0 && !_images.Any(i => i.IsPrimary))
            _images.OrderBy(i => i.OrderIndex).First().SetPrimary(true);
    }

    public void UpdateIngredient(Guid ingredientId, string name, decimal? quantity, string? unit, string? notes)
    {
        var ing = _ingredients.SingleOrDefault(i => i.Id == ingredientId)
                  ?? throw new DomainException("INGREDIENT_NOT_FOUND", "Nguyên liệu không thuộc công thức này.");
        ing.Update(name, quantity, unit, notes);
    }

    public void UpdateStep(Guid stepId, string title, string description, int? timerMinutes, string? imageUrl)
    {
        var step = _steps.SingleOrDefault(s => s.Id == stepId)
                   ?? throw new DomainException("STEP_NOT_FOUND", "Bước không thuộc công thức này.");
        step.Update(title, description, timerMinutes, imageUrl);
    }

    /// <summary>Đổi thứ tự nguyên liệu. orderedIds phải chứa đúng và đủ id của mọi nguyên liệu.</summary>
    public void ReorderIngredients(IReadOnlyList<Guid> orderedIds)
    {
        if (orderedIds.Count != _ingredients.Count || orderedIds.Distinct().Count() != orderedIds.Count)
            throw new DomainException("INGREDIENT_ORDER_INVALID",
                "Danh sách thứ tự phải chứa đúng và đủ id của mọi nguyên liệu.");

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var ing = _ingredients.SingleOrDefault(x => x.Id == orderedIds[i])
                      ?? throw new DomainException("INGREDIENT_NOT_FOUND", "Nguyên liệu không thuộc công thức này.");
            ing.SetOrder(i);
        }
    }

    /// <summary>
    /// Đổi thứ tự các bước, gán lại StepNumber liên tục 1..N (D16).
    /// Gán số âm ở bước trung gian để không va chạm unique (RecipeId, StepNumber) khi hoán đổi.
    /// Handler phải bọc trong transaction.
    /// </summary>
    /// <summary>
    /// Pha 1 khi đánh số lại: đẩy StepNumber sang vùng tạm (+10000) để các lệnh UPDATE lần lượt của EF
    /// không vi phạm unique index (RecipeId, StepNumber). Gọi + SaveChanges trước ReorderSteps, trong cùng transaction.
    /// </summary>
    public void MoveStepNumbersToTemporaryRange()
    {
        foreach (var s in _steps) s.SetNumber(s.StepNumber + TemporaryStepNumberOffset);
    }

    private const int TemporaryStepNumberOffset = 10_000;

    public void ReorderSteps(IReadOnlyList<Guid> orderedIds)
    {
        if (orderedIds.Count != _steps.Count || orderedIds.Distinct().Count() != orderedIds.Count)
            throw new DomainException("STEP_ORDER_INVALID",
                "Danh sách thứ tự phải chứa đúng và đủ id của mọi bước.");

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var s = _steps.SingleOrDefault(x => x.Id == orderedIds[i])
                    ?? throw new DomainException("STEP_NOT_FOUND", "Bước không thuộc công thức này.");
            s.SetNumber(-(i + 1));
        }

        foreach (var s in _steps)
            s.SetNumber(-s.StepNumber);
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

    /// <summary>
    /// Soft delete (D08 / ADR-0001): đánh dấu IsDeleted — global query filter ẩn khỏi mọi truy vấn ngay,
    /// dữ liệu (kể cả ảnh cần restore) được giữ. Không xoá vật lý.
    /// </summary>
    public void MarkDeleted()
    {
        IsDeleted = true;
        Status = RecipeStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Tên gọi tương thích với ADR-0001 (main/TV3) — cùng hành vi với <see cref="MarkDeleted"/>.
    /// Giữ cả hai để không phá vỡ call site đã có trên nhánh khác.
    /// </summary>
    public void SoftDelete() => MarkDeleted();
}
