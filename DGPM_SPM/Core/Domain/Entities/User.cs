namespace DGPM_SPM.Core.Domain.Entities;

public class User : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TitName { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? FactoryNo { get; set; }
    public string? DptCode { get; set; }
    public bool? DelFlg { get; set; }
    public IReadOnlyList<Role> Roles { get; set; } = [];
}
