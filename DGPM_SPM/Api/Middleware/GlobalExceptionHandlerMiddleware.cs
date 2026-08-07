using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Common.Extensions;

namespace DGPM_SPM.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogWarning(
                "API request {RequestId} failed with business error {ErrorCode}",
                context.TraceIdentifier,
                ex.ErrorCode);
            await HandleApiExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(ex, "Unhandled exception for request {RequestId}", context.TraceIdentifier);
            await HandleExceptionAsync(context);
        }
    }

    private static Task HandleApiExceptionAsync(HttpContext context, ApiException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        var response = ApiResponse<object>.ErrorResult(
            exception.ErrorCode,
            exception.Message,
            context.TraceIdentifier);
        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private static Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        var response = ApiResponse<object>.ErrorResult(
            ErrorCodes.InternalError.ToUnderlyingString(),
            ErrorCodes.InternalError.GetDescription("message"),
            context.TraceIdentifier);

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
