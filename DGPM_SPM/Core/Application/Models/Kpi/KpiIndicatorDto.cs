namespace DGPM_SPM.Core.Application.Models.Kpi;

public class KpiIndicatorDto
{
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public string? Unit { get; set; }

    /// <summary>N=數值, P=百分比, A=金額。</summary>
    public string DataType { get; set; } = string.Empty;

    public byte DecimalPlaces { get; set; }
    public int SortOrder { get; set; }

    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = string.Empty;

    public string? Memo { get; set; }
    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}
