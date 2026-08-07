using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IExchangeRateService
{
    Task<ApiResponse<PagedResult<ExchangeRateDto>>> GetPagedAsync(
        ExchangeRateFilter filter,
        CancellationToken ct = default);

    Task<ApiResponse<ExchangeRateDto>> CreateAsync(
        SaveExchangeRateRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<ExchangeRateDto>> UpdateAsync(
        int rateId,
        SaveExchangeRateRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<ExchangeRateDto>> SetStatusAsync(
        int rateId,
        SetExchangeRateStatusRequest request,
        CancellationToken ct = default);
}
