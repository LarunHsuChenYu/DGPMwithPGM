using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.UserManagement;

namespace PGM.Core.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> LogoutAsync(CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<UserInfoDto>> GetMeAsync(CancellationToken ct = default);
    Task<ApiResponse<List<MenuDto>>> GetMenusAsync(CancellationToken ct = default);
    /// <summary>取得目前登入使用者可用角色（MAP_USER_ROLE ∩ DIM_ROLE）。</summary>
    Task<ApiResponse<IReadOnlyList<RoleOptionDto>>> GetMyRolesAsync(CancellationToken ct = default);
    /// <summary>切換 ROLE_ID：換發 JWT 並回傳該角色選單；不需重輸帳密。</summary>
    Task<ApiResponse<LoginResponse>> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken ct = default);
    /// <summary>已登入使用者變更密碼；成功後不再視為預設密碼（FORCE_PWD → AUTH）。</summary>
    Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}
