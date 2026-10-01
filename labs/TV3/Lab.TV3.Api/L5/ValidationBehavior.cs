using MediatR;

namespace Lab.TV3.Api.L5;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var validator in validators)
            errors.AddRange(validator.Validate(request));

        if (errors.Count > 0)
            throw new LabValidationException(errors);

        return await next(cancellationToken);
    }
}
