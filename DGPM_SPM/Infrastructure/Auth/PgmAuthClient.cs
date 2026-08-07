using System.Net.Http.Json;
using System.Text.Json;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Common.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DGPM_SPM.Infrastructure.Auth;

/// <summary>將 Auth API 轉發至 PGM（DGPM 唯一 Auth 路徑）。</summary>
public class PgmAuthClient : IPgmAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly AuthOptions _options;
    private readonly ILogger<PgmAuthClient> _logger;

    public PgmAuthClient(
        HttpClient http,
        IOptions<AuthOptions> options,
        ILogger<PgmAuthClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (!_options.AllowPGMLoginEntry)
        {
            return ApiResponse<LoginResponse>.ErrorResult(
                "AUTH_ENTRY_DISABLED",
                "目前不允許由業務系統登入，請使用指定入口。");
        }

        request.SystemCode = string.IsNullOrWhiteSpace(request.SystemCode)
            ? _options.SystemCode
            : request.SystemCode.Trim();

        return await SendAsync<LoginResponse>(
            HttpMethod.Post, "api/auth/login", request, stripAuthorization: true, ct);
    }

    public Task<ApiResponse<object>> LogoutAsync(CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/auth/logout", new { }, stripAuthorization: false, ct);

    public Task<ApiResponse<LoginResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken ct = default)
        => SendAsync<LoginResponse>(
            HttpMethod.Post, "api/auth/refresh", request, stripAuthorization: true, ct);

    public Task<ApiResponse<UserInfoDto>> GetMeAsync(CancellationToken ct = default)
        => SendAsync<UserInfoDto>(HttpMethod.Get, "api/auth/me", body: null, stripAuthorization: false, ct);

    public Task<ApiResponse<List<MenuDto>>> GetMenusAsync(CancellationToken ct = default)
        => SendAsync<List<MenuDto>>(HttpMethod.Get, "api/auth/menus", body: null, stripAuthorization: false, ct);

    public Task<ApiResponse<LoginResponse>> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken ct = default)
        => SendAsync<LoginResponse>(
            HttpMethod.Post, "api/auth/switch-role", request, stripAuthorization: false, ct);

    public Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        => SendAsync<object>(
            HttpMethod.Post, "api/auth/change-password", request, stripAuthorization: false, ct);

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool stripAuthorization,
        CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(method, path);
            if (stripAuthorization)
                req.Headers.Authorization = null;

            if (body is not null)
                req.Content = JsonContent.Create(body);

            using var resp = await _http.SendAsync(req, ct);
            var status = (int)resp.StatusCode;
            var payload = await resp.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);

            _logger.LogInformation(
                "PGM forward {Method} {Path} => HTTP {Status}; code={Code}; hasData={HasData}; anonymous={Anonymous}",
                method.Method,
                path,
                status,
                payload?.Code,
                payload is { Data: not null },
                stripAuthorization);

            if (payload is not null)
                return payload;

            return ApiResponse<T>.ErrorResult(
                "PGM_UNAVAILABLE",
                $"權限平台回應異常（HTTP {status}）。");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "PGM forward {Method} {Path} connection failed", method.Method, path);
            return ApiResponse<T>.ErrorResult(
                "PGM_UNAVAILABLE",
                "無法連線至權限平台（PGM），請稍後再試或聯絡管理員。");
        }
    }
}
