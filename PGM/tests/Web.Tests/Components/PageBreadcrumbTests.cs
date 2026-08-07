using PGM.Web.Components;

namespace PGM.Web.Tests.Components;

public class PageBreadcrumbTests : BunitContext
{
    [Fact]
    public void Render_ShowsModuleAndCurrentLabels()
    {
        var cut = Render<PageBreadcrumb>(parameters => parameters
            .Add(p => p.Module, "系統參數")
            .Add(p => p.Current, "匯率參數設定"));

        cut.Markup.ShouldContain("首頁");
        cut.Markup.ShouldContain("系統參數");
        cut.Markup.ShouldContain("匯率參數設定");
        cut.Find("a").GetAttribute("href").ShouldBe("/");
        cut.Find("strong").TextContent.ShouldBe("匯率參數設定");
    }
}
