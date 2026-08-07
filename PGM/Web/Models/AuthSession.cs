namespace PGM.Web.Models;

/// <summary>
/// 登入後存放於 protected browser session storage 的認證狀態。
/// 注意：後端 /api/auth/refresh 尚未實作（永遠回 401），token 過期即需重新登入。
/// </summary>
public class AuthSession
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
    public List<MenuDto> Menus { get; set; } = new();

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    public static AuthSession FromLogin(LoginResponse login) => new()
    {
        AccessToken = login.AccessToken,
        ExpiresAt = login.ExpiresAt,
        User = login.User,
        Menus = login.Menus
    };
}
