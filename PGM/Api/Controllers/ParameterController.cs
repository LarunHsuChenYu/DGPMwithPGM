using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Parameter;

namespace PGM.Api.Controllers;

/// <summary>相容讀取：參數清單（MemoryCache）。</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/parameters")]
public class ParameterController : ControllerBase
{
    private readonly IParameterService _parameterService;

    public ParameterController(IParameterService parameterService)
    {
        _parameterService = parameterService;
    }

    [HttpGet("{setItem}")]
    public async Task<ActionResult<ApiResponse<List<ParameterItemDto>>>> Get(string setItem, CancellationToken ct)
        => Ok(await _parameterService.GetParameterListAsync(setItem, ct));
}
