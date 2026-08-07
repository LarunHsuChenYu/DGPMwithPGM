namespace DGPM_SPM.Core.Domain.Entities;

public abstract class BaseEntity
{
    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}
