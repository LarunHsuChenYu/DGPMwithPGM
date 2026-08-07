using Microsoft.Extensions.Caching.Memory;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Models.Parameter;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Services;

/// <summary>
/// 參數讀取（相容快取）＋ ParamSet 維護（SET_PARAM CRUD、SET_PARAMITEM 只讀）。
/// </summary>
[ScopedRegistration]
public class ParameterService : IParameterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IParameterMapper _parameterMapper;
    private readonly IRequestContext _requestContext;
    private readonly ICurrentUser _currentUser;

    public ParameterService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IParameterMapper parameterMapper,
        IRequestContext requestContext,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _parameterMapper = parameterMapper;
        _requestContext = requestContext;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<ParameterItemDto>>> GetParameterListAsync(
        string setItem,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(setItem))
        {
            return ApiResponse<List<ParameterItemDto>>.SuccessResult([], traceId: _requestContext.TraceId);
        }

        var trimmed = setItem.Trim();
        var cacheKey = CacheKey(trimmed);
        if (_cache.TryGetValue(cacheKey, out List<ParameterItemDto>? cached) && cached is not null)
        {
            return ApiResponse<List<ParameterItemDto>>.SuccessResult(cached, traceId: _requestContext.TraceId);
        }

        var entities = await _unitOfWork.Parameters.GetAllByItemAsync(trimmed, ct);
        var list = _parameterMapper.ToItemDtos(entities).ToList();

        _cache.Set(cacheKey, list, TimeSpan.FromHours(6));
        return ApiResponse<List<ParameterItemDto>>.SuccessResult(list, traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<IReadOnlyList<ParameterCategoryDto>>> GetCategoriesAsync(
        CancellationToken ct = default)
    {
        var categories = await _unitOfWork.Parameters.GetActiveCategoriesAsync(ct);
        return Success(_parameterMapper.ToCategoryDtos(categories));
    }

    public async Task<ApiResponse<IReadOnlyList<ParameterDto>>> GetByCategoryAsync(
        string setItem,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(setItem))
            return Invalid<IReadOnlyList<ParameterDto>>("請選擇代碼類別。");

        var trimmed = setItem.Trim();
        var categoryName = await _unitOfWork.Parameters.GetCategoryNameAsync(trimmed, ct);
        if (categoryName is null)
            return Invalid<IReadOnlyList<ParameterDto>>("代碼類別不存在或已停用。");

        var rows = await _unitOfWork.Parameters.GetActiveByItemJoinAsync(trimmed, ct);
        var list = rows
            .Select(row => _parameterMapper.ToDto(row, categoryName))
            .ToList();
        return Success<IReadOnlyList<ParameterDto>>(list);
    }

    public async Task<ApiResponse<int>> GetNextSortOrderAsync(string setItem, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(setItem))
            return Invalid<int>("請選擇代碼類別。");

        var trimmed = setItem.Trim();
        if (!await _unitOfWork.Parameters.IsCategoryActiveAsync(trimmed, ct))
            return Invalid<int>("代碼類別不存在或已停用。");

        var next = await _unitOfWork.Parameters.GetNextSortOrderAsync(trimmed, ct);
        return Success(next);
    }

    public async Task<ApiResponse<ParameterDto?>> CreateAsync(
        CreateParameterRequest request,
        CancellationToken ct = default)
    {
        var setItem = request.SetItem?.Trim() ?? string.Empty;
        var setId = request.SetId?.Trim() ?? string.Empty;
        var setValue = request.SetValue?.Trim() ?? string.Empty;
        var sortOrder = request.SortOrder;

        var validationError = ValidateWrite(setItem, setId, setValue, requireSetId: true);
        if (validationError is not null)
            return Invalid<ParameterDto?>(validationError);

        if (!await _unitOfWork.Parameters.IsCategoryActiveAsync(setItem, ct))
            return Invalid<ParameterDto?>("代碼類別不存在或已停用。");

        var existing = await _unitOfWork.Parameters.GetByKeyAsync(setItem, setId, ct);
        var now = DateTime.Now;
        var auditUser = GetAuditUser();

        if (existing is not null)
        {
            if (!existing.DelFlg)
                return Invalid<ParameterDto?>("此代碼已存在");

            existing.SetValue = setValue;
            existing.SortOrder = sortOrder;
            existing.MdfDate = now;
            existing.MdfUser = auditUser;

            await ExecuteWriteAsync(
                token => _unitOfWork.Parameters.ReviveAsync(existing, token),
                ct);
        }
        else
        {
            var entity = new Parameter
            {
                SetItem = setItem,
                SetId = setId,
                SetValue = setValue,
                SortOrder = sortOrder,
                DelFlg = false,
                CrtDate = now,
                CrtUser = auditUser
            };

            await ExecuteWriteAsync(
                token => _unitOfWork.Parameters.AddAsync(entity, token),
                ct);
        }

        InvalidateCache(setItem);
        return await LoadDtoAsync(setItem, setId, ct);
    }

    public async Task<ApiResponse<ParameterDto?>> UpdateAsync(
        string setItem,
        string setId,
        UpdateParameterRequest request,
        CancellationToken ct = default)
    {
        var trimmedItem = setItem?.Trim() ?? string.Empty;
        var trimmedId = setId?.Trim() ?? string.Empty;
        var setValue = request.SetValue?.Trim() ?? string.Empty;

        var validationError = ValidateWrite(trimmedItem, trimmedId, setValue, requireSetId: true);
        if (validationError is not null)
            return Invalid<ParameterDto?>(validationError);

        var existing = await _unitOfWork.Parameters.GetByKeyAsync(trimmedItem, trimmedId, ct);
        if (existing is null || existing.DelFlg)
            return Invalid<ParameterDto?>("代碼不存在或已刪除。");

        existing.SetValue = setValue;
        existing.SortOrder = request.SortOrder;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        await ExecuteWriteAsync(
            token => _unitOfWork.Parameters.UpdateAsync(existing, token),
            ct);

        InvalidateCache(trimmedItem);
        return await LoadDtoAsync(trimmedItem, trimmedId, ct);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        string setItem,
        string setId,
        CancellationToken ct = default)
    {
        var trimmedItem = setItem?.Trim() ?? string.Empty;
        var trimmedId = setId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedItem) || string.IsNullOrWhiteSpace(trimmedId))
            return Invalid<bool>("代碼類別與代碼不可為空白。");

        var existing = await _unitOfWork.Parameters.GetByKeyAsync(trimmedItem, trimmedId, ct);
        if (existing is null || existing.DelFlg)
            return Invalid<bool>("代碼不存在或已刪除。");

        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        await ExecuteWriteAsync(
            token => _unitOfWork.Parameters.SoftDeleteAsync(existing, token),
            ct);

        InvalidateCache(trimmedItem);
        return Success(true);
    }

    private async Task<ApiResponse<ParameterDto?>> LoadDtoAsync(
        string setItem,
        string setId,
        CancellationToken ct)
    {
        var categoryName = await _unitOfWork.Parameters.GetCategoryNameAsync(setItem, ct) ?? string.Empty;
        var entity = await _unitOfWork.Parameters.GetByKeyAsync(setItem, setId, ct);
        if (entity is null || entity.DelFlg)
            return Success<ParameterDto?>(null);

        return Success<ParameterDto?>(_parameterMapper.ToDto(entity, categoryName));
    }

    private static string? ValidateWrite(string setItem, string setId, string setValue, bool requireSetId)
    {
        if (string.IsNullOrWhiteSpace(setItem) || setItem.Length > 50)
            return "代碼類別為必填，且長度不可超過 50 字元。";
        if (requireSetId && (string.IsNullOrWhiteSpace(setId) || setId.Length > 20))
            return "代碼為必填，且長度不可超過 20 字元。";
        if (string.IsNullOrWhiteSpace(setValue) || setValue.Length > 50)
            return "代碼名稱為必填，且長度不可超過 50 字元。";
        return null;
    }

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

    private void InvalidateCache(string setItem) =>
        _cache.Remove(CacheKey(setItem));

    private static string CacheKey(string setItem) => $"SetParam_{setItem}";

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
