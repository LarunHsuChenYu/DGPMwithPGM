namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// KPI 資料權限（kpi.KPI_USER_DATA_SCOPE）：使用者可見的區域 / 經銷商範圍。
/// ⚠ 對應 SQL/40_kpi_dealer_kpi.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// </summary>
public class KpiUserDataScope : BaseEntity
{
    public int ScopeId { get; set; }

    /// <summary>邏輯對應 dbo.EMP_USER.USER_ID（不建跨界 FK）。</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>R=區域, D=經銷商。</summary>
    public string ScopeType { get; set; } = string.Empty;

    /// <summary>ScopeType = "R" 時必填。</summary>
    public int? RegionId { get; set; }

    /// <summary>ScopeType = "D" 時必填。</summary>
    public int? DealerId { get; set; }

    /// <summary>查詢時 JOIN org.REGION 附帶的顯示欄位；非 KPI_USER_DATA_SCOPE 資料表欄位。</summary>
    public string? RegionCode { get; set; }
    public string? RegionName { get; set; }

    /// <summary>查詢時 JOIN org.DEALER 附帶的顯示欄位；非 KPI_USER_DATA_SCOPE 資料表欄位。</summary>
    public string? DealerCode { get; set; }
    public string? DealerName { get; set; }
}
