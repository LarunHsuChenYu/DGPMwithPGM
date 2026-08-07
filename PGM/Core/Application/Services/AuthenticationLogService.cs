using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Queries;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Extensions;

namespace PGM.Core.Application.Services;

/// <summary>
/// 系統資料查詢 / 使用者登入軌跡查詢（重用 dbo.AUTHENTICATION_LOG，既有 QMS 相容表）。
/// 查詢專用，無交易；登入/登出寫入由 AuthService 既有流程負責。
/// </summary>
[ScopedRegistration]
public class AuthenticationLogService : IAuthenticationLogService
{
    /// <summary>I=登入中（尚未登出）, O=已登出（同 AuthService 寫入之 AUTH_STATUS 代碼）。</summary>
    private static readonly string[] ValidAuthStatuses = ["I", "O"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthenticationLogMapper _mapper;
    private readonly IRequestContext _requestContext;

    public AuthenticationLogService(
        IUnitOfWork unitOfWork,
        IAuthenticationLogMapper mapper,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<PagedResult<AuthenticationLogDto>>> GetPagedAsync(
        AuthenticationLogFilter filter,
        CancellationToken ct = default)
    {
        NormalizeFilter(filter);
        if (!IsValidFilter(filter))
            return Invalid<PagedResult<AuthenticationLogDto>>();

        var result = await _unitOfWork.AuthenticationLogs.GetPagedAsync(filter, ct);
        return ApiResponse<PagedResult<AuthenticationLogDto>>.SuccessResult(
            result.Map(_mapper.ToDtos),
            traceId: _requestContext.TraceId);
    }

    private static void NormalizeFilter(AuthenticationLogFilter filter)
    {
        filter.Keyword = NormalizeOptional(filter.Keyword);
        filter.AuthStatus = NormalizeOptional(filter.AuthStatus, uppercase: true);
    }

    private static bool IsValidFilter(AuthenticationLogFilter filter)
        => (filter.AuthStatus is null || ValidAuthStatuses.Contains(filter.AuthStatus))
           && IsValidDateRange(filter);

    private static bool IsValidDateRange(AuthenticationLogFilter filter)
        => filter.LoginDateFrom is null
           || filter.LoginDateTo is null
           || filter.LoginDateFrom.Value.Date <= filter.LoginDateTo.Value.Date;

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
