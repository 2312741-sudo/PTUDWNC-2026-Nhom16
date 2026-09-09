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
}
