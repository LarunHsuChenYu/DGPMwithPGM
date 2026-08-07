using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Application.Queries;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Services;

/// <summary>
/// 系統權限管理 / 系統功能管理（dbo.SysFun）。
/// Fun_ID 建立後不可改；Action_Type=M 時 Parent_ID 強制 null；頂層僅 NULL（'0' 正規化為 null）；刪除為軟刪 Del_YN=Y。
/// </summary>
[ScopedRegistration]
public class FunctionService : IFunctionService
{
    private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "M", "P", "B"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFunctionMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public FunctionService(
        IUnitOfWork unitOfWork,
        IFunctionMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<FunctionDto>>> GetPagedAsync(
        FunctionFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (filter.ActionType is not null && !AllowedActionTypes.Contains(filter.ActionType))
            return Invalid<PagedResult<FunctionDto>>("功能類型僅允許 M（標題）、P（頁面）或 B（按鈕）。");

        var result = await _unitOfWork.Functions.GetPagedAsync(filter, ct);
        return Success(result.Map(_mapper.ToDtos));
    }

    public async Task<ApiResponse<FunctionDto?>> GetByFunIdAsync(string funId, CancellationToken ct = default)
    {
        funId = (funId ?? string.Empty).Trim();
        if (funId.Length == 0)
            return Invalid<FunctionDto?>("功能代碼不正確。");

        var entity = await _unitOfWork.Functions.GetByFunIdAsync(funId, ct);
        if (entity is null || entity.DelYn == "Y")
            return NotFound<FunctionDto?>();

        return Success<FunctionDto?>(_mapper.ToDto(entity));
    }

    public async Task<ApiResponse<List<FunctionOptionDto>>> GetParentOptionsAsync(CancellationToken ct = default)
    {
        var entities = await _unitOfWork.Functions.GetModuleOptionsAsync(ct);
        return Success(_mapper.ToOptionDtos(entities).ToList());
    }

    public async Task<ApiResponse<List<FunctionOptionDto>>> GetOptionsAsync(
        string? excludeFunId,
        CancellationToken ct = default)
    {
        var entities = await _unitOfWork.Functions.GetActiveOptionsAsync(
            NormalizeOptional(excludeFunId),
            ct);
        return Success(_mapper.ToOptionDtos(entities).ToList());
    }

    public async Task<ApiResponse<FunctionDto?>> CreateAsync(
        SaveFunctionRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);

        var validationError = ValidateRequest(request, isCreate: true);
        if (validationError is not null)
            return Invalid<FunctionDto?>(validationError);

        if (await _unitOfWork.Functions.ExistsFunIdAsync(request.FunId, ct))
            return Duplicate<FunctionDto?>();

        var parentError = await ValidateParentAsync(request.ActionType, request.ParentId, currentFunId: null, ct);
        if (parentError is not null)
            return Invalid<FunctionDto?>(parentError);

        var now = DateTime.Now;
        var auditUser = GetAuditUser();
        var entity = new SysFun
        {
            FunId = request.FunId,
            FunName = request.FunName,
            ParentId = request.ActionType == "M" ? null : request.ParentId,
            ActionType = request.ActionType,
            UrlPath = request.UrlPath,
            Icon = null,
            SortOrder = request.SortOrder,
            IsMenu = request.IsMenu,
            IsEnabled = request.IsEnabled,
            FunDesc = request.FunDesc,
            DelYn = "N",
            CrePerson = auditUser,
            CreDate = now,
            ChgPerson = auditUser,
            ChgDate = now
        };

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            await _unitOfWork.Functions.AddAsync(entity, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        var created = await _unitOfWork.Functions.GetByFunIdAsync(entity.FunId, ct);
        return Success(created is null ? null : _mapper.ToDto(created));
    }

    public async Task<ApiResponse<FunctionDto?>> UpdateAsync(
        string funId,
        SaveFunctionRequest request,
        CancellationToken ct = default)
    {
        funId = (funId ?? string.Empty).Trim();
        NormalizeRequest(request);

        if (funId.Length == 0)
            return Invalid<FunctionDto?>("功能代碼不正確。");

        var existing = await _unitOfWork.Functions.GetByFunIdAsync(funId, ct);
        if (existing is null || existing.DelYn == "Y")
            return NotFound<FunctionDto?>();

        if (!request.FunId.Equals(existing.FunId, StringComparison.OrdinalIgnoreCase))
            return Invalid<FunctionDto?>("功能代碼建立後不可修改。");

        // 以路徑 Fun_ID 為準，避免大小寫差異寫入
        request.FunId = existing.FunId;

        var validationError = ValidateRequest(request, isCreate: false);
        if (validationError is not null)
            return Invalid<FunctionDto?>(validationError);

        var parentError = await ValidateParentAsync(request.ActionType, request.ParentId, existing.FunId, ct);
        if (parentError is not null)
            return Invalid<FunctionDto?>(parentError);

        existing.FunName = request.FunName;
        existing.ParentId = request.ActionType == "M" ? null : request.ParentId;
        existing.ActionType = request.ActionType;
        existing.UrlPath = request.UrlPath;
        existing.SortOrder = request.SortOrder;
        existing.IsMenu = request.IsMenu;
        existing.IsEnabled = request.IsEnabled;
        existing.FunDesc = request.FunDesc;
        existing.ChgDate = DateTime.Now;
        existing.ChgPerson = GetAuditUser();

        var affected = await ExecuteWriteAsync(
            token => _unitOfWork.Functions.UpdateAsync(existing, token),
            ct);
        if (affected <= 0)
            return NotFound<FunctionDto?>();

        var updated = await _unitOfWork.Functions.GetByFunIdAsync(funId, ct);
        return Success(updated is null ? null : _mapper.ToDto(updated));
    }

    public async Task<ApiResponse<bool>> CanDeleteAsync(string funId, CancellationToken ct = default)
    {
        funId = (funId ?? string.Empty).Trim();
        if (funId.Length == 0)
            return Invalid<bool>("功能代碼不正確。");

        var existing = await _unitOfWork.Functions.GetByFunIdAsync(funId, ct);
        if (existing is null || existing.DelYn == "Y")
            return NotFound<bool>();

        var blockMessage = await GetDeleteBlockMessageAsync(existing.FunId, ct);
        if (blockMessage is not null)
            return Invalid<bool>(blockMessage);

        return Success(true);
    }

    public async Task<ApiResponse<bool>> SoftDeleteAsync(string funId, CancellationToken ct = default)
    {
        funId = (funId ?? string.Empty).Trim();
        if (funId.Length == 0)
            return Invalid<bool>("功能代碼不正確。");

        var existing = await _unitOfWork.Functions.GetByFunIdAsync(funId, ct);
        if (existing is null || existing.DelYn == "Y")
            return NotFound<bool>();

        var blockMessage = await GetDeleteBlockMessageAsync(existing.FunId, ct);
        if (blockMessage is not null)
            return Invalid<bool>(blockMessage);

        existing.DelYn = "Y";
        existing.ChgDate = DateTime.Now;
        existing.ChgPerson = GetAuditUser();

        var affected = await ExecuteWriteAsync(
            token => _unitOfWork.Functions.SoftDeleteAsync(existing, token),
            ct);
        return Success(affected > 0);
    }

    /// <summary>
    /// 刪除阻擋訊息：子層或角色權限引用（MAP_RIGHT_FUNCTION，對應 SRS SysRoleFun）。
    /// </summary>
    private async Task<string?> GetDeleteBlockMessageAsync(string funId, CancellationToken ct)
    {
        var hasChildren = await _unitOfWork.Functions.HasActiveChildrenAsync(funId, ct);
        var hasRoleRef = await _unitOfWork.Roles.IsFunctionReferencedAsync(funId, ct);
        if (hasChildren || hasRoleRef)
            return "已設定子層功能/已設定角色權限，不能刪除!";

        return null;
    }

    private static void NormalizeRequest(SaveFunctionRequest request)
    {
        request.FunId = (request.FunId ?? string.Empty).Trim();
        request.FunName = (request.FunName ?? string.Empty).Trim();
        request.ActionType = (request.ActionType ?? string.Empty).Trim().ToUpperInvariant();
        request.UrlPath = NormalizeOptional(request.UrlPath);
        // 本專案頂層 Parent_ID 僅 NULL；'0'／空白一律正規化為 null（TableList 曾寫 0 or NULL，已定案不用 0）
        request.ParentId = NormalizeParentId(request.ParentId);
        request.FunDesc = NormalizeOptional(request.FunDesc);
        // 選單否：SRS 預設空值，不自動回落；交由 ValidateRequest 檢核必填 Y/N
        request.IsMenu = (request.IsMenu ?? string.Empty).Trim().ToUpperInvariant();
        request.IsEnabled = NormalizeYn(request.IsEnabled, defaultValue: "N");

        if (request.ActionType == "M")
            request.ParentId = null;
    }

    private static void NormalizeFilter(FunctionFilter filter)
    {
        filter.Keyword = NormalizeOptional(filter.Keyword);
        filter.ParentId = NormalizeParentId(filter.ParentId);
        filter.ActionType = NormalizeOptional(filter.ActionType)?.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>頂層僅允許 NULL；歷史／誤傳的 '0' 視為 NULL。</summary>
    private static string? NormalizeParentId(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized == "0" ? null : normalized;
    }

    private static string NormalizeYn(string? value, string defaultValue)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized is "Y" or "N" ? normalized : defaultValue;
    }

    private static string? ValidateRequest(SaveFunctionRequest request, bool isCreate)
    {
        if (request.FunId.Length is 0 or > 20)
            return "功能代碼為必填，且長度不可超過 20 字元。";

        if (request.FunName.Length is 0 or > 50)
            return "功能名稱為必填，且長度不可超過 50 字元。";

        if (!AllowedActionTypes.Contains(request.ActionType))
            return "功能類型僅允許 M（標題）、P（頁面）或 B（按鈕）。";

        if (request.ActionType != "M" && request.ParentId is null)
            return "非標題類型時，上層選單為必填。";

        if (request.ParentId is { Length: > 20 })
            return "上層選單功能代碼長度不可超過 20 字元。";

        if (request.UrlPath is { Length: > 50 })
            return "前端路由或 URL 長度不可超過 50 字元。";

        if (request.FunDesc is { Length: > 500 })
            return "說明長度不可超過 500 字元。";

        if (request.IsMenu is not ("Y" or "N"))
            return "選單否為必填，僅允許 Y 或 N。";

        if (request.IsEnabled is not ("Y" or "N"))
            return "啟用否僅允許 Y 或 N。";

        if (request.SortOrder is < 0 or > 9999.99m)
            return "階層序號超出允許範圍。";

        _ = isCreate;
        return null;
    }

    private async Task<string?> ValidateParentAsync(
        string actionType,
        string? parentId,
        string? currentFunId,
        CancellationToken ct)
    {
        if (actionType == "M")
            return null;

        if (parentId is null)
            return "非標題類型時，上層選單為必填。";

        if (currentFunId is not null &&
            parentId.Equals(currentFunId, StringComparison.OrdinalIgnoreCase))
            return "上層選單不可為自己。";

        var parent = await _unitOfWork.Functions.GetByFunIdAsync(parentId, ct);
        if (parent is null || parent.DelYn == "Y")
            return "上層選單不存在或已刪除。";

        if (currentFunId is not null &&
            await _unitOfWork.Functions.IsDescendantAsync(currentFunId, parentId, ct))
            return "上層選單不可選擇目前功能的下層節點。";

        return null;
    }

    private async Task<int> ExecuteWriteAsync(
        Func<CancellationToken, Task<int>> action,
        CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var affected = await action(ct);
            await _unitOfWork.CommitAsync(ct);
            return affected;
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

    private ApiResponse<T> NotFound<T>() =>
        ApiResponse<T>.ErrorResult(
            ErrorCodes.DataNotFound.GetDescription("code"),
            ErrorCodes.DataNotFound.GetDescription("message"),
            _requestContext.TraceId);

    private ApiResponse<T> Duplicate<T>() =>
        ApiResponse<T>.ErrorResult(
            ErrorCodes.DuplicateData.GetDescription("code"),
            ErrorCodes.DuplicateData.GetDescription("message"),
            _requestContext.TraceId);
}
