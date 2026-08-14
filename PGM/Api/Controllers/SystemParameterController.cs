using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGM.Api.Infrastructure;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Parameter;

namespace PGM.Api.Controllers;

/// <summary>ParamSet：系統代碼維護（SET_PARAM CRUD；SET_PARAMITEM 只讀）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/system/parameters")]
[RequireAuthFunction("AUTH04")]
public class SystemParameterController : ControllerBase
{
    private readonly IParameterService _parameterService;

    public SystemParameterController(IParameterService parameterService)
    {
        _parameterService = parameterService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParameterCategoryDto>>>> GetCategories(
        CancellationToken ct)
        => Ok(await _parameterService.GetCategoriesAsync(ct));

    [HttpGet("{setItem}/next-sort-order")]
    public async Task<ActionResult<ApiResponse<int>>> GetNextSortOrder(string setItem, CancellationToken ct)
        => Ok(await _parameterService.GetNextSortOrderAsync(setItem, ct));

    [HttpGet("{setItem}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParameterDto>>>> GetByCategory(
        string setItem,
        CancellationToken ct)
        => Ok(await _parameterService.GetByCategoryAsync(setItem, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ParameterDto?>>> Create(
        [FromBody] CreateParameterRequest request,
        CancellationToken ct)
        => Ok(await _parameterService.CreateAsync(request, ct));

    [HttpPut("{setItem}/{setId}")]
    public async Task<ActionResult<ApiResponse<ParameterDto?>>> Update(
        string setItem,
        string setId,
        [FromBody] UpdateParameterRequest request,
        CancellationToken ct)
        => Ok(await _parameterService.UpdateAsync(setItem, setId, request, ct));

    [HttpDelete("{setItem}/{setId}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        string setItem,
        string setId,
        CancellationToken ct)
        => Ok(await _parameterService.DeleteAsync(setItem, setId, ct));
}
