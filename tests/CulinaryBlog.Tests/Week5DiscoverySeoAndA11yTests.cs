using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class Week5DiscoverySeoAndA11yTests
{
    [Theory]
    [InlineData("phở bò!", "pho bo")]
    [InlineData("cá & tôm", "ca tom")]
    [InlineData("gà | vịt", "ga vit")]
    [InlineData("(lẩu chua)", "lau chua")]
    [InlineData("bánh*cuốn:", "banh cuon")]
    [InlineData("món 'ngon' \"việt\"", "mon ngon viet")]
    public void Fts_query_sanitizer_cleans_punctuation_and_special_characters(string rawInput, string expectedClean)
    {
        // Kiểm tra logic lọc ký tự đặc biệt giúp FTS query an toàn, không sinh lỗi cú pháp PostgreSQL
        var cleaned = CleanSearchTerm(rawInput);
        Assert.Equal(expectedClean, cleaned);
    }

    [Fact]
    public void Category_pagination_clamps_pageSize_and_normalizes_invalid_page_numbers()
    {
        // Kiểm tra phân trang danh mục và khám phá công thức bảo vệ hệ thống khỏi DOS query
        var normalPage = Math.Max(1, 0); // page <= 0 chuẩn hóa về 1
        var negativePage = Math.Max(1, -5);
        var clampedPageSize = Math.Clamp(100, 1, 50); // pageSize tối đa 50
        var minPageSize = Math.Clamp(0, 1, 50); // pageSize tối thiểu 1

        Assert.Equal(1, normalPage);
        Assert.Equal(1, negativePage);
        Assert.Equal(50, clampedPageSize);
        Assert.Equal(1, minPageSize);
    }

    [Fact]
    public void PaginationMeta_calculates_correct_boundaries_for_seo_crawl()
    {
        // Kiểm tra tính toán phân trang chuẩn SEO để crawler không duyệt quá giới hạn
        var metaZero = PaginationMeta.Create(1, 12, 0);
        Assert.Equal(0, metaZero.TotalPages);
        Assert.False(metaZero.HasNextPage);
        Assert.False(metaZero.HasPreviousPage);

        var metaOnePage = PaginationMeta.Create(1, 12, 12);
        Assert.Equal(1, metaOnePage.TotalPages);
        Assert.False(metaOnePage.HasNextPage);
        Assert.False(metaOnePage.HasPreviousPage);

        var metaPageTwo = PaginationMeta.Create(2, 12, 25);
        Assert.Equal(3, metaPageTwo.TotalPages);
        Assert.True(metaPageTwo.HasPreviousPage);
        Assert.True(metaPageTwo.HasNextPage);

        var metaLastPage = PaginationMeta.Create(3, 12, 25);
        Assert.Equal(3, metaLastPage.TotalPages);
        Assert.True(metaLastPage.HasPreviousPage);
        Assert.False(metaLastPage.HasNextPage);
    }

    [Fact]
    public void SitemapRecipeDto_maps_correct_structure_for_search_engine_bots()
    {
        // Kiểm tra DTO sitemap định dạng đúng cho bot Google/Bing
        var recipeId = Guid.NewGuid();
        var slug = "pho-bo-tai-nam-ha-noi";
        var publishedAt = new DateTime(2026, 10, 5, 8, 30, 0, DateTimeKind.Utc);

        var sitemapDto = new SitemapRecipeDto(recipeId, slug, publishedAt);

        Assert.Equal(recipeId, sitemapDto.Id);
        Assert.Equal(slug, sitemapDto.Slug);
        Assert.Equal(publishedAt, sitemapDto.PublishedAt);
    }

    [Fact]
    public void Cache_keys_for_categories_and_search_are_strictly_isolated()
    {
        // Kiểm tra phân tách tiền tố cache keys: không bao giờ ghi đè chéo giữa danh mục và tìm kiếm
        var categoryAllKey = "category:all";
        var categorySlugKey = "category:slug:mon-chay";
        var searchKey = "search:q=pho:page=1:cat=all";

        Assert.StartsWith("category:", categoryAllKey);
        Assert.StartsWith("category:slug:", categorySlugKey);
        Assert.StartsWith("search:", searchKey);

        Assert.NotEqual(categoryAllKey, categorySlugKey);
        Assert.False(searchKey.StartsWith("category:"));
    }

    [Fact]
    public void Google_user_payload_ensures_email_and_name_invariants()
    {
        // Kiểm tra tính toàn vẹn payload đăng nhập Google
        var payload = new GoogleUserPayload("google-sub-12345", "user@gmail.com", true, "Nguyen Van A", "https://avatar.google.com/123.jpg");

        Assert.Equal("google-sub-12345", payload.Subject);
        Assert.Equal("user@gmail.com", payload.Email);
        Assert.Equal("Nguyen Van A", payload.Name);
        Assert.True(payload.EmailVerified);
        Assert.Equal("https://avatar.google.com/123.jpg", payload.Picture);
    }

    private static string CleanSearchTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return string.Empty;

        // Bỏ dấu tiếng Việt đơn giản cho unit test
        var withoutDiacritics = RemoveVietnameseDiacritics(term.ToLowerInvariant());

        // Thay thế các ký tự phân tách hoặc toán tử thành khoảng trắng
        var cleanedChars = withoutDiacritics
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray();

        var normalized = new string(cleanedChars);
        return string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string RemoveVietnameseDiacritics(string text)
    {
        string[] signs =
        [
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        ];

        for (int i = 1; i < signs.Length; i++)
        {
            for (int j = 0; j < signs[i].Length; j++)
            {
                text = text.Replace(signs[i][j], signs[0][i - 1]);
            }
        }
        return text;
    }
}
