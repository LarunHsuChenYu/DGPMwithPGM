using Microsoft.AspNetCore.Mvc;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Api.Controllers;

/// <summary>
/// 健康檢查：確認行程存活，並探測 SQL 連線（回傳 Server／Database 名稱供 Debug vs 正式比對，不含帳密）。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IRequestContext _requestContext;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IRequestContext requestContext,
        IDbConnectionFactory connectionFactory,
        ILogger<HealthController> logger)
    {
        _requestContext = requestContext;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct)
    {
        var (server, database) = _connectionFactory.GetTargetInfo();
        var dbOk = false;
        string? dbError = null;

        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            _ = await cmd.ExecuteScalarAsync(ct);
            dbOk = true;
        }
        catch (Exception ex)
        {
            dbError = ex.GetType().Name;
            _logger.LogWarning(ex, "Health DB probe failed for {Server}/{Database}", server, database);
        }

        var payload = new
        {
            Status = dbOk ? "Healthy" : "Degraded",
            Phase = "Phase 0",
            Database = new
            {
                Connected = dbOk,
                Server = server,
                Name = database,
                Error = dbError
            }
        };

        var body = ApiResponse<object>.SuccessResult(payload, traceId: _requestContext.TraceId);
        return dbOk ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
