using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Common.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

/// <summary>Auth 一律轉發至 PGM；本系統不再提供 Local Auth 登入／維護。</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IPgmAuthClient _pgmAuthClient;

    public AuthController(IPgmAuthClient pgmAuthClient)
    {
        _pgmAuthClient = pgmAuthClient;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        // 匿名登入：若客戶端誤帶 Authorization，不應影響轉發（AuthForwardingHandler 已剝除）。
        var result = await _pgmAuthClient.LoginAsync(request, ct);

        if (result.Code == "AUTH_ENTRY_DISABLED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (result.Code == "PGM_UNAVAILABLE")
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);

        // 無此系統角色：用 403，避免 Web 一律顯示「登入已過期」。
        if (result.Code == "AUTH_NO_ROLE")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (result.Code != ErrorCodes.Success.GetDescription("code") && result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var result = await _pgmAuthClient.LogoutAsync(ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _pgmAuthClient.RefreshAsync(request, ct);
        if (result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> Me(CancellationToken ct)
    {
        var result = await _pgmAuthClient.GetMeAsync(ct);
        if (result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("menus")]
    public async Task<ActionResult<ApiResponse<List<MenuDto>>>> Menus(CancellationToken ct)
    {
        var result = await _pgmAuthClient.GetMenusAsync(ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-role")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> SwitchRole(
        [FromBody] SwitchRoleRequest request,
        CancellationToken ct)
    {
        var result = await _pgmAuthClient.SwitchRoleAsync(request, ct);
        if (result.Code == "PGM_UNAVAILABLE")
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        if (result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var result = await _pgmAuthClient.ChangePasswordAsync(request, ct);
        if (result.Code == "PGM_UNAVAILABLE")
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);

        return Ok(result);
    }
}
