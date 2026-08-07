using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace DGPM_SPM.Api.Infrastructure;

/// <summary>
/// 將目前 request 的 Bearer 轉發至 PGM。
/// 匿名端點（login／refresh）一律不轉發，避免把上一任／已登出的 JWT 帶去污染登入。
/// </summary>
public sealed class AuthForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthForwardingHandler> _logger;

    public AuthForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAnonymousAuth = IsAnonymousAuthPath(path);

        if (isAnonymousAuth)
        {
            // 明確移除：避免 HttpClient／上游 handler 殘留 Authorization。
            request.Headers.Authorization = null;
            if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("Authorization") == true)
            {
                _logger.LogInformation(
                    "Skip forwarding Authorization for anonymous PGM auth {Path}",
                    path);
            }

            return base.SendAsync(request, cancellationToken);
        }

        var auth = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth)
            && AuthenticationHeaderValue.TryParse(auth, out var header))
        {
            request.Headers.Authorization = header;
        }
        else
        {
            request.Headers.Authorization = null;
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsAnonymousAuthPath(string absolutePath)
    {
        // 相容 BaseAddress 相對路徑與完整 AbsolutePath（…/api/auth/login）。
        return absolutePath.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
               || absolutePath.EndsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase)
               || absolutePath.Equals("api/auth/login", StringComparison.OrdinalIgnoreCase)
               || absolutePath.Equals("api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }
}
