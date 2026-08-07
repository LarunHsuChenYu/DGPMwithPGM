using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Parameter;

namespace DGPM_SPM.Api.Tests.Controllers;

public class ParameterControllerTests
{
    private readonly IParameterService _service = Substitute.For<IParameterService>();
    private readonly ParameterController _sut;

    public ParameterControllerTests()
    {
        _sut = new ParameterController(_service);
    }

    [Fact]
    public async Task Get_Always_ReturnsOk()
    {
        _service.GetParameterListAsync("CURRENCY", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<ParameterItemDto>>.SuccessResult(new List<ParameterItemDto>()));

        var result = await _sut.Get("CURRENCY", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
