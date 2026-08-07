namespace DGPM_SPM.Core.Application.Models.Auth;

public class UserInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? FactoryNo { get; set; }
}
