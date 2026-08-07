using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiImportService
{
    /// <summary>建立匯入批次並逐筆驗證、寫入 KPI 數據，回傳批次彙總與逐列結果。</summary>
    Task<ApiResponse<KpiImportResultDto>> ImportAsync(
        CreateKpiImportRequest request,
        CancellationToken ct = default);

    /// <summary>查詢單一匯入批次結果。</summary>
    Task<ApiResponse<KpiImportBatchDto>> GetBatchAsync(
        long batchId,
        CancellationToken ct = default);

    /// <summary>分頁查詢匯入批次（與「KPI 匯入日誌查詢」共用）。</summary>
    Task<ApiResponse<PagedResult<KpiImportBatchDto>>> GetBatchPagedAsync(
        KpiImportBatchFilter filter,
        CancellationToken ct = default);
}
