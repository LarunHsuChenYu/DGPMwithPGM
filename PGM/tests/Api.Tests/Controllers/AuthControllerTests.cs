using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Common.Extensions;

namespace PGM.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService);
    }

    // ── Login ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WhenServiceReturnsSuccess_ReturnsOk()
    {
        var successCode = ErrorCodes.Success.GetDescription("code");
        var response = ApiResponse<LoginResponse>.SuccessResult(
            new LoginResponse { AccessToken = "tok", RefreshToken = "ref" });
        _authService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
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
        _authService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Login(new LoginRequest { UserId = "u1", Password = "bad" }, CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    // ── Logout ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Always_ReturnsOk()
    {
        var response = ApiResponse<object>.SuccessResult(new object());
        _authService.LogoutAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Logout(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    // ── Refresh ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WhenDataNotNull_ReturnsOk()
    {
        var response = ApiResponse<LoginResponse>.SuccessResult(
            new LoginResponse { AccessToken = "new-tok", RefreshToken = "new-ref" });
        _authService.RefreshAsync(Arg.Any<RefreshTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Refresh(new RefreshTokenRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_WhenDataIsNull_ReturnsUnauthorized()
    {
        var errorCode = ErrorCodes.UnauthorizedAccess.GetDescription("code");
        var response = ApiResponse<LoginResponse>.ErrorResult(errorCode, "invalid token");
        _authService.RefreshAsync(Arg.Any<RefreshTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.Refresh(new RefreshTokenRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    // ── Me ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_WhenDataNotNull_ReturnsOk()
    {
        var response = ApiResponse<UserInfoDto>.SuccessResult(new UserInfoDto { UserId = "u1" });
        _authService.GetMeAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Me(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Me_WhenDataIsNull_ReturnsUnauthorized()
    {
        var errorCode = ErrorCodes.UnauthorizedAccess.GetDescription("code");
        var response = ApiResponse<UserInfoDto>.ErrorResult(errorCode, "no user");
        _authService.GetMeAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Me(CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    // ── Menus ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Menus_Always_ReturnsOk()
    {
        var response = ApiResponse<List<MenuDto>>.SuccessResult(new List<MenuDto>());
        _authService.GetMenusAsync(Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Menus(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
