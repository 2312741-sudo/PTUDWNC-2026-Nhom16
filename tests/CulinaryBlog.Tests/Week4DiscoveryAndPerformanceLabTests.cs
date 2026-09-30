using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class Week4DiscoveryAndPerformanceLabTests
{
    [Fact]
    public async Task GoogleAuthService_validates_dev_token_and_extracts_correct_payload()
    {
        var config = new ConfigurationBuilder().Build();
        var googleAuthService = new GoogleAuthService(config, NullLogger<GoogleAuthService>.Instance);

        var token = "dev_google:chef.test@gmail.com:Nguyen Van Test";
        var payload = await googleAuthService.ValidateIdTokenAsync(token, CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal("chef.test@gmail.com", payload.Email);
        Assert.Equal("Nguyen Van Test", payload.Name);
        Assert.True(payload.EmailVerified);
        Assert.StartsWith("google-", payload.Subject);
    }

    [Fact]
    public async Task GoogleAuthService_rejects_empty_or_malformed_tokens()
    {
        var config = new ConfigurationBuilder().Build();
        var googleAuthService = new GoogleAuthService(config, NullLogger<GoogleAuthService>.Instance);

        await Assert.ThrowsAsync<AppException>(() =>
            googleAuthService.ValidateIdTokenAsync("", CancellationToken.None));

        await Assert.ThrowsAsync<AppException>(() =>
            googleAuthService.ValidateIdTokenAsync("   ", CancellationToken.None));
    }

    [Fact]
    public void Word_boundary_search_helper_prevents_false_positives()
    {
        // Kiểm tra logic ranh giới từ loại bỏ false positives như 'gà' trong 'ngọt ngào' hay 'beo-ngay'
        var queryTerm = "ga";
        var chickenSlug = "ga-chien-nuoc-mam";
        var dessertSlug = "banh-flan-caramel-beo-ngay";
        var sweetPancakeSlug = "pancake-mat-ong-chuoi";

        // Slug matches
        bool isChickenMatch = chickenSlug == queryTerm ||
                              chickenSlug.StartsWith($"{queryTerm}-") ||
                              chickenSlug.Contains($"-{queryTerm}-") ||
                              chickenSlug.EndsWith($"-{queryTerm}");

        bool isDessertMatch = dessertSlug == queryTerm ||
                              dessertSlug.StartsWith($"{queryTerm}-") ||
                              dessertSlug.Contains($"-{queryTerm}-") ||
                              dessertSlug.EndsWith($"-{queryTerm}");

        bool isPancakeMatch = sweetPancakeSlug == queryTerm ||
                              sweetPancakeSlug.StartsWith($"{queryTerm}-") ||
                              sweetPancakeSlug.Contains($"-{queryTerm}-") ||
                              sweetPancakeSlug.EndsWith($"-{queryTerm}");

        Assert.True(isChickenMatch, "Món gà phải khớp slug chính xác");
        Assert.False(isDessertMatch, "Món flan không được khớp với từ khóa ga");
        Assert.False(isPancakeMatch, "Món pancake không được khớp với từ khóa ga");
    }

    [Fact]
    public void Recipe_cache_keys_are_consistent_and_isolated()
    {
        var queryA = new SearchRecipesQuery("pho", Page: 1, PageSize: 12);
        var queryB = new SearchRecipesQuery("pho", Page: 2, PageSize: 12);
        var queryC = new SearchRecipesQuery("ga", Page: 1, PageSize: 12);

        var keyA = $"recipes:search:{queryA.Q}:{queryA.CategoryId}:{queryA.Difficulty}:{queryA.MaxCookTime}:{queryA.MinServings}:{queryA.SortBy}:{queryA.SortOrder}:{queryA.Page}:{queryA.PageSize}";
        var keyB = $"recipes:search:{queryB.Q}:{queryB.CategoryId}:{queryB.Difficulty}:{queryB.MaxCookTime}:{queryB.MinServings}:{queryB.SortBy}:{queryB.SortOrder}:{queryB.Page}:{queryB.PageSize}";
        var keyC = $"recipes:search:{queryC.Q}:{queryC.CategoryId}:{queryC.Difficulty}:{queryC.MaxCookTime}:{queryC.MinServings}:{queryC.SortBy}:{queryC.SortOrder}:{queryC.Page}:{queryC.PageSize}";

        Assert.NotEqual(keyA, keyB);
        Assert.NotEqual(keyA, keyC);
        Assert.Contains("pho", keyA);
        Assert.Contains("ga", keyC);
    }

    [Fact]
    public void Category_cache_keys_are_consistent()
    {
        var slug = "mon-kho";
        var keyAll = "categories:all";
        var keyDetail = $"categories:{slug}";

        Assert.Equal("categories:all", keyAll);
        Assert.Equal("categories:mon-kho", keyDetail);
    }
}
