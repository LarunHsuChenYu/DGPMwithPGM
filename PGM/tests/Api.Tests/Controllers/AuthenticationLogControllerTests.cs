using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;

namespace PGM.Api.Tests.Controllers;

public class AuthenticationLogControllerTests
{
    private readonly IAuthenticationLogService _service = Substitute.For<IAuthenticationLogService>();
    private readonly AuthenticationLogController _sut;

    public AuthenticationLogControllerTests()
    {
        _sut = new AuthenticationLogController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<AuthenticationLogDto> { Datas = new List<AuthenticationLogDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<AuthenticationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<AuthenticationLogDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new AuthenticationLogFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
