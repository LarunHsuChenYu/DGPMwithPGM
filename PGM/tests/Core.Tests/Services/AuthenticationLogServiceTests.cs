using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;
using PGM.Core.Application.Services;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class AuthenticationLogServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuthenticationLogRepository _repository = Substitute.For<IAuthenticationLogRepository>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly AuthenticationLogService _sut;

    public AuthenticationLogServiceTests()
    {
        _uow.AuthenticationLogs.Returns(_repository);
        _requestContext.TraceId.Returns("test-trace");
        _sut = new AuthenticationLogService(_uow, new AuthenticationLogMapper(), _requestContext);
    }

    private static AuthenticationLog CreateLog(char authStatus = 'O')
        => new()
        {
            Guid = "session-guid-1",
            UserId = "user01",
            IdentityContent = "Role=ADMIN$user01$SELF",
            Ip = "10.0.0.1",
            LoginType = 'G',
            AuthStatus = authStatus,
            LoginTime = new DateTime(2026, 7, 15, 9, 0, 0),
            LogoutTime = new DateTime(2026, 7, 15, 18, 0, 0)
        };

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage_AndNormalizesFilter()
    {
        var filter = new AuthenticationLogFilter
        {
            Keyword = " user01 ",
            AuthStatus = "o",
            Page = 2,
            PageSize = 10
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuthenticationLog>
            {
                Datas = [CreateLog()],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        var dto = result.Data.Datas.Single();
        dto.UserId.ShouldBe("user01");
        dto.Ip.ShouldBe("10.0.0.1");
        dto.LoginType.ShouldBe("G");
        dto.AuthStatus.ShouldBe("O");
        dto.LoginTime.ShouldBe(new DateTime(2026, 7, 15, 9, 0, 0));
        dto.LogoutTime.ShouldBe(new DateTime(2026, 7, 15, 18, 0, 0));
        result.Data.TotalRow.ShouldBe(11);
        filter.Keyword.ShouldBe("user01");
        filter.AuthStatus.ShouldBe("O");
    }

    [Fact]
    public async Task GetPagedAsync_WhenNotLoggedOut_LogoutTimeIsNull()
    {
        var filter = new AuthenticationLogFilter();
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuthenticationLog>
            {
                Datas = [CreateLog(authStatus: 'I')],
                TotalRow = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        var dto = result.Data.ShouldNotBeNull().Datas.Single();
        dto.AuthStatus.ShouldBe("I");
        dto.LogoutTime.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_BlankFilterValues_NormalizedToNull()
    {
        var filter = new AuthenticationLogFilter
        {
            Keyword = "  ",
            AuthStatus = ""
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuthenticationLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        filter.Keyword.ShouldBeNull();
        filter.AuthStatus.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidAuthStatus_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new AuthenticationLogFilter { AuthStatus = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<AuthenticationLogFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithReversedDateRange_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new AuthenticationLogFilter
        {
            LoginDateFrom = new DateTime(2026, 7, 20),
            LoginDateTo = new DateTime(2026, 7, 10)
        });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<AuthenticationLogFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithSameDayDateRange_Succeeds()
    {
        var filter = new AuthenticationLogFilter
        {
            LoginDateFrom = new DateTime(2026, 7, 15),
            LoginDateTo = new DateTime(2026, 7, 15)
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuthenticationLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        await _repository.Received(1).GetPagedAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var filter = new AuthenticationLogFilter();
        _repository.GetPagedAsync(filter, cts.Token)
            .Returns(new PagedResult<AuthenticationLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter, cts.Token);

        result.Code.ShouldBe("100");
        await _repository.Received(1).GetPagedAsync(filter, cts.Token);
    }
}
