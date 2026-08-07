namespace PGM.Core.Application.Models.Auth;

/// <summary>
/// 使用者登入軌跡查詢列表項目（系統資料查詢 / 使用者登入軌跡查詢）。
/// 純查詢用途；刻意不回傳 GUID（session 識別）與 IDENTITY_CONTENT（登入身分內容），避免洩漏敏感資訊。
/// </summary>
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

    /// <summary>登出時間；尚未登出（AuthStatus != O）時為 null。</summary>
    public DateTime? LogoutTime { get; set; }
}
