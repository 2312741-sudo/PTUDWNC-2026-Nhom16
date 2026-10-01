using MediatR;

namespace Lab.TV3.Api.L5;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // TODO 1: chạy Validate(request) của TỪNG validator trong `validators`, gom mọi lỗi vào một danh sách.
        // TODO 2: nếu danh sách lỗi có phần tử -> throw new LabValidationException(danhSáchLỗi) và KHÔNG gọi next.
        // TODO 3: nếu không có lỗi (hoặc không có validator nào) -> gọi next() đúng 1 lần và trả nguyên response.
        // TODO 4: không bắt/nuốt exception do next() ném ra.
        throw new NotImplementedException();
    }
}
