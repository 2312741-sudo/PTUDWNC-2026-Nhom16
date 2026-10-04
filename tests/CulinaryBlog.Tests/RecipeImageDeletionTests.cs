using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// C3 — Xoá ảnh rồi tải ảnh mới. Lỗi cũ: xoá ảnh duy nhất (ảnh chính) rồi tải ảnh mới -> 422 recipe.version_conflict.
/// RecipeImage là BaseEntity nên AuditableEntityInterceptor đổi Delete thành UPDATE IsDeleted = true; dòng xoá mềm vẫn
/// giữ IsPrimary = true và vẫn chiếm chỗ trong partial unique index ux_recipe_images_one_primary (lọc "IsPrimary" = true,
/// không lọc IsDeleted) -> INSERT ảnh chính mới / UPDATE ảnh còn lại thành chính vi phạm 23505 -> DbUpdateException -> 422.
/// Chạy trên Postgres thật (ApiFactory + migration thật); thay IFileStorageService và IImageResizeQueue để loại MinIO.
/// Lỗi DB do ApiExceptionHandler ghi log được thu lại và in kèm khi test đỏ.
/// </summary>
public sealed class RecipeImageDeletionTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly WebApplicationFactory<Program> _app;
    private readonly CapturingLoggerProvider _logs = new();

    public RecipeImageDeletionTests(ApiFactory factory)
    {
        factory.EnsureMigrated();
        _app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddScoped<IFileStorageService, InMemoryFileStorage>();
            services.RemoveAll<IImageResizeQueue>();
            services.AddSingleton<IImageResizeQueue>(new FakeImageResizeQueue());
            services.AddSingleton<ILoggerProvider>(_logs);
        }));
    }

    [Fact]
    public async Task Delete_only_primary_image_then_upload_new_returns_201_and_new_is_primary()
    {
        var (client, recipeId) = await NewRecipe();
        var first = await UploadId(client, recipeId, "Ảnh duy nhất");

        await AssertStatus(HttpStatusCode.NoContent, await client.DeleteAsync($"/api/v1/recipes/{recipeId}/images/{first}"));

        var again = await UploadPng(client, recipeId, "Ảnh mới");
        await AssertStatus(HttpStatusCode.Created, again);
        var dto = await DataOf(again);
        Assert.True(dto.GetProperty("isPrimary").GetBoolean(), "Ảnh tải sau khi xoá hết phải là ảnh chính.");

        var rows = await ImagesInDb(recipeId);
        var deleted = Assert.Single(rows, r => r.Id == first);
        Assert.True(deleted.IsDeleted, "Ảnh cũ phải được xoá mềm (D08).");
        var active = Assert.Single(rows, r => !r.IsDeleted);
        Assert.Equal(dto.GetProperty("id").GetGuid(), active.Id);
        Assert.True(active.IsPrimary);
    }

    [Fact]
    public async Task Two_images_delete_primary_returns_204_and_remaining_becomes_primary()
    {
        var (client, recipeId) = await NewRecipe();
        var primary = await UploadId(client, recipeId, "Ảnh chính");
        var other = await UploadId(client, recipeId, "Ảnh phụ");

        await AssertStatus(HttpStatusCode.NoContent, await client.DeleteAsync($"/api/v1/recipes/{recipeId}/images/{primary}"));

        var active = (await ImagesInDb(recipeId)).Where(r => !r.IsDeleted).ToList();
        var remaining = Assert.Single(active);
        Assert.Equal(other, remaining.Id);
        Assert.True(remaining.IsPrimary, "Ảnh còn lại phải thành ảnh chính.");

        // Qua API chi tiết cũng thấy đúng 1 ảnh, là ảnh chính
        var detail = await DataOf(await client.GetAsync($"/api/v1/recipes/{recipeId}"));
        var images = detail.GetProperty("images").EnumerateArray().ToList();
        var img = Assert.Single(images);
        Assert.Equal(other, img.GetProperty("id").GetGuid());
        Assert.True(img.GetProperty("isPrimary").GetBoolean());
    }

    [Fact]
    public async Task Delete_non_primary_then_upload_keeps_single_primary()
    {
        var (client, recipeId) = await NewRecipe();
        var primary = await UploadId(client, recipeId, "Ảnh chính");
        var other = await UploadId(client, recipeId, "Ảnh phụ");

        await AssertStatus(HttpStatusCode.NoContent, await client.DeleteAsync($"/api/v1/recipes/{recipeId}/images/{other}"));
        var third = await UploadPng(client, recipeId, "Ảnh thứ ba");
        await AssertStatus(HttpStatusCode.Created, third);
        Assert.False((await DataOf(third)).GetProperty("isPrimary").GetBoolean());

        var active = (await ImagesInDb(recipeId)).Where(r => !r.IsDeleted).ToList();
        Assert.Equal(2, active.Count);
        Assert.Equal(primary, Assert.Single(active, r => r.IsPrimary).Id);
    }

    // ---------------------------------------------------------------- helpers

    private sealed record ImageRow(Guid Id, bool IsPrimary, bool IsDeleted, int OrderIndex);

    private async Task<List<ImageRow>> ImagesInDb(Guid recipeId)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.RecipeImages.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.RecipeId == recipeId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => new ImageRow(i.Id, i.IsPrimary, i.IsDeleted, i.OrderIndex))
            .ToListAsync();
    }

    private async Task<(HttpClient Client, Guid RecipeId)> NewRecipe()
    {
        var (client, _, _) = await NewAuthorClient();
        var categoryId = await AnyCategoryId();
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Chả giò {Guid.NewGuid():N}"[..30], "Chả giò rế", null, 20, 15, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        return (client, (await DataOf(res)).GetProperty("id").GetGuid());
    }

    private async Task<Guid> UploadId(HttpClient client, Guid recipeId, string altText)
    {
        var res = await UploadPng(client, recipeId, altText);
        await AssertStatus(HttpStatusCode.Created, res);
        return (await DataOf(res)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> UploadPng(HttpClient client, Guid recipeId, string altText)
    {
        var file = new ByteArrayContent(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { file, "file", "anh.png" },
            { new StringContent(altText), "altText" },
        };
        return client.PostAsync($"/api/v1/recipes/{recipeId}/images", form);
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    /// <summary>Đỏ thì in kèm lỗi DB mà ApiExceptionHandler đã ghi (nguyên nhân thật của 422)</summary>
    private async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage res)
    {
        if (res.StatusCode == expected) return;
        var dbErrors = string.Join(" | ", _logs.Messages.Where(m => m.Contains("DB update failed")));
        Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode} (cần {(int)expected}): "
            + $"{await res.Content.ReadAsStringAsync()} || log: {dbErrors}");
    }

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _app.CreateClient();
        var cmd = new RegisterCommand($"tv3-imgdel-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB test chưa seed thì tạo bằng tài khoản Admin.</summary>
    private async Task<Guid> AnyCategoryId()
    {
        var list = await DataOf(await _app.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return items[0].GetProperty("id").GetGuid();

        var (client, email, password) = await NewAuthorClient();
        using (var scope = _app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var added = await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, Roles.Admin);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        await AssertStatus(HttpStatusCode.OK, login);
        var adminToken = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeImageDeletionTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        public Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            return Task.FromResult(new StoredFile(key, key, contentType, content.Length));
        }

        public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Logger(Messages);
        public void Dispose() { }

        private sealed class Logger(ConcurrentQueue<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel)) sink.Enqueue(formatter(state, exception));
            }
        }
    }
}
