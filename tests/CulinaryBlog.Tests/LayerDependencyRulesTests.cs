using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// C7 — Quy tắc phụ thuộc giữa các tầng bằng NetArchTest (mức KIỂU, đọc IL bằng Mono.Cecil).
/// Bổ sung cho ArchitectureTests (mức assembly reference): một assembly có thể không reference EF Core
/// nhưng quy tắc mức kiểu còn chỉ ra đúng kiểu nào vi phạm (FailingTypeNames).
/// Minimal API trong Program.cs nằm chung kiểu Program với composition root (DI, migrate, seed) nên không đưa vào R3.
/// </summary>
public sealed class LayerDependencyRulesTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(DisplayName).Assembly;
    private static readonly System.Reflection.Assembly Application = typeof(RegisterCommand).Assembly;
    private static readonly System.Reflection.Assembly Infrastructure = typeof(AuthDbContext).Assembly;
    private static readonly System.Reflection.Assembly Api = typeof(Program).Assembly;

    private static void AssertRule(TestResult result) =>
        Assert.True(result.IsSuccessful, "Kiểu vi phạm: " + string.Join(", ", result.FailingTypeNames ?? []));

    [Fact]
    public void R1_Application_does_not_depend_on_Infrastructure_EFCore_Npgsql_or_AspNetCore()
    {
        AssertRule(Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny(
                "CulinaryBlog.Infrastructure", "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult());
    }

    [Fact]
    public void R2_Domain_does_not_depend_on_outer_layers_or_frameworks()
    {
        AssertRule(Types.InAssembly(Domain)
            .ShouldNot().HaveDependencyOnAny(
                "CulinaryBlog.Application", "CulinaryBlog.Infrastructure", "CulinaryBlog.API", "CulinaryBlog.Api",
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MediatR", "FluentValidation")
            .GetResult());
    }

    [Fact]
    public void R3_Controllers_do_not_depend_on_EFCore_or_Infrastructure()
    {
        var controllers = Types.InAssembly(Api).That().Inherit(typeof(ControllerBase));
        Assert.NotEmpty(controllers.GetTypes());   // không có controller nào thì quy tắc xanh rỗng

        AssertRule(controllers
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "CulinaryBlog.Infrastructure")
            .GetResult());
    }

    /// <summary>
    /// Đối chứng dương: gói NetArchTest (2021, netstandard2.0) phải thật sự đọc được assembly net10 và thấy phụ thuộc.
    /// Nếu Cecil không nạp được kiểu thì R1-R3 xanh giả; hai khẳng định dưới sẽ đỏ trước.
    /// </summary>
    [Fact]
    public void R4_Positive_control_NetArchTest_really_sees_dependencies_in_net10_assemblies()
    {
        var repositories = Types.InAssembly(Infrastructure).That().HaveNameEndingWith("Repository");
        Assert.NotEmpty(repositories.GetTypes());
        AssertRule(repositories.Should().HaveDependencyOn("Microsoft.EntityFrameworkCore").GetResult());

        // Application CÓ dùng MediatR -> quy tắc "không phụ thuộc MediatR" phải đỏ và nêu được tên kiểu.
        var mustFail = Types.InAssembly(Application).ShouldNot().HaveDependencyOn("MediatR").GetResult();
        Assert.False(mustFail.IsSuccessful);
        Assert.Contains(typeof(CreateRecipeHandler).FullName, mustFail.FailingTypeNames);
    }
}
