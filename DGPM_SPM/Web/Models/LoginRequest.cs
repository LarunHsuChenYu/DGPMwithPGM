namespace DGPM_SPM.Web.Models;

/// <summary>對應 POST /api/auth/login 的 request body。</summary>
public class LoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>多角色時可指定；格式與 QMS 相容時可為 ROLE_ID$USER_ID$SELF。</summary>
    public string? RoleId { get; set; }
}
