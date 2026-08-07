namespace DGPM_SPM.Core.Domain.Entities;

public class AuthenticationLog
{
    public string Guid { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? IdentityContent { get; set; }
    public string? Ip { get; set; }
    public char LoginType { get; set; }
    public char AuthStatus { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LogoutTime { get; set; }
}
