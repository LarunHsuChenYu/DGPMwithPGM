namespace PGM.Core.Common.Jwt;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; }
    /// <summary>Access Token 有效分鐘數（對齊舊 Web JwtOptions，預設 240）</summary>
    public int AccessTokenMinutes { get; set; } = 10;
}
