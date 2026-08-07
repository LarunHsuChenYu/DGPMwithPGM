namespace DGPM_SPM.Core.Application.Models.Auth;

/// <summary>對應 PGM 契約 POST /api/auth/switch-role（Bearer；重簽 JWT）。</summary>
public class SwitchRoleRequest
{
    public string RoleId { get; set; } = string.Empty;
}
