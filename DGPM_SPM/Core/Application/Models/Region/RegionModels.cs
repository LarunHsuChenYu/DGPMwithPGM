namespace DGPM_SPM.Core.Application.Models.Region;

public class RegionDto
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public int? ParentRegionId { get; set; }
    public string? ParentRegionName { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RegionOptionDto
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
}

public class RegionSaveRequest
{
    public string RegionCode { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public int? ParentRegionId { get; set; }

    public int SortOrder { get; set; }
}

public class RegionStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
