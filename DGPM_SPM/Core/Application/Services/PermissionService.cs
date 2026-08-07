using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Common.Attributes;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>以 PGM menus API 為功能授權真相（不再使用本地 SysFun／角色授權鏈）。</summary>
[ScopedRegistration]
public class PermissionService : IPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;
    private readonly IPgmAuthClient _pgmAuthClient;

    public PermissionService(
        ICurrentUser currentUser,
        IRequestContext requestContext,
        IPgmAuthClient pgmAuthClient)
    {
        _currentUser = currentUser;
        _requestContext = requestContext;
        _pgmAuthClient = pgmAuthClient;
    }

    public async Task<ApiResponse<PermissionResponse>> CheckAsync(string functionId, CancellationToken ct = default)
    {
        var allowedFunctionIds = await GetAllowedFunctionIdsAsync(ct);
        var response = new PermissionResponse
        {
            FunctionId = functionId,
            Allowed = !string.IsNullOrWhiteSpace(functionId) && allowedFunctionIds.Contains(functionId)
        };

        return ApiResponse<PermissionResponse>.SuccessResult(response, traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<List<PermissionResponse>>> CheckBatchAsync(
        IEnumerable<string> functionIds,
        CancellationToken ct = default)
    {
        var allowedFunctionIds = await GetAllowedFunctionIdsAsync(ct);
        var result = functionIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => new PermissionResponse
            {
                FunctionId = id,
                Allowed = !string.IsNullOrWhiteSpace(id) && allowedFunctionIds.Contains(id)
            })
            .ToList();

        return ApiResponse<List<PermissionResponse>>.SuccessResult(result, traceId: _requestContext.TraceId);
    }

    private async Task<HashSet<string>> GetAllowedFunctionIdsAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var menus = await _pgmAuthClient.GetMenusAsync(ct);
        return (menus.Data ?? [])
            .Select(m => m.FunctionId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
