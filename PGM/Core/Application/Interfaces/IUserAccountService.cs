using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;

namespace PGM.Core.Application.Interfaces;

public interface IUserAccountService
{
    Task<ApiResponse<PagedResult<UserAccountDto>>> GetPagedAsync(
        UserAccountFilter filter,
        CancellationToken ct = default);

    Task<ApiResponse<UserAccountDto?>> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<RoleOptionDto>>> GetRoleOptionsAsync(CancellationToken ct = default);
    Task<ApiResponse<UserAccountDto?>> CreateAsync(CreateUserAccountRequest request, CancellationToken ct = default);
    Task<ApiResponse<UserAccountDto?>> UpdateAsync(
        string userId,
        UpdateUserAccountRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<bool>> UpdateStatusAsync(
        string userId,
        UserAccountStatusRequest request,
        CancellationToken ct = default);

    /// <summary>管理員代他人重設密碼（AUTH09）；預設重設為 0000。</summary>
    Task<ApiResponse<object>> AdminResetPasswordAsync(
        string userId,
        AdminResetPasswordRequest request,
        CancellationToken ct = default);
}
