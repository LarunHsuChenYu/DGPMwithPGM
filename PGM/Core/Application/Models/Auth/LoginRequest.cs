namespace PGM.Core.Application.Models.Auth;

public class LoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>多角色時可指定；格式與 QMS 相容時可為 ROLE_ID$USER_ID$SELF</summary>
    public string? RoleId { get; set; }
    /// <summary>消費端系統碼（PGM／DGPM）；省略時預設 PGM。</summary>
    public string? SystemCode { get; set; }
}
