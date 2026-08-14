namespace PGM.Core.Application.Models.Auth;

public class PgmUiModeDto
{
    /// <summary><c>On</c>｜<c>Off</c>。</summary>
    public string Mode { get; set; } = PGM.Core.Common.Auth.PgmUiMode.Default;

    /// <summary>目前登入帳號是否可切 Mode（掛 PGMAdmin 或舊 ADMIN）。</summary>
    public bool CanEdit { get; set; }
}

public class UpdatePgmUiModeRequest
{
    /// <summary><c>On</c>｜<c>Off</c>。</summary>
    public string Mode { get; set; } = string.Empty;
}

public class AdminResetPasswordRequest
{
    /// <summary>新密碼；空白則重設為預設 <c>0000</c>。</summary>
    public string? NewPassword { get; set; }
}
