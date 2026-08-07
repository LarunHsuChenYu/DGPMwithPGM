using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

/// <summary>系統權限管理 / KPI 資料權限管理（kpi.KPI_USER_DATA_SCOPE，provisional draft）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/kpi/data-permissions")]
public class KpiDataPermissionController : ControllerBase
{
    private readonly IKpiDataPermissionService _kpiDataPermissionService;

    public KpiDataPermissionController(IKpiDataPermissionService kpiDataPermissionService)
    {
        _kpiDataPermissionService = kpiDataPermissionService;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<KpiUserPermissionDto>>> GetByUserId(
        string userId,
        CancellationToken ct)
        => Ok(await _kpiDataPermissionService.GetByUserIdAsync(userId, ct));

    [HttpPut("{userId}")]
    public async Task<ActionResult<ApiResponse<KpiUserPermissionDto>>> Save(
        string userId,
        [FromBody] SaveKpiUserPermissionRequest request,
        CancellationToken ct)
        => Ok(await _kpiDataPermissionService.SaveAsync(userId, request, ct));
}
