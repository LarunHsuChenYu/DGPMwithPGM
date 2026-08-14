using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Common.Attributes;

namespace PGM.Core.Application.Services;

/// <summary>
/// 以目前 JWT 角色之選單（ROLE → MAP_ROLE_FUNCTION → SET_FUNCTION）比對 Fun_ID。
/// </summary>
[ScopedRegistration]
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public PermissionService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _requestContext = requestContext;
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
        var roleId = ExtractPlainRoleId(_currentUser.RoleId);
        if (string.IsNullOrWhiteSpace(roleId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return (await _unitOfWork.Roles.GetGrantedFunctionIdsAsync(roleId, ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ExtractPlainRoleId(string? composedOrPlain)
    {
        if (string.IsNullOrWhiteSpace(composedOrPlain))
            return string.Empty;
        return composedOrPlain.Split('$')[0];
    }
}
