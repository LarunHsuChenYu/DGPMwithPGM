using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PGM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/system/roles")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> GetPaged(
        [FromQuery] RoleFilter filter,
        CancellationToken ct)
        => Ok(await _roleService.GetPagedAsync(filter, ct));

    [HttpGet("{roleId}")]
    public async Task<ActionResult<ApiResponse<RoleDto?>>> GetById(
        string roleId,
        CancellationToken ct)
        => Ok(await _roleService.GetByIdAsync(roleId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto?>>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken ct)
        => Ok(await _roleService.CreateAsync(request, ct));

    [HttpPut("{roleId}")]
    public async Task<ActionResult<ApiResponse<RoleDto?>>> Update(
        string roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
        => Ok(await _roleService.UpdateAsync(roleId, request, ct));

    [HttpPut("{roleId}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        string roleId,
        [FromBody] RoleStatusRequest request,
        CancellationToken ct)
        => Ok(await _roleService.UpdateStatusAsync(roleId, request, ct));

    [HttpGet("{roleId}/permissions")]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto?>>> GetPermissions(
        string roleId,
        CancellationToken ct)
        => Ok(await _roleService.GetPermissionsAsync(roleId, ct));

    [HttpPut("{roleId}/permissions")]
    public async Task<ActionResult<ApiResponse<bool>>> SavePermissions(
        string roleId,
        [FromBody] SaveRolePermissionsRequest request,
        CancellationToken ct)
        => Ok(await _roleService.SavePermissionsAsync(roleId, request, ct));
}
