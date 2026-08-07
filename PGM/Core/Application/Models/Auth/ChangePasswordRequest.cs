namespace PGM.Core.Application.Models.Auth;

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
