using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Services;

namespace PGM.Core.Tests.Services;

public class PermissionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _uow.Roles.Returns(_roleRepo);
        _requestContext.TraceId.Returns("test-trace");
        _currentUser.UserId.Returns("user1");
        _currentUser.RoleId.Returns("ADMIN$user1$SELF");

        _roleRepo.GetGrantedFunctionIdsAsync("ADMIN", Arg.Any<CancellationToken>())
            .Returns(new List<string> { "F001", "F002" });

        _sut = new PermissionService(_uow, _currentUser, _requestContext);
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
        await _roleRepo.Received(1).GetGrantedFunctionIdsAsync("ADMIN", cts.Token);
    }
}
