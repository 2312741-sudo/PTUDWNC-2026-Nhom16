using CulinaryBlog.Application.Common;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CulinaryBlog.Application.Features.Recipes.Commands.DeleteRecipe;

public record DeleteRecipeCommand(Guid Id) : IRequest<Unit>;
public sealed class DeleteRecipeCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteRecipeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteRecipeCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes.FindAsync([request.Id], ct) ?? throw new NotFoundException("Recipe", request.Id);
        db.Recipes.Remove(recipe); await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
