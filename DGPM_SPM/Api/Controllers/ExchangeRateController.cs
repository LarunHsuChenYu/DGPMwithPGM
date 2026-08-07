using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Application.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGPM_SPM.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRateController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ExchangeRateDto>>>> GetPaged(
        [FromQuery] ExchangeRateFilter filter,
        CancellationToken ct)
        => Ok(await _exchangeRateService.GetPagedAsync(filter, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExchangeRateDto>>> Create(
        [FromBody] SaveExchangeRateRequest request,
        CancellationToken ct)
        => Ok(await _exchangeRateService.CreateAsync(request, ct));

    [HttpPut("{rateId:int}")]
    public async Task<ActionResult<ApiResponse<ExchangeRateDto>>> Update(
        int rateId,
        [FromBody] SaveExchangeRateRequest request,
        CancellationToken ct)
        => Ok(await _exchangeRateService.UpdateAsync(rateId, request, ct));

    [HttpPut("{rateId:int}/status")]
    public async Task<ActionResult<ApiResponse<ExchangeRateDto>>> SetStatus(
        int rateId,
        [FromBody] SetExchangeRateStatusRequest request,
        CancellationToken ct)
        => Ok(await _exchangeRateService.SetStatusAsync(rateId, request, ct));
}
