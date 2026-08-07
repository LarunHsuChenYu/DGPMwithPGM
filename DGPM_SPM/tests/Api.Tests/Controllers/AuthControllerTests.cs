using DGPM_SPM.Api.Controllers;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Application.Models.Enums;
using DGPM_SPM.Core.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace DGPM_SPM.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly IPgmAuthClient _pgmAuth = Substitute.For<IPgmAuthClient>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_pgmAuth);
    }

    [Fact]
    public async Task Login_WhenServiceReturnsSuccess_ReturnsOk()
    {
        var response = ApiResponse<LoginResponse>.SuccessResult(
            new LoginResponse { AccessToken = "tok", RefreshToken = "ref" });
        _pgmAuth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "pw" }, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
    }

    [Fact]
    public async Task Login_WhenServiceReturnsError_ReturnsUnauthorized()
    {
        var errorCode = ErrorCodes.IncorrectPassword.GetDescription("code");
        var response = ApiResponse<LoginResponse>.ErrorResult(errorCode, "wrong password");
        _pgmAuth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "bad" }, CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenNoRole_ReturnsForbidden()
    {
        var response = ApiResponse<LoginResponse>.ErrorResult(
            "AUTH_NO_ROLE", "尚未設定角色，請聯絡管理員");
        _pgmAuth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "pw" }, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Login_WhenEntryDisabled_ReturnsForbidden()
    {
        var response = ApiResponse<LoginResponse>.ErrorResult(
            "AUTH_ENTRY_DISABLED", "目前不允許由業務系統登入，請使用指定入口。");
        _pgmAuth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "pw" }, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Login_WhenUnreachable_ReturnsServiceUnavailable()
    {
        var response = ApiResponse<LoginResponse>.ErrorResult(
            "PGM_UNAVAILABLE", "無法連線至權限平台（PGM），請稍後再試或聯絡管理員。");
        _pgmAuth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "pw" }, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Logout_Always_ReturnsOk()
    {
        var response = ApiResponse<object>.SuccessResult(new object());
        _pgmAuth.LogoutAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Logout(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_WhenDataNotNull_ReturnsOk()
    {
        var response = ApiResponse<LoginResponse>.SuccessResult(
            new LoginResponse { AccessToken = "new-tok", RefreshToken = "new-ref" });
        _pgmAuth.RefreshAsync(Arg.Any<RefreshTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Refresh(new RefreshTokenRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_WhenDataIsNull_ReturnsUnauthorized()
    {
        var errorCode = ErrorCodes.UnauthorizedAccess.GetDescription("code");
        var response = ApiResponse<LoginResponse>.ErrorResult(errorCode, "invalid token");
        _pgmAuth.RefreshAsync(Arg.Any<RefreshTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Refresh(new RefreshTokenRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Me_WhenDataNotNull_ReturnsOk()
    {
        var response = ApiResponse<UserInfoDto>.SuccessResult(new UserInfoDto { UserId = "u1" });
        _pgmAuth.GetMeAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Me(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Me_WhenDataIsNull_ReturnsUnauthorized()
    {
        var errorCode = ErrorCodes.UnauthorizedAccess.GetDescription("code");
        var response = ApiResponse<UserInfoDto>.ErrorResult(errorCode, "no user");
        _pgmAuth.GetMeAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Me(CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Menus_Always_ReturnsOk()
    {
        var response = ApiResponse<List<MenuDto>>.SuccessResult(new List<MenuDto>());
        _pgmAuth.GetMenusAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Menus(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SwitchRole_DelegatesToPgmClient()
    {
        var response = ApiResponse<LoginResponse>.SuccessResult(new LoginResponse { AccessToken = "new-tok" });
        _pgmAuth.SwitchRoleAsync(Arg.Any<SwitchRoleRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.SwitchRole(new SwitchRoleRequest { RoleId = "ADMIN" }, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
    }

    [Fact]
    public async Task ChangePassword_DelegatesToPgmClient()
    {
        var response = ApiResponse<object>.SuccessResult(new object());
        _pgmAuth.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.ChangePassword(
            new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
    }
}
