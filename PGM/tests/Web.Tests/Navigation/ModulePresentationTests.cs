using PGM.Web.Navigation;

namespace PGM.Web.Tests.Navigation;

public class ModulePresentationTests
{
    [Theory]
    [InlineData("Permission", "權", "#7c3aed")]
    [InlineData("SysConfig", "參", "#0d9488")]
    [InlineData("Syslog", "查", "#475569")]
    public void For_KnownModule_ReturnsExpectedPresentation(string functionId, string abbr, string color)
    {
        var (actualAbbr, actualColor, description) = ModulePresentation.For(functionId);

        actualAbbr.ShouldBe(abbr);
        actualColor.ShouldBe(color);
        description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void For_UnknownModule_UsesFallbackStyle()
    {
        var (abbr, color, description) = ModulePresentation.For("CustomMod");

        abbr.ShouldBe("C");
        color.ShouldBe("#64748b");
        description.ShouldBe("請選擇子功能進入");
    }

    [Fact]
    public void OverviewUrl_EncodesFunctionId()
    {
        ModulePresentation.OverviewUrl("Permission").ShouldBe("/module/Permission");
        ModulePresentation.OverviewUrl(null).ShouldBe("/module/");
    }
}
