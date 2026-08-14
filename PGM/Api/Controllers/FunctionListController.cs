using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Queries;
using PGM.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PGM.Api.Controllers;

/// <summary>系統權限管理 / 系統功能管理（dbo.SysFun）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/permission/function-list")]
[RequireAuthFunction("AUTH06")]
public class FunctionListController : ControllerBase
{
    private readonly IFunctionService _functionService;

    public FunctionListController(IFunctionService functionService)
    {
        _functionService = functionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FunctionDto>>>> GetPaged(
        [FromQuery] FunctionFilter filter,
        CancellationToken ct)
        => Ok(await _functionService.GetPagedAsync(filter, ct));

    [HttpGet("parent-options")]
    public async Task<ActionResult<ApiResponse<List<FunctionOptionDto>>>> GetParentOptions(
        CancellationToken ct)
        => Ok(await _functionService.GetParentOptionsAsync(ct));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<List<FunctionOptionDto>>>> GetOptions(
        [FromQuery] string? excludeFunId,
        CancellationToken ct)
        => Ok(await _functionService.GetOptionsAsync(excludeFunId, ct));

    [HttpGet("{funId}/can-delete")]
    public async Task<ActionResult<ApiResponse<bool>>> CanDelete(
        string funId,
        CancellationToken ct)
        => Ok(await _functionService.CanDeleteAsync(funId, ct));

    [HttpGet("{funId}")]
    public async Task<ActionResult<ApiResponse<FunctionDto?>>> GetByFunId(
        string funId,
        CancellationToken ct)
        => Ok(await _functionService.GetByFunIdAsync(funId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FunctionDto?>>> Create(
        [FromBody] SaveFunctionRequest request,
        CancellationToken ct)
        => Ok(await _functionService.CreateAsync(request, ct));

    [HttpPut("{funId}")]
    public async Task<ActionResult<ApiResponse<FunctionDto?>>> Update(
        string funId,
        [FromBody] SaveFunctionRequest request,
        CancellationToken ct)
        => Ok(await _functionService.UpdateAsync(funId, request, ct));

    [HttpDelete("{funId}")]
    public async Task<ActionResult<ApiResponse<bool>>> SoftDelete(
        string funId,
        CancellationToken ct)
        => Ok(await _functionService.SoftDeleteAsync(funId, ct));
}
