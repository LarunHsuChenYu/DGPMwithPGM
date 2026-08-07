using System.Text.RegularExpressions;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Application.Queries;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Services;

/// <summary>
/// 角色與權限管理（系統權限管理 / 角色與權限管理）。
/// 沿用 dbo.DIM_ROLE / MAP_ROLE_RIGHT / MAP_RIGHT_FUNCTION 相容結構；
/// 功能清單來源為 dbo.SysFun。欄位型別與長度為 SDS 前暫定值。
/// 授權讀取展開角色全部 RIGHT；選單目前直接讀 SysFun（角色過濾待正式表）。
/// 儲存則改寫為單一專屬 RIGHT（RIGHT_ID = ROLE_ID），不影響其他角色。
/// </summary>
[ScopedRegistration]
public partial class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoleMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public RoleService(
        IUnitOfWork unitOfWork,
        IRoleMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<RoleDto>>> GetPagedAsync(
        RoleFilter filter,
        CancellationToken ct = default)
    {
        var result = await _unitOfWork.Roles.GetPagedAsync(filter, ct);
        return Success(result.Map(_mapper.ToDtos));
    }

    public async Task<ApiResponse<RoleDto?>> GetByIdAsync(string roleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return Invalid<RoleDto?>("角色代碼不可為空白。");

        var entity = await _unitOfWork.Roles.GetByIdAsync(roleId.Trim(), ct);
        return Success(entity is null ? null : _mapper.ToDto(entity));
    }

    public async Task<ApiResponse<RoleDto?>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId) || request.RoleId.Trim().Length > 50)
            return Invalid<RoleDto?>("角色代碼為必填，且長度不可超過 50 字元。");

        var roleId = request.RoleId.Trim();
        if (!RoleIdPattern().IsMatch(roleId))
            return Invalid<RoleDto?>("角色代碼僅允許英數字、底線與連字號。");

        var nameError = ValidateNameAndType(request.RoleName, request.RoleType);
        if (nameError is not null)
            return Invalid<RoleDto?>(nameError);

        if (await _unitOfWork.Roles.ExistsAsync(roleId, ct))
            return Invalid<RoleDto?>("角色代碼已存在。");

        var entity = new Role
        {
            RoleId = roleId,
            RoleName = request.RoleName.Trim(),
            RoleType = NormalizeOptional(request.RoleType),
            SystemCode = string.IsNullOrWhiteSpace(request.SystemCode)
                ? "PGM"
                : request.SystemCode.Trim().ToUpperInvariant(),
            DelFlg = false,
            CrtDate = DateTime.Now,
            CrtUser = GetAuditUser()
        };

        await ExecuteWriteAsync(token => _unitOfWork.Roles.AddAsync(entity, token), ct);

        var created = await _unitOfWork.Roles.GetByIdAsync(roleId, ct);
        return Success(created is null ? null : _mapper.ToDto(created));
    }

    public async Task<ApiResponse<RoleDto?>> UpdateAsync(
        string roleId,
        UpdateRoleRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return Invalid<RoleDto?>("角色代碼不可為空白。");

        var existing = await _unitOfWork.Roles.GetByIdAsync(roleId.Trim(), ct);
        if (existing is null)
            return Invalid<RoleDto?>("找不到指定的角色。");

        var nameError = ValidateNameAndType(request.RoleName, request.RoleType);
        if (nameError is not null)
            return Invalid<RoleDto?>(nameError);

        existing.RoleName = request.RoleName.Trim();
        existing.RoleType = NormalizeOptional(request.RoleType);
        existing.SystemCode = string.IsNullOrWhiteSpace(request.SystemCode)
            ? existing.SystemCode
            : request.SystemCode.Trim().ToUpperInvariant();
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        await ExecuteWriteAsync(token => _unitOfWork.Roles.UpdateAsync(existing, token), ct);

        var updated = await _unitOfWork.Roles.GetByIdAsync(existing.RoleId, ct);
        return Success(updated is null ? null : _mapper.ToDto(updated));
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(
        string roleId,
        RoleStatusRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return Invalid<bool>("角色代碼不可為空白。");

        var existing = await _unitOfWork.Roles.GetByIdAsync(roleId.Trim(), ct);
        if (existing is null)
            return Invalid<bool>("找不到指定的角色。");

        existing.DelFlg = !request.IsActive;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        var affected = 0;
        await ExecuteWriteAsync(
            async token => affected = await _unitOfWork.Roles.UpdateStatusAsync(existing, token),
            ct);
        return Success(affected > 0);
    }

    public async Task<ApiResponse<RolePermissionsDto?>> GetPermissionsAsync(
        string roleId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return Invalid<RolePermissionsDto?>("角色代碼不可為空白。");

        var role = await _unitOfWork.Roles.GetByIdAsync(roleId.Trim(), ct);
        if (role is null)
            return Success<RolePermissionsDto?>(null);

        var functions = await _unitOfWork.Menus.GetAllActiveAsync(ct);
        var grantedIds = (await _unitOfWork.Roles.GetGrantedFunctionIdsAsync(role.RoleId, ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var functionDtos = _mapper.ToFunctionDtos(functions);
        foreach (var dto in functionDtos)
            dto.Granted = grantedIds.Contains(dto.FunctionId);

        return Success<RolePermissionsDto?>(new RolePermissionsDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Functions = functionDtos
        });
    }

    public async Task<ApiResponse<bool>> SavePermissionsAsync(
        string roleId,
        SaveRolePermissionsRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return Invalid<bool>("角色代碼不可為空白。");

        var role = await _unitOfWork.Roles.GetByIdAsync(roleId.Trim(), ct);
        if (role is null)
            return Invalid<bool>("找不到指定的角色。");

        var functionIds = (request.FunctionIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeFunctionIds = (await _unitOfWork.Menus.GetAllActiveAsync(ct))
            .Select(f => f.FunId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!functionIds.All(activeFunctionIds.Contains))
            return Invalid<bool>("包含不存在或已停用的功能，請重新整理後再儲存。");

        await ExecuteWriteAsync(
            token => _unitOfWork.Roles.ReplaceFunctionsAsync(
                role.RoleId, functionIds, GetAuditUser(), DateTime.Now, token),
            ct);
        return Success(true);
    }

    private static string? ValidateNameAndType(string roleName, string? roleType)
    {
        if (string.IsNullOrWhiteSpace(roleName) || roleName.Trim().Length > 100)
            return "角色名稱為必填，且長度不可超過 100 字元。";
        if (roleType?.Trim().Length > 20)
            return "角色類型長度不可超過 20 字元。";
        return null;
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

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex RoleIdPattern();
}
