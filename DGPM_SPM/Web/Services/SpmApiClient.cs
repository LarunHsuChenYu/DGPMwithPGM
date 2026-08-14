using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DGPM_SPM.Web.Models;

namespace DGPM_SPM.Web.Services;

/// <summary>
/// DGPM_SPM Api 的 HTTP client。Scoped（每 circuit 一份）：
/// 每次請求由 AuthTokenStore 取出 JWT 自動帶 Authorization: Bearer；
/// 連線失敗、401、業務錯誤一律轉為繁中友善訊息（ApiResult），不洩漏內部細節。
/// 業務頁面 worker 請沿用 GetAsync / PostAsync 泛型方法呼叫後續新增的業務 API。
/// </summary>
public class SpmApiClient
{
    public const string ClientName = "SpmApi";

    private const string ConnectionErrorMessage = "無法連線至伺服器，請稍後再試或聯絡系統管理員。";
    private const string ServerErrorMessage = "系統處理發生錯誤，請稍後再試或聯絡系統管理員。";
    private const string UnauthorizedMessage = "登入已過期或無存取權限，請重新登入。";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthTokenStore _tokenStore;
    private readonly ILogger<SpmApiClient> _logger;

    public SpmApiClient(
        IHttpClientFactory httpClientFactory,
        AuthTokenStore tokenStore,
        ILogger<SpmApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    // ---------- Auth API ----------

    /// <summary>POST /api/auth/login（匿名）。失敗時回傳登入情境的友善訊息。</summary>
    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 登入前清掉記憶體中可能殘留的舊 token，避免同 circuit 誤帶 Authorization。
        await _tokenStore.ClearAsync();

        var result = await SendAsync<LoginResponse>(
            HttpMethod.Post, "api/auth/login", request, requireAuth: false, ct);

        if (result.Succeeded)
            return result;

        return ApiResult<LoginResponse>.Fail(
            MapLoginError(result.ErrorCode, result.ErrorMessage),
            result.ErrorCode,
            unauthorized: false,
            result.TraceId,
            result.HttpStatusCode);
    }

    /// <summary>POST /api/auth/logout（Bearer）。</summary>
    public Task<ApiResult<object>> LogoutAsync(CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/auth/logout", body: null, requireAuth: true, ct);

    /// <summary>GET /api/auth/me（Bearer）。</summary>
    public Task<ApiResult<UserInfoDto>> GetMeAsync(CancellationToken ct = default)
        => SendAsync<UserInfoDto>(HttpMethod.Get, "api/auth/me", body: null, requireAuth: true, ct);

    /// <summary>GET /api/auth/menus（Bearer）。</summary>
    public Task<ApiResult<List<MenuDto>>> GetMenusAsync(CancellationToken ct = default)
        => SendAsync<List<MenuDto>>(HttpMethod.Get, "api/auth/menus", body: null, requireAuth: true, ct);

    // ---------- Exchange Rate API ----------

    public Task<ApiResult<ExchangeRatePage>> GetExchangeRatesAsync(
        string? currencyCode,
        string? rateYmFrom,
        string? rateYmTo,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        AddQuery(query, "currencyCode", currencyCode);
        AddQuery(query, "rateYmFrom", rateYmFrom);
        AddQuery(query, "rateYmTo", rateYmTo);
        AddQuery(query, "status", status);

        return GetAsync<ExchangeRatePage>($"api/exchange-rates?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<ExchangeRateDto>> CreateExchangeRateAsync(
        SaveExchangeRateRequest request,
        CancellationToken ct = default)
        => PostAsync<ExchangeRateDto>("api/exchange-rates", request, ct);

    public Task<ApiResult<ExchangeRateDto>> UpdateExchangeRateAsync(
        int rateId,
        SaveExchangeRateRequest request,
        CancellationToken ct = default)
        => PutAsync<ExchangeRateDto>($"api/exchange-rates/{rateId}", request, ct);

    public Task<ApiResult<ExchangeRateDto>> SetExchangeRateStatusAsync(
        int rateId,
        string status,
        CancellationToken ct = default)
        => PutAsync<ExchangeRateDto>(
            $"api/exchange-rates/{rateId}/status",
            new SetExchangeRateStatusRequest { Status = status },
            ct);

    // ---------- KPI Indicator API ----------

    public Task<ApiResult<KpiIndicatorPage>> GetKpiIndicatorsAsync(
        string? keyword,
        string? dataType,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "dataType", dataType);
        AddQuery(query, "status", status);

        return GetAsync<KpiIndicatorPage>($"api/kpi/indicators?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<KpiIndicatorDto>> CreateKpiIndicatorAsync(
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default)
        => PostAsync<KpiIndicatorDto>("api/kpi/indicators", request, ct);

    public Task<ApiResult<KpiIndicatorDto>> UpdateKpiIndicatorAsync(
        int indicatorId,
        SaveKpiIndicatorRequest request,
        CancellationToken ct = default)
        => PutAsync<KpiIndicatorDto>($"api/kpi/indicators/{indicatorId}", request, ct);

    public Task<ApiResult<KpiIndicatorDto>> SetKpiIndicatorStatusAsync(
        int indicatorId,
        string status,
        CancellationToken ct = default)
        => PutAsync<KpiIndicatorDto>(
            $"api/kpi/indicators/{indicatorId}/status",
            new SetKpiIndicatorStatusRequest { Status = status },
            ct);

    // ---------- KPI Import API ----------

    public Task<ApiResult<KpiImportResultDto>> CreateKpiImportAsync(
        CreateKpiImportRequest request,
        CancellationToken ct = default)
        => PostAsync<KpiImportResultDto>("api/kpi/imports", request, ct);

    public Task<ApiResult<KpiImportBatchPage>> GetKpiImportBatchesAsync(
        string? periodYm,
        string? importStatus,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        AddQuery(query, "periodYm", periodYm);
        AddQuery(query, "importStatus", importStatus);

        return GetAsync<KpiImportBatchPage>($"api/kpi/imports?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<KpiImportBatchDto>> GetKpiImportBatchAsync(
        long batchId,
        CancellationToken ct = default)
        => GetAsync<KpiImportBatchDto>($"api/kpi/imports/{batchId}", ct);

    // ---------- KPI Data Permission API ----------

    public Task<ApiResult<KpiUserPermissionDto>> GetKpiUserPermissionAsync(
        string userId,
        CancellationToken ct = default)
        => GetAsync<KpiUserPermissionDto>(
            $"api/kpi/data-permissions/{Uri.EscapeDataString(userId.Trim())}", ct);

    public Task<ApiResult<KpiUserPermissionDto>> SaveKpiUserPermissionAsync(
        string userId,
        SaveKpiUserPermissionRequest request,
        CancellationToken ct = default)
        => PutAsync<KpiUserPermissionDto>(
            $"api/kpi/data-permissions/{Uri.EscapeDataString(userId.Trim())}", request, ct);

    // ---------- KPI Review API ----------

    public Task<ApiResult<KpiDataPage>> GetKpiReviewDataAsync(
        string? periodYm,
        string? keyword,
        string? reviewStatus,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        AddQuery(query, "periodYm", periodYm);
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "reviewStatus", reviewStatus);

        return GetAsync<KpiDataPage>($"api/kpi/review/data?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<KpiDataDto>> ReviewKpiDataAsync(
        long dataId,
        ReviewKpiDataRequest request,
        CancellationToken ct = default)
        => PutAsync<KpiDataDto>($"api/kpi/review/data/{dataId}/review", request, ct);

    public Task<ApiResult<KpiDataDto>> UnlockKpiDataAsync(
        long dataId,
        UnlockKpiDataRequest request,
        CancellationToken ct = default)
        => PutAsync<KpiDataDto>($"api/kpi/review/data/{dataId}/unlock", request, ct);

    // ---------- KPI Change Log Query API（系統資料查詢 / KPI 異動紀錄查詢）----------

    public Task<ApiResult<PagedResult<KpiChangeLogDto>>> GetKpiChangeLogsAsync(
        string? periodYm,
        string? keyword,
        string? actionType,
        DateTime? actionDateFrom,
        DateTime? actionDateTo,
        string? actionUser,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        AddQuery(query, "periodYm", periodYm);
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "actionType", actionType);
        AddQuery(query, "actionDateFrom", actionDateFrom?.ToString("yyyy-MM-dd"));
        AddQuery(query, "actionDateTo", actionDateTo?.ToString("yyyy-MM-dd"));
        AddQuery(query, "actionUser", actionUser);

        return GetAsync<PagedResult<KpiChangeLogDto>>($"api/query/kpi-changes?{string.Join('&', query)}", ct);
    }

    // ---------- System Auth（轉發 PGM；PgmUiMode=Off）----------

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        => PostAsync<object>("api/auth/change-password", request, ct);

    public Task<ApiResult<PgmUiModeDto>> GetPgmUiModeAsync(CancellationToken ct = default)
        => GetAsync<PgmUiModeDto>("api/system/ui-mode", ct);

    public Task<ApiResult<PgmUiModeDto>> SetPgmUiModeAsync(string mode, CancellationToken ct = default)
        => PutAsync<PgmUiModeDto>("api/system/ui-mode", new { mode }, ct);

    public Task<ApiResult<PagedResult<UserAccountDto>>> GetUserAccountsAsync(
        string? keyword, bool? isActive, string? roleId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "isActive", isActive?.ToString().ToLowerInvariant());
        AddQuery(query, "roleId", roleId);
        return GetAsync<PagedResult<UserAccountDto>>($"api/system/users?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<UserAccountDto>> GetUserAccountAsync(string userId, CancellationToken ct = default)
        => GetAsync<UserAccountDto>($"api/system/users/{Uri.EscapeDataString(userId)}", ct);

    public Task<ApiResult<IReadOnlyList<RoleOptionDto>>> GetUserRoleOptionsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<RoleOptionDto>>("api/system/users/role-options", ct);

    public Task<ApiResult<UserAccountDto>> CreateUserAccountAsync(CreateUserAccountRequest request, CancellationToken ct = default)
        => PostAsync<UserAccountDto>("api/system/users", request, ct);

    public Task<ApiResult<UserAccountDto>> UpdateUserAccountAsync(string userId, UpdateUserAccountRequest request, CancellationToken ct = default)
        => PutAsync<UserAccountDto>($"api/system/users/{Uri.EscapeDataString(userId)}", request, ct);

    public Task<ApiResult<bool>> SetUserAccountStatusAsync(string userId, bool isActive, CancellationToken ct = default)
        => PutAsync<bool>($"api/system/users/{Uri.EscapeDataString(userId)}/status", new UserAccountStatusRequest { IsActive = isActive }, ct);

    public Task<ApiResult<object>> AdminResetPasswordAsync(string userId, string? newPassword = null, CancellationToken ct = default)
        => PutAsync<object>($"api/system/users/{Uri.EscapeDataString(userId)}/reset-password", new { newPassword }, ct);

    public Task<ApiResult<PagedResult<RoleDto>>> GetRolesAsync(string? keyword, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "isActive", isActive?.ToString().ToLowerInvariant());
        return GetAsync<PagedResult<RoleDto>>($"api/system/roles?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<RoleDto>> CreateRoleAsync(SaveRoleRequest request, CancellationToken ct = default)
        => PostAsync<RoleDto>("api/system/roles", request, ct);

    public Task<ApiResult<RoleDto>> UpdateRoleAsync(string roleId, SaveRoleRequest request, CancellationToken ct = default)
        => PutAsync<RoleDto>($"api/system/roles/{Uri.EscapeDataString(roleId)}", request, ct);

    public Task<ApiResult<bool>> SetRoleStatusAsync(string roleId, bool isActive, CancellationToken ct = default)
        => PutAsync<bool>($"api/system/roles/{Uri.EscapeDataString(roleId)}/status", new RoleStatusRequest { IsActive = isActive }, ct);

    public Task<ApiResult<RolePermissionsDto>> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
        => GetAsync<RolePermissionsDto>($"api/system/roles/{Uri.EscapeDataString(roleId)}/permissions", ct);

    public Task<ApiResult<bool>> SaveRolePermissionsAsync(string roleId, SaveRolePermissionsRequest request, CancellationToken ct = default)
        => PutAsync<bool>($"api/system/roles/{Uri.EscapeDataString(roleId)}/permissions", request, ct);

    public Task<ApiResult<PagedResult<AuthenticationLogDto>>> GetLoginHistoryAsync(
        string? keyword, DateTime? loginDateFrom, DateTime? loginDateTo, string? authStatus,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "loginDateFrom", loginDateFrom?.ToString("yyyy-MM-dd"));
        AddQuery(query, "loginDateTo", loginDateTo?.ToString("yyyy-MM-dd"));
        AddQuery(query, "authStatus", authStatus);
        return GetAsync<PagedResult<AuthenticationLogDto>>($"api/query/login-history?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<PagedResult<FunctionDto>>> GetFunctionListAsync(
        string? keyword, string? parentId, string? actionType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        AddQuery(query, "keyword", keyword);
        AddQuery(query, "parentId", parentId);
        AddQuery(query, "actionType", actionType);
        return GetAsync<PagedResult<FunctionDto>>($"api/permission/function-list?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<List<FunctionOptionDto>>> GetFunctionParentOptionsAsync(CancellationToken ct = default)
        => GetAsync<List<FunctionOptionDto>>("api/permission/function-list/parent-options", ct);

    public Task<ApiResult<List<FunctionOptionDto>>> GetFunctionOptionsAsync(string? excludeFunId, CancellationToken ct = default)
    {
        var url = "api/permission/function-list/options";
        if (!string.IsNullOrWhiteSpace(excludeFunId))
            url += $"?excludeFunId={Uri.EscapeDataString(excludeFunId)}";
        return GetAsync<List<FunctionOptionDto>>(url, ct);
    }

    public Task<ApiResult<FunctionDto>> CreateFunctionAsync(SaveFunctionRequest request, CancellationToken ct = default)
        => PostAsync<FunctionDto>("api/permission/function-list", request, ct);

    public Task<ApiResult<FunctionDto>> UpdateFunctionAsync(string funId, SaveFunctionRequest request, CancellationToken ct = default)
        => PutAsync<FunctionDto>($"api/permission/function-list/{Uri.EscapeDataString(funId)}", request, ct);

    public Task<ApiResult<bool>> CanDeleteFunctionAsync(string funId, CancellationToken ct = default)
        => GetAsync<bool>($"api/permission/function-list/{Uri.EscapeDataString(funId)}/can-delete", ct);

    public Task<ApiResult<bool>> DeleteFunctionAsync(string funId, CancellationToken ct = default)
        => DeleteAsync<bool>($"api/permission/function-list/{Uri.EscapeDataString(funId)}", ct);

    public Task<ApiResult<IReadOnlyList<ParameterCategoryDto>>> GetParameterCategoriesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<ParameterCategoryDto>>("api/system/parameters/categories", ct);

    public Task<ApiResult<IReadOnlyList<ParameterDto>>> GetParametersByCategoryAsync(string setItem, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<ParameterDto>>($"api/system/parameters/{Uri.EscapeDataString(setItem)}", ct);

    public Task<ApiResult<int>> GetParameterNextSortOrderAsync(string setItem, CancellationToken ct = default)
        => GetAsync<int>($"api/system/parameters/{Uri.EscapeDataString(setItem)}/next-sort-order", ct);

    public Task<ApiResult<ParameterDto>> CreateParameterAsync(CreateParameterRequest request, CancellationToken ct = default)
        => PostAsync<ParameterDto>("api/system/parameters", request, ct);

    public Task<ApiResult<ParameterDto>> UpdateParameterAsync(string setItem, string setId, UpdateParameterRequest request, CancellationToken ct = default)
        => PutAsync<ParameterDto>($"api/system/parameters/{Uri.EscapeDataString(setItem)}/{Uri.EscapeDataString(setId)}", request, ct);

    public Task<ApiResult<bool>> DeleteParameterAsync(string setItem, string setId, CancellationToken ct = default)
        => DeleteAsync<bool>($"api/system/parameters/{Uri.EscapeDataString(setItem)}/{Uri.EscapeDataString(setId)}", ct);

    // ---------- 供後續業務頁面使用的泛型方法 ----------

    public Task<ApiResult<T>> GetAsync<T>(string relativeUrl, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Get, relativeUrl, body: null, requireAuth: true, ct);

    public Task<ApiResult<T>> PostAsync<T>(string relativeUrl, object? body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, relativeUrl, body, requireAuth: true, ct);

    public Task<ApiResult<T>> PutAsync<T>(string relativeUrl, object? body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Put, relativeUrl, body, requireAuth: true, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string relativeUrl, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Delete, relativeUrl, body: null, requireAuth: true, ct);

    // ---------- 核心傳送邏輯 ----------

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string relativeUrl, object? body, bool requireAuth, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);

            if (body is not null)
                request.Content = JsonContent.Create(body);

            if (requireAuth)
            {
                var session = await _tokenStore.GetAsync();
                if (session is null || session.IsExpired)
                    return ApiResult<T>.Fail(UnauthorizedMessage, unauthorized: true);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", session.AccessToken);
            }
            else
            {
                // 匿名呼叫（login／refresh）絕不可帶上一任 JWT。
                request.Headers.Authorization = null;
            }

            var client = _httpClientFactory.CreateClient(ClientName);
            var baseAddress = client.BaseAddress?.ToString() ?? "(unset)";
            using var response = await client.SendAsync(request, ct);

            var payload = await TryReadPayloadAsync<T>(response, ct);
            var status = (int)response.StatusCode;
            var traceId = payload?.TraceId;

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(
                    "API {Method} {Url} 未授權 HTTP {StatusCode}; base={Base}; code={Code}; traceId={TraceId}",
                    method, relativeUrl, status, baseAddress, payload?.Code, traceId);
                return ApiResult<T>.Fail(
                    UnauthorizedMessage, payload?.Code ?? string.Empty, unauthorized: true, traceId, status);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "API {Method} {Url} 非成功 HTTP {StatusCode}; base={Base}; code={Code}; message={Message}; traceId={TraceId}",
                    method, relativeUrl, status, baseAddress, payload?.Code, payload?.Message, traceId);
                return ApiResult<T>.Fail(
                    MapHttpError(status, payload?.Code, payload?.Message),
                    payload?.Code ?? status.ToString(),
                    traceId: traceId,
                    httpStatusCode: status);
            }

            if (payload is null)
            {
                _logger.LogWarning(
                    "API {Method} {Url} HTTP {StatusCode} 但無法解析 JSON; base={Base}",
                    method, relativeUrl, status, baseAddress);
                return ApiResult<T>.Fail(ServerErrorMessage, httpStatusCode: status);
            }

            if (payload.Code != ApiResponseCodes.Success)
            {
                _logger.LogWarning(
                    "API {Method} {Url} 業務錯誤 code={Code}; message={Message}; traceId={TraceId}; base={Base}",
                    method, relativeUrl, payload.Code, payload.Message, payload.TraceId, baseAddress);
                return ApiResult<T>.Fail(
                    PreferApiMessage(payload.Code, payload.Message),
                    payload.Code,
                    traceId: payload.TraceId,
                    httpStatusCode: status);
            }

            return ApiResult<T>.Ok(payload.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API {Method} {Url} 連線失敗", method, relativeUrl);
            return ApiResult<T>.Fail(ConnectionErrorMessage);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("API {Method} {Url} 逾時", method, relativeUrl);
            return ApiResult<T>.Fail(ConnectionErrorMessage);
        }
    }

    private static async Task<ApiResponse<T>?> TryReadPayloadAsync<T>(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        }
        catch (JsonException)
        {
            // 非預期的回應內容（例如 proxy 的 HTML 錯誤頁）：交由呼叫端以狀態碼判斷。
            return null;
        }
    }

    /// <summary>業務錯誤：優先顯示後端 Message（驗證細節），否則回落友善預設文案。</summary>
    private static string PreferApiMessage(string code, string? apiMessage)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage) &&
            !apiMessage.Equals("Success", StringComparison.OrdinalIgnoreCase) &&
            code is "200" or "300" or "404" or "409")
            return apiMessage.Trim();

        return MapBusinessError(code);
    }

    /// <summary>一般業務錯誤碼 → 繁中友善訊息（對應後端 ErrorCodes 的 code）。</summary>
    private static string MapBusinessError(string code) => code switch
    {
        "200" => "必要參數缺漏或格式錯誤，請確認輸入內容。",
        "300" => "驗證失敗，請確認輸入內容。",
        "400" => UnauthorizedMessage,
        "404" => "資料不存在，可能已被其他使用者異動，請重新查詢。",
        "409" => "資料重複，請確認輸入內容。",
        "9998" => "無使用權限或來源不被允許，請聯絡系統管理員。",
        _ => ServerErrorMessage
    };

    /// <summary>HTTP 非成功：標出狀態碼（尤其 IIS WebDAV 造成的 405），必要時帶後端訊息。</summary>
    private static string MapHttpError(int status, string? code, string? apiMessage)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage) &&
            code is "200" or "300" or "404" or "409")
            return apiMessage.Trim();

        return status switch
        {
            401 => UnauthorizedMessage,
            403 => "無使用權限，請聯絡系統管理員。",
            404 => "資料不存在，可能已被其他使用者異動，請重新查詢。",
            405 => "伺服器拒絕此操作方法（HTTP 405）。若部署於 IIS，請確認 Api 站台已停用 WebDAV 並允許 PUT／DELETE（見 web.config）。",
            502 or 503 or 504 => ConnectionErrorMessage,
            _ => $"{ServerErrorMessage}（HTTP {status}）"
        };
    }

    /// <summary>
    /// 登入情境錯誤碼 → 友善訊息。帳號／密碼錯誤刻意回相同訊息，避免洩漏帳號是否存在。
    /// 不可把 AUTH_NO_ROLE／AUTH_INVALID 誤映成「登入已過期」。
    /// </summary>
    private static string MapLoginError(string code, string fallbackMessage) => code switch
    {
        "200" or "300" or "AUTH_INVALID" => "帳號或密碼錯誤，請重新輸入。",
        "400" or "AUTH_NO_ROLE" => "此帳號目前無此系統可用角色，請聯絡系統管理員至 PGM 設定 DGPM 角色。",
        "AUTH_ENTRY_DISABLED" => "本系統登入入口已停用，請至 PGM 系統登入。",
        "PGM_UNAVAILABLE" => string.IsNullOrWhiteSpace(fallbackMessage)
            ? ConnectionErrorMessage
            : fallbackMessage.Trim(),
        _ when !string.IsNullOrWhiteSpace(fallbackMessage)
             && !string.Equals(fallbackMessage, UnauthorizedMessage, StringComparison.Ordinal)
            => fallbackMessage.Trim(),
        _ when !string.IsNullOrWhiteSpace(code)
            => $"登入失敗（{code}），請稍後再試或聯絡系統管理員。",
        _ => UnauthorizedMessage
    };

    private static void AddQuery(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}
