using DGPM_SPM.Web.Models;
using DGPM_SPM.Web.Navigation;

namespace DGPM_SPM.Web.Tests.Navigation;

public class NavMenuItemsTests
{
    [Fact]
    public void Build_NullOrEmpty_ReturnsEmpty()
    {
        NavMenuItems.Build(null).ShouldBeEmpty();
        NavMenuItems.Build([]).ShouldBeEmpty();
    }

    [Fact]
    public void Build_GroupsChildrenUnderParent_AndNormalizesUrl()
    {
        var menus = new List<MenuDto>
        {
            new() { FunctionId = "SysConfig", FunctionName = "系統參數", ParentId = null, SortId = 1 },
            new()
            {
                FunctionId = "ExchangeRates",
                FunctionName = "匯率",
                ParentId = "SysConfig",
                FunctionUrl = "parameters/exchange-rates",
                SortId = 2
            },
            new()
            {
                FunctionId = "Orphan",
                FunctionName = "孤立",
                ParentId = "Missing",
                FunctionUrl = "/orphan",
                SortId = 9
            }
        };

        var tree = NavMenuItems.Build(menus);

        tree.Count.ShouldBe(1);
        tree[0].Title.ShouldBe("系統參數");
        tree[0].Url.ShouldBeNull();
        tree[0].Children.ShouldNotBeNull();
        tree[0].Children!.Count.ShouldBe(1);
        tree[0].Children![0].Title.ShouldBe("匯率");
        tree[0].Children![0].Url.ShouldBe("/parameters/exchange-rates");
    }

    [Fact]
    public void Build_IncludesAllChildrenFromMenus_TrustingPgmAsSourceOfTruth()
    {
        var menus = new List<MenuDto>
        {
            new() { FunctionId = "Permission", FunctionName = "系統權限", ParentId = null, SortId = 1 },
            new()
            {
                FunctionId = "RoleKPIList",
                FunctionName = "KPI 權限",
                ParentId = "Permission",
                FunctionUrl = "/system/kpi-permissions",
                SortId = 1
            },
            new()
            {
                FunctionId = "ExchangeRates",
                FunctionName = "匯率",
                ParentId = "Permission",
                FunctionUrl = "/parameters/exchange-rates",
                SortId = 2
            }
        };

        var tree = NavMenuItems.Build(menus);

        tree.Count.ShouldBe(1);
        tree[0].FunctionId.ShouldBe("Permission");
        tree[0].Children!.Count.ShouldBe(2);
        tree[0].Children![0].FunctionId.ShouldBe("RoleKPIList");
        tree[0].Children![1].FunctionId.ShouldBe("ExchangeRates");
    }

    [Fact]
    public void Build_MarksPgmAuthLinkAsExternal()
    {
        var menus = new List<MenuDto>
        {
            new() { FunctionId = "Permission", FunctionName = "系統權限管理", ParentId = null, SortId = 1 },
            new()
            {
                FunctionId = "PgmAuthLink",
                FunctionName = "帳號與角色維護",
                ParentId = "Permission",
                FunctionUrl = "ext:pgm",
                SortId = 1
            },
            new()
            {
                FunctionId = "RoleKPIList",
                FunctionName = "KPI 資料權限設定",
                ParentId = "Permission",
                FunctionUrl = "/system/kpi-permissions",
                SortId = 2
            }
        };

        var tree = NavMenuItems.Build(menus);

        tree[0].Children!.Count.ShouldBe(2);
        tree[0].Children![0].FunctionId.ShouldBe("PgmAuthLink");
        tree[0].Children![0].IsExternal.ShouldBeTrue();
        tree[0].Children![0].Url.ShouldBe("ext:pgm");
        NavMenuItems.ResolveHref(tree[0].Children![0], "https://localhost:7230")
            .ShouldBe("https://localhost:7230");
        tree[0].Children![1].IsExternal.ShouldBeFalse();
        tree[0].Children![1].Url.ShouldBe("/system/kpi-permissions");
    }

    [Fact]
    public void ResolveHref_AcceptsLegacyExternalPgmMarker()
    {
        var item = new NavMenuItem("前往 PGM", "external:pgm", "PgmAuthLink", IsExternal: true);
        NavMenuItems.ResolveHref(item, "http://localhost:8965")
            .ShouldBe("http://localhost:8965");
    }
}
