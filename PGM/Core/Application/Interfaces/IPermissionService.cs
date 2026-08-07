using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;

namespace PGM.Core.Application.Interfaces;

public interface IPermissionService
{
    Task<ApiResponse<PermissionResponse>> CheckAsync(string functionId, CancellationToken ct = default);
    Task<ApiResponse<List<PermissionResponse>>> CheckBatchAsync(IEnumerable<string> functionIds, CancellationToken ct = default);
}
