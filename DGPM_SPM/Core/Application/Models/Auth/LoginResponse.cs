namespace DGPM_SPM.Core.Application.Models.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
    public List<MenuDto> Menus { get; set; } = new();
    public bool PasswordExpired { get; set; }
}
