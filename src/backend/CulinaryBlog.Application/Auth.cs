using MediatR;
using FluentValidation;
using CulinaryBlog.Domain;

namespace CulinaryBlog.Application;

public sealed record UserDto(string Id, string Email, string DisplayName, string[] Roles);
public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, UserDto User);
public sealed record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResponse>;
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public sealed record GetMeQuery : IRequest<UserDto>;
public interface ICurrentUser { string? UserId { get; } bool IsInRole(string role); }
public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken ct);
    Task<UserDto> GetAsync(string id, CancellationToken ct);
}
public sealed class AppException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed class RegisterHandler(IIdentityService identity) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct) => identity.RegisterAsync(request, ct);
}
public sealed class LoginHandler(IIdentityService identity) : IRequestHandler<LoginCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct) => identity.LoginAsync(request, ct);
}
public sealed class GetMeHandler(IIdentityService identity, ICurrentUser user) : IRequestHandler<GetMeQuery, UserDto>
{
    public Task<UserDto> Handle(GetMeQuery request, CancellationToken ct) => identity.GetAsync(user.UserId ?? throw new AppException(401, "auth.unauthorized", "Vui lòng đăng nhập."), ct);
}
public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100)
            .Must(x => x is not null && !x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>'));
    }
}
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
