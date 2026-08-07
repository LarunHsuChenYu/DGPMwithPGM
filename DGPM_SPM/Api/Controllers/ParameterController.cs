using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Parameter;

namespace DGPM_SPM.Api.Controllers;

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
