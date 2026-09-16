namespace CulinaryBlog.Domain.Common;

/// <summary>
/// Vi phạm bất biến (invariant) của Domain. Application sẽ ánh xạ sang ProblemDetails phù hợp
/// (ví dụ RECIPE_PUBLISH_INCOMPLETE -> 400 theo D02/D07).
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
