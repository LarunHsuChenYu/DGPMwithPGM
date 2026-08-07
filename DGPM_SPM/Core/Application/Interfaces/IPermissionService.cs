using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IPermissionService
{
    Task<ApiResponse<PermissionResponse>> CheckAsync(string functionId, CancellationToken ct = default);
    Task<ApiResponse<List<PermissionResponse>>> CheckBatchAsync(IEnumerable<string> functionIds, CancellationToken ct = default);
}
