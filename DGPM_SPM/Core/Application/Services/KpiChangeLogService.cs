using System.Globalization;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>
/// 系統資料查詢 / KPI 異動紀錄查詢（重用 kpi.KPI_CHANGE_LOG，provisional draft）。
/// 查詢專用，無交易；異動寫入由 KPI 匯入 / 覆核既有流程負責。
/// </summary>
[ScopedRegistration]
public class KpiChangeLogService : IKpiChangeLogService
{
    /// <summary>I=匯入, M=修改, R=覆核, U=解鎖（同 kpi.KPI_CHANGE_LOG 之 CHECK 約束）。</summary>
    private static readonly string[] ValidActionTypes = ["I", "M", "R", "U"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IKpiChangeLogMapper _mapper;
    private readonly IRequestContext _requestContext;

    public KpiChangeLogService(
        IUnitOfWork unitOfWork,
        IKpiChangeLogMapper mapper,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<KpiChangeLogDto>>> GetPagedAsync(
        KpiChangeLogFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<KpiChangeLogDto>>();

        var result = await _unitOfWork.KpiChangeLogs.GetPagedAsync(filter, ct);
        return ApiResponse<PagedResult<KpiChangeLogDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    private static void NormalizeFilter(KpiChangeLogFilter filter)
    {
        filter.PeriodYm = NormalizeOptional(filter.PeriodYm);
        filter.Keyword = NormalizeOptional(filter.Keyword);
        filter.ActionType = NormalizeOptional(filter.ActionType, uppercase: true);
        filter.ActionUser = NormalizeOptional(filter.ActionUser);
    }

    private static bool IsValidFilter(KpiChangeLogFilter filter)
        => (filter.PeriodYm is null || IsValidPeriodYm(filter.PeriodYm))
           && (filter.ActionType is null || ValidActionTypes.Contains(filter.ActionType))
           && IsValidDateRange(filter);

    private static bool IsValidDateRange(KpiChangeLogFilter filter)
        => filter.ActionDateFrom is null
           || filter.ActionDateTo is null
           || filter.ActionDateFrom.Value.Date <= filter.ActionDateTo.Value.Date;

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
        => ApiResponse<T>.ErrorResult(
            ErrorCodes.InvalidParameter.GetDescription("code"),
            ErrorCodes.InvalidParameter.GetDescription("message"),
            _requestContext.TraceId);
}
