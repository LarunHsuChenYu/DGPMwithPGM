using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using DGPM_SPM.Web.Models;

namespace DGPM_SPM.Web.Services;

/// <summary>
/// 自訂認證狀態：由 AuthTokenStore（protected browser session storage）還原登入狀態，
/// 登入/登出時透過 NotifyAuthenticationStateChanged 即時更新 AuthorizeRouteView 與 AuthorizeView。
/// </summary>
public class SpmAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AuthenticationType = "SpmJwt";

    private readonly AuthTokenStore _tokenStore;

    public SpmAuthenticationStateProvider(AuthTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _tokenStore.GetAsync();
        return new AuthenticationState(BuildPrincipal(session));
    }

    /// <summary>登入成功後呼叫：保存 session 並通知認證狀態變更。</summary>
    public async Task SignInAsync(AuthSession session)
    {
        await _tokenStore.SetAsync(session);
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(BuildPrincipal(session))));
    }

    /// <summary>登出（或偵測到 token 失效）時呼叫：清除 session 並通知認證狀態變更。</summary>
    public async Task SignOutAsync()
    {
        await _tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(BuildPrincipal(null))));
    }

    private static ClaimsPrincipal BuildPrincipal(AuthSession? session)
    {
        if (session is null || session.IsExpired || string.IsNullOrWhiteSpace(session.AccessToken))
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.UserId),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(session.User.UserName)
                ? session.User.UserId
                : session.User.UserName)
        };

        if (!string.IsNullOrWhiteSpace(session.User.RoleName))
            claims.Add(new Claim(ClaimTypes.Role, session.User.RoleName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
    }
}
