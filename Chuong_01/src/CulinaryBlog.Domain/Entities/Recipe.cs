using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Exceptions;
namespace CulinaryBlog.Domain.Entities;
public class Recipe
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public string Instructions { get; private set; } = "";
    public int PrepTimeMinutes { get; private set; }
    public int CookTimeMinutes { get; private set; }
    public int Servings { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    protected Recipe() { }
    public static Recipe Create(string title, string? description, string instructions,
        int prepTimeMinutes, int cookTimeMinutes, int servings, Difficulty difficulty, Guid categoryId, Guid authorId)
    {
        var entity = new Recipe { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
        entity.SetValues(title, description, instructions, prepTimeMinutes, cookTimeMinutes, servings, difficulty, categoryId, authorId);
        return entity;
    }
    public void Update(string title, string? description, string instructions,
        int prepTimeMinutes, int cookTimeMinutes, int servings, Difficulty difficulty, Guid categoryId, Guid authorId)
    {
        if (Title == title?.Trim() && Description == description?.Trim() && Instructions == instructions?.Trim()
            && PrepTimeMinutes == prepTimeMinutes && CookTimeMinutes == cookTimeMinutes && Servings == servings
            && Difficulty == difficulty && CategoryId == categoryId && AuthorId == authorId) return;
        SetValues(title!, description, instructions!, prepTimeMinutes, cookTimeMinutes, servings, difficulty, categoryId, authorId);
        UpdatedAt = DateTime.UtcNow;
    }
    private void SetValues(string title, string? description, string instructions,
        int prepTimeMinutes, int cookTimeMinutes, int servings, Difficulty difficulty, Guid categoryId, Guid authorId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200) throw new DomainException("Tiêu đề phải có từ 1 đến 200 ký tự.");
        if (description?.Trim().Length > 4000) throw new DomainException("Mô tả tối đa 4000 ký tự.");
        if (string.IsNullOrWhiteSpace(instructions) || instructions.Trim().Length > 20000) throw new DomainException("Hướng dẫn phải có từ 1 đến 20000 ký tự.");
        if (prepTimeMinutes < 0 || cookTimeMinutes < 0) throw new DomainException("Thời gian chuẩn bị và nấu không được âm.");
        if (servings <= 0) throw new DomainException("Số khẩu phần phải lớn hơn 0.");
        if (!Enum.IsDefined(difficulty)) throw new DomainException("Độ khó không hợp lệ.");
        if (categoryId == Guid.Empty || authorId == Guid.Empty) throw new DomainException("CategoryId và AuthorId phải khác GUID rỗng.");
        Title = title.Trim(); Description = description?.Trim(); Instructions = instructions.Trim();
        PrepTimeMinutes = prepTimeMinutes; CookTimeMinutes = cookTimeMinutes; Servings = servings;
        Difficulty = difficulty; CategoryId = categoryId; AuthorId = authorId;
    }
}
