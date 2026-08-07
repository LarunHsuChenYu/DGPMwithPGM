namespace PGM.Web.Models;

/// <summary>對應後端 AuthenticationLogDto（使用者登入軌跡查詢列表）。</summary>
public class AuthenticationLogDto
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>登入來源 IP。</summary>
    public string? Ip { get; set; }

    /// <summary>登入類型（既有 QMS 代碼，例如 G）。</summary>
    public string LoginType { get; set; } = string.Empty;

    /// <summary>登入狀態（I=登入中, O=已登出）。</summary>
    public string AuthStatus { get; set; } = string.Empty;

    public DateTime LoginTime { get; set; }

    /// <summary>登出時間；尚未登出時為 null。</summary>
    public DateTime? LogoutTime { get; set; }
}
