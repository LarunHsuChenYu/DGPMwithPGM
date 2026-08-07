namespace DGPM_SPM.Web.Models;

/// <summary>對應後端 KpiUserDataScopeDto transport contract；欄位異動時需與 Api 同步。</summary>
public class KpiUserDataScopeDto
{
    public int ScopeId { get; set; }

    /// <summary>R=區域, D=經銷商</summary>
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
    public IReadOnlyList<KpiUserDataScopeDto> RegionScopes { get; set; } = [];
    public IReadOnlyList<KpiUserDataScopeDto> DealerScopes { get; set; } = [];
}

/// <summary>儲存使用者 KPI 資料權限（全量覆寫）。</summary>
public class SaveKpiUserPermissionRequest
{
    public List<int> RegionIds { get; set; } = [];
    public List<int> DealerIds { get; set; } = [];
}
