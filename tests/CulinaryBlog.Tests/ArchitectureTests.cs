using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Inner_layers_do_not_reference_infrastructure_or_web()
    {
        foreach (var assembly in new[] { typeof(DisplayName).Assembly, typeof(RegisterCommand).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), r => r.Name!.Contains("Infrastructure") || r.Name.Contains("AspNetCore") || r.Name.Contains("EntityFrameworkCore"));
        Assert.DoesNotContain(typeof(DisplayName).Assembly.GetReferencedAssemblies(), r => !r.Name!.StartsWith("System") && r.Name != "netstandard");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<b>Tâm</b>")]
    [InlineData("name\n")]
    public void Display_name_rejects_invalid_values(string value) => Assert.Throws<ArgumentException>(() => new DisplayName(value));
    [Fact]
    public void Display_name_trims_text() => Assert.Equal("Tâm", new DisplayName(" Tâm ").Value);
    [Fact]
    public async Task Validation_pipeline_stops_handler_on_invalid_password()
    {
        var called = false;
        var behavior = new ValidationBehavior<RegisterCommand, AuthResponse>([new RegisterValidator()]);
        RequestHandlerDelegate<AuthResponse> next = _ => { called = true; return Task.FromResult<AuthResponse>(null!); };
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(new("a@example.test", "weak", "Tâm"), next, default));
        Assert.False(called);
    }

    [Theory]
    [InlineData("Nguyễn Thanh Tâm", "https://example.com/avatar.png", "Đầu bếp nghiệp dư")]
    [InlineData("Tam", null, null)]
    public async Task Update_profile_validator_accepts_valid_inputs(string name, string? avatar, string? bio)
    {
        var validator = new UpdateProfileValidator();
        var result = await validator.ValidateAsync(new UpdateProfileCommand(name, avatar, bio));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Tên không được để trống")]
    [InlineData("   ", "Tên không được chỉ chứa khoảng trắng")]
    [InlineData("<script>alert(1)</script>", "Tên không được chứa thẻ HTML")]
    [InlineData("Name\nWithNewline", "Tên không được chứa control character")]
    public async Task Update_profile_validator_rejects_invalid_display_name(string invalidName, string reason)
    {
        _ = reason;
        var validator = new UpdateProfileValidator();
        var result = await validator.ValidateAsync(new UpdateProfileCommand(invalidName, null, null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path/image.jpg")]
    public async Task Update_profile_validator_rejects_invalid_avatar_url(string invalidUrl)
    {
        var validator = new UpdateProfileValidator();
        var result = await validator.ValidateAsync(new UpdateProfileCommand("Tâm", invalidUrl, null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.AvatarUrl));
    }

    [Fact]
    public async Task Update_profile_validator_rejects_excessive_lengths()
    {
        var validator = new UpdateProfileValidator();
        var tooLongName = new string('a', 101);
        var tooLongBio = new string('b', 2001);
        var result = await validator.ValidateAsync(new UpdateProfileCommand(tooLongName, null, tooLongBio));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.Bio));
    }

    [Fact]
    public async Task Logout_validator_checks_refresh_token_length()
    {
        var validator = new LogoutValidator();
        Assert.True((await validator.ValidateAsync(new LogoutCommand(null))).IsValid);
        Assert.True((await validator.ValidateAsync(new LogoutCommand("valid-token-string"))).IsValid);
        var tooLongToken = new string('x', 501);
        var result = await validator.ValidateAsync(new LogoutCommand(tooLongToken));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LogoutCommand.RefreshToken));
    }
}
