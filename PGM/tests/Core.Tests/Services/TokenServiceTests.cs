using Microsoft.Extensions.Options;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Services;
using PGM.Core.Common.Jwt;

namespace PGM.Core.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateSut(string secretKey = "abcdefghijklmnopqrstuvwxyz123456")
        => new(Options.Create(new JwtSettings
        {
            SecretKey = secretKey,
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 60
        }));

    [Fact]
    public void CreateTokens_WithValidSettings_ReturnsTokens()
    {
        var sut = CreateSut();
        var user = new UserInfoDto { UserId = "user1", UserName = "Test" };

        var (accessToken, refreshToken, expiresAt) = sut.CreateTokens(user, ["F001"], Guid.NewGuid().ToString());

        accessToken.ShouldNotBeNullOrWhiteSpace();
        refreshToken.ShouldNotBeNullOrWhiteSpace();
        expiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public void CreateTokens_WithShortSecretKey_Throws()
    {
        var sut = CreateSut("short");
        var user = new UserInfoDto { UserId = "user1", UserName = "Test" };

        Should.Throw<InvalidOperationException>(() =>
            sut.CreateTokens(user, [], null));
    }

    [Fact]
    public void ValidateAccessToken_WithValidToken_ReturnsPrincipal()
    {
        var settings = new JwtSettings
        {
            SecretKey = "abcdefghijklmnopqrstuvwxyz123456",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 60
        };
        var sut = new TokenService(Options.Create(settings));
        var user = new UserInfoDto { UserId = "user1", UserName = "Test" };
        var (accessToken, _, _) = sut.CreateTokens(user, [], null);

        var principal = sut.ValidateAccessToken(accessToken);

        principal.ShouldNotBeNull();
        principal.FindFirst(JwtClaimNames.UserId)?.Value.ShouldBe("user1");
    }

    [Fact]
    public void ValidateAccessToken_WithMalformedToken_ReturnsNull()
    {
        var sut = CreateSut();

        var principal = sut.ValidateAccessToken("not-a-jwt");

        principal.ShouldBeNull();
    }
}
