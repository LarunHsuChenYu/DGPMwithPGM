using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

/// <summary>系統資料查詢 / KPI 異動紀錄查詢（重用 kpi.KPI_CHANGE_LOG，provisional draft）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/query/kpi-changes")]
public class KpiChangeLogController : ControllerBase
{
    private readonly IKpiChangeLogService _kpiChangeLogService;

    public KpiChangeLogController(IKpiChangeLogService kpiChangeLogService)
    {
        _kpiChangeLogService = kpiChangeLogService;
    }

    /// <summary>分頁查詢 KPI 異動紀錄（含經銷商 / 指標名稱）。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiChangeLogDto>>>> GetPaged(
        [FromQuery] KpiChangeLogFilter filter,
        CancellationToken ct)
        => Ok(await _kpiChangeLogService.GetPagedAsync(filter, ct));
}
