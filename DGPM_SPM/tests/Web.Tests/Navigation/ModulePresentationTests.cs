using DGPM_SPM.Web.Navigation;

namespace DGPM_SPM.Web.Tests.Navigation;

public class ModulePresentationTests
{
    [Theory]
    [InlineData("Masterdata", "基", "#2563eb")]
    [InlineData("Permission", "權", "#7c3aed")]
    [InlineData("SysConfig", "參", "#0d9488")]
    [InlineData("KPIIndicator", "K", "#d97706")]
    [InlineData("Syslog", "查", "#475569")]
    [InlineData("Dashboard", "儀", "#dc2626")]
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
