using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application;

// DTO riêng cho dashboard tác giả: khác RecipeSummaryDto ở chỗ có UpdatedAt và RowVersion
// (cần RowVersion để xoá/sửa từ dashboard mà không ghi đè thay đổi của phiên khác).
public sealed record MyRecipeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    Guid CategoryId,
    string CategoryName,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string Status,
    string? PrimaryImageUrl,
    int IngredientCount,
    int StepCount,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string RowVersion);

public sealed record MyRecipeCountsDto(int All, IReadOnlyDictionary<string, int> ByStatus);

// AuthorId KHÔNG bind từ query string: endpoint gán từ claim của token.
public sealed record GetMyRecipesQuery(
    string AuthorId,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "updatedAt",
    string SortOrder = "desc",
    string? Status = null,
    string? Q = null) : IRequest<PagedResult<MyRecipeSummaryDto>>;

public sealed record GetMyRecipeCountsQuery(string AuthorId) : IRequest<MyRecipeCountsDto>;

public interface IMyRecipesRepository
{
    Task<PagedResult<MyRecipeSummaryDto>> GetByAuthorAsync(GetMyRecipesQuery query, CancellationToken ct);
    Task<MyRecipeCountsDto> CountByAuthorAsync(string authorId, CancellationToken ct);
}

public sealed class GetMyRecipesHandler(IMyRecipesRepository repository)
    : IRequestHandler<GetMyRecipesQuery, PagedResult<MyRecipeSummaryDto>>
{
    public Task<PagedResult<MyRecipeSummaryDto>> Handle(GetMyRecipesQuery request, CancellationToken ct) =>
        repository.GetByAuthorAsync(request, ct);
}

public sealed class GetMyRecipeCountsHandler(IMyRecipesRepository repository)
    : IRequestHandler<GetMyRecipeCountsQuery, MyRecipeCountsDto>
{
    public Task<MyRecipeCountsDto> Handle(GetMyRecipeCountsQuery request, CancellationToken ct) =>
        repository.CountByAuthorAsync(request.AuthorId, ct);
}

public sealed class GetMyRecipesValidator : AbstractValidator<GetMyRecipesQuery>
{
    public static readonly string[] AllowedSortFields = ["updatedAt", "createdAt", "title", "publishedAt"];
    private static readonly string[] AllowedSortOrders = ["asc", "desc"];

    public GetMyRecipesValidator()
    {
        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("Không xác định được tác giả.");

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

        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(x => Enum.TryParse<RecipeStatus>(x!.Trim(), ignoreCase: true, out _))
                .WithMessage($"Trạng thái không hợp lệ. Cho phép: {string.Join(", ", Enum.GetNames<RecipeStatus>())}.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Q), () =>
        {
            RuleFor(x => x.Q)
                .MaximumLength(200).WithMessage("Từ khóa tìm kiếm không được vượt quá 200 ký tự.");
        });
    }
}