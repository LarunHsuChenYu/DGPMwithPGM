using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>
/// dbo.DIM_ROLE 與角色授權鏈（MAP_ROLE_FUNCTION → SET_FUNCTION；BMW／SRS）。
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IDbSession _session;

    public RoleRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Role>> GetAllByUserIdAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DR.ROLE_ID,
                   DR.ROLE_NAME,
                   DR.SYSTEM_CODE,
                   DR.DEL_FLG,
                   DR.CRT_DATE,
                   DR.CRT_USER,
                   DR.MDF_DATE,
                   DR.MDF_USER
            FROM dbo.DIM_ROLE AS DR
                INNER JOIN dbo.MAP_USER_ROLE AS MUR
                    ON MUR.ROLE_ID = DR.ROLE_ID
            WHERE DR.DEL_FLG = 0
                AND MUR.USER_ID = @UserId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { UserId = userId }, _session.CurrentTransaction, cancellationToken: ct);
        var result = await conn.QueryAsync<Role>(cmd);
        return result.ToList();
    }

    public async Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT ROLE_ID,
                   ROLE_NAME,
                   SYSTEM_CODE,
                   DEL_FLG,
                   CRT_DATE,
                   CRT_USER,
                   MDF_DATE,
                   MDF_USER
            FROM dbo.DIM_ROLE
            WHERE DEL_FLG = 0
            ORDER BY ROLE_NAME, ROLE_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            transaction: _session.CurrentTransaction,
            cancellationToken: ct);
        var result = await conn.QueryAsync<Role>(cmd);
        return result.ToList();
    }

    public async Task<PagedResult<Role>> GetPagedAsync(
        RoleFilter filter,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.DIM_ROLE AS R
            WHERE (@Keyword IS NULL
                   OR R.ROLE_ID LIKE '%' + @Keyword + '%'
                   OR R.ROLE_NAME LIKE '%' + @Keyword + '%')
              AND (@IsActive IS NULL
                   OR R.DEL_FLG = CASE WHEN @IsActive = 1 THEN 0 ELSE 1 END);

            SELECT R.ROLE_ID,
                   R.ROLE_NAME,
                   R.SYSTEM_CODE,
                   R.DEL_FLG,
                   R.CRT_DATE,
                   R.CRT_USER,
                   R.MDF_DATE,
                   R.MDF_USER
            FROM dbo.DIM_ROLE AS R
            WHERE (@Keyword IS NULL
                   OR R.ROLE_ID LIKE '%' + @Keyword + '%'
                   OR R.ROLE_NAME LIKE '%' + @Keyword + '%')
              AND (@IsActive IS NULL
                   OR R.DEL_FLG = CASE WHEN @IsActive = 1 THEN 0 ELSE 1 END)
            ORDER BY R.ROLE_ID
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim(),
            filter.IsActive,
            filter.RowSkip,
            filter.PageSize
        };

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            parameters,
            _session.CurrentTransaction,
            cancellationToken: ct);
        using var result = await conn.QueryMultipleAsync(cmd);
        var totalRow = await result.ReadSingleAsync<int>();
        var roles = (await result.ReadAsync<Role>()).ToList();

        return new PagedResult<Role>
        {
            Datas = roles,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Role?> GetByIdAsync(string roleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                   ROLE_ID,
                   ROLE_NAME,
                   SYSTEM_CODE,
                   DEL_FLG,
                   CRT_DATE,
                   CRT_USER,
                   MDF_DATE,
                   MDF_USER
            FROM dbo.DIM_ROLE
            WHERE ROLE_ID = @RoleId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RoleId = roleId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<Role>(cmd);
    }

    public async Task<bool> ExistsAsync(string roleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.DIM_ROLE WHERE ROLE_ID = @RoleId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RoleId = roleId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task<int> AddAsync(Role entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.DIM_ROLE
                (ROLE_ID, ROLE_NAME, SYSTEM_CODE, DEL_FLG, CRT_DATE, CRT_USER)
            VALUES
                (@RoleId, @RoleName, @SystemCode, @DelFlg, @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateAsync(Role entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.DIM_ROLE
            SET ROLE_NAME = @RoleName,
                SYSTEM_CODE = @SystemCode,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE ROLE_ID = @RoleId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateStatusAsync(Role entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.DIM_ROLE
            SET DEL_FLG = @DelFlg,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE ROLE_ID = @RoleId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<IReadOnlyList<string>> GetGrantedFunctionIdsAsync(
        string roleId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT FUNCTION_ID
            FROM dbo.MAP_ROLE_FUNCTION
            WHERE ROLE_ID = @RoleId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RoleId = roleId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        var result = await conn.QueryAsync<string>(cmd);
        return result.ToList();
    }

    public async Task<bool> IsFunctionReferencedAsync(string funId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.MAP_ROLE_FUNCTION
                WHERE FUNCTION_ID = @FunId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { FunId = funId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task ReplaceFunctionsAsync(
        string roleId,
        IReadOnlyCollection<string> functionIds,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default)
    {
        // SRS：對角色全量覆寫 MAP_ROLE_FUNCTION（刪後插）。
        const string deleteSql = """
            DELETE FROM dbo.MAP_ROLE_FUNCTION WHERE ROLE_ID = @RoleId;
            """;
        const string insertSql = """
            INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
            VALUES (@RoleId, @FunctionId, @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var deleteCmd = new CommandDefinition(
            deleteSql,
            new { RoleId = roleId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        if (functionIds.Count == 0)
            return;

        var rows = functionIds.Select(functionId => new
        {
            RoleId = roleId,
            FunctionId = functionId,
            CrtDate = auditDate,
            CrtUser = auditUser
        });
        var insertCmd = new CommandDefinition(
            insertSql,
            rows,
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(insertCmd);
    }
}
