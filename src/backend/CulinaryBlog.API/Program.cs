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
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;
// Nạp .env (giá trị thật của máy) TRƯỚC khi dựng builder, vì CreateBuilder đọc biến môi trường.
// Default nằm trong appsettings*.json; .env chỉ override, và bị bỏ qua khi Production.
EnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{envPort}");
}

// N1-3 (tuần 4): log vào Seq khi có cấu hình trỏ tới Seq (docker-compose.dev.yml hoặc
// dịch vụ log của Render). Console LUÔN bật vì đó là nơi duy nhất còn lại khi Seq không sống —
// sink Seq hỏng không được làm mất log.
builder.Host.UseSerilog((context, config) =>
{
    config.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CulinaryBlog.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}");

    // Sink Seq phải nằm TRONG logger của UseSerilog. Trước đây nó được gán riêng vào
    // Log.Logger, nhưng UseSerilog lập tức thay thế logger đó — Seq nhận 0 event của app.
    var url = context.Configuration["Seq:Url"];
    if (!string.IsNullOrWhiteSpace(url))
    {
        try { config.WriteTo.Seq(url); }
        catch (Exception ex)
        {
            // Seq hỏng KHÔNG được làm app không khởi động; console vẫn còn log.
            Console.Error.WriteLine($"Khong duoc gan Seq sink ({ex.Message}). Chi ghi log ra console.");
        }
    }
});
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
builder.Services.AddScoped<IMyRecipesRepository, MyRecipesRepository>();
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

// N1-5 (tuần 4): cache phải dùng chung giữa các API instance nên dùng Redis thật thay vì
// ConcurrentDictionary trong tiến trình. Connection là singleton (nhiều thread dùng chung) và
// AbortOnConnectFail=false để Redis chết không làm app không khởi động — /health/ready lo việc báo.
var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(RecipeCacheService.BuildConfiguration(redisOptions)));
builder.Services.AddSingleton<IRecipeCacheService>(sp => new RecipeCacheService(
    sp.GetRequiredService<ILogger<RecipeCacheService>>(),
    sp.GetRequiredService<IConnectionMultiplexer>(),
    sp.GetRequiredService<IOptions<RedisOptions>>()));
// B2 (issue #21, N1-7): validate lúc khởi động — AccessKey/SecretKey rỗng thì fail-fast,
// không đợi tới lúc người dùng bấm "Tải lên" mới nhận 503/500. Bỏ qua môi trường Testing
// vì ApiFactory không nạp cấu hình Minio (không có storage thật) — nếu validate, toàn bộ test đỏ.
var minioOptions = builder.Services.AddOptions<MinioOptions>();
minioOptions.Bind(builder.Configuration.GetSection("Minio"));
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IValidateOptions<MinioOptions>, MinioOptionsValidator>();
    minioOptions.ValidateOnStart();
}
builder.Services.AddScoped<MinioStorageService>();
builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<MinioStorageService>());
builder.Services.AddScoped<IObjectStorageReader>(sp => sp.GetRequiredService<MinioStorageService>());
// D23 (TV4): resize ảnh 300x300/800x600 ngoài request qua Hangfire (queue PostgreSQL, retry 3).
// IObjectStorageWriter tách riêng IFileStorageService: cần ghi object với key phái sinh CHỦ ĐỘNG (HANDOFF 5.1).
builder.Services.AddScoped<IObjectStorageWriter>(sp => sp.GetRequiredService<MinioStorageService>());
builder.Services.AddScoped<ResizeImageJob>();
// N1-6: đăng ký SitemapGenerator ở NGOÀI if/else trên. Nếu chỉ đăng ký trong nhánh non-Testing
// thì ở môi trường Testing kiểu này không phải service đã biết, và minimal API sẽ coi tham số
// của /sitemap.xml là body → app không khởi động được ("Body was inferred...").
builder.Services.Configure<SitemapOptions>(builder.Configuration.GetSection(SitemapOptions.SectionName));
builder.Services.AddScoped<SitemapGenerator>();
builder.Services.AddScoped<SitemapGenerationJob>();
// Cron sitemap chỉ có ý nghĩa khi Hangfire thật sự chạy; Testing/E2E dùng nhánh inline ở trên.
var sitemapCron = new SitemapOptions().Cron;
if (builder.Environment.IsEnvironment("Testing"))
{
    // Testing/E2E: không bật worker nền — chạy job inline để assert DB/MinIO deterministic.
    builder.Services.AddScoped<IImageResizeQueue, InlineImageResizeQueue>();
}
else
{
    var hangfireConnection = Program.NormalizePostgreSqlConnectionString(
        builder.Configuration.GetConnectionString("Database")
        ?? builder.Configuration["DATABASE_URL"]
        ?? throw new InvalidOperationException("Configure ConnectionStrings:Database or DATABASE_URL."));
    builder.Services.AddHangfire((_, cfg) => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));
    builder.Services.AddHangfireServer((_, options) =>
    {
        options.WorkerCount = 4;
        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<IImageResizeQueue, HangfireImageResizeQueue>();
    sitemapCron = builder.Configuration.GetSection(SitemapOptions.SectionName).Get<SitemapOptions>()?.Cron
        ?? new SitemapOptions().Cron;
}
// B4 (issue #22): "object-storage" kiểm tra CREDENTIAL THẬT (StatObject) và thuộc tag "ready" ⇒
// /health/ready trả 503 khi AccessKey sai, thay vì Healthy rồi mới nổ 500 lúc upload.
// "minio" giữ làm check PHỤ chỉ TCP (tag "all") để vẫn thấy cổng có mở hay không khi chẩn đoán.
builder.Services.AddSingleton<ObjectStorageCredentialProbe>();
builder.Services.AddHealthChecks()
    .AddCheck<LivenessHealthCheck>("liveness", tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "all"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "all"])
    .AddCheck<ObjectStorageHealthCheck>("object-storage", tags: ["ready", "all"])
    .AddCheck<MinIOHealthCheck>("minio", tags: ["all"]);

// OpenTelemetry (D5/TV4): trace HTTP -> ASP.NET -> EF Core -> DB; metrics request/DB (FR-OBS-001/003).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("CulinaryBlog.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Microsoft.EntityFrameworkCore")
        .AddOtlpExporter());

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
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role",
        NameClaimType = "sub"
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
builder.Services.AddControllers();
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

// N1-6: đăng lịch sitemap 02:00 UTC.
// Phải đăng SAU khi đã Build() và thông qua IRecurringJobManager lấy từ DI. Gọi static API
// RecurringJob.AddOrUpdate lúc đang đăng ký service sẽ ném "Current JobStorage instance has not
// been initialized yet" và làm app KHÔNG KHỞI ĐỘNG ĐƯỢC ở mọi môi trường thật (chỉ lộ ra khi
// chạy ngoài test, vì Testing không bật Hangfire).
if (!builder.Environment.IsEnvironment("Testing"))
{
    using var sitemapScope = app.Services.CreateScope();
    var recurringJobs = sitemapScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<SitemapGenerationJob>(
        "sitemap-daily",
        job => job.RunAsync(default),
        sitemapCron,
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
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
// N1-5: đánh dấu bản chạy. Khi có nhiều tiến trình API sau load balancer, log của chúng trộn
// vào nhau và rất khó biết request rơi vào máy nào; header này (và property Instance trong log)
// là thứ duy nhất chứng minh được traffic thật sự được chia giữa các instance.
var instanceId = builder.Configuration["InstanceId"] ?? Environment.MachineName;
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Served-By"] = instanceId;
    await next();
});
Log.Information("Dang chay tren instance {InstanceId}", instanceId);
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// D23: dashboard Hangfire chỉ Admin (không public). Không bật ở Testing (Hangfire chỉ đăng ký ngoài Testing).
if (!builder.Environment.IsEnvironment("Testing"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AdminDashboardAuthorizationFilter()]
    });
}
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

auth.MapPost("/change-password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
{
    await sender.Send(command, ct);
    return Results.NoContent();
})
    .RequireAuthorization().WithName("ChangePassword").Produces(204).ProducesValidationProblem().ProducesProblem(400).ProducesProblem(401);

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

// SEO (D26/TV4): nguồn cho sitemap.xml — CHỈ Published, không Draft/Archived/Deleted.
recipes.MapGet("/sitemap", async (ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new GetSitemapQuery(), ct) }))
    .WithName("GetSitemapRecipes").Produces<object>(200);

// N1-6 (tuần 4): sitemap.xml do cron 02:00 UTC sinh, lưu trong Redis để mọi API instance
// (và lần chạy sau) đọc chung một bản. Nếu chưa có bản nào thì sinh tại chỗ để không bao giờ
// trả 404 cho crawler.
app.MapGet("/sitemap.xml", async (SitemapGenerator generator, ILogger<SitemapGenerator> log, CancellationToken ct) =>
{
    var xml = await generator.GetCachedXmlAsync(ct);
    if (xml is null)
    {
        var result = await generator.GenerateAsync(ct);
        xml = await generator.GetCachedXmlAsync(ct) ?? string.Empty;
        if (!result.Acquired)
            log.LogWarning("sitemap.xml chưa có bản cache và lock đang bị giữ ({Reason}); trả nội dung rỗng tạm thời", result.Reason);
    }
    return Results.Content(xml, "application/xml; charset=utf-8");
}).WithName("GetSitemapXml").Produces<string>(200, "application/xml");

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

recipes.MapDelete("/{id:guid}", async (Guid id, HttpRequest request, ISender sender, CancellationToken ct) =>
{
    var rowVersion = request.Headers.IfMatch.Count > 0 ? request.Headers.IfMatch.ToString().Trim('"') : null;
    await sender.Send(new DeleteRecipeCommand(id, rowVersion), ct);
    return Results.NoContent();
})
    .RequireAuthorization("AuthorPolicy").WithName("DeleteRecipe")
    .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

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

// D3 (TV4): archive — ẩn public ngay, giữ dữ liệu. Delete soft (D08) giữ ảnh để restore;
// endpoint DELETE đã khai báo ở trên (bản C2.4 có If-Match/RowVersion nên mới hơn, trả 422 khi bản ghi đã đổi).
recipes.MapPatch("/{id:guid}/archive", async (Guid id, ISender sender, CancellationToken ct) =>
    Results.Ok(new { data = await sender.Send(new ArchiveRecipeCommand(id), ct) }))
    .RequireAuthorization("AuthorPolicy").WithName("ArchiveRecipe")
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

// D27 (TV4, PA-2): proxy ảnh base media URL — GET /api/v1/resources/images/{key}.
// key = recipes/{recipeId}/{uuid}.ext (IMAGE_CONTRACT §1). Published -> public + cache;
// Draft/Archived -> chỉ owner/Admin (Bearer) else 403 image.forbidden; không tồn tại -> 404.
// Không đụng IFileStorageService/StoredFile (giữ contract TV3 — HANDOFF 5.1); dùng IObjectStorageReader.
var resourcesImages = app.MapGroup("/api/v1/resources/images").WithTags("Resources");
resourcesImages.MapGet("/{**key}", async (string key, IObjectStorageReader storage, IApplicationDbContext db, ICurrentUser currentUser, HttpContext context, CancellationToken ct) =>
{
    var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length < 3 || !segments[0].Equals("recipes", StringComparison.OrdinalIgnoreCase))
        throw new AppException(404, "image.not_found", "Không tìm thấy ảnh.");
    if (!Guid.TryParse(segments[1], out var recipeIdFromKey))
        throw new AppException(404, "image.not_found", "Không tìm thấy ảnh.");

    var recipe = await db.Recipes
        .AsNoTracking()
        .Where(r => r.Id == recipeIdFromKey && !r.IsDeleted)
        .Select(r => new { r.Status, r.AuthorId })
        .FirstOrDefaultAsync(ct);
    if (recipe is null)
        throw new AppException(404, "image.not_found", "Không tìm thấy ảnh.");

    var isPublished = recipe.Status == CulinaryBlog.Domain.Enums.RecipeStatus.Published;
    if (!isPublished)
    {
        var userId = currentUser.UserId;
        var isAdmin = currentUser.IsInRole(CulinaryBlog.Domain.Roles.Admin);
        var isOwner = userId is not null && string.Equals(userId, recipe.AuthorId, StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            throw new AppException(403, "image.forbidden", "Ảnh này chỉ dành cho chủ sở hữu hoặc quản trị viên.");
    }

    var content = await storage.ReadAsync(key, ct);
    if (content is null)
        throw new AppException(404, "image.not_found", "Không tìm thấy ảnh.");

    context.Response.Headers.CacheControl = isPublished ? "public, max-age=3600" : "no-store";
    return Results.Stream(content.Stream, contentType: content.ContentType, fileDownloadName: null);
})
    .WithName("GetRecipeImage").Produces<object>(200).ProducesProblem(403).ProducesProblem(404);

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true, ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live"), ResponseWriter = HealthReportWriter.WriteJson });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthReportWriter.WriteJson });
app.MapControllers();
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
