using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Common.Attributes;
using PGM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace PGM.Core.Application.Mapping;

/// <summary>
/// 登入軌跡查詢對應。char 欄位轉字串、以及「尚未登出時 LogoutTime 不具意義」的判斷
/// 無法由 Mapperly 自動產生，故以 user-implemented mapping 手寫；
/// 同時刻意不對應 Guid 與 IdentityContent（不回傳敏感資訊）。
/// </summary>
[Mapper]
[ScopedRegistration]
public partial class AuthenticationLogMapper : IAuthenticationLogMapper
{
    private const char LoggedOutStatus = 'O';

    public AuthenticationLogDto ToDto(AuthenticationLog authenticationLog) => new()
    {
        UserId = authenticationLog.UserId,
        Ip = authenticationLog.Ip,
        LoginType = authenticationLog.LoginType.ToString(),
        AuthStatus = authenticationLog.AuthStatus.ToString(),
        LoginTime = authenticationLog.LoginTime,
        // 既有寫入流程在登入時會把 LOGOUT_TIME 先寫成登入時間（QMS 相容行為），
        // 僅在 AUTH_STATUS = 'O' 時 LOGOUT_TIME 才是真正的登出時間。
        LogoutTime = authenticationLog.AuthStatus == LoggedOutStatus
                     && authenticationLog.LogoutTime != default
            ? authenticationLog.LogoutTime
            : null
    };

    public IReadOnlyList<AuthenticationLogDto> ToDtos(IEnumerable<AuthenticationLog> authenticationLogs)
        => authenticationLogs.Select(ToDto).ToList();
}
