using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Queries;
using PGM.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PGM.Api.Controllers;

/// <summary>系統資料查詢 / 使用者登入軌跡查詢（重用 dbo.AUTHENTICATION_LOG，既有 QMS 相容表）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/query/login-history")]
[RequireAuthFunction("AUTH08")]
public class AuthenticationLogController : ControllerBase
{
    private readonly IAuthenticationLogService _authenticationLogService;

    public AuthenticationLogController(IAuthenticationLogService authenticationLogService)
    {
        _authenticationLogService = authenticationLogService;
    }

    /// <summary>分頁查詢登入/登出軌跡（純查詢，不提供刪改）。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuthenticationLogDto>>>> GetPaged(
        [FromQuery] AuthenticationLogFilter filter,
        CancellationToken ct)
        => Ok(await _authenticationLogService.GetPagedAsync(filter, ct));
}
