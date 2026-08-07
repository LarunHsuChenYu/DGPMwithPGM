using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Services;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class PermissionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _uow.Menus.Returns(_menuRepo);
        _requestContext.TraceId.Returns("test-trace");
        _currentUser.UserId.Returns("user1");
        _currentUser.RoleId.Returns("ADMIN$user1$SELF");

        _menuRepo.GetMenuByRoleIdAsync("ADMIN", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>
            {
                new() { FunId = "F001", IsMenu = "Y", IsEnabled = "Y", DelYn = "N" },
                new() { FunId = "F002", IsMenu = "Y", IsEnabled = "Y", DelYn = "N" }
            });

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
        await _menuRepo.Received(1).GetMenuByRoleIdAsync("ADMIN", Arg.Any<string?>(), cts.Token);
    }
}
