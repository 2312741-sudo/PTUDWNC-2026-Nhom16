namespace CulinaryBlog.Domain;

public static class RecipeStatusValues
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";

    public static readonly string[] All = [Draft, Published, Archived];
}

public static class RecipeDifficultyValues
{
    public const string Easy = "Easy";
    public const string Medium = "Medium";
    public const string Hard = "Hard";
    public const string Expert = "Expert";

    public static readonly string[] All = [Easy, Medium, Hard, Expert];
}

public class Recipe
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public int PrepTimeMinutes { get; private set; }
    public int CookTimeMinutes { get; private set; }
    public int Servings { get; private set; }
    public string Difficulty { get; private set; } = RecipeDifficultyValues.Medium;
    public string Status { get; private set; } = RecipeStatusValues.Draft;
    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid CategoryId { get; private set; }
    public string AuthorId { get; private set; } = string.Empty;
    public string? PrimaryImageUrl { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public Category? Category { get; set; }

    private Recipe() { }

    public Recipe(
        string title,
        string slug,
        string description,
        string instructions,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string difficulty,
        Guid categoryId,
        string authorId,
        string? primaryImageUrl = null,
        string status = RecipeStatusValues.Draft)
    {
        Id = Guid.NewGuid();
        SetTitle(title);
        SetSlug(slug);
        SetDescription(description);
        Instructions = instructions ?? string.Empty;
        SetTimes(prepTimeMinutes, cookTimeMinutes, servings);
        SetDifficulty(difficulty);
        CategoryId = categoryId;
        AuthorId = string.IsNullOrWhiteSpace(authorId) ? throw new ArgumentException("AuthorId không được để trống.", nameof(authorId)) : authorId;
        PrimaryImageUrl = primaryImageUrl;
        Status = RecipeStatusValues.All.Contains(status) ? status : RecipeStatusValues.Draft;
        if (Status == RecipeStatusValues.Published)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }

        IsDeleted = false;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        Status = RecipeStatusValues.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        Status = RecipeStatusValues.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = RecipeStatusValues.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPrimaryImage(string? imageUrl)
    {
        PrimaryImageUrl = imageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề công thức không được để trống.", nameof(title));

        var trimmed = title.Trim();
        if (trimmed.Length is < 5 or > 200)
            throw new ArgumentException("Tiêu đề công thức phải có từ 5 đến 200 ký tự.", nameof(title));

        if (trimmed.Any(char.IsControl) || trimmed.Contains('<') || trimmed.Contains('>'))
            throw new ArgumentException("Tiêu đề không được chứa ký tự điều khiển hoặc thẻ HTML.", nameof(title));

        Title = trimmed;
    }

    private void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug không được để trống.", nameof(slug));

        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length > 220)
            throw new ArgumentException("Slug không được vượt quá 220 ký tự.", nameof(slug));

        Slug = trimmed;
    }

    private void SetDescription(string description)
    {
        var trimmed = (description ?? string.Empty).Trim();
        if (trimmed.Length > 2000)
            throw new ArgumentException("Mô tả công thức không được vượt quá 2000 ký tự.", nameof(description));

        Description = trimmed;
    }

    private void SetTimes(int prepTime, int cookTime, int servings)
    {
        if (prepTime <= 0)
            throw new ArgumentException("Thời gian chuẩn bị phải lớn hơn 0 phút.", nameof(prepTime));
        if (cookTime < 0)
            throw new ArgumentException("Thời gian nấu không được âm.", nameof(cookTime));
        if (servings <= 0)
            throw new ArgumentException("Số khẩu phần phải lớn hơn 0.", nameof(servings));

        PrepTimeMinutes = prepTime;
        CookTimeMinutes = cookTime;
        Servings = servings;
    }

    private void SetDifficulty(string difficulty)
    {
        if (!RecipeDifficultyValues.All.Contains(difficulty))
            throw new ArgumentException($"Độ khó '{difficulty}' không hợp lệ. Cho phép: {string.Join(", ", RecipeDifficultyValues.All)}.", nameof(difficulty));

        Difficulty = difficulty;
    }
}
