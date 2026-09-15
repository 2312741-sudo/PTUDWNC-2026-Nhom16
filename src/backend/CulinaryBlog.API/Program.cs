using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;

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
builder.Services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Configure ConnectionStrings:Database."),
    pg => pg.CommandTimeout(30)));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "";
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AuthDbContext>();
builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 100_000);
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
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
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); app.MapScalarApiReference(); }
var auth = app.MapGroup("/api/v1/auth").WithTags("Authentication");
auth.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
    Results.Created("/api/v1/auth/me", await sender.Send(command, ct)))
    .WithName("Register").Produces<AuthResponse>(201).ProducesValidationProblem().ProducesProblem(409);
auth.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(command, ct)))
    .WithName("Login").Produces<AuthResponse>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403);
auth.MapGet("/me", async (ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new GetMeQuery(), ct)))
    .RequireAuthorization().WithName("GetMe").Produces<UserDto>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);

var categories = app.MapGroup("/api/v1/categories").WithTags("Categories");
categories.MapGet("", async (ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(new GetCategoriesQuery(), ct)))
    .WithName("GetCategories").Produces<IReadOnlyList<CategoryDto>>(200);

categories.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(new GetCategoryBySlugQuery(slug), ct)))
    .WithName("GetCategoryBySlug").Produces<CategoryDto>(200).ProducesProblem(404);

categories.MapPost("", async (CreateCategoryCommand command, ISender sender, CancellationToken ct) =>
{
    var created = await sender.Send(command, ct);
    return Results.Created($"/api/v1/categories/{created.Slug}", created);
})
    .RequireAuthorization("AdminPolicy").WithName("CreateCategory")
    .Produces<CategoryDto>(201).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(409);

categories.MapPut("/{id:guid}", async (Guid id, UpdateCategoryCommand command, ISender sender, CancellationToken ct) =>
{
    if (id != command.Id)
        return Results.BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Id trong URL không khớp với body.", Extensions = { ["code"] = "request.invalid" } });
    var updated = await sender.Send(command, ct);
    return Results.Ok(updated);
})
    .RequireAuthorization("AdminPolicy").WithName("UpdateCategory")
    .Produces<CategoryDto>(200).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

categories.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteCategoryCommand(id), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AdminPolicy").WithName("DeleteCategory")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

app.Run();

public partial class Program;
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue("sub");
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
