using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/regions")]
public class RegionController : ControllerBase
{
    private readonly IRegionService _regionService;

    public RegionController(IRegionService regionService)
    {
        _regionService = regionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RegionDto>>>> GetPaged(
        [FromQuery] RegionFilter filter,
        CancellationToken ct)
        => Ok(await _regionService.GetPagedAsync(filter, ct));

    [HttpGet("{regionId:int}")]
    public async Task<ActionResult<ApiResponse<RegionDto?>>> GetById(
        int regionId,
        CancellationToken ct)
        => Ok(await _regionService.GetByIdAsync(regionId, ct));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<List<RegionOptionDto>>>> GetOptions(
        [FromQuery] int? excludeRegionId,
        CancellationToken ct)
        => Ok(await _regionService.GetOptionsAsync(excludeRegionId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegionDto?>>> Create(
        [FromBody] RegionSaveRequest request,
        CancellationToken ct)
        => Ok(await _regionService.CreateAsync(request, ct));

    [HttpPut("{regionId:int}")]
    public async Task<ActionResult<ApiResponse<RegionDto?>>> Update(
        int regionId,
        [FromBody] RegionSaveRequest request,
        CancellationToken ct)
        => Ok(await _regionService.UpdateAsync(regionId, request, ct));

    [HttpPut("{regionId:int}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        int regionId,
        [FromBody] RegionStatusRequest request,
        CancellationToken ct)
        => Ok(await _regionService.UpdateStatusAsync(regionId, request, ct));
}
