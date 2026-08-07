namespace DGPM_SPM.Core.Application.Queries;

/// <summary>經銷商列表查詢條件（分頁由 FilterBase 提供）。</summary>
public class DealerFilter : FilterBase
{
    /// <summary>關鍵字，模糊比對經銷商代碼或名稱。</summary>
    public string? Keyword { get; set; }

    /// <summary>所屬區域。</summary>
    public int? RegionId { get; set; }

    /// <summary>A=啟用, I=停用；空值 = 不限。</summary>
    public string? Status { get; set; }
}
