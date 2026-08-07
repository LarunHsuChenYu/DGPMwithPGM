using System.Security.Claims;
using PGM.Core.Application.Interfaces;
using PGM.Core.Common.Jwt;

namespace PGM.Api.Infrastructure;

/// <summary>
/// ICurrentUser 的 HTTP 實作。因為依賴 IHttpContextAccessor（HTTP 細節），放在 Api 層。
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        Principal?.FindFirst(JwtClaimNames.UserId)?.Value
        ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Principal?.FindFirst("sub")?.Value;

    public string? UserName => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? RoleId => Principal?.FindFirst(JwtClaimNames.RoleId)?.Value;

    public string? RoleName => Principal?.FindFirst(JwtClaimNames.RoleName)?.Value;

    public string? SessionGuid => Principal?.FindFirst(JwtClaimNames.SessionId)?.Value;

    public string? SystemCode => Principal?.FindFirst(JwtClaimNames.SystemCode)?.Value;

    public string? ClientIp
    {
        get
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ip)) return null;
            return ip.Length > 45 ? ip[..45] : ip;
        }
    }
}
