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

    Task<ApiResponse<PgmUiModeDto>> GetUiModeAsync(CancellationToken ct = default);
    Task<ApiResponse<PgmUiModeDto>> SetUiModeAsync(UpdatePgmUiModeRequest request, CancellationToken ct = default);

    /// <summary>轉發任意 PGM API（系統權限維護；JWT 由 AuthForwardingHandler 帶入）。</summary>
    Task<ApiResponse<T>> ForwardAsync<T>(
        HttpMethod method,
        string relativePath,
        object? body = null,
        CancellationToken ct = default);
}
