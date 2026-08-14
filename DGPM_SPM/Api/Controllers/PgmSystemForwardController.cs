using System.Text.Json;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

/// <summary>
/// 系統權限維護轉發（PgmUiMode=Off 時由 DGPM 操作；資料仍在 PGM）。
/// 路徑對齊 PGM，JWT sys=DGPM 由 AuthForwardingHandler 帶入。
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
public class PgmSystemForwardController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IPgmAuthClient _pgm;

    public PgmSystemForwardController(IPgmAuthClient pgm)
    {
        _pgm = pgm;
    }

    [HttpGet("api/system/ui-mode")]
    public Task<ActionResult<ApiResponse<PgmUiModeDto>>> GetUiMode(CancellationToken ct)
        => ForwardOk(() => _pgm.GetUiModeAsync(ct));

    [HttpPut("api/system/ui-mode")]
    public Task<ActionResult<ApiResponse<PgmUiModeDto>>> SetUiMode(
        [FromBody] UpdatePgmUiModeRequest request,
        CancellationToken ct)
        => ForwardOk(() => _pgm.SetUiModeAsync(request, ct));

    [HttpGet("api/system/users")]
    public Task<IActionResult> GetUsers(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/system/users" + Request.QueryString, ct);

    [HttpGet("api/system/users/role-options")]
    public Task<IActionResult> GetUserRoleOptions(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/system/users/role-options", ct);

    [HttpGet("api/system/users/{userId}")]
    public Task<IActionResult> GetUser(string userId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/system/users/{Uri.EscapeDataString(userId)}", ct);

    [HttpPost("api/system/users")]
    public Task<IActionResult> CreateUser(CancellationToken ct)
        => ForwardRaw(HttpMethod.Post, "api/system/users", ct, readBody: true);

    [HttpPut("api/system/users/{userId}")]
    public Task<IActionResult> UpdateUser(string userId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/users/{Uri.EscapeDataString(userId)}", ct, readBody: true);

    [HttpPut("api/system/users/{userId}/status")]
    public Task<IActionResult> UpdateUserStatus(string userId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/users/{Uri.EscapeDataString(userId)}/status", ct, readBody: true);

    [HttpPut("api/system/users/{userId}/reset-password")]
    public Task<IActionResult> ResetPassword(string userId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/users/{Uri.EscapeDataString(userId)}/reset-password", ct, readBody: true);

    [HttpGet("api/system/roles")]
    public Task<IActionResult> GetRoles(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/system/roles" + Request.QueryString, ct);

    [HttpGet("api/system/roles/{roleId}")]
    public Task<IActionResult> GetRole(string roleId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/system/roles/{Uri.EscapeDataString(roleId)}", ct);

    [HttpPost("api/system/roles")]
    public Task<IActionResult> CreateRole(CancellationToken ct)
        => ForwardRaw(HttpMethod.Post, "api/system/roles", ct, readBody: true);

    [HttpPut("api/system/roles/{roleId}")]
    public Task<IActionResult> UpdateRole(string roleId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/roles/{Uri.EscapeDataString(roleId)}", ct, readBody: true);

    [HttpPut("api/system/roles/{roleId}/status")]
    public Task<IActionResult> UpdateRoleStatus(string roleId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/roles/{Uri.EscapeDataString(roleId)}/status", ct, readBody: true);

    [HttpGet("api/system/roles/{roleId}/permissions")]
    public Task<IActionResult> GetRolePermissions(string roleId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/system/roles/{Uri.EscapeDataString(roleId)}/permissions", ct);

    [HttpPut("api/system/roles/{roleId}/permissions")]
    public Task<IActionResult> SaveRolePermissions(string roleId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/system/roles/{Uri.EscapeDataString(roleId)}/permissions", ct, readBody: true);

    [HttpGet("api/permission/function-list")]
    public Task<IActionResult> GetFunctions(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/permission/function-list" + Request.QueryString, ct);

    [HttpGet("api/permission/function-list/parent-options")]
    public Task<IActionResult> GetFunctionParentOptions(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/permission/function-list/parent-options", ct);

    [HttpGet("api/permission/function-list/options")]
    public Task<IActionResult> GetFunctionOptions(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/permission/function-list/options" + Request.QueryString, ct);

    [HttpPost("api/permission/function-list")]
    public Task<IActionResult> CreateFunction(CancellationToken ct)
        => ForwardRaw(HttpMethod.Post, "api/permission/function-list", ct, readBody: true);

    [HttpPut("api/permission/function-list/{funId}")]
    public Task<IActionResult> UpdateFunction(string funId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Put, $"api/permission/function-list/{Uri.EscapeDataString(funId)}", ct, readBody: true);

    [HttpGet("api/permission/function-list/{funId}/can-delete")]
    public Task<IActionResult> CanDeleteFunction(string funId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/permission/function-list/{Uri.EscapeDataString(funId)}/can-delete", ct);

    [HttpDelete("api/permission/function-list/{funId}")]
    public Task<IActionResult> DeleteFunction(string funId, CancellationToken ct)
        => ForwardRaw(HttpMethod.Delete, $"api/permission/function-list/{Uri.EscapeDataString(funId)}", ct);

    [HttpGet("api/system/parameters/categories")]
    public Task<IActionResult> GetParamCategories(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/system/parameters/categories", ct);

    [HttpGet("api/system/parameters/{setItem}")]
    public Task<IActionResult> GetParams(string setItem, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/system/parameters/{Uri.EscapeDataString(setItem)}", ct);

    [HttpGet("api/system/parameters/{setItem}/next-sort-order")]
    public Task<IActionResult> GetNextSort(string setItem, CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, $"api/system/parameters/{Uri.EscapeDataString(setItem)}/next-sort-order", ct);

    [HttpPost("api/system/parameters")]
    public Task<IActionResult> CreateParam(CancellationToken ct)
        => ForwardRaw(HttpMethod.Post, "api/system/parameters", ct, readBody: true);

    [HttpPut("api/system/parameters/{setItem}/{setId}")]
    public Task<IActionResult> UpdateParam(string setItem, string setId, CancellationToken ct)
        => ForwardRaw(
            HttpMethod.Put,
            $"api/system/parameters/{Uri.EscapeDataString(setItem)}/{Uri.EscapeDataString(setId)}",
            ct,
            readBody: true);

    [HttpDelete("api/system/parameters/{setItem}/{setId}")]
    public Task<IActionResult> DeleteParam(string setItem, string setId, CancellationToken ct)
        => ForwardRaw(
            HttpMethod.Delete,
            $"api/system/parameters/{Uri.EscapeDataString(setItem)}/{Uri.EscapeDataString(setId)}",
            ct);

    [HttpGet("api/query/login-history")]
    public Task<IActionResult> GetLoginHistory(CancellationToken ct)
        => ForwardRaw(HttpMethod.Get, "api/query/login-history" + Request.QueryString, ct);

    private async Task<ActionResult<ApiResponse<T>>> ForwardOk<T>(Func<Task<ApiResponse<T>>> call)
    {
        var result = await call();
        if (result.Code == "PGM_UNAVAILABLE")
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        if (result.Code is "AUTH_UI_MODE" or "AUTH_FORBIDDEN" or "400")
            return StatusCode(StatusCodes.Status403Forbidden, result);
        return Ok(result);
    }

    private async Task<IActionResult> ForwardRaw(
        HttpMethod method,
        string path,
        CancellationToken ct,
        bool readBody = false)
    {
        object? body = null;
        if (readBody && Request.ContentLength is > 0)
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, JsonOptions, ct);
        }

        var result = await _pgm.ForwardAsync<JsonElement?>(method, path, body, ct);
        if (result.Code == "PGM_UNAVAILABLE")
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        if (result.Code is "AUTH_UI_MODE" or "AUTH_FORBIDDEN")
            return StatusCode(StatusCodes.Status403Forbidden, result);
        return Ok(result);
    }
}
