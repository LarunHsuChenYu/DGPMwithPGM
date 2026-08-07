using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Queries;

namespace PGM.Core.Application.Interfaces;

public interface IAuthenticationLogService
{
    /// <summary>分頁查詢登入/登出軌跡（系統資料查詢 / 使用者登入軌跡查詢）。</summary>
    Task<ApiResponse<PagedResult<AuthenticationLogDto>>> GetPagedAsync(
        AuthenticationLogFilter filter,
        CancellationToken ct = default);
}
