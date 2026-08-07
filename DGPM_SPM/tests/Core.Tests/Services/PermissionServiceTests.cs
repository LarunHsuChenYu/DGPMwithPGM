using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Application.Services;

namespace DGPM_SPM.Core.Tests.Services;

public class PermissionServiceTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly IPgmAuthClient _pgmAuth = Substitute.For<IPgmAuthClient>();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _requestContext.TraceId.Returns("test-trace");
        _currentUser.UserId.Returns("user1");
        _currentUser.RoleId.Returns("ADMIN$user1$SELF");

        _pgmAuth.GetMenusAsync(Arg.Any<CancellationToken>())
            .Returns(ApiResponse<List<MenuDto>>.SuccessResult(
            [
                new MenuDto { FunctionId = "F001", FunctionName = "功能一" },
                new MenuDto { FunctionId = "F002", FunctionName = "功能二" }
            ]));

        _sut = new PermissionService(_currentUser, _requestContext, _pgmAuth);
    }

    [Fact]
    public async Task CheckAsync_WithAllowedFunction_ReturnsTrue()
    {
        var result = await _sut.CheckAsync("F001");

        result.Data.ShouldNotBeNull();
        result.Data.Allowed.ShouldBeTrue();
        result.Data.FunctionId.ShouldBe("F001");
    }

    [Fact]
    public async Task CheckAsync_WithDeniedFunction_ReturnsFalse()
    {
        var result = await _sut.CheckAsync("F999");

        result.Data.ShouldNotBeNull();
        result.Data.Allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckBatchAsync_ReturnsDistinctResults()
    {
        using var cts = new CancellationTokenSource();

        var result = await _sut.CheckBatchAsync(["F001", "F999", "F001"], cts.Token);

        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(2);
        result.Data.Single(r => r.FunctionId == "F001").Allowed.ShouldBeTrue();
        result.Data.Single(r => r.FunctionId == "F999").Allowed.ShouldBeFalse();
        await _pgmAuth.Received(1).GetMenusAsync(cts.Token);
    }
}
