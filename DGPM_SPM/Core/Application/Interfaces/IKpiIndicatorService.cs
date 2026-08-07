using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiIndicatorService
{
    Task<ApiResponse<PagedResult<KpiIndicatorDto>>> GetPagedAsync(
        KpiIndicatorFilter filter,
        CancellationToken ct = default);

    Task<ApiResponse<KpiIndicatorDto>> CreateAsync(
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<KpiIndicatorDto>> UpdateAsync(
        int indicatorId,
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<KpiIndicatorDto>> SetStatusAsync(
        int indicatorId,
        SetKpiIndicatorStatusRequest request,
        CancellationToken ct = default);
}
