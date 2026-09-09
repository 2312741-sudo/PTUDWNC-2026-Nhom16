using CulinaryBlog.Domain.Enums;
namespace CulinaryBlog.Application.DTOs;
public record RecipeDto(Guid Id, string Title, string? Description, int PrepTimeMinutes, int CookTimeMinutes,
    int Servings, Difficulty Difficulty, Guid CategoryId, Guid AuthorId, DateTime CreatedAt, DateTime? UpdatedAt);
public record RecipeDetailDto(Guid Id, string Title, string? Description, string Instructions, int PrepTimeMinutes,
    int CookTimeMinutes, int Servings, Difficulty Difficulty, Guid CategoryId, Guid AuthorId,
    DateTime CreatedAt, DateTime? UpdatedAt, CategoryDto Category);
