using PGM.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace PGM.Web.Tests.Components;

public class PagerTests : BunitContext
{
    [Fact]
    public void Render_ShowsRowSummaryAndCurrentPage()
    {
        var cut = Render<Pager>(parameters => parameters
            .Add(p => p.CurrentPage, 2)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.TotalRows, 42)
            .Add(p => p.FromRow, 11)
            .Add(p => p.ToRow, 20)
            .Add(p => p.PageSize, 10)
            .Add(p => p.AriaLabel, "分頁測試"));

        cut.Markup.ShouldContain("顯示第 11 至 20 筆，共 42 筆");
        cut.Markup.ShouldContain("2 / 5");
        cut.Find("nav").GetAttribute("aria-label").ShouldBe("分頁測試");
    }

    [Fact]
    public async Task ClickNext_InvokesOnPageChanged()
    {
        int? requestedPage = null;

        var cut = Render<Pager>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 3)
            .Add(p => p.TotalRows, 30)
            .Add(p => p.FromRow, 1)
            .Add(p => p.ToRow, 10)
            .Add(p => p.PageSize, 10)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page => requestedPage = page)));

        // 下一頁按鈕（‹ 上一頁、› 下一頁）；取「下一頁」
        var buttons = cut.FindAll("button");
        buttons.Count.ShouldBeGreaterThanOrEqualTo(4);
        await buttons[2].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        requestedPage.ShouldBe(2);
    }
}
