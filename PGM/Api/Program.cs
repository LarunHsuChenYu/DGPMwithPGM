using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PGM.Api.Infrastructure;
using PGM.Api.IoC;
using PGM.Api.Middleware;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Common.Extensions;
using PGM.Core.Common.Jwt;
using PGM.Infrastructure.Persistence;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);
var env = builder.Configuration["env:name"];

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

AgentDebugLog("H0", "Api/Program.cs:entry", "Api startup begin", new
{
    baseDir = AppContext.BaseDirectory,
    contentRoot = builder.Environment.ContentRootPath,
    aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    envName = env,
    hasJwtSecretEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JwtSettings__SecretKey")),
    hasConnEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"))
});
// #endregion

try
{
    // #region agent log
    AgentDebugLog("H_B", "Api/Program.cs:before-nlog", "Configuring NLog");
    // #endregion

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // #region agent log
    AgentDebugLog("H_B", "Api/Program.cs:after-nlog", "NLog configured OK");
    // #endregion

    logger.Info("PGM.Api starting up");

    // ---------- Dapper 欄位對應（一次性註冊）----------
    DapperTypeMapConfig.Register();

    // ---------- Controllers ----------
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IRequestContext, RequestContext>();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddMemoryCache();

    // ---------- JWT 設定（缺漏 SecretKey 直接 throw）----------
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
    if (jwtSettings is null)
        throw new InvalidOperationException("JwtSettings section is missing.");

    // #region agent log
    AgentDebugLog("H_A", "Api/Program.cs:jwt-check", "JwtSettings loaded", new
    {
        sectionPresent = true,
        secretPresent = !string.IsNullOrWhiteSpace(jwtSettings.SecretKey),
        secretLength = jwtSettings.SecretKey.Length,
        issuer = jwtSettings.Issuer,
        audience = jwtSettings.Audience
    });
    // #endregion

    var secretKey = jwtSettings.SecretKey;
    if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        throw new InvalidOperationException("JwtSettings:SecretKey missing or too short (min 32 chars).");

    // Fail-fast：連線字串僅在 Development 會來自 User Secrets；正式環境必須用環境變數／IIS 提供。
    // Web 不直連 DB；若此處缺漏，使用者會在 Web 看到「無法連線／系統錯誤」而非明確的 DB 設定訊息。
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is missing. " +
            "Development: dotnet user-secrets set --project Api. " +
            "Production/IIS: set ConnectionStrings__DefaultConnection (Api 站台／應用程式集區).");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 匿名登入／refresh：移除誤帶的 Authorization，避免失效 JWT 走認證失敗路徑。
                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/api/auth/login")
                    || path.StartsWithSegments("/api/auth/refresh"))
                {
                    context.Request.Headers.Remove("Authorization");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var result = ApiResponse<object>.ErrorResult(
                    ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                    ErrorCodes.UnauthorizedAccess.GetDescription("message"));

                return context.Response.WriteAsJsonAsync(result);
            }
        };
    });

    // ---------- Swagger（預設關閉；appsettings EnableSwagger=true 可重開）----------
    var enableSwagger = builder.Configuration.GetValue("EnableSwagger", false);
    if (enableSwagger)
    {
        builder.Services.AddSwaggerGen();
    }

    // ---------- 註冊 Core Service / Mapper / Infrastructure ----------
    builder.Services.Register();

    var app = builder.Build();

    // ---------- Pipeline ----------
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PGM API V1");
            c.RoutePrefix = string.Empty;
            c.DefaultModelsExpandDepth(-1);
        });
    }

    app.UseHttpsRedirection();

    // 全域例外處理保持在應用 middleware 前方
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseMiddleware<TracingMiddleware>();

    // HTTP 請求摘要（取代原 Serilog RequestLogging）
    app.Use(async (context, next) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            sw.Stop();
            var status = context.Response.StatusCode;
            var level = status >= 500 ? NLog.LogLevel.Error
                : status >= 400 ? NLog.LogLevel.Warn
                : NLog.LogLevel.Info;
            logger.Log(level,
                "HTTP {0} {1} => {2} in {3}ms",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                sw.ElapsedMilliseconds);
        }
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // #region agent log
    AgentDebugLog("H0", "Api/Program.cs:before-run", "Api pipeline built; calling Run()");
    // #endregion

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // #region agent log
    AgentDebugLog("H_FAIL", "Api/Program.cs:catch", "Api start-up failed", new
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

// 供 Integration.Tests 的 WebApplicationFactory<Program> 參考（top-level statements）
public partial class Program { }
