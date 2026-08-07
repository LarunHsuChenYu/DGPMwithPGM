namespace DGPM_SPM.Api.Middleware;

public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TracingMiddleware> _logger;
    private const string RequestIdHeader = "X-Request-Id";

    public TracingMiddleware(RequestDelegate next, ILogger<TracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault();
        requestId = string.IsNullOrWhiteSpace(requestId)
            ? Guid.NewGuid().ToString()
            : requestId.ToString();

        context.Items["RequestId"] = requestId;
        context.Response.Headers[RequestIdHeader] = requestId;
        context.TraceIdentifier = requestId;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = requestId,
            ["Path"] = context.Request.Path.ToString(),
            ["Method"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIP"] = context.Connection.RemoteIpAddress?.ToString()
        }))
        {
            await _next(context);
        }
    }
}
