using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class KpiIndicatorControllerTests
{
    private readonly IKpiIndicatorService _service = Substitute.For<IKpiIndicatorService>();
    private readonly KpiIndicatorController _sut;

    public KpiIndicatorControllerTests()
    {
        _sut = new KpiIndicatorController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<KpiIndicatorDto> { Datas = new List<KpiIndicatorDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<KpiIndicatorFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<KpiIndicatorDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new KpiIndicatorFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<SaveKpiIndicatorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiIndicatorDto>.SuccessResult(new KpiIndicatorDto()));

        var result = await _sut.Create(new SaveKpiIndicatorRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync(1, Arg.Any<SaveKpiIndicatorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiIndicatorDto>.SuccessResult(new KpiIndicatorDto()));

        var result = await _sut.Update(1, new SaveKpiIndicatorRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetStatus_Always_ReturnsOk()
    {
        _service.SetStatusAsync(1, Arg.Any<SetKpiIndicatorStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiIndicatorDto>.SuccessResult(new KpiIndicatorDto()));

        var result = await _sut.SetStatus(1, new SetKpiIndicatorStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
