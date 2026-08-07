namespace DGPM_SPM.Core.Application.Models.Kpi;

/// <summary>建立 / 編輯 KPI 指標的共用請求（欄位以暫定 schema kpi.KPI_INDICATOR 為準）。</summary>
public class SaveKpiIndicatorRequest
{
    public string IndicatorCode { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public string? Unit { get; set; }

    /// <summary>N=數值, P=百分比, A=金額。</summary>
    public string DataType { get; set; } = "N";

    public byte DecimalPlaces { get; set; } = 2;
    public int SortOrder { get; set; }
    public string? Memo { get; set; }
}

public class SetKpiIndicatorStatusRequest
{
    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = string.Empty;
}
