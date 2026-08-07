namespace DGPM_SPM.Web.Models;

/// <summary>對應後端 KpiChangeLogDto（KPI 異動紀錄查詢列表）。</summary>
public class KpiChangeLogDto
{
    public long LogId { get; set; }
    public long DataId { get; set; }

    /// <summary>資料年月 yyyyMM</summary>
    public string? PeriodYm { get; set; }

    public string? DealerCode { get; set; }
    public string? DealerName { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? Unit { get; set; }

    /// <summary>I=匯入, M=修改, R=覆核, U=解鎖</summary>
    public string ActionType { get; set; } = string.Empty;

    public decimal? OldValue { get; set; }
    public decimal? NewValue { get; set; }

    /// <summary>修改/解鎖原因（覆核備註亦記錄於此）</summary>
    public string? Reason { get; set; }

    public string ActionUser { get; set; } = string.Empty;
    public DateTime? ActionDate { get; set; }
}
