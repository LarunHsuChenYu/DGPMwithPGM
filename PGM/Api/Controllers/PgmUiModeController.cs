using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;

namespace PGM.Api.Controllers;

/// <summary>系統權限 UI Mode（SET_PARAM Auth／PgmUiMode）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/system/ui-mode")]
public class PgmUiModeController : ControllerBase
{
    private readonly IPgmUiModeService _uiModeService;

    public PgmUiModeController(IPgmUiModeService uiModeService)
    {
        _uiModeService = uiModeService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PgmUiModeDto>>> Get(CancellationToken ct)
        => Ok(await _uiModeService.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<PgmUiModeDto>>> Set(
        [FromBody] UpdatePgmUiModeRequest request,
        CancellationToken ct)
    {
        var result = await _uiModeService.SetAsync(request, ct);
        if (!string.Equals(result.Code, "100", StringComparison.Ordinal)
            && result.Code is "400" or "AUTH_FORBIDDEN")
            return StatusCode(StatusCodes.Status403Forbidden, result);
        if (result.Code is "200")
            return BadRequest(result);
        return Ok(result);
    }
}
