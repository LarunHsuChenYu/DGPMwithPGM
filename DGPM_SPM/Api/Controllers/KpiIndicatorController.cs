using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/kpi/indicators")]
public class KpiIndicatorController : ControllerBase
{
    private readonly IKpiIndicatorService _kpiIndicatorService;

    public KpiIndicatorController(IKpiIndicatorService kpiIndicatorService)
    {
        _kpiIndicatorService = kpiIndicatorService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiIndicatorDto>>>> GetPaged(
        [FromQuery] KpiIndicatorFilter filter,
        CancellationToken ct)
        => Ok(await _kpiIndicatorService.GetPagedAsync(filter, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<KpiIndicatorDto>>> Create(
        [FromBody] SaveKpiIndicatorRequest request,
        CancellationToken ct)
        => Ok(await _kpiIndicatorService.CreateAsync(request, ct));

    [HttpPut("{indicatorId:int}")]
    public async Task<ActionResult<ApiResponse<KpiIndicatorDto>>> Update(
        int indicatorId,
        [FromBody] SaveKpiIndicatorRequest request,
        CancellationToken ct)
        => Ok(await _kpiIndicatorService.UpdateAsync(indicatorId, request, ct));

    [HttpPut("{indicatorId:int}/status")]
    public async Task<ActionResult<ApiResponse<KpiIndicatorDto>>> SetStatus(
        int indicatorId,
        [FromBody] SetKpiIndicatorStatusRequest request,
        CancellationToken ct)
        => Ok(await _kpiIndicatorService.SetStatusAsync(indicatorId, request, ct));
}
