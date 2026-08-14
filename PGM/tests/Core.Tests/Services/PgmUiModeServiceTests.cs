using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Services;
using PGM.Core.Common.Auth;
using PGM.Core.Common.Extensions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class PgmUiModeServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IParameterRepository _paramRepo = Substitute.For<IParameterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly PgmUiModeService _sut;

    public PgmUiModeServiceTests()
    {
        _uow.Parameters.Returns(_paramRepo);
        _requestContext.TraceId.Returns("trace");
        _sut = new PgmUiModeService(_uow, _currentUser, _requestContext);
    }

    [Fact]
    public async Task GetModeValueAsync_WhenMissing_DefaultsToOn()
    {
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var mode = await _sut.GetModeValueAsync();

        mode.ShouldBe(PgmUiMode.On);
    }

    [Fact]
    public async Task SetAsync_WhenNotPgmAdmin_ReturnsUnauthorized()
    {
        _currentUser.RoleId.Returns("DGPMAdmin$user$SELF");
        _currentUser.UserId.Returns("user");
        var roleRepo = Substitute.For<IRoleRepository>();
        _uow.Roles.Returns(roleRepo);
        roleRepo.GetAllByUserIdAsync("user", Arg.Any<CancellationToken>())
            .Returns(new List<Role>
            {
                new() { RoleId = "DGPMAdmin", SystemCode = "DGPM" }
            });

        var result = await _sut.SetAsync(new UpdatePgmUiModeRequest { Mode = PgmUiMode.Off });

        result.Code.ShouldBe(ErrorCodes.UnauthorizedAccess.GetDescription("code"));
        await _paramRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task SetAsync_WhenUserHasPgmAdminRole_AllowsFromDgpmSession()
    {
        _currentUser.RoleId.Returns("DGPMAdmin$Admin$SELF");
        _currentUser.UserId.Returns("Admin");
        var roleRepo = Substitute.For<IRoleRepository>();
        _uow.Roles.Returns(roleRepo);
        roleRepo.GetAllByUserIdAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(new List<Role>
            {
                new() { RoleId = "PGMAdmin", SystemCode = "PGM" },
                new() { RoleId = "DGPMAdmin", SystemCode = "DGPM" }
            });
        _paramRepo.IsCategoryActiveAsync(PgmUiMode.SetItem, Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var result = await _sut.SetAsync(new UpdatePgmUiModeRequest { Mode = "Off" });

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.Mode.ShouldBe(PgmUiMode.Off);
    }

    [Fact]
    public async Task SetAsync_WhenPgmAdmin_WritesOff()
    {
        _currentUser.RoleId.Returns("PGMAdmin$user$SELF");
        _currentUser.UserId.Returns("Admin");
        _paramRepo.IsCategoryActiveAsync(PgmUiMode.SetItem, Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var result = await _sut.SetAsync(new UpdatePgmUiModeRequest { Mode = "Off" });

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.Mode.ShouldBe(PgmUiMode.Off);
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
        await _paramRepo.Received(1).AddAsync(
            Arg.Is<Parameter>(p => p.SetValue == PgmUiMode.Off),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenOnlyDgpmAdmin_CanEditFalse()
    {
        GivenUser(
            "CathyWang",
            "DGPMAdmin$CathyWang$SELF",
            new Role { RoleId = "DGPMAdmin", SystemCode = "DGPM" });
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var result = await _sut.GetAsync();

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_WhenAshtonHsuHasPgmAdminFromDgpmSession_CanEditTrue()
    {
        GivenAshtonHsuOnDgpmSession();
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var result = await _sut.GetAsync();

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task SetAsync_WhenAshtonHsuHasPgmAdminFromDgpmSession_Allows()
    {
        GivenAshtonHsuOnDgpmSession();
        GivenWritableModeParam();

        var result = await _sut.SetAsync(new UpdatePgmUiModeRequest { Mode = "Off" });

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.Mode.ShouldBe(PgmUiMode.Off);
        result.Data.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task SetAsync_WhenLegacyAdminJwtRole_Allows()
    {
        _currentUser.RoleId.Returns("ADMIN$Admin$SELF");
        _currentUser.UserId.Returns("Admin");
        GivenWritableModeParam();

        var result = await _sut.SetAsync(new UpdatePgmUiModeRequest { Mode = "Off" });

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        result.Data!.Mode.ShouldBe(PgmUiMode.Off);
    }

    [Fact]
    public async Task GetAsync_WhenLegacyAdminJwtRole_CanEditTrue()
    {
        _currentUser.RoleId.Returns("ADMIN$Admin$SELF");
        _currentUser.UserId.Returns("Admin");
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);

        var result = await _sut.GetAsync();

        result.Data!.CanEdit.ShouldBeTrue();
    }

    private void GivenAshtonHsuOnDgpmSession() =>
        GivenUser(
            "AshtonHsu",
            "DGPMAdmin$AshtonHsu$SELF",
            new Role { RoleId = "PGMAdmin", SystemCode = "PGM" },
            new Role { RoleId = "DGPMAdmin", SystemCode = "DGPM" },
            new Role { RoleId = "DGPMUploader", SystemCode = "DGPM" },
            new Role { RoleId = "DGPMReviewer", SystemCode = "DGPM" });

    private void GivenUser(string userId, string jwtRoleId, params Role[] roles)
    {
        _currentUser.UserId.Returns(userId);
        _currentUser.RoleId.Returns(jwtRoleId);
        var roleRepo = Substitute.For<IRoleRepository>();
        _uow.Roles.Returns(roleRepo);
        roleRepo.GetAllByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(roles.ToList());
    }

    private void GivenWritableModeParam()
    {
        _paramRepo.IsCategoryActiveAsync(PgmUiMode.SetItem, Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync(PgmUiMode.SetItem, PgmUiMode.SetId, Arg.Any<CancellationToken>())
            .Returns((Parameter?)null);
    }
}

public class AuthMaintenanceGateTests
{
    private readonly IPgmUiModeService _uiMode = Substitute.For<IPgmUiModeService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly AuthMaintenanceGate _sut;

    public AuthMaintenanceGateTests()
    {
        _uow.Roles.Returns(_roles);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("Admin");
        _sut = new AuthMaintenanceGate(_uiMode, _currentUser, _uow);
    }

    [Fact]
    public async Task Evaluate_ModeOn_SysDgpm_Denies()
    {
        _uiMode.GetModeValueAsync(Arg.Any<CancellationToken>()).Returns(PgmUiMode.On);
        _currentUser.SystemCode.Returns("DGPM");
        _currentUser.RoleId.Returns("DGPMAdmin");

        var d = await _sut.EvaluateAsync("AUTH01", isWrite: true);

        d.Allowed.ShouldBeFalse();
        d.Code.ShouldBe(AuthMaintenanceGate.DeniedCode);
    }

    [Fact]
    public async Task Evaluate_ModeOff_SysDgpm_WithFun_Allows()
    {
        _uiMode.GetModeValueAsync(Arg.Any<CancellationToken>()).Returns(PgmUiMode.Off);
        _currentUser.SystemCode.Returns("DGPM");
        _currentUser.RoleId.Returns("DGPMAdmin$Admin$SELF");
        _roles.GetGrantedFunctionIdsAsync("DGPMAdmin", Arg.Any<CancellationToken>())
            .Returns(new List<string> { "AUTH01", "AUTH09" });

        var d = await _sut.EvaluateAsync("AUTH01", isWrite: true);

        d.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Evaluate_ModeOff_SysPgm_Write_Denies()
    {
        _uiMode.GetModeValueAsync(Arg.Any<CancellationToken>()).Returns(PgmUiMode.Off);
        _currentUser.SystemCode.Returns("PGM");
        _currentUser.RoleId.Returns("PGMAdmin");

        var d = await _sut.EvaluateAsync("AUTH01", isWrite: true);

        d.Allowed.ShouldBeFalse();
        d.Code.ShouldBe(AuthMaintenanceGate.DeniedCode);
    }
}
