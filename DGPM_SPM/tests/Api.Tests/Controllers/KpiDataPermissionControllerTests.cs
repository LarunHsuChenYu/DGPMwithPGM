using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;

namespace DGPM_SPM.Api.Tests.Controllers;

public class KpiDataPermissionControllerTests
{
    private readonly IKpiDataPermissionService _service = Substitute.For<IKpiDataPermissionService>();
    private readonly KpiDataPermissionController _sut;

    public KpiDataPermissionControllerTests()
    {
        _sut = new KpiDataPermissionController(_service);
    }

    [Fact]
    public async Task GetByUserId_Always_ReturnsOk()
    {
        _service.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiUserPermissionDto>.SuccessResult(new KpiUserPermissionDto()));

        var result = await _sut.GetByUserId("u1", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Save_Always_ReturnsOk()
    {
        _service.SaveAsync("u1", Arg.Any<SaveKpiUserPermissionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiUserPermissionDto>.SuccessResult(new KpiUserPermissionDto()));

        var result = await _sut.Save("u1", new SaveKpiUserPermissionRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
