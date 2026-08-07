namespace DGPM_SPM.Core.Domain.Entities;

public class Parameter : BaseEntity
{
    public string SetItem { get; set; } = string.Empty;
    public string SetType { get; set; } = string.Empty;
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Memo { get; set; }
    public bool DelFlg { get; set; }
}
