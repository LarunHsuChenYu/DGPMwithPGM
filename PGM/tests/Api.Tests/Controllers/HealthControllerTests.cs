using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PGM.Api.Tests.Controllers;

public class HealthControllerTests
{
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ILogger<HealthController> _logger = Substitute.For<ILogger<HealthController>>();
    private readonly HealthController _sut;

    public HealthControllerTests()
    {
        _requestContext.TraceId.Returns("test-trace-health");
        _connectionFactory.GetTargetInfo().Returns(("localhost", "TestDB"));
        _sut = new HealthController(_requestContext, _connectionFactory, _logger);
    }

    [Fact]
    public async Task Get_WhenDbConnectionFails_Returns503()
    {
        // DbConnection 拋例外 → dbOk = false → 503
        var mockConn = Substitute.For<System.Data.Common.DbConnection>();
        mockConn.OpenAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("connection failed")));
        _connectionFactory.CreateConnection().Returns(mockConn);

        var result = await _sut.Get(CancellationToken.None);

        var statusCodeResult = result.Result.ShouldBeOfType<ObjectResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }
}
