namespace PGM.Core.Application.Interfaces;

/// <summary>
/// 抽象化的目前使用者上下文。實作放在 Api 層（HttpContext 是 HTTP 細節，不屬於 Core）。
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? RoleId { get; }
    string? RoleName { get; }
    string? SessionGuid { get; }
    string? SystemCode { get; }
    string? ClientIp { get; }
    bool IsAuthenticated { get; }
}
