using CulinaryBlog.Application;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>C7 — Unit test validator + handler của MyRecipes (dashboard tác giả). Không cần DB.</summary>
public sealed class MyRecipesValidatorTests
{
    private static readonly GetMyRecipesValidator Validator = new();

    private static GetMyRecipesQuery Query(
        string authorId = "author-1", int page = 1, int pageSize = 20,
        string sortBy = "updatedAt", string sortOrder = "desc", string? status = null, string? q = null) =>
        new(authorId, page, pageSize, sortBy, sortOrder, status, q);

    private static void AssertValid(GetMyRecipesQuery query)
    {
        var result = Validator.Validate(query);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    private static void AssertInvalidOn(string property, GetMyRecipesQuery query)
    {
        var result = Validator.Validate(query);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    // A1
    [Fact]
    public void Default_query_with_author_is_valid() => AssertValid(Query());

    // A2
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_author_is_rejected(string authorId) =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.AuthorId), Query(authorId: authorId));

    // A3
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_below_one_is_rejected(int page) =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.Page), Query(page: page));

    [Fact]
    public void Page_one_is_valid() => AssertValid(Query(page: 1));

    // A4
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void PageSize_outside_1_to_50_is_rejected(int pageSize) =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.PageSize), Query(pageSize: pageSize));

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void PageSize_at_boundaries_is_valid(int pageSize) => AssertValid(Query(pageSize: pageSize));

    // A5
    [Theory]
    [InlineData("updatedAt")]
    [InlineData("CreatedAt")]
    [InlineData(" TITLE ")]
    [InlineData("publishedAt")]
    public void Allowed_sort_fields_are_valid_ignoring_case_and_whitespace(string sortBy) =>
        AssertValid(Query(sortBy: sortBy));

    [Theory]
    [InlineData("id")]
    [InlineData("")]
    public void Unknown_sort_field_is_rejected(string sortBy) =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.SortBy), Query(sortBy: sortBy));

    // A6
    [Theory]
    [InlineData("asc")]
    [InlineData("DESC")]
    [InlineData(" desc ")]
    public void Allowed_sort_orders_are_valid(string sortOrder) => AssertValid(Query(sortOrder: sortOrder));

    [Fact]
    public void Unknown_sort_order_is_rejected() =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.SortOrder), Query(sortOrder: "up"));

    // A7 — chuỗi số (vd "99") cố ý không test: Enum.TryParse chấp nhận, xem PLAN_TUAN4.local.md
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Draft")]
    [InlineData("published")]
    [InlineData("Archived")]
    public void Empty_or_known_status_is_valid(string? status) => AssertValid(Query(status: status));

    [Fact]
    public void Unknown_status_is_rejected() =>
        AssertInvalidOn(nameof(GetMyRecipesQuery.Status), Query(status: "Foo"));

    // A8
    [Theory]
    [InlineData(null, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Search_text_is_limited_to_200_characters(int? length, bool valid)
    {
        var q = length is null ? null : new string('a', length.Value);
        var query = Query(q: q);

        if (valid) AssertValid(query);
        else AssertInvalidOn(nameof(GetMyRecipesQuery.Q), query);
    }

    // A9
    [Fact]
    public async Task List_handler_forwards_query_to_repository_and_returns_its_result()
    {
        var repo = new FakeMyRecipesRepository();
        var query = Query(authorId: "author-9", page: 2, pageSize: 5, sortBy: "title", sortOrder: "asc", status: "Draft", q: "pho");

        var result = await new GetMyRecipesHandler(repo).Handle(query, CancellationToken.None);

        Assert.Same(query, repo.ReceivedQuery);
        Assert.Same(repo.ListResult, result);
    }

    [Fact]
    public async Task Counts_handler_forwards_author_id_and_returns_repository_result()
    {
        var repo = new FakeMyRecipesRepository();

        var result = await new GetMyRecipeCountsHandler(repo)
            .Handle(new GetMyRecipeCountsQuery("author-9"), CancellationToken.None);

        Assert.Equal("author-9", repo.ReceivedAuthorId);
        Assert.Same(repo.CountsResult, result);
    }

    private sealed class FakeMyRecipesRepository : IMyRecipesRepository
    {
        public GetMyRecipesQuery? ReceivedQuery { get; private set; }
        public string? ReceivedAuthorId { get; private set; }

        public PagedResult<MyRecipeSummaryDto> ListResult { get; } =
            new([], PaginationMeta.Create(1, 20, 0));

        public MyRecipeCountsDto CountsResult { get; } =
            new(0, new Dictionary<string, int> { ["Draft"] = 0 });

        public Task<PagedResult<MyRecipeSummaryDto>> GetByAuthorAsync(GetMyRecipesQuery query, CancellationToken ct)
        {
            ReceivedQuery = query;
            return Task.FromResult(ListResult);
        }

        public Task<MyRecipeCountsDto> CountByAuthorAsync(string authorId, CancellationToken ct)
        {
            ReceivedAuthorId = authorId;
            return Task.FromResult(CountsResult);
        }
    }
}
