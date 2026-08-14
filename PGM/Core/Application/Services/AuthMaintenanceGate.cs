using PGM.Core.Application.Interfaces;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Auth;

namespace PGM.Core.Application.Services;

/// <summary>
/// Mode＝On：僅 sys=PGM 可維護 AUTH；Mode＝Off：僅 sys=DGPM 可維護（PGM Web 寫入拒絕）。
/// 另須 MAP_ROLE_FUNCTION 含對應 AUTH Fun（DGPMAdmin／PGMAdmin 皆可）。
/// </summary>
[ScopedRegistration]
public class AuthMaintenanceGate : IAuthMaintenanceGate
{
    public const string DeniedCode = "AUTH_UI_MODE";
    public const string ForbiddenCode = "AUTH_FORBIDDEN";

    private readonly IPgmUiModeService _uiMode;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AuthMaintenanceGate(
        IPgmUiModeService uiMode,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _uiMode = uiMode;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthMaintenanceDecision> EvaluateAsync(
        string requiredFunctionId,
        bool isWrite,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return AuthMaintenanceDecision.Deny(ForbiddenCode, "尚未登入。");
        }

        var sys = NormalizeSystemCode(_currentUser.SystemCode);
        var mode = await _uiMode.GetModeValueAsync(ct);

        if (PgmUiMode.IsOn(mode))
        {
            if (!string.Equals(sys, "PGM", StringComparison.OrdinalIgnoreCase))
            {
                return AuthMaintenanceDecision.Deny(
                    DeniedCode,
                    "系統權限 UI 目前在 PGM（PgmUiMode=On），請至 PGM 維護。");
            }
        }
        else
        {
            if (string.Equals(sys, "PGM", StringComparison.OrdinalIgnoreCase))
            {
                if (isWrite)
                {
                    return AuthMaintenanceDecision.Deny(
                        DeniedCode,
                        "系統權限 UI 目前在 DGPM（PgmUiMode=Off），PGM Web 僅供唯讀／已關閉寫入。");
                }

                // Mode=Off + sys=PGM：讀取仍允許（唯讀）
            }
            else if (!string.Equals(sys, "DGPM", StringComparison.OrdinalIgnoreCase))
            {
                return AuthMaintenanceDecision.Deny(DeniedCode, "不支援的系統代碼。");
            }
        }

        if (string.IsNullOrWhiteSpace(requiredFunctionId))
            return AuthMaintenanceDecision.Allow();

        var roleId = ExtractPlainRoleId(_currentUser.RoleId);
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return AuthMaintenanceDecision.Deny(ForbiddenCode, "缺少角色，無法授權。");
        }

        var granted = await _unitOfWork.Roles.GetGrantedFunctionIdsAsync(roleId, ct);
        var ok = granted.Any(id =>
            string.Equals(id, requiredFunctionId, StringComparison.OrdinalIgnoreCase));
        if (!ok)
        {
            return AuthMaintenanceDecision.Deny(
                ForbiddenCode,
                $"缺少功能授權（{requiredFunctionId}）。");
        }

        return AuthMaintenanceDecision.Allow();
    }

    private static string NormalizeSystemCode(string? systemCode) =>
        string.IsNullOrWhiteSpace(systemCode) ? "PGM" : systemCode.Trim().ToUpperInvariant();

    private static string ExtractPlainRoleId(string? composedOrPlain)
    {
        if (string.IsNullOrWhiteSpace(composedOrPlain))
            return string.Empty;
        return composedOrPlain.Split('$')[0];
    }
}
