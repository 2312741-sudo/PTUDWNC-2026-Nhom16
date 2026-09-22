using System.Text.Json.Serialization;
using MediatR;
using FluentValidation;
using CulinaryBlog.Domain;

namespace CulinaryBlog.Application;

[method: JsonConstructor]
public sealed record UserDto(
    string Id,
    string Email,
    string FullName,
    string UserName,
    string[] Roles,
    string? AvatarUrl = null,
    string? Bio = null,
    bool EmailConfirmed = false,
    DateTimeOffset? CreatedAt = null,
    string? DisplayName = null)
{
    public UserDto(string id, string email, string displayName, string[] roles, string? avatarUrl = null, string? bio = null)
        : this(id, email, displayName, email, roles, avatarUrl, bio, false, DateTimeOffset.UtcNow, displayName) { }
}

[method: JsonConstructor]
public sealed record AuthResponse(
    string AccessToken,
    string? RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset ExpiresAt,
    UserDto User)
{
    public AuthResponse(string accessToken, string tokenType, int expiresIn, UserDto user)
        : this(accessToken, null, tokenType, expiresIn, DateTimeOffset.UtcNow.AddSeconds(expiresIn), user) { }
}

[method: JsonConstructor]
public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FullName = null,
    string? UserName = null,
    string? DisplayName = null,
    string? Role = null,
    string[]? Roles = null) : IRequest<AuthResponse>
{
    public RegisterCommand(string email, string password, string displayName)
        : this(email, password, displayName, email, displayName, null, null) { }

    public string ResolvedName => !string.IsNullOrWhiteSpace(FullName) ? FullName : (DisplayName ?? "");
    public string ResolvedUserName => !string.IsNullOrWhiteSpace(UserName) ? UserName : Email;
}

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public sealed record GetMeQuery : IRequest<UserDto>;

[method: JsonConstructor]
public sealed record UpdateProfileCommand(
    string? DisplayName = null,
    string? AvatarUrl = null,
    string? Bio = null,
    string? FullName = null) : IRequest<UserDto>
{
    public UpdateProfileCommand(string displayName, string? avatarUrl, string? bio)
        : this(displayName, avatarUrl, bio, displayName) { }

    public string ResolvedName => !string.IsNullOrWhiteSpace(FullName) ? FullName : (DisplayName ?? "");
}

public interface ICurrentUser { string? UserId { get; } bool IsInRole(string role); }

public interface IIdentityService
{
    Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken ct);
    Task<UserDto> GetAsync(string id, CancellationToken ct);
    Task<UserDto> UpdateAsync(string id, UpdateProfileCommand command, CancellationToken ct);
    Task<AuthResponse> LoginWithGoogleAsync(GoogleUserPayload payload, CancellationToken ct);
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

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Tên không được để trống.")
            .MaximumLength(100)
            .Must(x => x is null || (!x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>')))
            .When(x => x.FullName is null || x.DisplayName is not null);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Tên không được để trống.")
            .MaximumLength(100)
            .Must(x => x is null || (!x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>')))
            .When(x => x.FullName is not null && x.DisplayName is null);
    }
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Tên không được để trống.")
            .Must(x => x is null || !string.IsNullOrWhiteSpace(x)).WithMessage("Tên không được chỉ chứa khoảng trắng.")
            .MaximumLength(100)
            .Must(x => x is null || (!x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>')))
            .When(x => x.DisplayName is not null || x.FullName is null);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Tên không được để trống.")
            .Must(x => x is null || !string.IsNullOrWhiteSpace(x)).WithMessage("Tên không được chỉ chứa khoảng trắng.")
            .MaximumLength(100)
            .Must(x => x is null || (!x.Any(char.IsControl) && !x.Contains('<') && !x.Contains('>')))
            .When(x => x.FullName is not null && x.DisplayName is null);

        RuleFor(x => x.AvatarUrl).MaximumLength(500)
            .Must(x => x is null || (Uri.TryCreate(x, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)));
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
