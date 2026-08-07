using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

/// <summary>經銷商KPI管理 / KPI數據覆核與解鎖（provisional draft，SDS 定稿後可能調整）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/kpi/review")]
public class KpiReviewController : ControllerBase
{
    private readonly IKpiReviewService _kpiReviewService;

    public KpiReviewController(IKpiReviewService kpiReviewService)
    {
        _kpiReviewService = kpiReviewService;
    }

    /// <summary>分頁查詢 KPI 數據（含覆核狀態）。</summary>
    [HttpGet("data")]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiDataDto>>>> GetPaged(
        [FromQuery] KpiDataFilter filter,
        CancellationToken ct)
        => Ok(await _kpiReviewService.GetPagedAsync(filter, ct));

    /// <summary>覆核確認（D/U → R，鎖定）。</summary>
    [HttpPut("data/{dataId:long}/review")]
    public async Task<ActionResult<ApiResponse<KpiDataDto>>> Review(
        long dataId,
        [FromBody] ReviewKpiDataRequest request,
        CancellationToken ct)
        => Ok(await _kpiReviewService.ReviewAsync(dataId, request, ct));

    /// <summary>解鎖退回（R → U，原因必填）。</summary>
    [HttpPut("data/{dataId:long}/unlock")]
    public async Task<ActionResult<ApiResponse<KpiDataDto>>> Unlock(
        long dataId,
        [FromBody] UnlockKpiDataRequest request,
        CancellationToken ct)
        => Ok(await _kpiReviewService.UnlockAsync(dataId, request, ct));
}
