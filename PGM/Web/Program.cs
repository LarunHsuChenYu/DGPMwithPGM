using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using PGM.Web.Components;
using PGM.Web.Models;
using PGM.Web.Services;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// #region agent log
static void AgentDebugLog(string hypothesisId, string location, string message, object? data = null)
{
    try
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["sessionId"] = "b6fa5d",
            ["runId"] = Environment.GetEnvironmentVariable("DEBUG_RUN_ID") ?? "iis-repro",
            ["hypothesisId"] = hypothesisId,
            ["location"] = location,
            ["message"] = message,
            ["data"] = data,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        var dir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(dir);
        File.AppendAllText(Path.Combine(dir, "debug-b6fa5d.log"), payload + Environment.NewLine);
        var workspaceLog = @"d:\07-DGPM\PGM\debug-b6fa5d.log";
        if (Directory.Exists(Path.GetDirectoryName(workspaceLog)!))
            File.AppendAllText(workspaceLog, payload + Environment.NewLine);
    }
    catch { /* never break startup for debug logging */ }
}

AgentDebugLog("H0", "Web/Program.cs:entry", "Web startup begin", new
{
    baseDir = AppContext.BaseDirectory,
    contentRoot = builder.Environment.ContentRootPath,
    aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    hasPgmApiEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PgmApi__BaseUrl")),
    configBaseUrl = builder.Configuration["PgmApi:BaseUrl"]
});
// #endregion

try
{
    logger.Info("PGM.Web starting up");

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // ---------- 認證狀態（自訂 AuthenticationStateProvider + AuthorizeRouteView）----------
    // AddAuthentication：滿足 AuthorizationMiddleware 對 IAuthenticationService 的依賴。
    // Cookie scheme 僅作為預設 scheme；實際登入狀態仍由 SpmAuthenticationStateProvider + session storage 提供。
    // 必須覆寫 LoginPath：預設 /Account/Login 不存在，未登入存取受保護頁會被導向 404（NotFound + MainLayout）。
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.ReturnUrlParameter = "returnUrl";
        });
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthTokenStore>();
    builder.Services.AddScoped<SpmAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
        sp.GetRequiredService<SpmAuthenticationStateProvider>());

    // ---------- PGM Api HTTP client（BaseUrl 由設定提供）----------
    var apiBaseUrl = builder.Configuration["PgmApi:BaseUrl"]
        ?? throw new InvalidOperationException("PgmApi:BaseUrl is missing.");
    // HttpClient 相對路徑組合：BaseAddress 需以 / 結尾，避免最後一段被吃掉
    if (!apiBaseUrl.EndsWith('/'))
        apiBaseUrl += "/";

    // #region agent log
    AgentDebugLog("H_E", "Web/Program.cs:api-base", "PgmApi BaseUrl resolved", new { apiBaseUrl });
    // #endregion

    builder.Services.AddHttpClient(PgmApiClient.ClientName, client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<PgmApiClient>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    // 相容舊 Cookie 預設路徑或外部書籤：/Account/Login → /login
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var returnUrl = context.Request.Query["ReturnUrl"].FirstOrDefault()
                ?? context.Request.Query["returnUrl"].FirstOrDefault();
            var location = string.IsNullOrWhiteSpace(returnUrl)
                ? "/login"
                : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(location);
            return;
        }

        await next();
    });

    // _Imports.razor 全域 [Authorize] 會讓端點（含 /not-found）帶 authorization metadata，需此 middleware。
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // #region agent log
    AgentDebugLog("H0", "Web/Program.cs:before-run", "Web pipeline built; calling Run()");
    // #endregion

    app.Run();
}
catch (Exception ex)
{
    // #region agent log
    AgentDebugLog("H_FAIL", "Web/Program.cs:catch", "Web start-up failed", new
    {
        type = ex.GetType().FullName,
        message = ex.Message,
        stack = ex.StackTrace?.Split('\n').Take(8).ToArray()
    });
    // #endregion
    try { logger.Error(ex, "Application start-up failed"); } catch { /* nlog may have failed */ }
    throw;
}
finally
{
    LogManager.Shutdown();
}
