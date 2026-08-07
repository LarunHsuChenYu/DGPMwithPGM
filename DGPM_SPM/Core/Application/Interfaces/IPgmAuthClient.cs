using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IPgmAuthClient
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> LogoutAsync(CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<UserInfoDto>> GetMeAsync(CancellationToken ct = default);
    Task<ApiResponse<List<MenuDto>>> GetMenusAsync(CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}
