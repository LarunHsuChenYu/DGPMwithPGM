namespace DGPM_SPM.Core.Application.Models.Kpi;

/// <summary>單筆 KPI 資料權限範圍（區域或經銷商，暫定 schema kpi.KPI_USER_DATA_SCOPE）。</summary>
public class KpiUserDataScopeDto
{
    public int ScopeId { get; set; }

    /// <summary>R=區域, D=經銷商。</summary>
    public string ScopeType { get; set; } = string.Empty;

    public int? RegionId { get; set; }
    public string? RegionCode { get; set; }
    public string? RegionName { get; set; }

    public int? DealerId { get; set; }
    public string? DealerCode { get; set; }
    public string? DealerName { get; set; }
}

/// <summary>使用者的 KPI 資料權限（主體資訊 + 目前授權範圍）。</summary>
public class KpiUserPermissionDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>ScopeType = "R" 的區域範圍。</summary>
    public IReadOnlyList<KpiUserDataScopeDto> RegionScopes { get; set; } = [];

    /// <summary>ScopeType = "D" 的經銷商範圍。</summary>
    public IReadOnlyList<KpiUserDataScopeDto> DealerScopes { get; set; } = [];
}

/// <summary>儲存使用者 KPI 資料權限的請求：以全量覆寫方式取代該使用者的授權範圍。</summary>
public class SaveKpiUserPermissionRequest
{
    public List<int> RegionIds { get; set; } = [];
    public List<int> DealerIds { get; set; } = [];
}
