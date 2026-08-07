using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>
/// dbo.EMP_USER 查詢（僅供 KPI 資料權限等業務功能；不再支援 Local Auth 帳號維護／登入）。
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbSession _session;

    public UserRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<User?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                USER_ID,
                USER_NAME,
                PASSWORD,
                TIT_NAME,
                EMAIL,
                TELEPHONE,
                FACTORY_NO,
                DPT_CODE,
                DEL_FLG,
                CRT_DATE,
                CRT_USER,
                MDF_DATE,
                MDF_USER
            FROM dbo.EMP_USER
            WHERE USER_ID = @UserId
                AND DEL_FLG = 0
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { UserId = userId }, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<User>(cmd);
    }
}
