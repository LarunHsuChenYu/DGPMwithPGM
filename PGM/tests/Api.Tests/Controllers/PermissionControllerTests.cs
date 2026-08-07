using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;

namespace PGM.Api.Tests.Controllers;

public class PermissionControllerTests
{
    private readonly IPermissionService _service = Substitute.For<IPermissionService>();
    private readonly PermissionController _sut;

    public PermissionControllerTests()
    {
        _sut = new PermissionController(_service);
    }

    [Fact]
    public async Task Check_Always_ReturnsOk()
    {
        _service.CheckAsync("F001", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PermissionResponse>.SuccessResult(new PermissionResponse()));

        var result = await _sut.Check("F001", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckBatch_Always_ReturnsOk()
    {
        _service.CheckBatchAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<PermissionResponse>>.SuccessResult(new List<PermissionResponse>()));

        var result = await _sut.CheckBatch(
            new PermissionBatchRequest { FunctionIds = new List<string> { "F001" } },
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
