using System.Text.Json.Serialization;

namespace DGPM_SPM.Core.Application.Queries;

/// <summary>
/// 分頁查詢基底。
///
/// 設計要點：
///   - 客戶端只需傳 Page + PageSize
///   - RowSkip 由 base 自動算出（避免 client 傳互相矛盾的參數）
///   - Setter 內建邊界檢查，非法值會被 clamp 到合法範圍
/// </summary>
public abstract class FilterBase
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>PageSize = 0 表示不分頁（一次取回全部符合條件資料）。</summary>
    public const int NoPagingPageSize = 0;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>頁碼（從 1 起）。傳入 &lt;1 會被自動修正為 1。</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每頁筆數。
    /// 0 = 不分頁；其餘非法值或超過 <see cref="MaxPageSize"/> 會被 clamp。
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            NoPagingPageSize => NoPagingPageSize,
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>SQL OFFSET 用。伺服器端計算，不接受客戶端傳入（[JsonIgnore]）。不分頁時為 0。</summary>
    [JsonIgnore]
    public int RowSkip => PageSize == NoPagingPageSize ? 0 : (Page - 1) * PageSize;
}
