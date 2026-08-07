using PGM.Core.Application.Models;

namespace PGM.Core.Tests.Models;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]      // 空資料 → 0 頁
    [InlineData(1, 20, 1)]      // 剛好一筆 → 1 頁
    [InlineData(20, 20, 1)]     // 剛好一頁 → 1 頁
    [InlineData(21, 20, 2)]     // 溢出一筆 → 2 頁
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    public void TotalPages_IsCeilingDivision(int totalRow, int pageSize, int expected)
    {
        var result = new PagedResult<string>
        {
            TotalRow = totalRow,
            PageSize = pageSize,
        };

        result.TotalPages.ShouldBe(expected);
    }

    [Fact]
    public void TotalPages_IsOne_WhenNoPagingAndHasRows()
    {
        // PageSize=0 表示不分頁：有資料時視為 1 頁
        var result = new PagedResult<string> { TotalRow = 100, PageSize = 0 };
        result.TotalPages.ShouldBe(1);
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void TotalPages_IsZero_WhenNoPagingAndEmpty()
    {
        var result = new PagedResult<string> { TotalRow = 0, PageSize = 0 };
        result.TotalPages.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, 5, true)]     // 第 1 頁，共 5 頁 → 還有下一頁
    [InlineData(5, 5, false)]    // 最後一頁 → 沒有下一頁
    [InlineData(6, 5, false)]    // 越界 → 沒有下一頁
    public void HasNextPage_ReflectsPagePosition(int currentPage, int totalPages, bool expected)
    {
        // 反推 totalRow：totalPages * pageSize
        var result = new PagedResult<string>
        {
            Page = currentPage,
            PageSize = 10,
            TotalRow = totalPages * 10,
        };

        result.HasNextPage.ShouldBe(expected);
    }

    [Fact]
    public void Map_ConvertsItems_AndPreservesMetadata()
    {
        // Arrange
        var source = new PagedResult<int>
        {
            Datas = new[] { 1, 2, 3 },
            TotalRow = 100,
            Page = 2,
            PageSize = 20,
        };

        // Act：把 int 轉成 string
        var mapped = source.Map(ints => ints.Select(i => $"item-{i}"));

        // Assert：Items 已轉型，其餘欄位原封不動
        mapped.Datas.ShouldBe(new[] { "item-1", "item-2", "item-3" });
        mapped.TotalRow.ShouldBe(100);
        mapped.Page.ShouldBe(2);
        mapped.PageSize.ShouldBe(20);
        mapped.TotalPages.ShouldBe(5);   // 100 / 20
    }
}
