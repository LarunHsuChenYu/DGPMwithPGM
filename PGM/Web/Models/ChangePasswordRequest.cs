namespace PGM.Web.Models;

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
