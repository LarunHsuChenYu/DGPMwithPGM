using System.Security.Claims;
using PGM.Core.Application.Models.Auth;

namespace PGM.Core.Application.Interfaces;

public interface ITokenService
{
    (string AccessToken, string RefreshToken, DateTime ExpiresAt) CreateTokens(
        UserInfoDto user,
        IEnumerable<string> functionIds,
        string? sessionGuid = null);

    ClaimsPrincipal? ValidateAccessToken(string token);
}
