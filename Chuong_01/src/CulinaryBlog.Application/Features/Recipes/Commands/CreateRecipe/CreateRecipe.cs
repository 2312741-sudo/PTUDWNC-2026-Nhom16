using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Recipes.Commands.CreateRecipe;
public record CreateRecipeCommand(string Title, string? Description, string Instructions, int PrepTimeMinutes, int CookTimeMinutes, int Servings, Difficulty Difficulty, Guid CategoryId, Guid AuthorId) : IRequest<RecipeDetailDto>;
public sealed class CreateRecipeCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateRecipeCommand, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([request.CategoryId], ct) ?? throw new NotFoundException("Category", request.CategoryId);
        var recipe = Recipe.Create(request.Title, request.Description, request.Instructions, request.PrepTimeMinutes, request.CookTimeMinutes, request.Servings, request.Difficulty, request.CategoryId, request.AuthorId);
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);
        return recipe.Adapt<RecipeDetailDto>() with { Category = category.Adapt<CategoryDto>() };
    }
}
