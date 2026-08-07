using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;

namespace PGM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/auth/permissions")]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("{functionId}")]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> Check(string functionId, CancellationToken ct)
        => Ok(await _permissionService.CheckAsync(functionId, ct));

    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<List<PermissionResponse>>>> CheckBatch(
        [FromBody] PermissionBatchRequest request,
        CancellationToken ct)
        => Ok(await _permissionService.CheckBatchAsync(request.FunctionIds, ct));
}
