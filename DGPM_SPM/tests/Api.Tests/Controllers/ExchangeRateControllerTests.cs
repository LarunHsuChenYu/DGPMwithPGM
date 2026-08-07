using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;

namespace DGPM_SPM.Api.Tests.Controllers;

public class ExchangeRateControllerTests
{
    private readonly IExchangeRateService _service = Substitute.For<IExchangeRateService>();
    private readonly ExchangeRateController _sut;

    public ExchangeRateControllerTests()
    {
        _sut = new ExchangeRateController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<ExchangeRateDto> { Datas = new List<ExchangeRateDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<ExchangeRateFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<ExchangeRateDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new ExchangeRateFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<SaveExchangeRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<ExchangeRateDto>.SuccessResult(new ExchangeRateDto()));

        var result = await _sut.Create(new SaveExchangeRateRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync(1, Arg.Any<SaveExchangeRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<ExchangeRateDto>.SuccessResult(new ExchangeRateDto()));

        var result = await _sut.Update(1, new SaveExchangeRateRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetStatus_Always_ReturnsOk()
    {
        _service.SetStatusAsync(1, Arg.Any<SetExchangeRateStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<ExchangeRateDto>.SuccessResult(new ExchangeRateDto()));

        var result = await _sut.SetStatus(1, new SetExchangeRateStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
