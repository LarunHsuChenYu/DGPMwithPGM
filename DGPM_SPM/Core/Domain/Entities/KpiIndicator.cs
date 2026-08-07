namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>對應 kpi.KPI_INDICATOR（provisional draft，SDS 定稿後可能調整）。</summary>
public class KpiIndicator : BaseEntity
{
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>單位（台、%、金額…）。</summary>
    public string? Unit { get; set; }

    /// <summary>N=數值, P=百分比, A=金額。</summary>
    public string DataType { get; set; } = "N";

    public byte DecimalPlaces { get; set; } = 2;
    public int SortOrder { get; set; }

    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = "A";

    public string? Memo { get; set; }
}
