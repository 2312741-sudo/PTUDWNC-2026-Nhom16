using Amazon.Runtime;
using Amazon.S3;
using Dapper;
using Hangfire;
using Hangfire.PostgreSql;
using Lab.TV3.Api;
using Lab.TV3.Api.L1;
using Lab.TV3.Api.L3;
using Lab.TV3.Api.L4;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using StackExchange.Redis;

// LAB C6 — TV3 Huỳnh Quốc Trung. App thử nghiệm độc lập, KHÔNG merge vào sản phẩm (nhánh practice/TV3/...).
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình lười (qua IServiceProvider) để WebApplicationFactory ghi đè được trong test
static string Cs(IServiceProvider sp) => sp.GetRequiredService<IConfiguration>().GetConnectionString("Lab")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Lab");

builder.Services.AddSingleton(sp => NpgsqlDataSource.Create(Cs(sp)));
builder.Services.AddSingleton<LabDb>();

// ---------------- L1: Identity/PBKDF2, JWT, refresh, logout, Google verify/link (K08, K09)
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<LabUser>, PasswordHasher<LabUser>>();
builder.Services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((o, cfg) =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = TokenService.ValidationParameters(cfg);
    });
builder.Services.AddAuthorization();

// ---------------- L3: FTS + Redis cache-aside + OutputCache + fallback (K11, K12)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opt = ConfigurationOptions.Parse(sp.GetRequiredService<IConfiguration>()["Redis:Configuration"] ?? "localhost:6379");
    opt.AbortOnConnectFail = false; // Redis chết không làm app chết -> fallback DB
    opt.ConnectTimeout = 1000;
    opt.SyncTimeout = 1000;
    opt.AsyncTimeout = 1000;
    return ConnectionMultiplexer.Connect(opt);
});
builder.Services.AddSingleton<RecipeCache>();
builder.Services.AddOutputCache(o =>
    o.AddPolicy("lab-detail", p => p.Expire(TimeSpan.FromSeconds(30)).Tag(RecipeCache.OutputTag)));

// ---------------- L4: MinIO upload/delete, resize, SMTP Mailhog, Hangfire (K13, K14, K15)
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new AmazonS3Client(
        new BasicAWSCredentials(cfg["Minio:AccessKey"], cfg["Minio:SecretKey"]),
        new AmazonS3Config { ServiceURL = cfg["Minio:Endpoint"], ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
});
builder.Services.AddSingleton<ObjectStorage>();
builder.Services.AddTransient<EmailJob>();
builder.Services.AddTransient<ResizeJob>();
builder.Services.AddTransient<SitemapJob>();
builder.Services.AddHangfire((sp, h) => h
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(Cs(sp)),
        new PostgreSqlStorageOptions { SchemaName = "lab_tv3_hangfire" })); // persistent -> sống qua restart
builder.Services.AddHangfireServer(o => { o.WorkerCount = 2; o.ServerName = "lab-tv3"; });

var app = builder.Build();

await LabDb.EnsureDatabaseAsync(app.Configuration.GetConnectionString("Lab")!);
await app.Services.GetRequiredService<LabDb>().EnsureSchemaAsync();

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseHangfireDashboard("/lab/hangfire"); // mặc định chỉ cho truy cập từ localhost

app.MapGet("/lab/health", () => Results.Ok(new { data = "ok" }));
app.MapL1Auth();
app.MapL3Search();
app.MapL4Media();

// Recurring: sinh sitemap mỗi giờ (K14)
app.Services.GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate<SitemapJob>(SitemapJob.RecurringId, j => j.RunAsync(CancellationToken.None), Cron.Hourly(), new RecurringJobOptions());

app.Run();

public partial class Program { }