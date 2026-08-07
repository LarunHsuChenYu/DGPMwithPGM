using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;
using PGM.Core.Common.Security;
using PGM.Core.Domain.Entities;
using System.Text;

namespace PGM.Core.Application.Services;

/// <summary>
/// 登入／角色切換／選單（ROLE → MAP_ROLE_FUNCTION → SET_FUNCTION）。
/// </summary>
[ScopedRegistration]
public class AuthService : IAuthService
{
    private enum AuthStatus { Login = 'I', Logout = 'O' }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;
    private readonly IAuthMapper _authMapper;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICurrentUser currentUser,
        IRequestContext requestContext,
        IAuthMapper authMapper)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _authMapper = authMapper;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;
        var systemCode = NormalizeSystemCode(request.SystemCode);

        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                ErrorCodes.InvalidParameter.GetDescription("message"),
                traceId);
        }

        var user = await _unitOfWork.Users.GetByUserIdAsync(request.UserId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.UserId) || user.DelFlg == true)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.AuthInvalid.GetDescription("code"),
                ErrorCodes.AuthInvalid.GetDescription("message"),
                traceId);
        }

        var passwordOk = !string.IsNullOrWhiteSpace(user.Password)
                         && BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

        if (!passwordOk)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.AuthInvalid.GetDescription("code"),
                ErrorCodes.AuthInvalid.GetDescription("message"),
                traceId);
        }

        var roles = (await _unitOfWork.Roles.GetAllByUserIdAsync(user.UserId, ct))
            .Where(r => string.Equals(r.SystemCode, systemCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (roles.Count == 0)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.AuthNoRole.GetDescription("code"),
                ErrorCodes.AuthNoRole.GetDescription("message"),
                traceId);
        }

        var selectedRole = SelectRole(roles, request.RoleId);
        var sessionGuid = Guid.NewGuid().ToString();
        var response = await BuildAuthenticatedResponseAsync(
            user,
            selectedRole,
            sessionGuid,
            systemCode,
            passwordExpired: DefaultPassword.IsDefaultHash(user.Password),
            ct);

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            await _unitOfWork.AuthenticationLogs.AddAsync(new AuthenticationLog
            {
                Guid = sessionGuid,
                UserId = user.UserId,
                IdentityContent = $"Role={response.User.RoleId};Sys={systemCode}",
                Ip = _currentUser.ClientIp ?? string.Empty,
                LoginType = 'G',
                AuthStatus = (char)AuthStatus.Login,
                LoginTime = DateTime.Now,
                LogoutTime = DateTime.Now
            }, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        return ApiResponse<LoginResponse>.SuccessResult(response, traceId: traceId);
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                ErrorCodes.UnauthorizedAccess.GetDescription("message"),
                traceId);
        }

        var newPassword = request.NewPassword?.Trim() ?? string.Empty;
        var confirm = request.ConfirmPassword?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirm))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "請輸入新密碼與確認密碼。",
                traceId);
        }

        if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "新密碼與確認密碼不一致。",
                traceId);
        }

        if (DefaultPassword.IsDefaultPlain(newPassword))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                $"新密碼不可為預設密碼（{DefaultPassword.Value}）。",
                traceId);
        }

        if (newPassword.Length < 8 || Encoding.UTF8.GetByteCount(newPassword) > 72)
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "新密碼須為 8 至 72 bytes。",
                traceId);
        }

        var user = await _unitOfWork.Users.GetByUserIdAsync(_currentUser.UserId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Password))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.AccountNotFound.GetDescription("code"),
                ErrorCodes.AccountNotFound.GetDescription("message"),
                traceId);
        }

        if (BCrypt.Net.BCrypt.Verify(newPassword, user.Password))
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "新密碼不可與舊密碼相同。",
                traceId);
        }

        var now = DateTime.Now;
        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        var auditUser = _currentUser.UserId;

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            await _unitOfWork.Users.UpdatePasswordAsync(
                user.UserId,
                hash,
                auditUser,
                now,
                ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        return ApiResponse<object>.SuccessResult(new { }, traceId: traceId);
    }

    public async Task<ApiResponse<object>> LogoutAsync(CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;

        if (!string.IsNullOrWhiteSpace(_currentUser.SessionGuid) && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            await _unitOfWork.BeginTransactionAsync(ct: ct);
            try
            {
                await _unitOfWork.AuthenticationLogs.UpdateLogoutAsync(new AuthenticationLog
                {
                    Guid = _currentUser.SessionGuid,
                    UserId = _currentUser.UserId,
                    LogoutTime = DateTime.Now,
                    AuthStatus = (char)AuthStatus.Logout
                }, ct);

                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        return ApiResponse<object>.SuccessResult(new { }, traceId: traceId);
    }

    public Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        // 第一版：RefreshToken 不落地，請重新登入。後續可加 refresh store。
        var traceId = _requestContext.TraceId;
        return Task.FromResult(ApiResponse<LoginResponse>.ErrorResult(
            ErrorCodes.UnauthorizedAccess.GetDescription("code"),
            ErrorCodes.UnauthorizedAccess.GetDescription("message"),
            traceId));
    }

    public async Task<ApiResponse<UserInfoDto>> GetMeAsync(CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ApiResponse<UserInfoDto>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                ErrorCodes.UnauthorizedAccess.GetDescription("message"),
                traceId);
        }

        var dbUser = await _unitOfWork.Users.GetByUserIdAsync(_currentUser.UserId, ct);
        if (dbUser is null || dbUser.DelFlg == true)
        {
            return ApiResponse<UserInfoDto>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                ErrorCodes.UnauthorizedAccess.GetDescription("message"),
                traceId);
        }

        var userInfo = new UserInfoDto
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName ?? string.Empty,
            RoleId = _currentUser.RoleId,
            RoleName = _currentUser.RoleName,
            SystemCode = _currentUser.SystemCode,
            DepartmentCode = dbUser.DptCode,
            FactoryNo = dbUser.FactoryNo
        };

        return ApiResponse<UserInfoDto>.SuccessResult(userInfo, traceId: traceId);
    }

    public async Task<ApiResponse<List<MenuDto>>> GetMenusAsync(CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;

        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ApiResponse<List<MenuDto>>.SuccessResult([], traceId: traceId);
        }

        var roleId = ExtractPlainRoleId(_currentUser.RoleId);
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return ApiResponse<List<MenuDto>>.SuccessResult([], traceId: traceId);
        }

        var menus = await LoadMenusByRoleAsync(roleId, _currentUser.SystemCode, ct);
        return ApiResponse<List<MenuDto>>.SuccessResult(menus, traceId: traceId);
    }

    public async Task<ApiResponse<IReadOnlyList<RoleOptionDto>>> GetMyRolesAsync(CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ApiResponse<IReadOnlyList<RoleOptionDto>>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                ErrorCodes.UnauthorizedAccess.GetDescription("message"),
                traceId);
        }

        var systemCode = NormalizeSystemCode(_currentUser.SystemCode);
        var roles = (await _unitOfWork.Roles.GetAllByUserIdAsync(_currentUser.UserId, ct))
            .Where(r => string.Equals(r.SystemCode, systemCode, StringComparison.OrdinalIgnoreCase))
            .Select(r => new RoleOptionDto { RoleId = r.RoleId, RoleName = r.RoleName })
            .ToList();

        return ApiResponse<IReadOnlyList<RoleOptionDto>>.SuccessResult(roles, traceId: traceId);
    }

    public async Task<ApiResponse<LoginResponse>> SwitchRoleAsync(
        SwitchRoleRequest request,
        CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;
        var systemCode = NormalizeSystemCode(_currentUser.SystemCode);

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                ErrorCodes.UnauthorizedAccess.GetDescription("message"),
                traceId);
        }

        var requestedRoleId = ExtractPlainRoleId(request.RoleId);
        if (string.IsNullOrWhiteSpace(requestedRoleId))
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "請選擇角色。",
                traceId);
        }

        var roles = (await _unitOfWork.Roles.GetAllByUserIdAsync(_currentUser.UserId, ct))
            .Where(r => string.Equals(r.SystemCode, systemCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var selectedRole = roles.FirstOrDefault(r =>
            r.RoleId.Equals(requestedRoleId, StringComparison.OrdinalIgnoreCase));

        if (selectedRole is null)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                "不具有該角色，無法切換。",
                traceId);
        }

        var user = await _unitOfWork.Users.GetByUserIdAsync(_currentUser.UserId, ct);
        if (user is null || user.DelFlg == true)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                ErrorCodes.AuthInvalid.GetDescription("code"),
                ErrorCodes.AuthInvalid.GetDescription("message"),
                traceId);
        }

        var sessionGuid = string.IsNullOrWhiteSpace(_currentUser.SessionGuid)
            ? Guid.NewGuid().ToString()
            : _currentUser.SessionGuid;

        var response = await BuildAuthenticatedResponseAsync(
            user,
            selectedRole,
            sessionGuid,
            systemCode,
            passwordExpired: false,
            ct);

        return ApiResponse<LoginResponse>.SuccessResult(response, traceId: traceId);
    }

    private async Task<LoginResponse> BuildAuthenticatedResponseAsync(
        User user,
        Role selectedRole,
        string sessionGuid,
        string systemCode,
        bool passwordExpired,
        CancellationToken ct)
    {
        var composedRoleId = $"{selectedRole.RoleId}${user.UserId}$SELF";
        var menus = await LoadMenusByRoleAsync(selectedRole.RoleId, systemCode, ct);

        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            RoleId = composedRoleId,
            RoleName = selectedRole.RoleName,
            DepartmentCode = user.DptCode,
            FactoryNo = user.FactoryNo,
            SystemCode = systemCode
        };

        var (accessToken, refreshToken, expiresAt) = _tokenService.CreateTokens(
            userInfo,
            menus.Select(m => m.FunctionId),
            sessionGuid);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = userInfo,
            Menus = menus,
            PasswordExpired = passwordExpired
        };
    }

    private async Task<List<MenuDto>> LoadMenusByRoleAsync(
        string roleId,
        string? systemCode,
        CancellationToken ct)
    {
        var menuEntities = (await _unitOfWork.Menus.GetMenuByRoleIdAsync(
                roleId,
                NormalizeSystemCode(systemCode),
                ct))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.FunId)
            .ToList();

        return _authMapper.ToMenuDtos(menuEntities).ToList();
    }

    private static Role SelectRole(IReadOnlyList<Role> roles, string? requestedRoleId)
    {
        if (!string.IsNullOrWhiteSpace(requestedRoleId))
        {
            var roleIdPart = ExtractPlainRoleId(requestedRoleId);
            return roles.FirstOrDefault(r => r.RoleId.Equals(roleIdPart, StringComparison.OrdinalIgnoreCase))
                   ?? PreferAdminRole(roles);
        }

        return PreferAdminRole(roles);
    }

    /// <summary>
    /// 同 systemCode 多角色時，預設偏好 *Admin（例：DGPMAdmin），
    /// 避免 Admin 帳號被偶然選到 Uploader 而看不到「系統權限管理」。
    /// </summary>
    private static Role PreferAdminRole(IReadOnlyList<Role> roles)
        => roles.FirstOrDefault(r =>
               r.RoleId.EndsWith("Admin", StringComparison.OrdinalIgnoreCase))
           ?? roles[0];

    private static string ExtractPlainRoleId(string? composedOrPlain)
    {
        if (string.IsNullOrWhiteSpace(composedOrPlain))
            return string.Empty;
        return composedOrPlain.Split('$')[0];
    }

    private static string NormalizeSystemCode(string? systemCode)
    {
        if (string.IsNullOrWhiteSpace(systemCode))
            return "PGM";
        return systemCode.Trim().ToUpperInvariant();
    }
}
