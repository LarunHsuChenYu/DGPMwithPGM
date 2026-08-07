using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiChangeLogService
{
    /// <summary>分頁查詢 KPI 異動紀錄（系統資料查詢 / KPI 異動紀錄查詢）。</summary>
    Task<ApiResponse<PagedResult<KpiChangeLogDto>>> GetPagedAsync(
        KpiChangeLogFilter filter,
        CancellationToken ct = default);
}
