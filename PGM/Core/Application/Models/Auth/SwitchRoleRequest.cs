namespace PGM.Core.Application.Models.Auth;

/// <summary>切換目前作業角色（不需重新登入）。</summary>
public class SwitchRoleRequest
{
    /// <summary>目標 ROLE_ID（可為純 ROLE_ID，或登入後的 composed 型式 ROLE$USER$SELF）。</summary>
    public string RoleId { get; set; } = string.Empty;
}
