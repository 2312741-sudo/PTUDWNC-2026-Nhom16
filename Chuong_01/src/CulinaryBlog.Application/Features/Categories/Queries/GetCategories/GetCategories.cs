using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery(int Page = 1, int PageSize = 10, string? Search = null, string SortBy = "name", bool Descending = false) : IRequest<PaginatedResult<CategoryDto>>;
public sealed class GetCategoriesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCategoriesQuery, PaginatedResult<CategoryDto>>
{
    public async Task<PaginatedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        QueryValidation.Validate(request.Page, request.PageSize, request.SortBy, "name", "createdat");
        var query = db.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(search) || (c.Description != null && c.Description.ToLower().Contains(search)));
        }
        var count = await query.CountAsync(ct);
        var sorted = (request.SortBy.ToLowerInvariant(), request.Descending) switch
        {
            ("createdat", false) => query.OrderBy(c => c.CreatedAt),
            ("createdat", true) => query.OrderByDescending(c => c.CreatedAt),
            (_, true) => query.OrderByDescending(c => c.Name),
            _ => query.OrderBy(c => c.Name)
        };
        var items = await sorted.ThenBy(c => c.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ProjectToType<CategoryDto>().ToListAsync(ct);
        return new(items, count, request.Page, request.PageSize);
    }
}
