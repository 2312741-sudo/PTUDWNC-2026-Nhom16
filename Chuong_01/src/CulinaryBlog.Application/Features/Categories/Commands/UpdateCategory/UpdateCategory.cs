using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(Guid Id, string Name, string? Description) : IRequest<CategoryDto>;
public sealed class UpdateCategoryCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([request.Id], ct) ?? throw new NotFoundException("Category", request.Id);
        category.Update(request.Name, request.Description);
        if (await db.Categories.AnyAsync(c => c.Id != request.Id && c.Slug == category.Slug, ct)) throw new ConflictException("Slug danh mục đã tồn tại.");
        await db.SaveChangesAsync(ct);
        return category.Adapt<CategoryDto>();
    }
}
