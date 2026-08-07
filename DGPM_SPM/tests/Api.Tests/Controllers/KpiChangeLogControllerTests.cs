using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class KpiChangeLogControllerTests
{
    private readonly IKpiChangeLogService _service = Substitute.For<IKpiChangeLogService>();
    private readonly KpiChangeLogController _sut;

    public KpiChangeLogControllerTests()
    {
        _sut = new KpiChangeLogController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<KpiChangeLogDto> { Datas = new List<KpiChangeLogDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<KpiChangeLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<KpiChangeLogDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new KpiChangeLogFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
