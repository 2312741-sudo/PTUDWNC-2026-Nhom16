namespace CulinaryBlog.Application.Common;
public static class QueryValidation
{
    public static void Validate(int page, int size, string sortBy, params string[] allowed)
    {
        if (page < 1 || size < 1 || size > 100 || (long)(page - 1) * size > int.MaxValue)
            throw new RequestValidationException("Page phải >= 1, pageSize từ 1 đến 100 và offset không vượt Int32.");
        if (!allowed.Contains(sortBy.ToLowerInvariant()))
            throw new RequestValidationException($"SortBy hợp lệ: {string.Join(", ", allowed)}.");
    }
}
