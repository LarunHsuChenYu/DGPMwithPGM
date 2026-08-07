using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>
/// 經銷商設定管理（基本資料管理 / 經銷商設定管理）。
/// ⚠ 業務欄位與驗證長度依 org.DEALER provisional draft，待 SDS 定稿確認。
/// </summary>
[ScopedRegistration]
public class DealerService : IDealerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDealerMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public DealerService(
        IUnitOfWork unitOfWork,
        IDealerMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<DealerDto>>> GetPagedAsync(
        DealerFilter filter,
        CancellationToken ct = default)
    {
        var result = await _unitOfWork.Dealers.GetPagedAsync(filter, ct);
        return Success(result.Map(_mapper.ToDtos));
    }

    public async Task<ApiResponse<DealerDto?>> GetByIdAsync(int dealerId, CancellationToken ct = default)
    {
        var entity = await _unitOfWork.Dealers.GetByIdAsync(dealerId, ct);
        return Success(entity is null ? null : _mapper.ToDto(entity));
    }

    public async Task<ApiResponse<DealerDto?>> CreateAsync(
        DealerSaveRequest request,
        CancellationToken ct = default)
    {
        var validationError = await ValidateAsync(request, null, ct);
        if (validationError is not null)
            return Invalid<DealerDto?>(validationError);

        var entity = new Dealer
        {
            Status = "A",
            CrtDate = DateTime.Now,
            CrtUser = GetAuditUser()
        };
        ApplyRequest(entity, request);

        await ExecuteWriteAsync(
            async token => entity.DealerId = await _unitOfWork.Dealers.AddAsync(entity, token),
            ct);

        var created = await _unitOfWork.Dealers.GetByIdAsync(entity.DealerId, ct);
        return Success(created is null ? null : _mapper.ToDto(created));
    }

    public async Task<ApiResponse<DealerDto?>> UpdateAsync(
        int dealerId,
        DealerSaveRequest request,
        CancellationToken ct = default)
    {
        var existing = await _unitOfWork.Dealers.GetByIdAsync(dealerId, ct);
        if (existing is null)
            return Invalid<DealerDto?>("找不到指定的經銷商。");

        var validationError = await ValidateAsync(request, dealerId, ct);
        if (validationError is not null)
            return Invalid<DealerDto?>(validationError);

        ApplyRequest(existing, request);
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        await ExecuteWriteAsync(token => _unitOfWork.Dealers.UpdateAsync(existing, token), ct);

        var updated = await _unitOfWork.Dealers.GetByIdAsync(dealerId, ct);
        return Success(updated is null ? null : _mapper.ToDto(updated));
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(
        int dealerId,
        DealerStatusRequest request,
        CancellationToken ct = default)
    {
        if (request.Status is not ("A" or "I"))
            return Invalid<bool>("狀態僅允許 A（啟用）或 I（停用）。");

        var existing = await _unitOfWork.Dealers.GetByIdAsync(dealerId, ct);
        if (existing is null)
            return Invalid<bool>("找不到指定的經銷商。");

        if (request.Status == "A")
        {
            var region = await _unitOfWork.Regions.GetByIdAsync(existing.RegionId, ct);
            if (region is null || region.Status != "A")
                return Invalid<bool>("所屬區域未啟用，無法啟用此經銷商。");
        }

        existing.Status = request.Status;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        var affected = 0;
        await ExecuteWriteAsync(
            async token => affected = await _unitOfWork.Dealers.UpdateStatusAsync(existing, token),
            ct);
        return Success(affected > 0);
    }

    private static void ApplyRequest(Dealer entity, DealerSaveRequest request)
    {
        entity.DealerCode = request.DealerCode.Trim().ToUpperInvariant();
        entity.DealerName = request.DealerName.Trim();
        entity.RegionId = request.RegionId;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
            ? null
            : request.CurrencyCode.Trim().ToUpperInvariant();
        entity.ContactName = NormalizeOptional(request.ContactName);
        entity.ContactEmail = NormalizeOptional(request.ContactEmail);
        entity.ContactTel = NormalizeOptional(request.ContactTel);
        entity.Memo = NormalizeOptional(request.Memo);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string?> ValidateAsync(
        DealerSaveRequest request,
        int? dealerId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DealerCode) || request.DealerCode.Trim().Length > 20)
            return "經銷商代碼為必填，且長度不可超過 20 字元。";

        if (string.IsNullOrWhiteSpace(request.DealerName) || request.DealerName.Trim().Length > 200)
            return "經銷商名稱為必填，且長度不可超過 200 字元。";

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode)
            && (request.CurrencyCode.Trim().Length != 3
                || !request.CurrencyCode.Trim().All(char.IsLetter)))
            return "幣別須為 3 碼英文字母（ISO 4217），例如 TWD、USD。";

        if (request.ContactName?.Trim().Length > 100)
            return "聯絡人姓名長度不可超過 100 字元。";

        if (request.ContactEmail?.Trim().Length > 200)
            return "聯絡人 Email 長度不可超過 200 字元。";

        if (request.ContactTel?.Trim().Length > 50)
            return "聯絡電話長度不可超過 50 字元。";

        if (request.Memo?.Trim().Length > 500)
            return "備註長度不可超過 500 字元。";

        var normalizedCode = request.DealerCode.Trim().ToUpperInvariant();
        if (await _unitOfWork.Dealers.ExistsCodeAsync(normalizedCode, dealerId, ct))
            return "經銷商代碼已存在。";

        var region = await _unitOfWork.Regions.GetByIdAsync(request.RegionId, ct);
        if (region is null || region.Status != "A")
            return "所屬區域不存在或未啟用。";

        return null;
    }

    private async Task ExecuteWriteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct)
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
