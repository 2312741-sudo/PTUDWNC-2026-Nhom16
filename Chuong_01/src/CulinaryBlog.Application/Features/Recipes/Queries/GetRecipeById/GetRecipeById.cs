using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeById;

public record GetRecipeByIdQuery(Guid Id) : IRequest<RecipeDetailDto>;
public sealed class GetRecipeByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRecipeByIdQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(GetRecipeByIdQuery request, CancellationToken ct) =>
        await db.Recipes.AsNoTracking().Where(r => r.Id == request.Id).ProjectToType<RecipeDetailDto>().SingleOrDefaultAsync(ct)
        ?? throw new NotFoundException("Recipe", request.Id);
}
