using System.Security.Claims;
using CulinaryBlog.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me/recipes")] // ĐỐI CHIẾU: prefix route với các controller C2 (vd. api/v1/...)
public sealed class MyRecipesController(ISender sender) : ControllerBase
{
    // GET /api/me/recipes?page=1&pageSize=20&sortBy=updatedAt&sortOrder=desc&status=Draft&q=phở
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "updatedAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var authorId = CurrentUserId();
        if (authorId is null) return Unauthorized();

        var result = await sender.Send(
            new GetMyRecipesQuery(authorId, page, pageSize, sortBy, sortOrder, status, q), ct);

        return Ok(new { data = result }); // C08
    }

    // GET /api/me/recipes/counts
    [HttpGet("counts")]
    public async Task<IActionResult> Counts(CancellationToken ct)
    {
        var authorId = CurrentUserId();
        if (authorId is null) return Unauthorized();

        var result = await sender.Send(new GetMyRecipeCountsQuery(authorId), ct);
        return Ok(new { data = result });
    }

    // ĐỐI CHIẾU: nếu C2 đã có ICurrentUserService thì dùng nó thay cho hàm này.
    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
