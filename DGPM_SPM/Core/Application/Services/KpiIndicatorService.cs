using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>
/// 經銷商KPI管理 / KPI指標設定（kpi.KPI_INDICATOR，provisional draft）。
/// 提供指標的分頁查詢、建立、編輯與啟停用。
/// </summary>
[ScopedRegistration]
public class KpiIndicatorService : IKpiIndicatorService
{
    private const string ActiveStatus = "A";
    private const string InactiveStatus = "I";
    private static readonly string[] ValidDataTypes = ["N", "P", "A"];

    /// <summary>KPI_VALUE 為 DECIMAL(18,6)，小數位數上限 6（暫定假設，待 SDS 確認）。</summary>
    private const byte MaxDecimalPlaces = 6;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IKpiIndicatorMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public KpiIndicatorService(
        IUnitOfWork unitOfWork,
        IKpiIndicatorMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<KpiIndicatorDto>>> GetPagedAsync(
        KpiIndicatorFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<KpiIndicatorDto>>();

        var result = await _unitOfWork.KpiIndicators.GetPagedAsync(filter, ct);
        return ApiResponse<PagedResult<KpiIndicatorDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<KpiIndicatorDto>> CreateAsync(
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);
        if (!IsValidRequest(request))
            return Invalid<KpiIndicatorDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiIndicatorDto>();

        if (await _unitOfWork.KpiIndicators.ExistsByCodeAsync(request.IndicatorCode, ct: ct))
            return Duplicate<KpiIndicatorDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var entity = await _unitOfWork.KpiIndicators.AddAsync(new KpiIndicator
            {
                IndicatorCode = request.IndicatorCode,
                IndicatorName = request.IndicatorName,
                Unit = request.Unit,
                DataType = request.DataType,
                DecimalPlaces = request.DecimalPlaces,
                SortOrder = request.SortOrder,
                Status = ActiveStatus,
                Memo = request.Memo,
                CrtUser = operatorId
            }, ct);

            await _unitOfWork.CommitAsync(ct);
            return Success(entity);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ApiResponse<KpiIndicatorDto>> UpdateAsync(
        int indicatorId,
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);
        if (indicatorId <= 0 || !IsValidRequest(request))
            return Invalid<KpiIndicatorDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiIndicatorDto>();

        var existing = await _unitOfWork.KpiIndicators.GetByIdAsync(indicatorId, ct);
        if (existing is null)
            return NotFound<KpiIndicatorDto>();

        if (await _unitOfWork.KpiIndicators.ExistsByCodeAsync(request.IndicatorCode, indicatorId, ct))
            return Duplicate<KpiIndicatorDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            existing.IndicatorCode = request.IndicatorCode;
            existing.IndicatorName = request.IndicatorName;
            existing.Unit = request.Unit;
            existing.DataType = request.DataType;
            existing.DecimalPlaces = request.DecimalPlaces;
            existing.SortOrder = request.SortOrder;
            existing.Memo = request.Memo;
            existing.MdfUser = operatorId;

            var updated = await _unitOfWork.KpiIndicators.UpdateAsync(existing, ct);
            if (updated is null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return NotFound<KpiIndicatorDto>();
            }

            await _unitOfWork.CommitAsync(ct);
            return Success(updated);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ApiResponse<KpiIndicatorDto>> SetStatusAsync(
        int indicatorId,
        SetKpiIndicatorStatusRequest request,
        CancellationToken ct = default)
    {
        request.Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant();
        if (indicatorId <= 0 || !IsValidStatus(request.Status))
            return Invalid<KpiIndicatorDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiIndicatorDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var updated = await _unitOfWork.KpiIndicators.SetStatusAsync(indicatorId, request.Status, operatorId, ct);
            if (updated is null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return NotFound<KpiIndicatorDto>();
            }

            await _unitOfWork.CommitAsync(ct);
            return Success(updated);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private ApiResponse<KpiIndicatorDto> Success(KpiIndicator entity)
        => ApiResponse<KpiIndicatorDto>.SuccessResult(
            _mapper.ToDto(entity),
            traceId: _requestContext.TraceId);

    private ApiResponse<T> Invalid<T>()
        => Error<T>(ErrorCodes.InvalidParameter);

    private ApiResponse<T> Unauthorized<T>()
        => Error<T>(ErrorCodes.UnauthorizedAccess);

    private ApiResponse<T> NotFound<T>()
        => Error<T>(ErrorCodes.DataNotFound);

    private ApiResponse<T> Duplicate<T>()
        => Error<T>(ErrorCodes.DuplicateData);

    private ApiResponse<T> Error<T>(ErrorCodes errorCode)
        => ApiResponse<T>.ErrorResult(
            errorCode.GetDescription("code"),
            errorCode.GetDescription("message"),
            _requestContext.TraceId);

    private static void NormalizeRequest(SaveKpiIndicatorRequest request)
    {
        request.IndicatorCode = (request.IndicatorCode ?? string.Empty).Trim().ToUpperInvariant();
        request.IndicatorName = (request.IndicatorName ?? string.Empty).Trim();
        request.Unit = NormalizeOptional(request.Unit);
        request.DataType = (request.DataType ?? string.Empty).Trim().ToUpperInvariant();
        request.Memo = NormalizeOptional(request.Memo);
    }

    private static bool IsValidRequest(SaveKpiIndicatorRequest request)
        => request.IndicatorCode.Length is > 0 and <= 30
           && request.IndicatorName.Length is > 0 and <= 200
           && (request.Unit is null || request.Unit.Length <= 20)
           && IsValidDataType(request.DataType)
           && request.DecimalPlaces <= MaxDecimalPlaces
           && (request.Memo is null || request.Memo.Length <= 500);

    private static void NormalizeFilter(KpiIndicatorFilter filter)
    {
        filter.Keyword = NormalizeOptional(filter.Keyword);
        filter.DataType = NormalizeOptional(filter.DataType, true);
        filter.Status = NormalizeOptional(filter.Status, true);
    }

    private static bool IsValidFilter(KpiIndicatorFilter filter)
        => (filter.DataType is null || IsValidDataType(filter.DataType))
           && (filter.Status is null || IsValidStatus(filter.Status));

    private static string? NormalizeOptional(string? value, bool uppercase = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return uppercase ? normalized.ToUpperInvariant() : normalized;
    }

    private static bool IsValidDataType(string dataType)
        => ValidDataTypes.Contains(dataType);

    private static bool IsValidStatus(string status)
        => status is ActiveStatus or InactiveStatus;
}
