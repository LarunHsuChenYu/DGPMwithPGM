using PGM.Core.Application.Interfaces;

namespace PGM.Api.Infrastructure;

/// <summary>
/// IRequestContext 的 HTTP 實作。因為依賴 IHttpContextAccessor
/// （這是 HTTP 細節，不屬於 Core），所以放在 Api 層。
/// </summary>
public class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TraceId => GetTraceId();

    public string GetTraceId()
    {
        var traceId = _httpContextAccessor.HttpContext?.Items["RequestId"]?.ToString();
        return traceId ?? "Unknown";
    }
}
