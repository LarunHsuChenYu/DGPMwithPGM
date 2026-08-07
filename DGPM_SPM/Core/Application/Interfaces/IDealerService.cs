using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IDealerService
{
    Task<ApiResponse<PagedResult<DealerDto>>> GetPagedAsync(DealerFilter filter, CancellationToken ct = default);
    Task<ApiResponse<DealerDto?>> GetByIdAsync(int dealerId, CancellationToken ct = default);
    Task<ApiResponse<DealerDto?>> CreateAsync(DealerSaveRequest request, CancellationToken ct = default);
    Task<ApiResponse<DealerDto?>> UpdateAsync(int dealerId, DealerSaveRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateStatusAsync(int dealerId, DealerStatusRequest request, CancellationToken ct = default);
}
