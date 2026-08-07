namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// KPI 數據（kpi.KPI_DATA，經銷商 × 指標 × 年月）。
/// ⚠ 對應 SQL/40_kpi_dealer_kpi.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// </summary>
public class KpiData : BaseEntity
{
    public long DataId { get; set; }
    public int DealerId { get; set; }
    public int IndicatorId { get; set; }

    /// <summary>資料年月 yyyyMM。</summary>
    public string PeriodYm { get; set; } = string.Empty;

    public decimal? KpiValue { get; set; }

    /// <summary>最近一次寫入之匯入批次；手動修正可為 NULL。</summary>
    public long? BatchId { get; set; }

    /// <summary>D=草稿, R=覆核完成(鎖定), U=已解鎖待修正。</summary>
    public string ReviewStatus { get; set; } = "D";

    /// <summary>最近覆核/解鎖人。</summary>
    public string? ReviewUser { get; set; }

    public DateTime? ReviewDate { get; set; }

    /// <summary>查詢時 JOIN org.DEALER 附帶的經銷商代碼；非 KPI_DATA 資料表欄位。</summary>
    public string? DealerCode { get; set; }

    /// <summary>查詢時 JOIN org.DEALER 附帶的經銷商名稱；非 KPI_DATA 資料表欄位。</summary>
    public string? DealerName { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的指標代碼；非 KPI_DATA 資料表欄位。</summary>
    public string? IndicatorCode { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的指標名稱；非 KPI_DATA 資料表欄位。</summary>
    public string? IndicatorName { get; set; }

    /// <summary>查詢時 JOIN kpi.KPI_INDICATOR 附帶的單位；非 KPI_DATA 資料表欄位。</summary>
    public string? Unit { get; set; }
}
