using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;
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
    public async Task<ActionResult<ApiResponse<PagedResult<UserAccountDto>>>> GetPaged(
        [FromQuery] UserAccountFilter filter,
        CancellationToken ct)
        => Ok(await _userAccountService.GetPagedAsync(filter, ct));

    [HttpGet("role-options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleOptionDto>>>> GetRoleOptions(
        CancellationToken ct)
        => Ok(await _userAccountService.GetRoleOptionsAsync(ct));

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> GetById(
        string userId,
        CancellationToken ct)
        => Ok(await _userAccountService.GetByIdAsync(userId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> Create(
        [FromBody] CreateUserAccountRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.CreateAsync(request, ct));

    [HttpPut("{userId}")]
    public async Task<ActionResult<ApiResponse<UserAccountDto?>>> Update(
        string userId,
        [FromBody] UpdateUserAccountRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.UpdateAsync(userId, request, ct));

    [HttpPut("{userId}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        string userId,
        [FromBody] UserAccountStatusRequest request,
        CancellationToken ct)
        => Ok(await _userAccountService.UpdateStatusAsync(userId, request, ct));
}
