using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiReviewService
{
    Task<ApiResponse<PagedResult<KpiDataDto>>> GetPagedAsync(
        KpiDataFilter filter,
        CancellationToken ct = default);

    /// <summary>覆核確認：D(草稿) / U(已解鎖) → R(覆核完成，鎖定)。</summary>
    Task<ApiResponse<KpiDataDto>> ReviewAsync(
        long dataId,
        ReviewKpiDataRequest request,
        CancellationToken ct = default);

    /// <summary>解鎖退回：R(覆核完成) → U(已解鎖待修正)，原因必填。</summary>
    Task<ApiResponse<KpiDataDto>> UnlockAsync(
        long dataId,
        UnlockKpiDataRequest request,
        CancellationToken ct = default);
}
