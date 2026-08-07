using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Parameter;

namespace PGM.Api.Tests.Controllers;

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
            .Returns(ApiResponse<List<ParameterItemDto>>.SuccessResult([]));

        var result = await _sut.Get("CURRENCY", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}

public class SystemParameterControllerTests
{
    private readonly IParameterService _service = Substitute.For<IParameterService>();
    private readonly SystemParameterController _sut;

    public SystemParameterControllerTests()
    {
        _sut = new SystemParameterController(_service);
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        _service.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(ApiResponse<IReadOnlyList<ParameterCategoryDto>>.SuccessResult([]));

        var result = await _sut.GetCategories(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<CreateParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<ParameterDto?>.SuccessResult(new ParameterDto { SetId = "A" }));

        var result = await _sut.Create(new CreateParameterRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        _service.DeleteAsync("ITEM", "A", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.Delete("ITEM", "A", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
