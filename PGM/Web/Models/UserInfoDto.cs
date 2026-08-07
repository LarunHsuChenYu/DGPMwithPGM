namespace PGM.Web.Models;

/// <summary>對應 GET /api/auth/me 與 LoginResponse.User 的使用者資訊。</summary>
public class UserInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? FactoryNo { get; set; }
}
