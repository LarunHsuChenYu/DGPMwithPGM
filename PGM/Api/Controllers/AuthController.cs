using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Common.Extensions;

namespace PGM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result.Code == ErrorCodes.AuthNoRole.GetDescription("code"))
            return StatusCode(StatusCodes.Status403Forbidden, result);
        if (result.Code != ErrorCodes.Success.GetDescription("code") && result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var result = await _authService.LogoutAsync(ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(request, ct);
        if (result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> Me(CancellationToken ct)
    {
        var result = await _authService.GetMeAsync(ct);
        if (result.Data is null)
            return Unauthorized(result);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("menus")]
    public async Task<ActionResult<ApiResponse<List<MenuDto>>>> Menus(CancellationToken ct)
        => Ok(await _authService.GetMenusAsync(ct));

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleOptionDto>>>> Roles(CancellationToken ct)
    {
        var result = await _authService.GetMyRolesAsync(ct);
        if (result.Code == ErrorCodes.UnauthorizedAccess.GetDescription("code"))
            return Unauthorized(result);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("switch-role")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> SwitchRole(
        [FromBody] SwitchRoleRequest request,
        CancellationToken ct)
    {
        var result = await _authService.SwitchRoleAsync(request, ct);
        if (result.Code == ErrorCodes.UnauthorizedAccess.GetDescription("code"))
            return Unauthorized(result);
        if (result.Code != ErrorCodes.Success.GetDescription("code"))
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var result = await _authService.ChangePasswordAsync(request, ct);
        if (result.Code != ErrorCodes.Success.GetDescription("code"))
        {
            if (result.Code == ErrorCodes.UnauthorizedAccess.GetDescription("code"))
                return Unauthorized(result);
            return BadRequest(result);
        }

        return Ok(result);
    }
}
