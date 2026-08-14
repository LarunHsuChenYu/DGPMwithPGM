using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;
using PGM.Core.Common.Security;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Services;

/// <summary>
/// 使用者帳號管理。沿用 dbo.EMP_USER / MAP_USER_ROLE 相容結構；
/// 欄位型別與長度為 SDS 前暫定值。
/// </summary>
[ScopedRegistration]
public class UserAccountService : IUserAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAccountMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public UserAccountService(
        IUnitOfWork unitOfWork,
        IUserAccountMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<UserAccountDto>>> GetPagedAsync(
        UserAccountFilter filter,
        CancellationToken ct = default)
    {
        var result = await _unitOfWork.Users.GetPagedAsync(filter, ct);
        return Success(result.Map(_mapper.ToDtos));
    }

    public async Task<ApiResponse<UserAccountDto?>> GetByIdAsync(
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Invalid<UserAccountDto?>("使用者帳號不可為空白。");

        var entity = await _unitOfWork.Users.GetForManagementAsync(userId.Trim(), ct);
        return Success(entity is null ? null : _mapper.ToDto(entity));
    }

    public async Task<ApiResponse<IReadOnlyList<RoleOptionDto>>> GetRoleOptionsAsync(
        CancellationToken ct = default)
    {
        var roles = await _unitOfWork.Roles.GetAllActiveAsync(ct);
        return Success(_mapper.ToRoleOptionDtos(roles));
    }

    public async Task<ApiResponse<UserAccountDto?>> CreateAsync(
        CreateUserAccountRequest request,
        CancellationToken ct = default)
    {
        var validationError = ValidateProfile(
            request.UserId,
            request.UserName,
            request.TitName,
            request.Email,
            request.Telephone,
            request.FactoryNo,
            request.DptCode);
        if (validationError is not null)
            return Invalid<UserAccountDto?>(validationError);

        // EMPSet：新增帳號一律預設密碼 0000；首次登入強制改密（見 DefaultPassword／AuthService）。
        var initialPassword = DefaultPassword.Value;

        var userId = request.UserId.Trim();
        if (await _unitOfWork.Users.ExistsAsync(userId, ct))
            return Invalid<UserAccountDto?>("使用者帳號已存在。");

        var roleIds = NormalizeRoleIds(request.RoleIds);
        var roleError = await ValidateRolesAsync(roleIds, ct);
        if (roleError is not null)
            return Invalid<UserAccountDto?>(roleError);

        var now = DateTime.Now;
        var auditUser = GetAuditUser();
        var entity = new User
        {
            UserId = userId,
            UserName = request.UserName.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(initialPassword),
            DelFlg = false,
            CrtDate = now,
            CrtUser = auditUser
        };
        ApplyProfile(entity, request.TitName, request.Email, request.Telephone, request.FactoryNo, request.DptCode);

        await ExecuteWriteAsync(async token =>
        {
            await _unitOfWork.Users.AddAsync(entity, token);
            await _unitOfWork.Users.ReplaceRolesAsync(userId, roleIds, auditUser, now, token);
        }, ct);

        var created = await _unitOfWork.Users.GetForManagementAsync(userId, ct);
        return Success(created is null ? null : _mapper.ToDto(created));
    }

    public async Task<ApiResponse<UserAccountDto?>> UpdateAsync(
        string userId,
        UpdateUserAccountRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Invalid<UserAccountDto?>("使用者帳號不可為空白。");

        var existing = await _unitOfWork.Users.GetForManagementAsync(userId.Trim(), ct);
        if (existing is null)
            return Invalid<UserAccountDto?>("找不到指定的使用者帳號。");

        var validationError = ValidateProfile(
            existing.UserId,
            request.UserName,
            request.TitName,
            request.Email,
            request.Telephone,
            request.FactoryNo,
            request.DptCode);
        if (validationError is not null)
            return Invalid<UserAccountDto?>(validationError);

        var roleIds = NormalizeRoleIds(request.RoleIds);
        var roleError = await ValidateRolesAsync(roleIds, ct);
        if (roleError is not null)
            return Invalid<UserAccountDto?>(roleError);

        var now = DateTime.Now;
        var auditUser = GetAuditUser();
        existing.UserName = request.UserName.Trim();
        existing.MdfDate = now;
        existing.MdfUser = auditUser;
        ApplyProfile(existing, request.TitName, request.Email, request.Telephone, request.FactoryNo, request.DptCode);

        await ExecuteWriteAsync(async token =>
        {
            await _unitOfWork.Users.UpdateAsync(existing, token);
            await _unitOfWork.Users.ReplaceRolesAsync(existing.UserId, roleIds, auditUser, now, token);
        }, ct);

        var updated = await _unitOfWork.Users.GetForManagementAsync(existing.UserId, ct);
        return Success(updated is null ? null : _mapper.ToDto(updated));
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(
        string userId,
        UserAccountStatusRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Invalid<bool>("使用者帳號不可為空白。");

        var existing = await _unitOfWork.Users.GetForManagementAsync(userId.Trim(), ct);
        if (existing is null)
            return Invalid<bool>("找不到指定的使用者帳號。");

        existing.DelFlg = !request.IsActive;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        var affected = 0;
        await ExecuteWriteAsync(
            async token => affected = await _unitOfWork.Users.UpdateStatusAsync(existing, token),
            ct);
        return Success(affected > 0);
    }

    public async Task<ApiResponse<object>> AdminResetPasswordAsync(
        string userId,
        AdminResetPasswordRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Invalid<object>("使用者帳號不可為空白。");

        var targetId = userId.Trim();
        var entity = await _unitOfWork.Users.GetForManagementAsync(targetId, ct);
        if (entity is null || entity.DelFlg == true)
        {
            return ApiResponse<object>.ErrorResult(
                ErrorCodes.DataNotFound.GetDescription("code"),
                "找不到指定使用者。",
                _requestContext.TraceId);
        }

        var plain = string.IsNullOrWhiteSpace(request.NewPassword)
            ? DefaultPassword.Value
            : request.NewPassword.Trim();

        if (plain.Length < 4 || plain.Length > 50)
            return Invalid<object>("新密碼長度須為 4～50 字元。");

        var hash = BCrypt.Net.BCrypt.HashPassword(plain);
        var auditUser = GetAuditUser();
        var now = DateTime.Now;

        await ExecuteWriteAsync(
            async token => await _unitOfWork.Users.UpdatePasswordAsync(
                targetId, hash, auditUser, now, token),
            ct);

        return Success<object>(new { userId = targetId, resetToDefault = DefaultPassword.IsDefaultPlain(plain) });
    }

    private async Task<string?> ValidateRolesAsync(
        IReadOnlyCollection<string> roleIds,
        CancellationToken ct)
    {
        if (roleIds.Count == 0)
            return "請至少指派一個啟用中的角色。";

        var activeRoleIds = (await _unitOfWork.Roles.GetAllActiveAsync(ct))
            .Select(role => role.RoleId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roleIds.All(activeRoleIds.Contains)
            ? null
            : "角色不存在或已停用，請重新選擇。";
    }

    private static List<string> NormalizeRoleIds(IEnumerable<string>? roleIds) =>
        (roleIds ?? [])
            .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
            .Select(roleId => roleId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? ValidateProfile(
        string userId,
        string userName,
        string? titName,
        string? email,
        string? telephone,
        string? factoryNo,
        string? dptCode)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Trim().Length > 50)
            return "使用者帳號為必填，且長度不可超過 50 字元。";
        if (string.IsNullOrWhiteSpace(userName) || userName.Trim().Length > 100)
            return "使用者姓名為必填，且長度不可超過 100 字元。";
        if (titName?.Trim().Length > 100)
            return "職稱長度不可超過 100 字元。";
        if (email?.Trim().Length > 200)
            return "Email 長度不可超過 200 字元。";
        if (telephone?.Trim().Length > 50)
            return "電話長度不可超過 50 字元。";
        if (factoryNo?.Trim().Length > 20)
            return "廠別長度不可超過 20 字元。";
        if (dptCode?.Trim().Length > 20)
            return "部門代碼長度不可超過 20 字元。";
        return null;
    }

    private static void ApplyProfile(
        User entity,
        string? titName,
        string? email,
        string? telephone,
        string? factoryNo,
        string? dptCode)
    {
        entity.TitName = NormalizeOptional(titName);
        entity.Email = NormalizeOptional(email);
        entity.Telephone = NormalizeOptional(telephone);
        entity.FactoryNo = NormalizeOptional(factoryNo);
        entity.DptCode = NormalizeOptional(dptCode);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task ExecuteWriteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            await action(ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private string GetAuditUser() =>
        string.IsNullOrWhiteSpace(_currentUser.UserId) ? "SYSTEM" : _currentUser.UserId;

    private ApiResponse<T> Success<T>(T data) =>
        ApiResponse<T>.SuccessResult(data, traceId: _requestContext.TraceId);

    private ApiResponse<T> Invalid<T>(string message) =>
        ApiResponse<T>.ErrorResult(
            ErrorCodes.InvalidParameter.GetDescription("code"),
            message,
            _requestContext.TraceId);
}
