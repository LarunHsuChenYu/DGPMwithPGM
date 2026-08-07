namespace DGPM_SPM.Core.Application.Models.Auth;

/// <summary>對應 PGM 契約 POST /api/auth/change-password（Bearer 或現行強制改密流程）。</summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
