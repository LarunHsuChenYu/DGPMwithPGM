namespace DGPM_SPM.Core.Application.Queries;

/// <summary>KPI 數據覆核查詢條件（分頁由 FilterBase 提供）。</summary>
public class KpiDataFilter : FilterBase
{
    /// <summary>資料年月 yyyyMM；null 表示不篩選。</summary>
    public string? PeriodYm { get; set; }

    /// <summary>關鍵字，模糊比對經銷商或指標的代碼、名稱。</summary>
    public string? Keyword { get; set; }

    /// <summary>覆核狀態（D=草稿, R=覆核完成, U=已解鎖）；null 表示不篩選。</summary>
    public string? ReviewStatus { get; set; }
}
