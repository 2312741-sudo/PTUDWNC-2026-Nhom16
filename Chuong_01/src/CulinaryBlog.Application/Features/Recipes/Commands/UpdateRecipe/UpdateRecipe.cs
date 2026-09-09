using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Recipes.Commands.UpdateRecipe;
public record UpdateRecipeCommand(Guid Id, string Title, string? Description, string Instructions, int PrepTimeMinutes, int CookTimeMinutes, int Servings, Difficulty Difficulty, Guid CategoryId, Guid AuthorId) : IRequest<RecipeDetailDto>;
public sealed class UpdateRecipeCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateRecipeCommand, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(UpdateRecipeCommand request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([request.CategoryId], ct) ?? throw new NotFoundException("Category", request.CategoryId);
        var recipe = await db.Recipes.FindAsync([request.Id], ct) ?? throw new NotFoundException("Recipe", request.Id);
        recipe.Update(request.Title, request.Description, request.Instructions, request.PrepTimeMinutes, request.CookTimeMinutes, request.Servings, request.Difficulty, request.CategoryId, request.AuthorId);
        await db.SaveChangesAsync(ct);
        return recipe.Adapt<RecipeDetailDto>() with { Category = category.Adapt<CategoryDto>() };
    }
}
