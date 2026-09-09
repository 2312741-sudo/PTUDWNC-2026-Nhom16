using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Common.Models;
using MediatR;
using CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;
using CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;
using CulinaryBlog.Application.Features.Categories.Commands.DeleteCategory;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryById;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
namespace CulinaryBlog.API.Endpoints;
public record UpdateCategoryRequest(string Name, string? Description);
public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories");
        group.MapGet("/", async ([AsParameters] GetCategoriesQuery query, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(query, ct)))
            .WithName("GetCategories").WithSummary("Lấy danh sách Categories")
            .WithDescription("Phân trang page/pageSize (1–100), search, sortBy, descending. Sắp xếp: name hoặc createdat.")
            .Produces<PaginatedResult<CategoryDto>>(200).ProducesProblem(400);
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new GetCategoryByIdQuery(id), ct)))
            .WithName("GetCategoryById").WithSummary("Lấy chi tiết Category")
            .WithDescription("Lấy tài nguyên theo UUID.").Produces<CategoryDto>(200).ProducesProblem(404);
        group.MapPost("/", async (CreateCategoryCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/v1/categories/{result.Id}", result);
        }).WithName("CreateCategory").WithSummary("Tạo Category")
            .WithDescription("Tạo tài nguyên mới; trả 201 và Location trỏ tới tài nguyên.")
            .Produces<CategoryDto>(201).ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateCategoryCommand(id, body.Name, body.Description), ct)))
            .WithName("UpdateCategory").WithSummary("Cập nhật toàn bộ Category")
            .WithDescription("Gửi đầy đủ trường có thể sửa; ID lấy từ URL. Không thay đổi CreatedAt. Gửi lại cùng payload không đổi UpdatedAt.")
            .Produces<CategoryDto>(200).ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteCategoryCommand(id), ct); return Results.NoContent();
        }).WithName("DeleteCategory").WithSummary("Xóa Category")
            .WithDescription("Xóa thành công trả 204, tài nguyên không tồn tại trả 404. Danh mục còn công thức trả 409.")
            .Produces(204).ProducesProblem(404).ProducesProblem(409);
        group.MapGet("/{id:guid}/recipes", async (Guid id, [AsParameters] GetRecipesQuery query, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new GetCategoryByIdQuery(id), ct);
            return Results.Ok(await sender.Send(query with { CategoryId = id }, ct));
        }).WithName("GetRecipesByCategory").WithSummary("Lấy công thức thuộc danh mục")
            .WithDescription("CategoryId trong URL quyết định danh mục; hỗ trợ cùng bộ lọc/phân trang/sắp xếp như GET /recipes. Danh mục không tồn tại trả 404.")
            .Produces<PaginatedResult<RecipeDto>>(200).ProducesProblem(400).ProducesProblem(404);
        return app;
    }
}
