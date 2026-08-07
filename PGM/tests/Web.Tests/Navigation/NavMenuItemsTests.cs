using PGM.Web.Models;
using PGM.Web.Navigation;

namespace PGM.Web.Tests.Navigation;

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
                FunctionId = "AUTH04",
                FunctionName = "系統代碼",
                ParentId = "SysConfig",
                FunctionUrl = "parameters/param-set",
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
        tree[0].Children![0].Title.ShouldBe("系統代碼");
        tree[0].Children![0].Url.ShouldBe("/parameters/param-set");
    }

    [Fact]
    public void Build_RootWithUrlAndNoChildren_IsFlatLink()
    {
        var menus = new List<MenuDto>
        {
            new()
            {
                FunctionId = "AUTH01",
                FunctionName = "帳號維護",
                ParentId = null,
                FunctionUrl = "/system/users",
                SortId = 1
            },
            new()
            {
                FunctionId = "AUTH03",
                FunctionName = "重設密碼",
                ParentId = null,
                FunctionUrl = "account/change-password",
                SortId = 2
            }
        };

        var tree = NavMenuItems.Build(menus);

        tree.Count.ShouldBe(2);
        tree[0].Title.ShouldBe("帳號維護");
        tree[0].Url.ShouldBe("/system/users");
        tree[0].Children.ShouldBeNull();
        tree[1].Title.ShouldBe("重設密碼");
        tree[1].Url.ShouldBe("/account/change-password");
    }

    [Fact]
    public void Build_GroupWithoutVisibleChildren_IsOmitted()
    {
        var menus = new List<MenuDto>
        {
            new() { FunctionId = "EmptyGroup", FunctionName = "空群組", ParentId = null, SortId = 1 }
        };

        NavMenuItems.Build(menus).ShouldBeEmpty();
    }
}
