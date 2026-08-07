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
[Route("api/kpi/imports")]
public class KpiImportController : ControllerBase
{
    private readonly IKpiImportService _kpiImportService;

    public KpiImportController(IKpiImportService kpiImportService)
    {
        _kpiImportService = kpiImportService;
    }

    /// <summary>建立 KPI 匯入批次並回傳批次彙總與逐列結果。</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<KpiImportResultDto>>> Import(
        [FromBody] CreateKpiImportRequest request,
        CancellationToken ct)
        => Ok(await _kpiImportService.ImportAsync(request, ct));

    /// <summary>分頁查詢匯入批次（與「KPI 匯入日誌查詢」共用）。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiImportBatchDto>>>> GetPaged(
        [FromQuery] KpiImportBatchFilter filter,
        CancellationToken ct)
        => Ok(await _kpiImportService.GetBatchPagedAsync(filter, ct));

    /// <summary>查詢單一匯入批次結果。</summary>
    [HttpGet("{batchId:long}")]
    public async Task<ActionResult<ApiResponse<KpiImportBatchDto>>> GetById(
        long batchId,
        CancellationToken ct)
        => Ok(await _kpiImportService.GetBatchAsync(batchId, ct));
}
