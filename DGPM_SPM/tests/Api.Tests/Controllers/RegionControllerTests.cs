using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class RegionControllerTests
{
    private readonly IRegionService _service = Substitute.For<IRegionService>();
    private readonly RegionController _sut;

    public RegionControllerTests()
    {
        _sut = new RegionController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<RegionDto> { Datas = new List<RegionDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<RegionFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<RegionDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new RegionFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Always_ReturnsOk()
    {
        _service.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RegionDto?>.SuccessResult(new RegionDto()));

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOptions_Always_ReturnsOk()
    {
        _service.GetOptionsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<RegionOptionDto>>.SuccessResult(new List<RegionOptionDto>()));

        var result = await _sut.GetOptions(null, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<RegionSaveRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RegionDto?>.SuccessResult(new RegionDto()));

        var result = await _sut.Create(new RegionSaveRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync(1, Arg.Any<RegionSaveRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RegionDto?>.SuccessResult(new RegionDto()));

        var result = await _sut.Update(1, new RegionSaveRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Always_ReturnsOk()
    {
        _service.UpdateStatusAsync(1, Arg.Any<RegionStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.UpdateStatus(1, new RegionStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
