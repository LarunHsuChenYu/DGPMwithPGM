using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Application.Queries;

namespace PGM.Core.Application.Interfaces;

public interface IRoleService
{
    Task<ApiResponse<PagedResult<RoleDto>>> GetPagedAsync(RoleFilter filter, CancellationToken ct = default);
    Task<ApiResponse<RoleDto?>> GetByIdAsync(string roleId, CancellationToken ct = default);
    Task<ApiResponse<RoleDto?>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<ApiResponse<RoleDto?>> UpdateAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateStatusAsync(string roleId, RoleStatusRequest request, CancellationToken ct = default);
    Task<ApiResponse<RolePermissionsDto?>> GetPermissionsAsync(string roleId, CancellationToken ct = default);
    Task<ApiResponse<bool>> SavePermissionsAsync(
        string roleId,
        SaveRolePermissionsRequest request,
        CancellationToken ct = default);
}
