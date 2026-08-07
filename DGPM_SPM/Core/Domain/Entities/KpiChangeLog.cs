namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// KPI 異動紀錄（kpi.KPI_CHANGE_LOG，含覆核/解鎖留痕）。
/// ⚠ 對應 SQL/40_kpi_dealer_kpi.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// </summary>
public class KpiChangeLog
{
    public long LogId { get; set; }
    public long DataId { get; set; }

    /// <summary>I=匯入, M=修改, R=覆核, U=解鎖。</summary>
    public string ActionType { get; set; } = string.Empty;

    public decimal? OldValue { get; set; }
    public decimal? NewValue { get; set; }

    /// <summary>修改/解鎖原因（覆核備註亦記錄於此）。</summary>
    public string? Reason { get; set; }

    public string ActionUser { get; set; } = string.Empty;
    public DateTime? ActionDate { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_DATA 附帶的資料年月 yyyyMM；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? PeriodYm { get; set; }

    /// <summary>查詢時 JOIN org.DEALER 附帶的經銷商代碼；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? DealerCode { get; set; }

    /// <summary>查詢時 JOIN org.DEALER 附帶的經銷商名稱；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? DealerName { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的指標代碼；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? IndicatorCode { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的指標名稱；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? IndicatorName { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的單位；非 KPI_CHANGE_LOG 資料表欄位。</summary>
    public string? Unit { get; set; }
}
