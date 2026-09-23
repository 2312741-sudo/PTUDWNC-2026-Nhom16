using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{envPort}");
}

builder.Host.UseSerilog((context, config) => config.MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Fatal)
    .Enrich.FromLogContext().WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}"));
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IConfiguration>().GetSection("Jwt").Get<JwtSettings>() ?? new();
    settings.Validate();
    return settings;
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    o.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuditableEntityInterceptor>();

builder.Services.AddDbContext<AuthDbContext>((sp, options) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var connectionString = Program.NormalizePostgreSqlConnectionString(
        cfg.GetConnectionString("Database")
        ?? cfg["DATABASE_URL"]
        ?? throw new InvalidOperationException("Configure ConnectionStrings:Database or DATABASE_URL."));
    options.UseNpgsql(
        connectionString,
        pg => pg.CommandTimeout(30));
    options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "";
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AuthDbContext>().AddSignInManager();
builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 100_000);
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<RecipeRepository>();
builder.Services.AddScoped<IRecipeRepository>(sp => sp.GetRequiredService<RecipeRepository>());
builder.Services.AddScoped<IRecipeDiscoveryRepository>(sp => sp.GetRequiredService<RecipeRepository>());
builder.Services.AddScoped<IRecipeImageRepository, RecipeImageRepository>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuthDbContext>());
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton<WelcomeEmailQueue>();
builder.Services.AddSingleton<IWelcomeEmailQueue>(sp => sp.GetRequiredService<WelcomeEmailQueue>());
builder.Services.AddHostedService<WelcomeEmailWorker>();
builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Environment.IsEnvironment("Testing") ? 1000 : 10;
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) => { context.HttpContext.Response.Headers.RetryAfter = "60"; return ValueTask.CompletedTask; };
    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.AddScoped<IFileStorageService, MinioStorageService>();
builder.Services.AddHealthChecks()
    .AddCheck<LivenessHealthCheck>("liveness", tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "all"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "all"])
    .AddCheck<MinIOHealthCheck>("minio", tags: ["all"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<JwtSettings>((options, jwt) =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole(CulinaryBlog.Domain.Roles.Admin));
    options.AddPolicy("AuthorPolicy", policy => policy.RequireRole(CulinaryBlog.Domain.Roles.Author, CulinaryBlog.Domain.Roles.Admin));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info = new() { Title = "CulinaryBlog API", Version = "v1" };
    document.Components ??= new();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" };
    return Task.CompletedTask;
}));
var app = builder.Build();
_ = app.Services.GetRequiredService<JwtSettings>();
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    try { await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync(); } catch { }
    Console.WriteLine("Database migrations applied successfully.");
    return;
}
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await DbSeeder.SeedAsync(authDb);
    Console.WriteLine("Database seeded successfully: 25 categories, 100 recipes (each with >=10 ingredients, >=5 steps).");
    return;
}

if (!args.Contains("--no-auto-migrate") && !builder.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        try
        {
            await authDb.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Auto-migration on startup skipped: {Message}", ex.Message);
        }

        try
        {
            await DbSeeder.SeedAsync(authDb);
            Log.Information("Database verified and seeded successfully on startup.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database seeding on startup skipped: {Message}", ex.Message);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization on startup error: {Message}", ex.Message);
    }
}

app.Use(async (context, next) =>
{
    // Generate server correlation IDs; do not trust arbitrary client strings in logs.
    context.TraceIdentifier = Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    if (context.Request.Path.StartsWithSegments("/api/v1/auth")) context.Response.Headers.CacheControl = "no-store";
    using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
    using (LogContext.PushProperty("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString()))
        await next();
});
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (log, context) => log.Set("UserId", context.User.FindFirstValue("sub") ?? "anonymous");
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapScalarApiReference();

var auth = app.MapGroup("/api/v1/auth").WithTags("Auth");
auth.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
    Results.Created("/api/v1/auth/me", new { data = await sender.Send(command, ct) }))
    .WithName("Register").Produces<object>().ProducesValidationProblem().ProducesProblem(400).ProducesProblem(409).RequireRateLimiting("auth");

auth.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(command, ct) }))
    .WithName("Login").Produces<object>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(423).RequireRateLimiting("auth");

auth.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(command, ct) }))
    .WithName("RefreshToken").Produces<object>().ProducesValidationProblem().ProducesProblem(401).RequireRateLimiting("auth");

auth.MapGet("/me", async (ISender sender, CancellationToken ct) => Results.Ok(new { data = await sender.Send(new GetMeQuery(), ct) }))
    .RequireAuthorization().WithName("GetMe").Produces<object>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

auth.MapPatch("/me", async (UpdateProfileCommand command, ISender sender, CancellationToken ct) => Results.Ok(new { data = await sender.Send(command, ct) }))
    .RequireAuthorization().WithName("UpdateMe").Produces<object>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

auth.MapPost("/logout", async (LogoutCommand? command, ISender sender, CancellationToken ct) =>
{
    await sender.Send(command ?? new LogoutCommand(), ct);
    return Results.NoContent();
})
    .RequireAuthorization().WithName("Logout").Produces(204).ProducesProblem(401);

auth.MapPost("/google", async (GoogleLoginCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(command, ct) }))
    .WithName("GoogleLogin").Produces<object>().ProducesValidationProblem().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(502);

var categories = app.MapGroup("/api/v1/categories").WithTags("Categories");
categories.MapGet("", async (ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new GetCategoriesQuery(), ct) }))
    .WithName("GetCategories").Produces<object>(200);

categories.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new GetCategoryBySlugQuery(slug), ct) }))
    .WithName("GetCategoryBySlug").Produces<object>(200).ProducesProblem(404);

categories.MapPost("", async (CreateCategoryCommand command, ISender sender, CancellationToken ct) =>
{
    var created = await sender.Send(command, ct);
    return Results.Created($"/api/v1/categories/{created.Slug}", new { data = created });
})
    .RequireAuthorization("AdminPolicy").WithName("CreateCategory")
    .Produces<object>(201).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(409);

categories.MapPut("/{id:guid}", async (Guid id, UpdateCategoryCommand command, ISender sender, CancellationToken ct) =>
{
    if (id != command.Id)
        return Results.BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Id trong URL không khớp với body.", Extensions = { ["code"] = "request.invalid" } });
    var updated = await sender.Send(command, ct);
    return Results.Ok(new { data = updated });
})
    .RequireAuthorization("AdminPolicy").WithName("UpdateCategory")
    .Produces<object>(200).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

categories.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteCategoryCommand(id), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AdminPolicy").WithName("DeleteCategory")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

var recipes = app.MapGroup("/api/v1/recipes").WithTags("Recipes");
recipes.MapGet("", async ([AsParameters] GetRecipesQuery query, ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(query, ct)))
    .WithName("GetRecipes").Produces<PagedResult<RecipeSummaryDto>>(200).ProducesValidationProblem();

recipes.MapGet("/search", async ([AsParameters] SearchRecipesQuery query, ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(query, ct)))
    .WithName("SearchRecipes").Produces<PagedResult<RecipeSummaryDto>>(200).ProducesValidationProblem();

recipes.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new GetRecipeBySlugQuery(slug), ct) }))
    .WithName("GetRecipeBySlug").Produces<object>(200).ProducesProblem(404);

recipes.MapPost("", async (CreateRecipeCommand command, ISender sender, CancellationToken ct) =>
{
    var created = await sender.Send(command, ct);
    return Results.Created($"/api/v1/recipes/{created.Slug}", new { data = created });
})
    .RequireAuthorization("AuthorPolicy").WithName("CreateRecipe")
    .Produces<object>(201).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

recipes.MapPut("/{id:guid}", async (Guid id, UpdateRecipeBody body, ISender sender, CancellationToken ct) =>
    Results.Ok(new
    {
        data = await sender.Send(new UpdateRecipeCommand(
            id, body.Title, body.Description, body.Instructions,
            body.PrepTimeMinutes, body.CookTimeMinutes, body.Servings,
            body.Difficulty, body.CategoryId, body.Nutrition, body.RowVersion), ct)
    }))
    .RequireAuthorization("AuthorPolicy").WithName("UpdateRecipe")
    .Produces<object>(200).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

recipes.MapPost("/{id:guid}/ingredients", async (Guid id, IngredientBody b, ISender sender, CancellationToken ct) =>
{
    var created = await sender.Send(new AddIngredientCommand(id, b.Name, b.Quantity, b.Unit, b.Notes), ct);
    return Results.Created($"/api/v1/recipes/{id}/ingredients/{created.Id}", new { data = created });
})
    .RequireAuthorization("AuthorPolicy").WithName("AddIngredient")
    .Produces<object>(201).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapPut("/{id:guid}/ingredients/{ingredientId:guid}",
    async (Guid id, Guid ingredientId, IngredientBody b, ISender sender, CancellationToken ct) =>
    Results.Ok(new
    {
        data = await sender.Send(new UpdateIngredientCommand(id, ingredientId, b.Name, b.Quantity, b.Unit, b.Notes), ct)
    }))
    .RequireAuthorization("AuthorPolicy").WithName("UpdateIngredient")
    .Produces<object>(200).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapDelete("/{id:guid}/ingredients/{ingredientId:guid}",
    async (Guid id, Guid ingredientId, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteIngredientCommand(id, ingredientId), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AuthorPolicy").WithName("DeleteIngredient")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapPost("/{id:guid}/steps", async (Guid id, StepBody b, ISender sender, CancellationToken ct) =>
{
    var created = await sender.Send(new AddStepCommand(id, b.Title, b.Description, b.TimerMinutes, b.ImageUrl), ct);
    return Results.Created($"/api/v1/recipes/{id}/steps/{created.Id}", new { data = created });
})
    .RequireAuthorization("AuthorPolicy").WithName("AddStep")
    .Produces<object>(201).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapPut("/{id:guid}/steps/{stepId:guid}",
    async (Guid id, Guid stepId, StepBody b, ISender sender, CancellationToken ct) =>
    Results.Ok(new
    {
        data = await sender.Send(new UpdateStepCommand(id, stepId, b.Title, b.Description, b.TimerMinutes, b.ImageUrl), ct)
    }))
    .RequireAuthorization("AuthorPolicy").WithName("UpdateStep")
    .Produces<object>(200).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapDelete("/{id:guid}/steps/{stepId:guid}",
    async (Guid id, Guid stepId, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteStepCommand(id, stepId), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AuthorPolicy").WithName("DeleteStep")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

recipes.MapPatch("/{id:guid}/steps/reorder",
    async (Guid id, ReorderStepsBody b, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new ReorderStepsCommand(id, b.OrderedStepIds), ct) }))
    .RequireAuthorization("AuthorPolicy").WithName("ReorderSteps")
    .Produces<object>(200).ProducesValidationProblem()
    .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

// D3.1/D3.2 (TV4): publish/unpublish — điều kiện >=1 ingredient VÀ >=1 step (C02). Thiếu -> 422 RECIPE_PUBLISH_INCOMPLETE.
recipes.MapPatch("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new PublishRecipeCommand(id), ct) }))
    .RequireAuthorization("AuthorPolicy").WithName("PublishRecipe")
    .Produces<object>(200).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

recipes.MapPatch("/{id:guid}/unpublish", async (Guid id, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new UnpublishRecipeCommand(id), ct) }))
    .RequireAuthorization("AuthorPolicy").WithName("UnpublishRecipe")
    .Produces<object>(200).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

// D1.3 (TV4): quản lý hình ảnh recipe — upload, chỉnh metadata, xóa (IMAGE_CONTRACT).
recipes.MapPost("/{id:guid}/images", async (Guid id, [Microsoft.AspNetCore.Mvc.FromForm] IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] string? altText, ISender sender, HttpRequest request, CancellationToken ct) =>
{
    using var buffer = new MemoryStream();
    await file.CopyToAsync(buffer, ct);
    buffer.Position = 0;
    var dto = await sender.Send(new UploadRecipeImageCommand(
        id,
        file.FileName,
        file.ContentType ?? "application/octet-stream",
        altText,
        buffer.Length,
        buffer), ct);
    return Results.Created($"/api/v1/recipes/{id}/images/{dto.Id}", new { data = dto });
})
    .RequireAuthorization().WithName("UploadRecipeImage")
    .DisableAntiforgery()
    .Produces<object>(201).ProducesValidationProblem().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

recipes.MapPatch("/{id:guid}/images/{imageId:guid}", async (Guid id, Guid imageId, RecipeImagePatch patch, ISender sender, CancellationToken ct) =>
{
    var dto = await sender.Send(new UpdateRecipeImageCommand(id, imageId, patch.IsPrimary, patch.AltText, patch.OrderIndex), ct);
    return Results.Ok(new { data = dto });
})
    .RequireAuthorization().WithName("UpdateRecipeImage")
    .Produces<object>(200).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

recipes.MapDelete("/{id:guid}/images/{imageId:guid}", async (Guid id, Guid imageId, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteRecipeImageCommand(id, imageId), ct);
    return Results.NoContent();
})
    .RequireAuthorization().WithName("DeleteRecipeImage")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true, ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live"), ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthReportWriter.WriteJson });
app.Run();

public partial class Program
{
    public static string NormalizePostgreSqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = Npgsql.SslMode.Prefer
            };

            if (!string.IsNullOrEmpty(uri.Query))
            {
                var queryPairs = uri.Query.TrimStart('?').Split('&');
                foreach (var pair in queryPairs)
                {
                    var kvp = pair.Split('=', 2);
                    if (kvp.Length == 2 && kvp[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Enum.TryParse<Npgsql.SslMode>(kvp[1], true, out var ssl))
                        {
                            npgsqlBuilder.SslMode = ssl;
                        }
                    }
                }
            }

            return npgsqlBuilder.ConnectionString;
        }

        return connectionString;
    }
}

public sealed record RecipeImagePatch(bool? IsPrimary = null, string? AltText = null, int? OrderIndex = null);

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue("sub");
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
