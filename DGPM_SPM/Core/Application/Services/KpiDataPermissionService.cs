using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Common.Extensions;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>
/// 系統權限管理 / KPI 資料權限管理（kpi.KPI_USER_DATA_SCOPE，provisional draft）。
/// 授權主體為使用者（dbo.EMP_USER.USER_ID），範圍維度為區域（R）與經銷商（D）；
/// 儲存採全量覆寫（先刪後插，於同一交易內完成）。
/// </summary>
[ScopedRegistration]
public class KpiDataPermissionService : IKpiDataPermissionService
{
    private const string RegionScopeType = "R";
    private const string DealerScopeType = "D";

    /// <summary>USER_ID 欄位長度為 NVARCHAR(50)（暫定 schema）。</summary>
    private const int MaxUserIdLength = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IKpiUserDataScopeMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public KpiDataPermissionService(
        IUnitOfWork unitOfWork,
        IKpiUserDataScopeMapper mapper,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<KpiUserPermissionDto>> GetByUserIdAsync(
        string userId,
        CancellationToken ct = default)
    {
        userId = NormalizeUserId(userId);
        if (!IsValidUserId(userId))
            return Invalid<KpiUserPermissionDto>();

        var user = await _unitOfWork.Users.GetByUserIdAsync(userId, ct);
        if (user is null)
            return NotFound<KpiUserPermissionDto>();

        var scopes = await _unitOfWork.KpiUserDataScopes.GetByUserIdAsync(user.UserId, ct);
        return Success(BuildDto(user, scopes));
    }

    public async Task<ApiResponse<KpiUserPermissionDto>> SaveAsync(
        string userId,
        SaveKpiUserPermissionRequest request,
        CancellationToken ct = default)
    {
        userId = NormalizeUserId(userId);
        if (!IsValidUserId(userId))
            return Invalid<KpiUserPermissionDto>();

        var regionIds = Normalize(request.RegionIds);
        var dealerIds = Normalize(request.DealerIds);
        if (regionIds.Any(id => id <= 0) || dealerIds.Any(id => id <= 0))
            return Invalid<KpiUserPermissionDto>();

        var operatorId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(operatorId))
            return Unauthorized<KpiUserPermissionDto>();

        var user = await _unitOfWork.Users.GetByUserIdAsync(userId, ct);
        if (user is null)
            return NotFound<KpiUserPermissionDto>();

        // 授權標的必須實際存在，避免寫入時才因 FK 失敗回 500。
        if (regionIds.Count > 0)
        {
            var existingRegionIds = await _unitOfWork.KpiUserDataScopes.GetExistingRegionIdsAsync(regionIds, ct);
            if (existingRegionIds.Count != regionIds.Count)
                return Invalid<KpiUserPermissionDto>();
        }

        if (dealerIds.Count > 0)
        {
            var existingDealerIds = await _unitOfWork.KpiUserDataScopes.GetExistingDealerIdsAsync(dealerIds, ct);
            if (existingDealerIds.Count != dealerIds.Count)
                return Invalid<KpiUserPermissionDto>();
        }

        var scopes = regionIds
            .Select(regionId => new KpiUserDataScope
            {
                UserId = user.UserId,
                ScopeType = RegionScopeType,
                RegionId = regionId,
                CrtUser = operatorId
            })
            .Concat(dealerIds.Select(dealerId => new KpiUserDataScope
            {
                UserId = user.UserId,
                ScopeType = DealerScopeType,
                DealerId = dealerId,
                CrtUser = operatorId
            }))
            .ToList();

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            await _unitOfWork.KpiUserDataScopes.ReplaceByUserIdAsync(user.UserId, scopes, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        var saved = await _unitOfWork.KpiUserDataScopes.GetByUserIdAsync(user.UserId, ct);
        return Success(BuildDto(user, saved));
    }

    private KpiUserPermissionDto BuildDto(User user, IReadOnlyList<KpiUserDataScope> scopes)
        => new()
        {
            UserId = user.UserId,
            UserName = user.UserName,
            RegionScopes = _mapper.ToDtos(scopes.Where(s => s.ScopeType == RegionScopeType)),
            DealerScopes = _mapper.ToDtos(scopes.Where(s => s.ScopeType == DealerScopeType))
        };

    private static string NormalizeUserId(string? userId)
        => (userId ?? string.Empty).Trim();

    private static bool IsValidUserId(string userId)
        => userId.Length is > 0 and <= MaxUserIdLength;

    private static List<int> Normalize(List<int>? ids)
        => ids is null ? [] : ids.Distinct().ToList();

    private ApiResponse<KpiUserPermissionDto> Success(KpiUserPermissionDto dto)
        => ApiResponse<KpiUserPermissionDto>.SuccessResult(dto, traceId: _requestContext.TraceId);

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
