namespace DGPM_SPM.Core.Application.Models.Auth;

public class PgmUiModeDto
{
    public string Mode { get; set; } = "On";
    public bool CanEdit { get; set; }
}

public class UpdatePgmUiModeRequest
{
    public string Mode { get; set; } = string.Empty;
}

public class AdminResetPasswordRequest
{
    public string? NewPassword { get; set; }
}
