using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class DealerControllerTests
{
    private readonly IDealerService _service = Substitute.For<IDealerService>();
    private readonly DealerController _sut;

    public DealerControllerTests()
    {
        _sut = new DealerController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<DealerDto> { Datas = new List<DealerDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<DealerFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<DealerDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new DealerFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Always_ReturnsOk()
    {
        _service.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ApiResponse<DealerDto?>.SuccessResult(new DealerDto()));

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<DealerSaveRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<DealerDto?>.SuccessResult(new DealerDto()));

        var result = await _sut.Create(new DealerSaveRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync(1, Arg.Any<DealerSaveRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<DealerDto?>.SuccessResult(new DealerDto()));

        var result = await _sut.Update(1, new DealerSaveRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Always_ReturnsOk()
    {
        _service.UpdateStatusAsync(1, Arg.Any<DealerStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.UpdateStatus(1, new DealerStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
