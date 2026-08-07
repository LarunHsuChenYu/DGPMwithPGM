namespace DGPM_SPM.Core.Application.Models.Auth;

public class LoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>多角色時可指定；格式與 QMS 相容時可為 ROLE_ID$USER_ID$SELF</summary>
    public string? RoleId { get; set; }
    /// <summary>外連 PGM 時傳 DGPM；本地模式可忽略。</summary>
    public string? SystemCode { get; set; }
}
