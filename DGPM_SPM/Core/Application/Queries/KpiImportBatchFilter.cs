namespace DGPM_SPM.Core.Application.Queries;

/// <summary>
/// KPI 匯入批次查詢條件（分頁由 FilterBase 提供）。
/// 與「KPI 匯入日誌查詢」共用同一資料模型。
/// </summary>
public class KpiImportBatchFilter : FilterBase
{
    /// <summary>資料年月 yyyyMM；null 表示不篩選。</summary>
    public string? PeriodYm { get; set; }

    /// <summary>匯入狀態（P=處理中, S=成功, F=失敗）；null 表示不篩選。</summary>
    public string? ImportStatus { get; set; }
}
