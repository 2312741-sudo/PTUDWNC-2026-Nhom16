namespace Lab.TV3.Api.L5;

/// <summary>Validator tự định nghĩa cho LAB K04 (không dùng FluentValidation). Trả danh sách lỗi; rỗng = hợp lệ.</summary>
public interface IValidator<in T>
{
    IReadOnlyList<string> Validate(T request);
}

/// <summary>Ném bởi ValidationBehavior khi có ít nhất một lỗi; chứa lỗi của mọi validator.</summary>
public sealed class LabValidationException(IReadOnlyList<string> errors)
    : Exception("Request không hợp lệ: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Ngưỡng của PerformanceBehavior; đăng ký singleton trong DI.</summary>
public sealed record PerformanceOptions(TimeSpan Threshold)
{
    public static PerformanceOptions Default { get; } = new(TimeSpan.FromMilliseconds(500));
}
