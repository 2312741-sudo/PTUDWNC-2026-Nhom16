using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<Unit>;
public sealed class DeleteCategoryCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([request.Id], ct) ?? throw new NotFoundException("Category", request.Id);
        if (await db.Recipes.AnyAsync(r => r.CategoryId == request.Id, ct)) throw new ConflictException("Danh mục còn công thức; hãy xóa hoặc chuyển công thức trước.");
        db.Categories.Remove(category); await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
