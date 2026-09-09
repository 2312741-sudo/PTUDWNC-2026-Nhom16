using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(int Page = 1, int PageSize = 10, string? Search = null, Guid? CategoryId = null,
    Difficulty? Difficulty = null, int? MinCookTime = null, int? MaxCookTime = null,
    string SortBy = "createdat", bool Descending = false) : IRequest<PaginatedResult<RecipeDto>>;
public sealed class GetRecipesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRecipesQuery, PaginatedResult<RecipeDto>>
{
    public async Task<PaginatedResult<RecipeDto>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        QueryValidation.Validate(request.Page, request.PageSize, request.SortBy, "title", "createdat", "cooktimeminutes", "preptimeminutes", "difficulty");
        if (request.MinCookTime < 0 || request.MaxCookTime < 0 || request.MinCookTime > request.MaxCookTime)
            throw new RequestValidationException("Khoảng thời gian nấu phải không âm, minCookTime <= maxCookTime.");
        if (request.Difficulty.HasValue && !Enum.IsDefined(request.Difficulty.Value)) throw new RequestValidationException("Độ khó không hợp lệ.");
        var query = db.Recipes.AsNoTracking();
        if (request.CategoryId.HasValue) query = query.Where(r => r.CategoryId == request.CategoryId);
        if (request.Difficulty.HasValue) query = query.Where(r => r.Difficulty == request.Difficulty);
        if (request.MinCookTime.HasValue) query = query.Where(r => r.CookTimeMinutes >= request.MinCookTime);
        if (request.MaxCookTime.HasValue) query = query.Where(r => r.CookTimeMinutes <= request.MaxCookTime);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Title.ToLower().Contains(search) || (r.Description != null && r.Description.ToLower().Contains(search)));
        }
        var count = await query.CountAsync(ct);
        var sorted = (request.SortBy.ToLowerInvariant(), request.Descending) switch
        {
            ("title", false) => query.OrderBy(r => r.Title), ("title", true) => query.OrderByDescending(r => r.Title),
            ("cooktimeminutes", false) => query.OrderBy(r => r.CookTimeMinutes), ("cooktimeminutes", true) => query.OrderByDescending(r => r.CookTimeMinutes),
            ("preptimeminutes", false) => query.OrderBy(r => r.PrepTimeMinutes), ("preptimeminutes", true) => query.OrderByDescending(r => r.PrepTimeMinutes),
            ("difficulty", false) => query.OrderBy(r => r.Difficulty), ("difficulty", true) => query.OrderByDescending(r => r.Difficulty),
            (_, true) => query.OrderByDescending(r => r.CreatedAt), _ => query.OrderBy(r => r.CreatedAt)
        };
        var items = await sorted.ThenBy(r => r.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ProjectToType<RecipeDto>().ToListAsync(ct);
        return new(items, count, request.Page, request.PageSize);
    }
}
