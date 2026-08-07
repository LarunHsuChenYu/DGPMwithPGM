using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/dealers")]
public class DealerController : ControllerBase
{
    private readonly IDealerService _dealerService;

    public DealerController(IDealerService dealerService)
    {
        _dealerService = dealerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DealerDto>>>> GetPaged(
        [FromQuery] DealerFilter filter,
        CancellationToken ct)
        => Ok(await _dealerService.GetPagedAsync(filter, ct));

    [HttpGet("{dealerId:int}")]
    public async Task<ActionResult<ApiResponse<DealerDto?>>> GetById(
        int dealerId,
        CancellationToken ct)
        => Ok(await _dealerService.GetByIdAsync(dealerId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DealerDto?>>> Create(
        [FromBody] DealerSaveRequest request,
        CancellationToken ct)
        => Ok(await _dealerService.CreateAsync(request, ct));

    [HttpPut("{dealerId:int}")]
    public async Task<ActionResult<ApiResponse<DealerDto?>>> Update(
        int dealerId,
        [FromBody] DealerSaveRequest request,
        CancellationToken ct)
        => Ok(await _dealerService.UpdateAsync(dealerId, request, ct));

    [HttpPut("{dealerId:int}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(
        int dealerId,
        [FromBody] DealerStatusRequest request,
        CancellationToken ct)
        => Ok(await _dealerService.UpdateStatusAsync(dealerId, request, ct));
}
