using Microsoft.Extensions.Caching.Memory;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Application.Services;
using PGM.Core.Common.Extensions;
using PGM.Core.Common.Jwt;
using PGM.Core.Common.Security;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class AuthServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();
    private readonly IAuthenticationLogRepository _authLogRepo = Substitute.For<IAuthenticationLogRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly IPgmUiModeService _uiMode = Substitute.For<IPgmUiModeService>();
    private readonly IAuthMapper _authMapper = new AuthMapper();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _uow.Users.Returns(_userRepo);
        _uow.Roles.Returns(_roleRepo);
        _uow.Menus.Returns(_menuRepo);
        _uow.AuthenticationLogs.Returns(_authLogRepo);
        _requestContext.TraceId.Returns("test-trace");
        _uiMode.GetModeValueAsync(Arg.Any<CancellationToken>())
            .Returns(PGM.Core.Common.Auth.PgmUiMode.On);

        _sut = new AuthService(
            _uow,
            _tokenService,
            _currentUser,
            _requestContext,
            _authMapper,
            _uiMode);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword("secret");
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "Test User", Password = hashed });

        _roleRepo.GetAllByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new List<Role> { new() { RoleId = "ADMIN", RoleName = "Administrator", SystemCode = "PGM" } });

        _menuRepo.GetMenuByRoleIdAsync("ADMIN", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>
            {
                new()
                {
                    FunId = "F001",
                    FunName = "Home",
                    SortOrder = 1,
                    IsMenu = "Y",
                    IsEnabled = "Y",
                    DelYn = "N"
                }
            });

        _tokenService.CreateTokens(Arg.Any<UserInfoDto>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string?>())
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddHours(1)));

        _currentUser.ClientIp.Returns("127.0.0.1");

        var result = await _sut.LoginAsync(new LoginRequest { UserId = "user1", Password = "secret" });

        result.Data.ShouldNotBeNull();
        result.Data.AccessToken.ShouldBe("access-token");
        result.Data.PasswordExpired.ShouldBeFalse();
        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        await _uow.Received(1).BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _authLogRepo.Received(1).AddAsync(Arg.Any<AuthenticationLog>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithDefaultPassword_SetsPasswordExpired()
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(DefaultPassword.Value);
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "New User", Password = hashed });
        _roleRepo.GetAllByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new List<Role> { new() { RoleId = "USER", RoleName = "一般使用者", SystemCode = "PGM" } });
        _menuRepo.GetMenuByRoleIdAsync("USER", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>());
        _tokenService.CreateTokens(Arg.Any<UserInfoDto>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string?>())
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddHours(1)));

        var result = await _sut.LoginAsync(new LoginRequest
        {
            UserId = "user1",
            Password = DefaultPassword.Value
        });

        result.Data.ShouldNotBeNull();
        result.Data.PasswordExpired.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidNewPassword_UpdatesHashAndHistory()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user1");
        var hashed = BCrypt.Net.BCrypt.HashPassword(DefaultPassword.Value);
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "New User", Password = hashed });

        var result = await _sut.ChangePasswordAsync(new ChangePasswordRequest
        {
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        result.Code.ShouldBe(ErrorCodes.Success.GetDescription("code"));
        await _userRepo.Received(1).UpdatePasswordAsync(
            "user1",
            Arg.Is<string>(hash => BCrypt.Net.BCrypt.Verify("NewPass123!", hash)),
            "user1",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsError()
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword("secret");
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "Test User", Password = hashed });

        var result = await _sut.LoginAsync(new LoginRequest { UserId = "user1", Password = "wrong" });

        result.Data.ShouldBeNull();
        result.Code.ShouldBe(ErrorCodes.AuthInvalid.GetDescription("code"));
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ReturnsError()
    {
        _userRepo.GetByUserIdAsync("unknown", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest { UserId = "unknown", Password = "secret" });

        result.Data.ShouldBeNull();
        result.Code.ShouldBe(ErrorCodes.AuthInvalid.GetDescription("code"));
    }

    [Fact]
    public async Task LoginAsync_WithNoRoles_ReturnsUnauthorized()
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword("secret");
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "Test User", Password = hashed });

        _roleRepo.GetAllByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new List<Role>());

        var result = await _sut.LoginAsync(new LoginRequest { UserId = "user1", Password = "secret" });

        result.Data.ShouldBeNull();
        result.Code.ShouldBe(ErrorCodes.AuthNoRole.GetDescription("code"));
    }

    [Fact]
    public async Task LoginAsync_WhenAuthenticationLogFails_RollsBackAndRethrows()
    {
        using var cts = new CancellationTokenSource();
        var hashed = BCrypt.Net.BCrypt.HashPassword("secret");
        _userRepo.GetByUserIdAsync("user1", cts.Token)
            .Returns(new User { UserId = "user1", UserName = "Test User", Password = hashed });
        _roleRepo.GetAllByUserIdAsync("user1", cts.Token)
            .Returns(new List<Role> { new() { RoleId = "ADMIN", RoleName = "Administrator", SystemCode = "PGM" } });
        _menuRepo.GetMenuByRoleIdAsync("ADMIN", Arg.Any<string?>(), cts.Token)
            .Returns(new List<SysFun>());
        _tokenService.CreateTokens(Arg.Any<UserInfoDto>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string?>())
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddHours(1)));
        _authLogRepo.AddAsync(Arg.Any<AuthenticationLog>(), cts.Token)
            .Returns(Task.FromException<int>(new InvalidOperationException("write failed")));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest { UserId = "user1", Password = "secret" }, cts.Token));

        await _uow.Received(1).BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            cts.Token);
        await _uow.Received(1).RollbackAsync(cts.Token);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenMultipleDgpmRoles_PrefersAdminRoleForMenus()
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword("secret");
        _userRepo.GetByUserIdAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "Admin", UserName = "系統管理員", Password = hashed });

        _roleRepo.GetAllByUserIdAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(new List<Role>
            {
                new() { RoleId = "DGPMUploader", RoleName = "DGPM KPI上傳", SystemCode = "DGPM" },
                new() { RoleId = "DGPMAdmin", RoleName = "DGPM管理者", SystemCode = "DGPM" }
            });

        _menuRepo.GetMenuByRoleIdAsync("DGPMAdmin", "DGPM", Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>
            {
                new()
                {
                    FunId = "KPIIndicator",
                    FunName = "經銷商KPI管理",
                    ActionType = "M",
                    SortOrder = 400,
                    IsMenu = "Y",
                    IsEnabled = "Y",
                    DelYn = "N"
                },
                new()
                {
                    FunId = "RoleKPIList",
                    FunName = "KPI 資料權限設定",
                    ParentId = "KPIIndicator",
                    ActionType = "P",
                    UrlPath = "/system/kpi-permissions",
                    SortOrder = 440,
                    IsMenu = "Y",
                    IsEnabled = "Y",
                    DelYn = "N"
                }
            });

        _tokenService.CreateTokens(
                Arg.Is<UserInfoDto>(u => u.RoleId!.StartsWith("DGPMAdmin$")),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>())
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddHours(1)));

        _currentUser.ClientIp.Returns("127.0.0.1");

        var result = await _sut.LoginAsync(new LoginRequest
        {
            UserId = "Admin",
            Password = "secret",
            SystemCode = "DGPM"
        });

        result.Data.ShouldNotBeNull();
        result.Data.User!.RoleName.ShouldBe("DGPM管理者");
        result.Data.Menus.ShouldContain(m => m.FunctionId == "KPIIndicator");
        result.Data.Menus.ShouldContain(m => m.FunctionId == "RoleKPIList");
        result.Data.Menus.ShouldNotContain(m => m.FunctionId == "PgmAuthLink");
        result.Data.Menus.ShouldNotContain(m => m.FunctionId == "Permission");
        await _menuRepo.Received(1).GetMenuByRoleIdAsync("DGPMAdmin", "DGPM", Arg.Any<CancellationToken>());
        await _menuRepo.DidNotReceive().GetMenuByRoleIdAsync("DGPMUploader", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchRoleAsync_WithOwnedRole_ReissuesTokenAndMenusForRole()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user1");
        _currentUser.SessionGuid.Returns("session-1");
        _currentUser.SystemCode.Returns("PGM");
        _userRepo.GetByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = "user1", UserName = "Test User", Password = "x" });
        _roleRepo.GetAllByUserIdAsync("user1", Arg.Any<CancellationToken>())
            .Returns(new List<Role>
            {
                new() { RoleId = "ADMIN", RoleName = "系統管理員", SystemCode = "PGM" },
                new() { RoleId = "VIEWER", RoleName = "檢視者", SystemCode = "PGM" }
            });
        _menuRepo.GetMenuByRoleIdAsync("VIEWER", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>
            {
                new() { FunId = "AUTH05", FunName = "系統報表", SortOrder = 50, IsMenu = "Y", IsEnabled = "Y", DelYn = "N" }
            });
        _tokenService.CreateTokens(
                Arg.Is<UserInfoDto>(u => u.RoleId!.StartsWith("VIEWER$")),
                Arg.Any<IEnumerable<string>>(),
                "session-1")
            .Returns(("role-token", "refresh-token", DateTime.UtcNow.AddHours(1)));

        var result = await _sut.SwitchRoleAsync(new SwitchRoleRequest { RoleId = "VIEWER" });

        result.Data.ShouldNotBeNull();
        result.Data.AccessToken.ShouldBe("role-token");
        result.Data.Menus.Count.ShouldBe(1);
        result.Data.Menus[0].FunctionId.ShouldBe("AUTH05");
        await _menuRepo.Received(1).GetMenuByRoleIdAsync("VIEWER", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMenusAsync_UsesRoleIdFromJwt()
    {
        _currentUser.UserId.Returns("user1");
        _currentUser.RoleId.Returns("USER$user1$SELF");
        _currentUser.SystemCode.Returns("PGM");
        _menuRepo.GetMenuByRoleIdAsync("USER", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SysFun>
            {
                new() { FunId = "AUTH01", FunName = "帳號維護", SortOrder = 10, IsMenu = "Y", IsEnabled = "Y", DelYn = "N" }
            });

        var result = await _sut.GetMenusAsync();

        result.Data.ShouldNotBeNull();
        result.Data.Single().FunctionId.ShouldBe("AUTH01");
        await _menuRepo.Received(1).GetMenuByRoleIdAsync("USER", Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _menuRepo.DidNotReceive().GetMenuByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
