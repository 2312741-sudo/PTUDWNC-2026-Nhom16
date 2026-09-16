using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application;

public sealed record RecipeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    Guid CategoryId,
    string CategoryName,
    string AuthorId,
    string AuthorDisplayName,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string Status,
    string? PrimaryImageUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public sealed record GetRecipesQuery(
    int Page = 1,
    int PageSize = 12,
    string SortBy = "createdAt",
    string SortOrder = "desc",
    Guid? CategoryId = null,
    string? Difficulty = null,
    int? MaxCookTime = null,
    int? MinServings = null) : IRequest<PagedResult<RecipeSummaryDto>>;

public sealed record SearchRecipesQuery(
    string Q,
    int Page = 1,
    int PageSize = 12,
    string SortBy = "createdAt",
    string SortOrder = "desc",
    Guid? CategoryId = null,
    string? Difficulty = null,
    int? MaxCookTime = null,
    int? MinServings = null) : IRequest<PagedResult<RecipeSummaryDto>>;

public interface IRecipeRepository
{
    Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct);
    Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct);
    Task AddAsync(Recipe recipe, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class GetRecipesHandler(IRecipeRepository repository) : IRequestHandler<GetRecipesQuery, PagedResult<RecipeSummaryDto>>
{
    public Task<PagedResult<RecipeSummaryDto>> Handle(GetRecipesQuery request, CancellationToken ct) =>
        repository.GetPublishedRecipesAsync(request, ct);
}

public sealed class SearchRecipesHandler(IRecipeRepository repository) : IRequestHandler<SearchRecipesQuery, PagedResult<RecipeSummaryDto>>
{
    public Task<PagedResult<RecipeSummaryDto>> Handle(SearchRecipesQuery request, CancellationToken ct) =>
        repository.SearchPublishedRecipesAsync(request, ct);
}

public sealed class GetRecipesValidator : AbstractValidator<GetRecipesQuery>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "title", "cookTimeMinutes", "prepTimeMinutes"];
    private static readonly string[] AllowedSortOrders = ["asc", "desc"];

    public GetRecipesValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Số bản ghi trên trang phải từ 1 đến 50.");

        RuleFor(x => x.SortBy)
            .Must(x => AllowedSortFields.Contains(x.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Trường sắp xếp không hợp lệ. Cho phép: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.SortOrder)
            .Must(x => AllowedSortOrders.Contains(x.Trim().ToLowerInvariant()))
            .WithMessage("Thứ tự sắp xếp chỉ chấp nhận 'asc' hoặc 'desc'.");

        When(x => !string.IsNullOrWhiteSpace(x.Difficulty), () =>
        {
            RuleFor(x => x.Difficulty)
                .Must(x => RecipeDifficultyValues.All.Contains(x!.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Độ khó không hợp lệ. Cho phép: {string.Join(", ", RecipeDifficultyValues.All)}.");
        });

        When(x => x.MaxCookTime.HasValue, () =>
        {
            RuleFor(x => x.MaxCookTime)
                .GreaterThanOrEqualTo(0).WithMessage("Thời gian nấu tối đa không được âm.");
        });

        When(x => x.MinServings.HasValue, () =>
        {
            RuleFor(x => x.MinServings)
                .GreaterThan(0).WithMessage("Số khẩu phần tối thiểu phải lớn hơn 0.");
        });
    }
}

public sealed class SearchRecipesValidator : AbstractValidator<SearchRecipesQuery>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "title", "cookTimeMinutes", "prepTimeMinutes"];
    private static readonly string[] AllowedSortOrders = ["asc", "desc"];

    public SearchRecipesValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty().WithMessage("Từ khóa tìm kiếm không được để trống.")
            .MinimumLength(2).WithMessage("Từ khóa tìm kiếm phải có từ 2 ký tự trở lên.")
            .MaximumLength(200).WithMessage("Từ khóa tìm kiếm không được vượt quá 200 ký tự.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Số bản ghi trên trang phải từ 1 đến 50.");

        RuleFor(x => x.SortBy)
            .Must(x => AllowedSortFields.Contains(x.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Trường sắp xếp không hợp lệ. Cho phép: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.SortOrder)
            .Must(x => AllowedSortOrders.Contains(x.Trim().ToLowerInvariant()))
            .WithMessage("Thứ tự sắp xếp chỉ chấp nhận 'asc' hoặc 'desc'.");

        When(x => !string.IsNullOrWhiteSpace(x.Difficulty), () =>
        {
            RuleFor(x => x.Difficulty)
                .Must(x => RecipeDifficultyValues.All.Contains(x!.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Độ khó không hợp lệ. Cho phép: {string.Join(", ", RecipeDifficultyValues.All)}.");
        });

        When(x => x.MaxCookTime.HasValue, () =>
        {
            RuleFor(x => x.MaxCookTime)
                .GreaterThanOrEqualTo(0).WithMessage("Thời gian nấu tối đa không được âm.");
        });

        When(x => x.MinServings.HasValue, () =>
        {
            RuleFor(x => x.MinServings)
                .GreaterThan(0).WithMessage("Số khẩu phần tối thiểu phải lớn hơn 0.");
        });
    }
}
