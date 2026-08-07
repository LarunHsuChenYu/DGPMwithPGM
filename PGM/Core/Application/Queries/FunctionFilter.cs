namespace PGM.Core.Application.Queries;

/// <summary>系統功能查詢條件（分頁由 FilterBase 提供）。</summary>
public class FunctionFilter : FilterBase
{
    /// <summary>關鍵字，模糊比對 Fun_ID 或 Fun_Name。</summary>
    public string? Keyword { get; set; }

    /// <summary>上層選單 Fun_ID；null 表示不篩選。</summary>
    public string? ParentId { get; set; }

    /// <summary>功能類型 M/P/B；null 表示不篩選。</summary>
    public string? ActionType { get; set; }
}
