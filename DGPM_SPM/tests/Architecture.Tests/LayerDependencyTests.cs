using System.Reflection;
using DGPM_SPM.Core.Application.Interfaces;

namespace DGPM_SPM.Architecture.Tests;

/// <summary>
/// 分層依賴守護測試。這些測試會在 CI 每次跑，任何人手滑把
/// Infrastructure 引用進 Core（或類似錯誤）都會立刻紅掉。
///
/// 比 code review 更可靠：機器不會累、不會分心。
/// </summary>
public class LayerDependencyTests
{
    // 用型別鎖定 assembly，比較 "Core" 這種字串更 refactor-safe
    private static readonly Assembly CoreAssembly = typeof(IRequestContext).Assembly;
    private static readonly Assembly InfrastructureAssembly =
        typeof(DGPM_SPM.Infrastructure.Persistence.DbSession).Assembly;

    private const string InfrastructureNamespace = "DGPM_SPM.Infrastructure";
    private const string ApiNamespace = "DGPM_SPM.Api";

    [Fact]
    public void Core_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Core 不應該引用 Infrastructure。違規型別：{FormatFailures(result)}");
    }

    [Fact]
    public void Core_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Core 不應該引用 Api。違規型別：{FormatFailures(result)}");
    }

    [Fact]
    public void Core_ShouldNotDependOn_AspNetCore()
    {
        // Core 不應該碰任何 HTTP/MVC 相關的東西
        var result = Types.InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Hosting")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Core 不應該引用 ASP.NET Core。違規型別：{FormatFailures(result)}");
    }

    [Fact]
    public void Core_ShouldNotDependOn_Dapper()
    {
        // Dapper 是 Infrastructure 選擇的 ORM，Core 不該知道
        var result = Types.InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Core 不應該直接使用 Dapper。違規型別：{FormatFailures(result)}");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Infrastructure 不應該引用 Api。違規型別：{FormatFailures(result)}");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        // 命名慣例：介面名開頭必須是 I
        // 這個測試同時保護了 IoC 掃描邏輯（依賴 "I{Name}" 命名對應）
        var result = Types.InAssembly(CoreAssembly)
            .That().AreInterfaces()
            .And().ResideInNamespace("DGPM_SPM.Core.Application.Interfaces")
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"介面命名違規：{FormatFailures(result)}");
    }

    private static string FormatFailures(TestResult result)
        => result.FailingTypeNames is null
            ? "(none)"
            : string.Join(", ", result.FailingTypeNames);
}
