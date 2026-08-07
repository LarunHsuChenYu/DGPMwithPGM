using System.Globalization;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Services;

[ScopedRegistration]
public class ExchangeRateService : IExchangeRateService
{
    private const string ActiveStatus = "A";
    private const string InactiveStatus = "I";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IExchangeRateMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public ExchangeRateService(
        IUnitOfWork unitOfWork,
        IExchangeRateMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<ExchangeRateDto>>> GetPagedAsync(
        ExchangeRateFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<ExchangeRateDto>>();

        var result = await _unitOfWork.ExchangeRates.GetPagedAsync(filter, ct);
        return ApiResponse<PagedResult<ExchangeRateDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<ExchangeRateDto>> CreateAsync(
        SaveExchangeRateRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);
        if (!IsValidRequest(request))
            return Invalid<ExchangeRateDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<ExchangeRateDto>();

        if (await _unitOfWork.ExchangeRates.ExistsAsync(request.CurrencyCode, request.RateYm, ct: ct))
            return Invalid<ExchangeRateDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var entity = await _unitOfWork.ExchangeRates.AddAsync(new ExchangeRate
            {
                CurrencyCode = request.CurrencyCode,
                RateYm = request.RateYm,
                RateValue = request.RateValue,
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

    public async Task<ApiResponse<ExchangeRateDto>> UpdateAsync(
        int rateId,
        SaveExchangeRateRequest request,
        CancellationToken ct = default)
    {
        NormalizeRequest(request);
        if (rateId <= 0 || !IsValidRequest(request))
            return Invalid<ExchangeRateDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<ExchangeRateDto>();

        var existing = await _unitOfWork.ExchangeRates.GetByIdAsync(rateId, ct);
        if (existing is null ||
            await _unitOfWork.ExchangeRates.ExistsAsync(request.CurrencyCode, request.RateYm, rateId, ct))
            return Invalid<ExchangeRateDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            existing.CurrencyCode = request.CurrencyCode;
            existing.RateYm = request.RateYm;
            existing.RateValue = request.RateValue;
            existing.Memo = request.Memo;
            existing.MdfUser = operatorId;

            var updated = await _unitOfWork.ExchangeRates.UpdateAsync(existing, ct);
            if (updated is null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return Invalid<ExchangeRateDto>();
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

    public async Task<ApiResponse<ExchangeRateDto>> SetStatusAsync(
        int rateId,
        SetExchangeRateStatusRequest request,
        CancellationToken ct = default)
    {
        request.Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant();
        if (rateId <= 0 || !IsValidStatus(request.Status))
            return Invalid<ExchangeRateDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<ExchangeRateDto>();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            var updated = await _unitOfWork.ExchangeRates.SetStatusAsync(rateId, request.Status, operatorId, ct);
            if (updated is null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return Invalid<ExchangeRateDto>();
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

    private ApiResponse<ExchangeRateDto> Success(ExchangeRate entity)
        => ApiResponse<ExchangeRateDto>.SuccessResult(
            _mapper.ToDto(entity),
            traceId: _requestContext.TraceId);

    private ApiResponse<T> Invalid<T>()
        => ApiResponse<T>.ErrorResult(
            ErrorCodes.InvalidParameter.GetDescription("code"),
            ErrorCodes.InvalidParameter.GetDescription("message"),
            _requestContext.TraceId);

    private ApiResponse<T> Unauthorized<T>()
        => ApiResponse<T>.ErrorResult(
            ErrorCodes.UnauthorizedAccess.GetDescription("code"),
            ErrorCodes.UnauthorizedAccess.GetDescription("message"),
            _requestContext.TraceId);

    private static void NormalizeRequest(SaveExchangeRateRequest request)
    {
        request.CurrencyCode = (request.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        request.RateYm = (request.RateYm ?? string.Empty).Trim();
        request.Memo = string.IsNullOrWhiteSpace(request.Memo) ? null : request.Memo.Trim();
    }

    private static bool IsValidRequest(SaveExchangeRateRequest request)
        => request.CurrencyCode.Length == 3
           && request.CurrencyCode.All(char.IsAsciiLetterUpper)
           && IsValidYm(request.RateYm)
           && request.RateValue > 0
           && request.Memo?.Length <= 500;

    private static void NormalizeFilter(ExchangeRateFilter filter)
    {
        filter.CurrencyCode = NormalizeOptional(filter.CurrencyCode, true);
        filter.RateYmFrom = NormalizeOptional(filter.RateYmFrom);
        filter.RateYmTo = NormalizeOptional(filter.RateYmTo);
        filter.Status = NormalizeOptional(filter.Status, true);
    }

    private static bool IsValidFilter(ExchangeRateFilter filter)
        => (filter.CurrencyCode is null ||
            (filter.CurrencyCode.Length == 3 && filter.CurrencyCode.All(char.IsAsciiLetterUpper)))
           && (filter.RateYmFrom is null || IsValidYm(filter.RateYmFrom))
           && (filter.RateYmTo is null || IsValidYm(filter.RateYmTo))
           && (filter.RateYmFrom is null || filter.RateYmTo is null ||
               string.CompareOrdinal(filter.RateYmFrom, filter.RateYmTo) <= 0)
           && (filter.Status is null || IsValidStatus(filter.Status));

    private static string? NormalizeOptional(string? value, bool uppercase = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return uppercase ? normalized.ToUpperInvariant() : normalized;
    }

    private static bool IsValidStatus(string status)
        => status is ActiveStatus or InactiveStatus;

    private static bool IsValidYm(string value)
        => DateTime.TryParseExact(
            value,
            "yyyyMM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
}
