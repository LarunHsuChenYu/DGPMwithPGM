using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;

namespace PGM.Api.Infrastructure;

/// <summary>
/// 系統權限維護閘道：PgmUiMode + JWT sys + MAP AUTH Fun。
/// GET＝唯讀（Mode=Off 且 sys=PGM 仍可讀）；其餘方法視為寫入。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireAuthFunctionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _functionIds;

    public RequireAuthFunctionAttribute(params string[] functionIds)
    {
        _functionIds = functionIds ?? [];
    }

    /// <summary>強制視為寫入（即使 HTTP GET）。</summary>
    public bool ForceWrite { get; set; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var gate = context.HttpContext.RequestServices.GetRequiredService<IAuthMaintenanceGate>();
        var method = context.HttpContext.Request.Method;
        var isWrite = ForceWrite
                      || !HttpMethods.IsGet(method)
                      && !HttpMethods.IsHead(method)
                      && !HttpMethods.IsOptions(method);

        AuthMaintenanceDecision? decision = null;
        if (_functionIds.Length == 0)
        {
            decision = await gate.EvaluateAsync(string.Empty, isWrite, context.HttpContext.RequestAborted);
        }
        else
        {
            foreach (var funId in _functionIds)
            {
                decision = await gate.EvaluateAsync(funId, isWrite, context.HttpContext.RequestAborted);
                if (decision.Allowed)
                    break;
            }
        }

        if (decision is null || !decision.Allowed)
        {
            context.Result = new ObjectResult(ApiResponse<object>.ErrorResult(
                decision?.Code ?? AuthMaintenanceGateCodes.Forbidden,
                decision?.Message ?? "無權存取系統權限功能。"))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

internal static class AuthMaintenanceGateCodes
{
    public const string Forbidden = "AUTH_FORBIDDEN";
}
