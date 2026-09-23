using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<JwtService>();
builder.Services.AddDbContext<AuthDbContext>((sp, options) =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Configure ConnectionStrings:Database."),
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
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuthDbContext>());
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<AuditableEntityInterceptor>();
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
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AuthorPolicy", p => p.RequireRole(Roles.Author, Roles.Admin))
    .AddPolicy("AdminPolicy", p => p.RequireRole(Roles.Admin));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions.TryAdd("code", $"http.{context.ProblemDetails.Status}");
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    o.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, ct) =>
{
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
    await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    return;
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) { app.MapOpenApi(); app.MapScalarApiReference(); }
var auth = app.MapGroup("/api/v1/auth").WithTags("Authentication");
auth.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
{
    var res = await sender.Send(command, ct);
    return Results.Created("/api/v1/auth/me", new { data = res });
})
    .WithName("Register").Produces<object>(201).ProducesValidationProblem().ProducesProblem(409).RequireRateLimiting("auth");

auth.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(command, ct) }))
    .WithName("Login").Produces<object>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).RequireRateLimiting("auth");

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

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true, ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live"), ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthReportWriter.WriteJson });
app.Run();

public partial class Program;
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue("sub");
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
