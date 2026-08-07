using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class KpiImportControllerTests
{
    private readonly IKpiImportService _service = Substitute.For<IKpiImportService>();
    private readonly KpiImportController _sut;

    public KpiImportControllerTests()
    {
        _sut = new KpiImportController(_service);
    }

    [Fact]
    public async Task Import_Always_ReturnsOk()
    {
        _service.ImportAsync(Arg.Any<CreateKpiImportRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiImportResultDto>.SuccessResult(new KpiImportResultDto()));

        var result = await _sut.Import(new CreateKpiImportRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<KpiImportBatchDto> { Datas = new List<KpiImportBatchDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetBatchPagedAsync(Arg.Any<KpiImportBatchFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<KpiImportBatchDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new KpiImportBatchFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Always_ReturnsOk()
    {
        _service.GetBatchAsync(1L, Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiImportBatchDto>.SuccessResult(new KpiImportBatchDto()));

        var result = await _sut.GetById(1L, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
