namespace CulinaryBlog.Application;

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage)
{
    public static PaginationMeta Create(int page, int pageSize, int total)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var totalPages = total > 0 ? (int)Math.Ceiling((double)total / pageSize) : 0;

        return new(
            Page: page,
            PageSize: pageSize,
            Total: total,
            TotalPages: totalPages,
            HasNextPage: page < totalPages,
            HasPreviousPage: page > 1 && totalPages > 0);
    }
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    PaginationMeta Meta);
