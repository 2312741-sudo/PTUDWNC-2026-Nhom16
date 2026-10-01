using System.Security.Claims;
using System.Threading.RateLimiting;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Lab.TV3.Api.L10;

public sealed class LabPost
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Draft";
}

public sealed record PostRequest(string Title);
public sealed record CommentRequest(string Body);

/// <summary>Tên policy dùng chung, tránh gõ chuỗi rải rác.</summary>
public static class LabPolicies
{
    public const string Admin = "Admin";
    public const string VerifiedAuthor = "VerifiedAuthor";
    public const string PostOwner = "PostOwner";
    public const string CommentLimiter = "l10-comments";
    public const int CommentPermitLimit = 5;
    public static readonly TimeSpan CommentWindow = TimeSpan.FromMinutes(1);
}

/// <summary>Yêu cầu resource-based: chỉ chủ bài (hoặc Admin) được sửa/publish bài đó.</summary>
public sealed class PostOwnerRequirement : IAuthorizationRequirement;

public sealed class PostOwnerHandler : AuthorizationHandler<PostOwnerRequirement, LabPost>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PostOwnerRequirement requirement, LabPost resource)
    {
        // So sánh với claim "sub" trong JWT, không tin owner_id gửi từ client
        if (context.User.IsInRole("Admin") || context.User.FindFirstValue("sub") == resource.OwnerId.ToString())
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public static class L10Authorization
{
    public static IServiceCollection AddL10Authorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PostOwnerHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(LabPolicies.Admin, p => p.RequireAuthenticatedUser().RequireRole("Admin"))
            // VerifiedAuthor: claim verified_author=true (Admin được coi là vượt cấp)
            .AddPolicy(LabPolicies.VerifiedAuthor, p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
                ctx.User.IsInRole("Admin") || ctx.User.HasClaim("verified_author", "true")))
            .AddPolicy(LabPolicies.PostOwner, p => p.AddRequirements(new PostOwnerRequirement()));

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Fixed window theo từng user (claim sub); khách vô danh gộp theo IP
            o.AddPolicy(LabPolicies.CommentLimiter, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.User.FindFirstValue("sub") ?? "ip:" + ctx.Connection.RemoteIpAddress,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LabPolicies.CommentPermitLimit,
                    Window = LabPolicies.CommentWindow,
                    QueueLimit = 0, // không xếp hàng: vượt là trả 429 ngay
                }));
            o.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retry.TotalSeconds)).ToString();
                await Http.Err(429, "RATE_LIMITED", "Bạn gửi quá nhiều yêu cầu, hãy thử lại sau")
                    .ExecuteAsync(ctx.HttpContext);
            };
        });
        return services;
    }

    public static void MapL10Authorization(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/lab/l10").RequireAuthorization(); // mặc định: phải đăng nhập (Guest -> 401)
        g.MapPost("/posts", Create);
        g.MapPut("/posts/{id:guid}", Update);
        g.MapPost("/posts/{id:guid}/publish", Publish).RequireAuthorization(LabPolicies.VerifiedAuthor);
        g.MapPost("/posts/{id:guid}/comments", Comment).RequireRateLimiting(LabPolicies.CommentLimiter);
        g.MapGet("/admin/stats", Stats).RequireAuthorization(LabPolicies.Admin);
    }

    private const string Columns = "id, owner_id, title, status";

    private static async Task<IResult> Create(PostRequest r, ClaimsPrincipal user, LabDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) return Http.Err(400, "TITLE_REQUIRED", "Tiêu đề không được trống");
        await using var c = await db.OpenAsync(ct);
        var post = await c.QuerySingleAsync<LabPost>(
            $"INSERT INTO lab_posts (id, owner_id, title) VALUES (@id, @owner, @title) RETURNING {Columns}",
            new { id = Guid.NewGuid(), owner = Guid.Parse(user.FindFirstValue("sub")!), title = r.Title.Trim() });
        return Results.Created($"/lab/l10/posts/{post.Id}", new { data = post });
    }

    private static async Task<IResult> Update(Guid id, PostRequest r, ClaimsPrincipal user, LabDb db,
        IAuthorizationService auth, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) return Http.Err(400, "TITLE_REQUIRED", "Tiêu đề không được trống");
        await using var c = await db.OpenAsync(ct);
        var post = await Find(c, id);
        if (post is null) return Http.Err(404, "POST_NOT_FOUND", "Không tìm thấy bài");
        if (!(await auth.AuthorizeAsync(user, post, LabPolicies.PostOwner)).Succeeded) return Results.Forbid();

        post = await c.QuerySingleAsync<LabPost>(
            $"UPDATE lab_posts SET title = @title, updated_at = now() WHERE id = @id RETURNING {Columns}",
            new { id, title = r.Title.Trim() });
        return Results.Ok(new { data = post });
    }

    private static async Task<IResult> Publish(Guid id, ClaimsPrincipal user, LabDb db,
        IAuthorizationService auth, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var post = await Find(c, id);
        if (post is null) return Http.Err(404, "POST_NOT_FOUND", "Không tìm thấy bài");
        // Policy VerifiedAuthor đã chạy ở endpoint; ở đây kiểm thêm quyền trên đúng tài nguyên
        if (!(await auth.AuthorizeAsync(user, post, LabPolicies.PostOwner)).Succeeded) return Results.Forbid();

        post = await c.QuerySingleAsync<LabPost>(
            $"UPDATE lab_posts SET status = 'Published', updated_at = now() WHERE id = @id RETURNING {Columns}", new { id });
        return Results.Ok(new { data = post });
    }

    private static async Task<IResult> Comment(Guid id, CommentRequest r, ClaimsPrincipal user, LabDb db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Body)) return Http.Err(400, "BODY_REQUIRED", "Nội dung không được trống");
        await using var c = await db.OpenAsync(ct);
        if (await Find(c, id) is null) return Http.Err(404, "POST_NOT_FOUND", "Không tìm thấy bài");
        var commentId = Guid.NewGuid();
        await c.ExecuteAsync("INSERT INTO lab_comments (id, post_id, user_id, body) VALUES (@commentId, @id, @uid, @body)",
            new { commentId, id, uid = Guid.Parse(user.FindFirstValue("sub")!), body = r.Body.Trim() });
        return Results.Created($"/lab/l10/posts/{id}/comments/{commentId}", new { data = new { id = commentId } });
    }

    private static async Task<IResult> Stats(LabDb db, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var s = await c.QuerySingleAsync<(long Users, long Posts, long Comments)>("""
            SELECT (SELECT count(*) FROM lab_users), (SELECT count(*) FROM lab_posts), (SELECT count(*) FROM lab_comments)
            """);
        return Results.Ok(new { data = new { users = s.Users, posts = s.Posts, comments = s.Comments } });
    }

    private static Task<LabPost?> Find(System.Data.IDbConnection c, Guid id) =>
        c.QuerySingleOrDefaultAsync<LabPost>($"SELECT {Columns} FROM lab_posts WHERE id = @id", new { id });
}
