namespace PGM.Core.Domain.Entities;

public class Role : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    /// <summary>系統隔離碼：PGM／DGPM。</summary>
    public string SystemCode { get; set; } = "PGM";
    public bool DelFlg { get; set; }
}
