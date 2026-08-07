using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IRegionService
{
    Task<ApiResponse<PagedResult<RegionDto>>> GetPagedAsync(RegionFilter filter, CancellationToken ct = default);
    Task<ApiResponse<RegionDto?>> GetByIdAsync(int regionId, CancellationToken ct = default);
    Task<ApiResponse<List<RegionOptionDto>>> GetOptionsAsync(int? excludeRegionId, CancellationToken ct = default);
    Task<ApiResponse<RegionDto?>> CreateAsync(RegionSaveRequest request, CancellationToken ct = default);
    Task<ApiResponse<RegionDto?>> UpdateAsync(int regionId, RegionSaveRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateStatusAsync(int regionId, RegionStatusRequest request, CancellationToken ct = default);
}
