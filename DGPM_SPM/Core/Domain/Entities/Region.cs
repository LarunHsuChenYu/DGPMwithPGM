namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// 區域組織（org.REGION）。
/// ⚠ 對應 SQL/20_org_master_data.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// </summary>
public class Region : BaseEntity
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;

    /// <summary>上層區域；null = 最上層。</summary>
    public int? ParentRegionId { get; set; }
    public string? ParentRegionName { get; set; }

    public int SortOrder { get; set; }

    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = "A";
}
