using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;
using PGM.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PGM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/system/users")]
public class UserAccountController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public UserAccountController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [HttpGet]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserAccountDto>>>> GetPaged(
        [FromQuery] UserAccountFilter filter,
        CancellationToken ct)
        => Ok(await _userAccountService.GetPagedAsync(filter, ct));

    [HttpGet("role-options")]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleOptionDto>>>> GetRoleOptions(
        CancellationToken ct)
        => Ok(await _userAccountService.GetRoleOptionsAsync(ct));

    [HttpGet("{userId}")]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> GetById(
        string userId,
        CancellationToken ct)
        => Ok(await _userAccountService.GetByIdAsync(userId, ct));

    [HttpPost]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> Create(
        [FromBody] CreateUserAccountRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.CreateAsync(request, ct));

    [HttpPut("{userId}")]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> Update(
        string userId,
        [FromBody] UpdateUserAccountRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.UpdateAsync(userId, request, ct));

    [HttpPut("{userId}/status")]
    [RequireAuthFunction("AUTH01")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        string userId,
        [FromBody] UserAccountStatusRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.UpdateStatusAsync(userId, request, ct));

    /// <summary>AUTH09：管理員代他人重設密碼（預設 0000）。</summary>
    [HttpPut("{userId}/reset-password")]
    [RequireAuthFunction("AUTH09")]
    public async Task<ActionResult<ApiResponse<object>>> AdminResetPassword(
        string userId,
        [FromBody] AdminResetPasswordRequest? request,
        CancellationToken ct)
        => Ok(await _userAccountService.AdminResetPasswordAsync(
            userId, request ?? new AdminResetPasswordRequest(), ct));
}
