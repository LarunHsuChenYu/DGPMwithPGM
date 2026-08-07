namespace DGPM_SPM.Core.Application.Models;

/// <summary>
/// 分頁查詢結果的通用信封。
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Datas { get; init; } = Array.Empty<T>();
    public int TotalRow { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    /// <summary>總頁數。PageSize 為 0（不分頁）時視為單頁；無資料則為 0。</summary>
    public int TotalPages => PageSize <= 0
        ? (TotalRow > 0 ? 1 : 0)
        : (int)Math.Ceiling(TotalRow / (double)PageSize);

    /// <summary>是否還有下一頁。不分頁（PageSize≤0）時恆為 false。</summary>
    public bool HasNextPage => PageSize > 0 && Page < TotalPages;

    /// <summary>
    /// 將 Items 從 T 轉成 TDest 型別，其餘欄位（TotalRow / Page / PageSize）原封不動帶過去。
    /// </summary>
    public PagedResult<TDest> Map<TDest>(Func<IEnumerable<T>, IEnumerable<TDest>> mapper)
        => new()
        {
            Datas = mapper(Datas).ToList(),
            TotalRow = TotalRow,
            Page = Page,
            PageSize = PageSize,
        };
}
