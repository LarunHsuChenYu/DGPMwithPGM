namespace DGPM_SPM.Core.Application.Queries;

/// <summary>KPI 指標查詢條件（分頁由 FilterBase 提供）。</summary>
public class KpiIndicatorFilter : FilterBase
{
    /// <summary>關鍵字，模糊比對指標代碼或名稱。</summary>
    public string? Keyword { get; set; }

    /// <summary>資料型態（N=數值, P=百分比, A=金額）；null 表示不篩選。</summary>
    public string? DataType { get; set; }

    /// <summary>狀態（A=啟用, I=停用）；null 表示不篩選。</summary>
    public string? Status { get; set; }
}
