using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
public sealed class CreateCategoryCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = Category.Create(request.Name, request.Description);
        if (await db.Categories.AnyAsync(c => c.Slug == category.Slug, ct)) throw new ConflictException("Slug danh mục đã tồn tại.");
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Adapt<CategoryDto>();
    }
}
