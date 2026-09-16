using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application;

public sealed record GoogleUserPayload(
    string Subject,
    string Email,
    bool EmailVerified,
    string Name,
    string? Picture);

public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthResponse>;

public interface IGoogleAuthService
{
    Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct);
}

public sealed class GoogleLoginHandler(IGoogleAuthService googleAuth, IIdentityService identity) : IRequestHandler<GoogleLoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        var payload = await googleAuth.ValidateIdTokenAsync(request.IdToken, ct);
        return await identity.LoginWithGoogleAsync(payload, ct);
    }
}

public sealed class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token không được để trống.")
            .MaximumLength(4096).WithMessage("Google ID Token vượt quá độ dài tối đa cho phép.");
    }
}
