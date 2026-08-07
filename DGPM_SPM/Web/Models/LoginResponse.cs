namespace DGPM_SPM.Web.Models;

/// <summary>對應 POST /api/auth/login 的 response data。</summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
    public List<MenuDto> Menus { get; set; } = new();
    public bool PasswordExpired { get; set; }
}
