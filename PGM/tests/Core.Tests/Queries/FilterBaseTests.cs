using PGM.Core.Application.Queries;

namespace PGM.Core.Tests.Queries;

public class FilterBaseTests
{
    // 用具體子類別做測試（abstract 沒辦法直接 new）
    private class TestFilter : FilterBase { }

    [Fact]
    public void Defaults_ArePage1AndDefaultPageSize()
    {
        var filter = new TestFilter();

        filter.Page.ShouldBe(1);
        filter.PageSize.ShouldBe(FilterBase.DefaultPageSize);
        filter.RowSkip.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(5, 50, 200)]
    public void RowSkip_IsCalculatedCorrectly(int page, int pageSize, int expectedRowSkip)
    {
        var filter = new TestFilter { Page = page, PageSize = pageSize };
        filter.RowSkip.ShouldBe(expectedRowSkip);
    }

    // ---------------- 邊界 clamp ----------------

    [Theory]
    [InlineData(0, 1)]       // 0 頁不合理 → 修正為 1
    [InlineData(-1, 1)]      // 負數 → 修正為 1
    [InlineData(-999, 1)]
    [InlineData(1, 1)]       // 合法值原封不動
    [InlineData(999, 999)]   // 合法值原封不動（不限制上限）
    public void Page_IsClampedToMinimum1(int input, int expected)
    {
        var filter = new TestFilter { Page = input };
        filter.Page.ShouldBe(expected);
    }

    [Theory]
    [InlineData(FilterBase.NoPagingPageSize, FilterBase.NoPagingPageSize)] // 0 = 不分頁
    [InlineData(-5, FilterBase.DefaultPageSize)]
    [InlineData(1, 1)]                             // 邊界合法
    [InlineData(50, 50)]                           // 合法區間
    [InlineData(FilterBase.MaxPageSize, FilterBase.MaxPageSize)]
    [InlineData(FilterBase.MaxPageSize + 1, FilterBase.MaxPageSize)]  // 上限 clamp
    [InlineData(99999, FilterBase.MaxPageSize)]                        // 遠超上限也 clamp
    public void PageSize_IsClampedToValidRange(int input, int expected)
    {
        var filter = new TestFilter { PageSize = input };
        filter.PageSize.ShouldBe(expected);
    }

    [Fact]
    public void RowSkip_IsZero_WhenNoPaging()
    {
        var filter = new TestFilter { Page = 3, PageSize = FilterBase.NoPagingPageSize };
        filter.RowSkip.ShouldBe(0);
    }
}
