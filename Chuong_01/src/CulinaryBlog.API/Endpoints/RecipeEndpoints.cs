using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Common.Models;
using MediatR;
using CulinaryBlog.Application.Features.Recipes.Commands.CreateRecipe;
using CulinaryBlog.Application.Features.Recipes.Commands.UpdateRecipe;
using CulinaryBlog.Application.Features.Recipes.Commands.DeleteRecipe;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeById;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
namespace CulinaryBlog.API.Endpoints;
public record UpdateRecipeRequest(string Title, string? Description, string Instructions, int PrepTimeMinutes, int CookTimeMinutes, int Servings, CulinaryBlog.Domain.Enums.Difficulty Difficulty, Guid CategoryId, Guid AuthorId);
public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes").WithTags("Recipes");
        group.MapGet("/", async ([AsParameters] GetRecipesQuery query, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(query, ct)))
            .WithName("GetRecipes").WithSummary("Lấy danh sách Recipes")
            .WithDescription("Phân trang page/pageSize (1–100), search, sortBy, descending. Recipe hỗ trợ categoryId, difficulty=Easy|Medium|Hard, minCookTime, maxCookTime.")
            .Produces<PaginatedResult<RecipeDto>>(200).ProducesProblem(400);
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new GetRecipeByIdQuery(id), ct)))
            .WithName("GetRecipeById").WithSummary("Lấy chi tiết Recipe")
            .WithDescription("Lấy tài nguyên theo UUID. Kết quả bao gồm Instructions và thông tin Category.").Produces<RecipeDetailDto>(200).ProducesProblem(404);
        group.MapPost("/", async (CreateRecipeCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/v1/recipes/{result.Id}", result);
        }).WithName("CreateRecipe").WithSummary("Tạo Recipe")
            .WithDescription("Tạo tài nguyên mới; trả 201 và Location trỏ tới tài nguyên.")
            .Produces<RecipeDetailDto>(201).ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
        group.MapPut("/{id:guid}", async (Guid id, UpdateRecipeRequest body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateRecipeCommand(id, body.Title, body.Description, body.Instructions, body.PrepTimeMinutes, body.CookTimeMinutes, body.Servings, body.Difficulty, body.CategoryId, body.AuthorId), ct)))
            .WithName("UpdateRecipe").WithSummary("Cập nhật toàn bộ Recipe")
            .WithDescription("Gửi đầy đủ trường có thể sửa; ID lấy từ URL. Không thay đổi CreatedAt. Gửi lại cùng payload không đổi UpdatedAt.")
            .Produces<RecipeDetailDto>(200).ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteRecipeCommand(id), ct); return Results.NoContent();
        }).WithName("DeleteRecipe").WithSummary("Xóa Recipe")
            .WithDescription("Xóa thành công trả 204, tài nguyên không tồn tại trả 404.")
            .Produces(204).ProducesProblem(404).ProducesProblem(409);
        return app;
    }
}
