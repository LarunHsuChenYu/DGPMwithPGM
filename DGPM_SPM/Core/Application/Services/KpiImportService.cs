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
/// 經銷商KPI管理 / KPI數據匯入（kpi.KPI_IMPORT_BATCH + kpi.KPI_DATA，provisional draft）。
/// 最小可用版本：接收前端解析後的結構化明細（非真實檔案上傳），逐筆驗證
/// dealer / indicator / period / value 後 upsert KPI 數據，並留下匯入批次與異動紀錄。
///
/// 暫定業務規則（SDS 定稿後需覆核）：
///   - 部分成功策略：驗證失敗的明細不寫入，其餘明細照常寫入；
///     批次狀態 S=全數成功、F=任一明細失敗（含部分成功）。
///   - 同一（經銷商×指標×年月）已存在時視為覆蓋更新；REVIEW_STATUS='R'（已覆核鎖定）不可覆蓋。
///   - 更新後 REVIEW_STATUS 一律回到 'D'（草稿），待覆核流程重新覆核。
///   - 單一批次明細上限 1000 筆。
/// </summary>
[ScopedRegistration]
public class KpiImportService : IKpiImportService
{
    private const string StatusProcessing = "P";
    private const string StatusSuccess = "S";
    private const string StatusFailed = "F";
    private const string ReviewStatusDraft = "D";
    private const string ReviewStatusLocked = "R";
    private const string ImportActionType = "I";

    /// <summary>單一批次明細上限（暫定，待 SDS 確認）。</summary>
    private const int MaxRows = 1000;

    /// <summary>批次 ERROR_MESSAGE 摘要最多列出的失敗明細數。</summary>
    private const int MaxErrorSummaryRows = 20;

    /// <summary>KPI_VALUE 為 DECIMAL(18,6)：整數位最多 12 位、小數位最多 6 位（暫定假設）。</summary>
    private const decimal MaxAbsValue = 999_999_999_999.999999m;
    private const int MaxDecimalPlaces = 6;

    private static readonly string[] ValidImportStatuses = [StatusProcessing, StatusSuccess, StatusFailed];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IKpiImportMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public KpiImportService(
        IUnitOfWork unitOfWork,
        IKpiImportMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<KpiImportResultDto>> ImportAsync(
        CreateKpiImportRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);
        if (!IsValidRequest(request))
            return Invalid<KpiImportResultDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiImportResultDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var batchId = await _unitOfWork.KpiImports.AddBatchAsync(new KpiImportBatch
            {
                FileName = request.FileName,
                PeriodYm = request.PeriodYm,
                ImportStatus = StatusProcessing,
                TotalRows = request.Rows.Count,
                ImportUser = operatorId
            }, ct);

            var rowResults = await ProcessRowsAsync(request, batchId, operatorId, ct);

            var failRows = rowResults.Count(r => !r.Success);
            await _unitOfWork.KpiImports.UpdateBatchResultAsync(new KpiImportBatch
            {
                BatchId = batchId,
                ImportStatus = failRows == 0 ? StatusSuccess : StatusFailed,
                TotalRows = rowResults.Count,
                SuccessRows = rowResults.Count - failRows,
                FailRows = failRows,
                ErrorMessage = BuildErrorSummary(rowResults)
            }, ct);

            var batch = await _unitOfWork.KpiImports.GetBatchByIdAsync(batchId, ct);
            await _unitOfWork.CommitAsync(ct);

            return ApiResponse<KpiImportResultDto>.SuccessResult(
                new KpiImportResultDto
                {
                    Batch = batch is null ? new KpiImportBatchDto { BatchId = batchId } : _mapper.ToDto(batch),
                    RowResults = rowResults
                },
                traceId: _requestContext.TraceId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ApiResponse<KpiImportBatchDto>> GetBatchAsync(
        long batchId,
        CancellationToken ct = default)
    {
        if (batchId <= 0)
            return Invalid<KpiImportBatchDto>();

        var batch = await _unitOfWork.KpiImports.GetBatchByIdAsync(batchId, ct);
        if (batch is null)
            return NotFound<KpiImportBatchDto>();

        return ApiResponse<KpiImportBatchDto>.SuccessResult(
            _mapper.ToDto(batch),
            traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<PagedResult<KpiImportBatchDto>>> GetBatchPagedAsync(
        KpiImportBatchFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<KpiImportBatchDto>>();

        var result = await _unitOfWork.KpiImports.GetBatchPagedAsync(filter, ct);
        return ApiResponse<PagedResult<KpiImportBatchDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    /// <summary>逐筆驗證並寫入 KPI 數據；回傳與輸入同序的逐列結果。</summary>
    private async Task<List<KpiImportRowResultDto>> ProcessRowsAsync(
        CreateKpiImportRequest request,
        long batchId,
        string operatorId,
        CancellationToken ct)
    {
        var dealerCodes = DistinctCodes(request.Rows.Select(r => r.DealerCode));
        var indicatorCodes = DistinctCodes(request.Rows.Select(r => r.IndicatorCode));

        var dealers = (await _unitOfWork.Dealers.GetActiveByCodesAsync(dealerCodes, ct))
            .ToDictionary(d => d.DealerCode, StringComparer.OrdinalIgnoreCase);
        var indicators = (await _unitOfWork.KpiIndicators.GetActiveByCodesAsync(indicatorCodes, ct))
            .ToDictionary(i => i.IndicatorCode, StringComparer.OrdinalIgnoreCase);
        var existingData = (await _unitOfWork.KpiImports.GetDataByPeriodAsync(request.PeriodYm, ct))
            .ToDictionary(d => (d.DealerId, d.IndicatorId));

        var rowResults = new List<KpiImportRowResultDto>(request.Rows.Count);
        var processedKeys = new HashSet<(int DealerId, int IndicatorId)>();

        foreach (var (row, index) in request.Rows.Select((row, index) => (row, index)))
        {
            var rowResult = new KpiImportRowResultDto
            {
                RowNo = index + 1,
                DealerCode = row.DealerCode,
                IndicatorCode = row.IndicatorCode,
                Value = row.Value
            };
            rowResults.Add(rowResult);

            var error = ValidateRow(row, dealers, indicators, out var dealer, out var indicator, out var value);
            if (error is null && !processedKeys.Add((dealer!.DealerId, indicator!.IndicatorId)))
                error = "同批次內經銷商×指標重複";

            KpiData? existing = null;
            if (error is null && existingData.TryGetValue((dealer!.DealerId, indicator!.IndicatorId), out existing)
                && existing.ReviewStatus == ReviewStatusLocked)
                error = "該筆數據已覆核鎖定，不可匯入覆蓋";

            if (error is not null)
            {
                rowResult.ErrorMessage = error;
                continue;
            }

            await UpsertDataAsync(existing, dealer!, indicator!, request.PeriodYm, value, batchId, operatorId, ct);
            rowResult.Success = true;
        }

        return rowResults;
    }

    /// <summary>單筆明細驗證；回傳 null 表示通過，否則為繁中錯誤訊息。</summary>
    private static string? ValidateRow(
        KpiImportRowRequest row,
        IReadOnlyDictionary<string, Dealer> dealers,
        IReadOnlyDictionary<string, KpiIndicator> indicators,
        out Dealer? dealer,
        out KpiIndicator? indicator,
        out decimal value)
    {
        dealer = null;
        indicator = null;
        value = 0m;

        if (row.DealerCode.Length is 0 or > 20)
            return "經銷商代碼不可空白且不可超過 20 字";

        if (row.IndicatorCode.Length is 0 or > 30)
            return "指標代碼不可空白且不可超過 30 字";

        if (!dealers.TryGetValue(row.DealerCode, out dealer))
            return "經銷商代碼不存在或已停用";

        if (!indicators.TryGetValue(row.IndicatorCode, out indicator))
            return "指標代碼不存在或已停用";

        if (!decimal.TryParse(
                row.Value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value))
            return "數值格式錯誤";

        if (Math.Abs(value) > MaxAbsValue)
            return "數值超出允許範圍";

        if (Math.Round(value, MaxDecimalPlaces) != value)
            return $"小數位數不可超過 {MaxDecimalPlaces} 位";

        return null;
    }

    /// <summary>寫入單筆 KPI 數據（新增或覆蓋更新），並留下匯入異動紀錄。</summary>
    private async Task UpsertDataAsync(
        KpiData? existing,
        Dealer dealer,
        KpiIndicator indicator,
        string periodYm,
        decimal value,
        long batchId,
        string operatorId,
        CancellationToken ct)
    {
        long dataId;
        decimal? oldValue = null;

        if (existing is null)
        {
            dataId = await _unitOfWork.KpiImports.AddDataAsync(new KpiData
            {
                DealerId = dealer.DealerId,
                IndicatorId = indicator.IndicatorId,
                PeriodYm = periodYm,
                KpiValue = value,
                BatchId = batchId,
                ReviewStatus = ReviewStatusDraft,
                CrtUser = operatorId
            }, ct);
        }
        else
        {
            dataId = existing.DataId;
            oldValue = existing.KpiValue;
            existing.KpiValue = value;
            existing.BatchId = batchId;
            existing.ReviewStatus = ReviewStatusDraft;
            existing.MdfUser = operatorId;
            await _unitOfWork.KpiImports.UpdateDataValueAsync(existing, ct);
        }

        await _unitOfWork.KpiImports.AddChangeLogAsync(new KpiChangeLog
        {
            DataId = dataId,
            ActionType = ImportActionType,
            OldValue = oldValue,
            NewValue = value,
            ActionUser = operatorId
        }, ct);
    }

    private static string? BuildErrorSummary(IReadOnlyList<KpiImportRowResultDto> rowResults)
    {
        var failures = rowResults.Where(r => !r.Success).ToList();
        if (failures.Count == 0)
            return null;

        var lines = failures
            .Take(MaxErrorSummaryRows)
            .Select(r => $"第 {r.RowNo} 列（{r.DealerCode}/{r.IndicatorCode}）：{r.ErrorMessage}");

        var summary = string.Join("；", lines);
        return failures.Count > MaxErrorSummaryRows
            ? $"{summary}；…共 {failures.Count} 筆失敗"
            : summary;
    }

    private static void NormalizeRequest(CreateKpiImportRequest request)
    {
        request.PeriodYm = (request.PeriodYm ?? string.Empty).Trim();
        request.FileName = NormalizeOptional(request.FileName);
        request.Rows ??= [];

        foreach (var row in request.Rows)
        {
            row.DealerCode = (row.DealerCode ?? string.Empty).Trim().ToUpperInvariant();
            row.IndicatorCode = (row.IndicatorCode ?? string.Empty).Trim().ToUpperInvariant();
            row.Value = (row.Value ?? string.Empty).Trim();
        }
    }

    private static bool IsValidRequest(CreateKpiImportRequest request)
        => IsValidPeriodYm(request.PeriodYm)
           && (request.FileName is null || request.FileName.Length <= 260)
           && request.Rows.Count is > 0 and <= MaxRows;

    private static void NormalizeFilter(KpiImportBatchFilter filter)
    {
        filter.PeriodYm = NormalizeOptional(filter.PeriodYm);
        filter.ImportStatus = NormalizeOptional(filter.ImportStatus, uppercase: true);
    }

    private static bool IsValidFilter(KpiImportBatchFilter filter)
        => (filter.PeriodYm is null || IsValidPeriodYm(filter.PeriodYm))
           && (filter.ImportStatus is null || ValidImportStatuses.Contains(filter.ImportStatus));

    private static bool IsValidPeriodYm(string periodYm)
        => periodYm.Length == 6
           && DateTime.TryParseExact(periodYm, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static IReadOnlyCollection<string> DistinctCodes(IEnumerable<string> codes)
        => codes.Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

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
