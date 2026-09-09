using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;
public sealed class GetCategoryByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken ct) =>
        await db.Categories.AsNoTracking().Where(c => c.Id == request.Id).ProjectToType<CategoryDto>().SingleOrDefaultAsync(ct)
        ?? throw new NotFoundException("Category", request.Id);
}
