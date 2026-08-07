namespace PGM.Web.Models;

/// <summary>對應 POST /api/auth/switch-role。</summary>
public class SwitchRoleRequest
{
    public string RoleId { get; set; } = string.Empty;
}
