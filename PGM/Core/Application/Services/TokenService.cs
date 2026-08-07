using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Jwt;

namespace PGM.Core.Application.Services;

[ScopedRegistration]
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string AccessToken, string RefreshToken, DateTime ExpiresAt) CreateTokens(
        UserInfoDto user,
        IEnumerable<string> functionIds,
        string? sessionGuid = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
            throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters.");

        var minutes = _settings.AccessTokenMinutes > 0
            ? _settings.AccessTokenMinutes
            : _settings.ExpirationHours * 60;

        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new(JwtClaimNames.UserId, user.UserId),
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(sessionGuid))
            claims.Add(new Claim(JwtClaimNames.SessionId, sessionGuid));

        if (!string.IsNullOrWhiteSpace(user.RoleId))
            claims.Add(new Claim(JwtClaimNames.RoleId, user.RoleId));
        if (!string.IsNullOrWhiteSpace(user.RoleName))
            claims.Add(new Claim(JwtClaimNames.RoleName, user.RoleName));
        if (!string.IsNullOrWhiteSpace(user.DepartmentCode))
            claims.Add(new Claim(JwtClaimNames.Department, user.DepartmentCode));
        if (!string.IsNullOrWhiteSpace(user.FactoryNo))
            claims.Add(new Claim(JwtClaimNames.Factory, user.FactoryNo));
        if (!string.IsNullOrWhiteSpace(user.SystemCode))
            claims.Add(new Claim(JwtClaimNames.SystemCode, user.SystemCode));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return (accessToken, refreshToken, expiresAt);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        // 無效或格式錯誤的 token 是可預期的驗證失敗；其他例外不得吞掉。
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
