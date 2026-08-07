namespace DGPM_SPM.Core.Application.Models.Kpi;

/// <summary>KPI 數據（含經銷商/指標名稱與覆核狀態），供覆核作業列表使用。</summary>
public class KpiDataDto
{
    public long DataId { get; set; }
    public int DealerId { get; set; }
    public string? DealerCode { get; set; }
    public string? DealerName { get; set; }
    public int IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? Unit { get; set; }

    /// <summary>資料年月 yyyyMM。</summary>
    public string PeriodYm { get; set; } = string.Empty;

    public decimal? KpiValue { get; set; }
    public long? BatchId { get; set; }

    /// <summary>D=草稿, R=覆核完成(鎖定), U=已解鎖待修正。</summary>
    public string ReviewStatus { get; set; } = string.Empty;

    public string? ReviewUser { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? CrtDate { get; set; }
    public DateTime? MdfDate { get; set; }
}

/// <summary>覆核確認請求（D/U → R）。備註選填，會寫入異動紀錄。</summary>
public class ReviewKpiDataRequest
{
    public string? Memo { get; set; }
}

/// <summary>解鎖退回請求（R → U）。原因必填，會寫入異動紀錄。</summary>
public class UnlockKpiDataRequest
{
    public string Reason { get; set; } = string.Empty;
}
