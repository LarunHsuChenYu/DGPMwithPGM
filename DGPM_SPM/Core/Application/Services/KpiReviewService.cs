using System.Globalization;
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
/// 經銷商KPI管理 / KPI數據覆核與解鎖（kpi.KPI_DATA.REVIEW_STATUS + kpi.KPI_CHANGE_LOG，provisional draft）。
///
/// 暫定狀態流程（SDS 定稿後需覆核）：
///   D(草稿) ──覆核確認──► R(覆核完成，鎖定)
///   U(已解鎖) ──覆核確認──► R(覆核完成，鎖定)
///   R(覆核完成) ──解鎖退回──► U(已解鎖待修正)
/// 覆核備註選填、解鎖原因必填，皆寫入 kpi.KPI_CHANGE_LOG（R=覆核, U=解鎖）留痕。
/// </summary>
[ScopedRegistration]
public class KpiReviewService : IKpiReviewService
{
    private const string ReviewStatusDraft = "D";
    private const string ReviewStatusLocked = "R";
    private const string ReviewStatusUnlocked = "U";
    private const string ReviewActionType = "R";
    private const string UnlockActionType = "U";

    private static readonly string[] ValidReviewStatuses =
        [ReviewStatusDraft, ReviewStatusLocked, ReviewStatusUnlocked];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IKpiReviewMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public KpiReviewService(
        IUnitOfWork unitOfWork,
        IKpiReviewMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<KpiDataDto>>> GetPagedAsync(
        KpiDataFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<KpiDataDto>>();

        var result = await _unitOfWork.KpiDatas.GetPagedAsync(filter, ct);
        return ApiResponse<PagedResult<KpiDataDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<KpiDataDto>> ReviewAsync(
        long dataId,
        ReviewKpiDataRequest request,
        CancellationToken ct = default)
    {
        request.Memo = NormalizeOptional(request.Memo);
        if (dataId <= 0 || (request.Memo is not null && request.Memo.Length > 500))
            return Invalid<KpiDataDto>();

        return await TransitionAsync(
            dataId,
            allowedFromStatuses: [ReviewStatusDraft, ReviewStatusUnlocked],
            toStatus: ReviewStatusLocked,
            actionType: ReviewActionType,
            reason: request.Memo,
            ct);
    }

    public async Task<ApiResponse<KpiDataDto>> UnlockAsync(
        long dataId,
        UnlockKpiDataRequest request,
        CancellationToken ct = default)
    {
        request.Reason = (request.Reason ?? string.Empty).Trim();
        if (dataId <= 0 || request.Reason.Length is 0 or > 500)
            return Invalid<KpiDataDto>();

        return await TransitionAsync(
            dataId,
            allowedFromStatuses: [ReviewStatusLocked],
            toStatus: ReviewStatusUnlocked,
            actionType: UnlockActionType,
            reason: request.Reason,
            ct);
    }

    /// <summary>共用狀態轉換：檢核目前狀態 → 更新 REVIEW_STATUS → 寫入異動紀錄。</summary>
    private async Task<ApiResponse<KpiDataDto>> TransitionAsync(
        long dataId,
        string[] allowedFromStatuses,
        string toStatus,
        string actionType,
        string? reason,
        CancellationToken ct)
    {
        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiDataDto>();

        var existing = await _unitOfWork.KpiDatas.GetByIdAsync(dataId, ct);
        if (existing is null)
            return NotFound<KpiDataDto>();

        if (!allowedFromStatuses.Contains(existing.ReviewStatus))
            return Invalid<KpiDataDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var affected = await _unitOfWork.KpiDatas.UpdateReviewStatusAsync(
                dataId, toStatus, operatorId, ct);
            if (affected == 0)
            {
                await _unitOfWork.RollbackAsync(ct);
                return NotFound<KpiDataDto>();
            }

            // 值未變動；OLD/NEW 皆記錄當下數值，保留覆核/解鎖時的數值快照（暫定，待 SDS 確認）
            await _unitOfWork.KpiDatas.AddChangeLogAsync(new KpiChangeLog
            {
                DataId = dataId,
                ActionType = actionType,
                OldValue = existing.KpiValue,
                NewValue = existing.KpiValue,
                Reason = reason,
                ActionUser = operatorId
            }, ct);

            var updated = await _unitOfWork.KpiDatas.GetByIdAsync(dataId, ct);
            await _unitOfWork.CommitAsync(ct);

            if (updated is null)
                return NotFound<KpiDataDto>();

            return ApiResponse<KpiDataDto>.SuccessResult(
                _mapper.ToDto(updated),
                traceId: _requestContext.TraceId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private static void NormalizeFilter(KpiDataFilter filter)
    {
        filter.PeriodYm = NormalizeOptional(filter.PeriodYm);
        filter.Keyword = NormalizeOptional(filter.Keyword);
        filter.ReviewStatus = NormalizeOptional(filter.ReviewStatus, uppercase: true);
    }

    private static bool IsValidFilter(KpiDataFilter filter)
        => (filter.PeriodYm is null || IsValidPeriodYm(filter.PeriodYm))
           && (filter.ReviewStatus is null || ValidReviewStatuses.Contains(filter.ReviewStatus));

    private static bool IsValidPeriodYm(string periodYm)
        => periodYm.Length == 6
           && DateTime.TryParseExact(periodYm, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static string? NormalizeOptional(string? value, bool uppercase = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return uppercase ? normalized.ToUpperInvariant() : normalized;
    }

    private ApiResponse<T> Invalid<T>()
        => Error<T>(ErrorCodes.InvalidParameter);

    private ApiResponse<T> Unauthorized<T>()
        => Error<T>(ErrorCodes.UnauthorizedAccess);

    private ApiResponse<T> NotFound<T>()
        => Error<T>(ErrorCodes.DataNotFound);

    private ApiResponse<T> Error<T>(ErrorCodes errorCode)
        => ApiResponse<T>.ErrorResult(
            errorCode.GetDescription("code"),
            errorCode.GetDescription("message"),
            _requestContext.TraceId);
}
