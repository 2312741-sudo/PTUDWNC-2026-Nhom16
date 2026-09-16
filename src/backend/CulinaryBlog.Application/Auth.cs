using MediatR;
using FluentValidation;
using CulinaryBlog.Domain;

namespace CulinaryBlog.Application;

public sealed record UserDto(string Id, string Email, string DisplayName, string[] Roles);
public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, UserDto User);
public sealed record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResponse>;
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public sealed record GetMeQuery : IRequest<UserDto>;
public sealed record UpdateProfileCommand(string DisplayName, string? AvatarUrl, string? Bio) : IRequest<UserDto>;
public interface ICurrentUser { string? UserId { get; } bool IsInRole(string role); }
public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken ct);
    Task<UserDto> GetAsync(string id, CancellationToken ct);
    Task<UserDto> UpdateAsync(string id, UpdateProfileCommand command, CancellationToken ct);
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
public sealed class UpdateProfileHandler(IIdentityService identity, ICurrentUser user) : IRequestHandler<UpdateProfileCommand, UserDto>
{
    public Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken ct) => identity.UpdateAsync(user.UserId ?? throw new AppException(401, "auth.unauthorized", "Vui lòng đăng nhập."), request, ct);
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
public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100).Must(x => !x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>'));
        RuleFor(x => x.AvatarUrl).MaximumLength(500).Must(x => x is null || Uri.TryCreate(x, UriKind.Absolute, out _));
        RuleFor(x => x.Bio).MaximumLength(2000).Must(x => x is null || !x.Any(char.IsControl));
    }
}
public sealed record LogoutCommand(string? RefreshToken = null) : IRequest;
public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken ct)
    {
        // Tuần 1: access-token-only. Revoke refresh token thật gắn khi C5 (rotation) merge — tuần 2.
        return Task.CompletedTask;
    }
}
public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken).MaximumLength(500);
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
