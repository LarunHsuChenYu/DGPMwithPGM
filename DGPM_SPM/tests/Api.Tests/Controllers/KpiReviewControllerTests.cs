using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class KpiReviewControllerTests
{
    private readonly IKpiReviewService _service = Substitute.For<IKpiReviewService>();
    private readonly KpiReviewController _sut;

    public KpiReviewControllerTests()
    {
        _sut = new KpiReviewController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<KpiDataDto> { Datas = new List<KpiDataDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<KpiDataFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<KpiDataDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new KpiDataFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Review_Always_ReturnsOk()
    {
        _service.ReviewAsync(1L, Arg.Any<ReviewKpiDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiDataDto>.SuccessResult(new KpiDataDto()));

        var result = await _sut.Review(1L, new ReviewKpiDataRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Unlock_Always_ReturnsOk()
    {
        _service.UnlockAsync(1L, Arg.Any<UnlockKpiDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<KpiDataDto>.SuccessResult(new KpiDataDto()));

        var result = await _sut.Unlock(1L, new UnlockKpiDataRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
