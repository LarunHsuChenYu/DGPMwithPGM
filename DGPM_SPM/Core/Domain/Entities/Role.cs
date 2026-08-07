namespace DGPM_SPM.Core.Domain.Entities;

public class Role : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    public bool DelFlg { get; set; }
}
