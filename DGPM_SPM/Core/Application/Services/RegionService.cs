using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Services;

[ScopedRegistration]
public class RegionService : IRegionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRegionMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public RegionService(
        IUnitOfWork unitOfWork,
        IRegionMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<RegionDto>>> GetPagedAsync(
        RegionFilter filter,
        CancellationToken ct = default)
    {
        var result = await _unitOfWork.Regions.GetPagedAsync(filter, ct);
        return Success(result.Map(_mapper.ToDtos));
    }

    public async Task<ApiResponse<RegionDto?>> GetByIdAsync(int regionId, CancellationToken ct = default)
    {
        var entity = await _unitOfWork.Regions.GetByIdAsync(regionId, ct);
        return Success(entity is null ? null : _mapper.ToDto(entity));
    }

    public async Task<ApiResponse<List<RegionOptionDto>>> GetOptionsAsync(
        int? excludeRegionId,
        CancellationToken ct = default)
    {
        var entities = await _unitOfWork.Regions.GetActiveOptionsAsync(excludeRegionId, ct);
        return Success(_mapper.ToOptionDtos(entities).ToList());
    }

    public async Task<ApiResponse<RegionDto?>> CreateAsync(
        RegionSaveRequest request,
        CancellationToken ct = default)
    {
        var validationError = await ValidateAsync(request, null, ct);
        if (validationError is not null)
            return Invalid<RegionDto?>(validationError);

        var now = DateTime.Now;
        var entity = new Region
        {
            RegionCode = request.RegionCode.Trim().ToUpperInvariant(),
            RegionName = request.RegionName.Trim(),
            ParentRegionId = request.ParentRegionId,
            SortOrder = request.SortOrder,
            Status = "A",
            CrtDate = now,
            CrtUser = GetAuditUser()
        };

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            entity.RegionId = await _unitOfWork.Regions.AddAsync(entity, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        var created = await _unitOfWork.Regions.GetByIdAsync(entity.RegionId, ct);
        return Success(created is null ? null : _mapper.ToDto(created));
    }

    public async Task<ApiResponse<RegionDto?>> UpdateAsync(
        int regionId,
        RegionSaveRequest request,
        CancellationToken ct = default)
    {
        var existing = await _unitOfWork.Regions.GetByIdAsync(regionId, ct);
        if (existing is null)
            return Invalid<RegionDto?>("找不到指定的區域。");

        var validationError = await ValidateAsync(request, regionId, ct);
        if (validationError is not null)
            return Invalid<RegionDto?>(validationError);

        existing.RegionCode = request.RegionCode.Trim().ToUpperInvariant();
        existing.RegionName = request.RegionName.Trim();
        existing.ParentRegionId = request.ParentRegionId;
        existing.SortOrder = request.SortOrder;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        await ExecuteWriteAsync(token => _unitOfWork.Regions.UpdateAsync(existing, token), ct);

        var updated = await _unitOfWork.Regions.GetByIdAsync(regionId, ct);
        return Success(updated is null ? null : _mapper.ToDto(updated));
    }

    public async Task<ApiResponse<bool>> UpdateStatusAsync(
        int regionId,
        RegionStatusRequest request,
        CancellationToken ct = default)
    {
        if (request.Status is not ("A" or "I"))
            return Invalid<bool>("狀態僅允許 A（啟用）或 I（停用）。");

        var existing = await _unitOfWork.Regions.GetByIdAsync(regionId, ct);
        if (existing is null)
            return Invalid<bool>("找不到指定的區域。");

        if (request.Status == "A" && existing.ParentRegionId.HasValue)
        {
            var parent = await _unitOfWork.Regions.GetByIdAsync(existing.ParentRegionId.Value, ct);
            if (parent is null || parent.Status != "A")
                return Invalid<bool>("上層區域未啟用，無法啟用此區域。");
        }

        if (request.Status == "I")
        {
            if (await _unitOfWork.Regions.HasActiveChildrenAsync(regionId, ct))
                return Invalid<bool>("此區域仍有啟用中的子區域，無法停用。");

            if (await _unitOfWork.Regions.HasActiveDealersAsync(regionId, ct))
                return Invalid<bool>("此區域仍有啟用中的經銷商，無法停用。");
        }

        existing.Status = request.Status;
        existing.MdfDate = DateTime.Now;
        existing.MdfUser = GetAuditUser();

        var affected = await ExecuteWriteAsync(
            token => _unitOfWork.Regions.UpdateStatusAsync(existing, token),
            ct);
        return Success(affected > 0);
    }

    private async Task<string?> ValidateAsync(
        RegionSaveRequest request,
        int? regionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RegionCode) || request.RegionCode.Trim().Length > 20)
            return "區域代碼為必填，且長度不可超過 20 字元。";

        if (string.IsNullOrWhiteSpace(request.RegionName) || request.RegionName.Trim().Length > 100)
            return "區域名稱為必填，且長度不可超過 100 字元。";

        if (request.SortOrder < 0)
            return "排序不可小於 0。";

        var normalizedCode = request.RegionCode.Trim().ToUpperInvariant();
        if (await _unitOfWork.Regions.ExistsCodeAsync(normalizedCode, regionId, ct))
            return "區域代碼已存在。";

        if (!request.ParentRegionId.HasValue)
            return null;

        if (request.ParentRegionId == regionId)
            return "上層區域不可為自己。";

        var parent = await _unitOfWork.Regions.GetByIdAsync(request.ParentRegionId.Value, ct);
        if (parent is null || parent.Status != "A")
            return "上層區域不存在或未啟用。";

        if (regionId.HasValue &&
            await _unitOfWork.Regions.IsDescendantAsync(regionId.Value, request.ParentRegionId.Value, ct))
            return "上層區域不可選擇目前區域的下層節點。";

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
}
