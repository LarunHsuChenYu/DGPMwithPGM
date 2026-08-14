namespace PGM.Core.Application.Interfaces;

/// <summary>
/// 系統權限（AUTH*）維護閘道：依 PgmUiMode + JWT sys + MAP_ROLE_FUNCTION。
/// </summary>
public interface IAuthMaintenanceGate
{
    /// <summary>
    /// 是否允許目前使用者存取指定 AUTH Fun。
    /// <paramref name="isWrite"/>＝true 時，Mode=Off 且 sys=PGM 一律拒絕寫入。
    /// </summary>
    Task<AuthMaintenanceDecision> EvaluateAsync(
        string requiredFunctionId,
        bool isWrite,
        CancellationToken ct = default);
}

public sealed class AuthMaintenanceDecision
{
    public bool Allowed { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public static AuthMaintenanceDecision Allow() => new() { Allowed = true };

    public static AuthMaintenanceDecision Deny(string code, string message) =>
        new() { Allowed = false, Code = code, Message = message };
}
