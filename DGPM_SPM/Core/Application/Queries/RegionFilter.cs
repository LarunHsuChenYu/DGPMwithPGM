namespace DGPM_SPM.Core.Application.Queries;

public class RegionFilter : FilterBase
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? ParentRegionId { get; set; }
}
