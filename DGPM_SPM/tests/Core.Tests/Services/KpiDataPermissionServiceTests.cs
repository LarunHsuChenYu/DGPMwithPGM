using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class KpiDataPermissionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IKpiUserDataScopeRepository _scopeRepository = Substitute.For<IKpiUserDataScopeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly KpiDataPermissionService _sut;

    public KpiDataPermissionServiceTests()
    {
        _uow.Users.Returns(_userRepository);
        _uow.KpiUserDataScopes.Returns(_scopeRepository);
        _currentUser.UserId.Returns("admin");
        _requestContext.TraceId.Returns("test-trace");
        _sut = new KpiDataPermissionService(_uow, new KpiUserDataScopeMapper(), _currentUser, _requestContext);
    }

    private static User BuildUser(string userId = "user01") => new()
    {
        UserId = userId,
        UserName = "測試使用者"
    };

    [Fact]
    public async Task GetByUserIdAsync_ReturnsScopesGroupedByType()
    {
        _userRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        _scopeRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(new List<KpiUserDataScope>
            {
                new()
                {
                    ScopeId = 1,
                    UserId = "user01",
                    ScopeType = "R",
                    RegionId = 10,
                    RegionCode = "N",
                    RegionName = "北區"
                },
                new()
                {
                    ScopeId = 2,
                    UserId = "user01",
                    ScopeType = "D",
                    DealerId = 20,
                    DealerCode = "D001",
                    DealerName = "台北經銷商"
                }
            });

        var result = await _sut.GetByUserIdAsync(" user01 ");

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.UserId.ShouldBe("user01");
        result.Data.UserName.ShouldBe("測試使用者");
        result.Data.RegionScopes.Single().RegionId.ShouldBe(10);
        result.Data.RegionScopes.Single().RegionName.ShouldBe("北區");
        result.Data.DealerScopes.Single().DealerId.ShouldBe(20);
        result.Data.DealerScopes.Single().DealerCode.ShouldBe("D001");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithBlankUserId_ReturnsInvalidParameter()
    {
        var result = await _sut.GetByUserIdAsync("   ");

        result.Code.ShouldBe("200");
        await _userRepository.DidNotReceive().GetByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserNotFound_ReturnsDataNotFound()
    {
        _userRepository.GetByUserIdAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.GetByUserIdAsync("ghost");

        result.Code.ShouldBe("404");
        await _scopeRepository.DidNotReceive().GetByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithValidRequest_ReplacesScopesInTransaction()
    {
        _userRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        _scopeRepository.GetExistingRegionIdsAsync(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 10 })),
                Arg.Any<CancellationToken>())
            .Returns([10]);
        _scopeRepository.GetExistingDealerIdsAsync(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 20, 21 })),
                Arg.Any<CancellationToken>())
            .Returns([20, 21]);
        _scopeRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest
        {
            RegionIds = [10, 10],
            DealerIds = [20, 21]
        });

        result.Code.ShouldBe("100");
        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _scopeRepository.Received(1).ReplaceByUserIdAsync(
            "user01",
            Arg.Is<IReadOnlyCollection<KpiUserDataScope>>(scopes =>
                scopes.Count == 3 &&
                scopes.Count(s => s.ScopeType == "R" && s.RegionId == 10 && s.DealerId == null) == 1 &&
                scopes.Count(s => s.ScopeType == "D" && s.DealerId != null && s.RegionId == null) == 2 &&
                scopes.All(s => s.UserId == "user01" && s.CrtUser == "admin")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEmptyScopes_ClearsAllPermissions()
    {
        _userRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        _scopeRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest());

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.RegionScopes.ShouldBeEmpty();
        result.Data.DealerScopes.ShouldBeEmpty();
        await _scopeRepository.Received(1).ReplaceByUserIdAsync(
            "user01",
            Arg.Is<IReadOnlyCollection<KpiUserDataScope>>(scopes => scopes.Count == 0),
            Arg.Any<CancellationToken>());
        await _scopeRepository.DidNotReceive().GetExistingRegionIdsAsync(
            Arg.Any<IReadOnlyCollection<int>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithUnknownRegionId_ReturnsInvalidParameter()
    {
        _userRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        _scopeRepository.GetExistingRegionIdsAsync(
                Arg.Any<IReadOnlyCollection<int>>(),
                Arg.Any<CancellationToken>())
            .Returns([10]);

        var result = await _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest
        {
            RegionIds = [10, 999]
        });

        result.Code.ShouldBe("200");
        await _scopeRepository.DidNotReceive().ReplaceByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<KpiUserDataScope>>(),
            Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithNonPositiveId_ReturnsInvalidParameter()
    {
        var result = await _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest
        {
            DealerIds = [0]
        });

        result.Code.ShouldBe("200");
        await _userRepository.DidNotReceive().GetByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WhenUserNotFound_ReturnsDataNotFound()
    {
        _userRepository.GetByUserIdAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.SaveAsync("ghost", new SaveKpiUserPermissionRequest());

        result.Code.ShouldBe("404");
        await _scopeRepository.DidNotReceive().ReplaceByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<KpiUserDataScope>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithoutOperator_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((string?)null);

        var result = await _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest());

        result.Code.ShouldBe("400");
        await _scopeRepository.DidNotReceive().ReplaceByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<KpiUserDataScope>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WhenRepositoryThrows_RollsBack()
    {
        _userRepository.GetByUserIdAsync("user01", Arg.Any<CancellationToken>())
            .Returns(BuildUser());
        _scopeRepository.ReplaceByUserIdAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<KpiUserDataScope>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SaveAsync("user01", new SaveKpiUserPermissionRequest()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
