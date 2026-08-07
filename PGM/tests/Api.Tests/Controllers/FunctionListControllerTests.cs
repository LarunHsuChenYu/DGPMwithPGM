using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;

namespace PGM.Api.Tests.Controllers;

public class FunctionListControllerTests
{
    private readonly IFunctionService _service = Substitute.For<IFunctionService>();
    private readonly FunctionListController _sut;

    public FunctionListControllerTests()
    {
        _sut = new FunctionListController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<FunctionDto> { Datas = new List<FunctionDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<FunctionFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<FunctionDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new FunctionFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetParentOptions_Always_ReturnsOk()
    {
        _service.GetParentOptionsAsync(Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<FunctionOptionDto>>.SuccessResult(new List<FunctionOptionDto>()));

        var result = await _sut.GetParentOptions(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOptions_Always_ReturnsOk()
    {
        _service.GetOptionsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<FunctionOptionDto>>.SuccessResult(new List<FunctionOptionDto>()));

        var result = await _sut.GetOptions(null, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CanDelete_Always_ReturnsOk()
    {
        _service.CanDeleteAsync("F001", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.CanDelete("F001", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByFunId_Always_ReturnsOk()
    {
        _service.GetByFunIdAsync("F001", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<FunctionDto?>.SuccessResult(new FunctionDto()));

        var result = await _sut.GetByFunId("F001", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<SaveFunctionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<FunctionDto?>.SuccessResult(new FunctionDto()));

        var result = await _sut.Create(new SaveFunctionRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync("F001", Arg.Any<SaveFunctionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<FunctionDto?>.SuccessResult(new FunctionDto()));

        var result = await _sut.Update("F001", new SaveFunctionRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SoftDelete_Always_ReturnsOk()
    {
        _service.SoftDeleteAsync("F001", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.SoftDelete("F001", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
