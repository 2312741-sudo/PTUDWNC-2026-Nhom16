namespace CulinaryBlog.Application.DTOs;
public record CategoryDto(Guid Id, string Name, string Slug, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);
