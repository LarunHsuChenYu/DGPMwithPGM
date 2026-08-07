using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>dbo.EMP_USER 與 MAP_USER_ROLE（BMW／PGM 欄位）。</summary>
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
                EMAIL,
                TELEPHONE,
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

    public async Task<PagedResult<User>> GetPagedAsync(
        UserAccountFilter filter,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.EMP_USER AS U
            WHERE (@Keyword IS NULL
                   OR U.USER_ID LIKE '%' + @Keyword + '%'
                   OR U.USER_NAME LIKE '%' + @Keyword + '%'
                   OR U.EMAIL LIKE '%' + @Keyword + '%'
                   OR U.DPT_CODE LIKE '%' + @Keyword + '%')
              AND (@IsActive IS NULL
                   OR U.DEL_FLG = CASE WHEN @IsActive = 1 THEN 0 ELSE 1 END)
              AND (@RoleId IS NULL OR EXISTS
                  (SELECT 1
                   FROM dbo.MAP_USER_ROLE AS MUR
                   INNER JOIN dbo.DIM_ROLE AS R ON R.ROLE_ID = MUR.ROLE_ID
                   WHERE MUR.USER_ID = U.USER_ID
                     AND MUR.ROLE_ID = @RoleId
                     AND R.DEL_FLG = 0));

            SELECT U.USER_ID,
                   U.USER_NAME,
                   U.EMAIL,
                   U.TELEPHONE,
                   U.DPT_CODE,
                   U.DEL_FLG,
                   U.CRT_DATE,
                   U.CRT_USER,
                   U.MDF_DATE,
                   U.MDF_USER
            FROM dbo.EMP_USER AS U
            WHERE (@Keyword IS NULL
                   OR U.USER_ID LIKE '%' + @Keyword + '%'
                   OR U.USER_NAME LIKE '%' + @Keyword + '%'
                   OR U.EMAIL LIKE '%' + @Keyword + '%'
                   OR U.DPT_CODE LIKE '%' + @Keyword + '%')
              AND (@IsActive IS NULL
                   OR U.DEL_FLG = CASE WHEN @IsActive = 1 THEN 0 ELSE 1 END)
              AND (@RoleId IS NULL OR EXISTS
                  (SELECT 1
                   FROM dbo.MAP_USER_ROLE AS MUR
                   INNER JOIN dbo.DIM_ROLE AS R ON R.ROLE_ID = MUR.ROLE_ID
                   WHERE MUR.USER_ID = U.USER_ID
                     AND MUR.ROLE_ID = @RoleId
                     AND R.DEL_FLG = 0))
            ORDER BY U.USER_ID
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim(),
            filter.IsActive,
            RoleId = string.IsNullOrWhiteSpace(filter.RoleId) ? null : filter.RoleId.Trim(),
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
        var users = (await result.ReadAsync<User>()).ToList();
        await AttachRolesAsync(users, ct);

        return new PagedResult<User>
        {
            Datas = users,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<User?> GetForManagementAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                   USER_ID,
                   USER_NAME,
                   EMAIL,
                   TELEPHONE,
                   DPT_CODE,
                   DEL_FLG,
                   CRT_DATE,
                   CRT_USER,
                   MDF_DATE,
                   MDF_USER
            FROM dbo.EMP_USER
            WHERE USER_ID = @UserId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        var user = await conn.QuerySingleOrDefaultAsync<User>(cmd);
        if (user is not null)
            await AttachRolesAsync([user], ct);
        return user;
    }

    public async Task<bool> ExistsAsync(string userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.EMP_USER WHERE USER_ID = @UserId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task<int> AddAsync(User entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.EMP_USER
                (USER_ID, USER_NAME, PASSWORD, EMAIL, TELEPHONE,
                 DPT_CODE, DEL_FLG, CRT_DATE, CRT_USER)
            VALUES
                (@UserId, @UserName, @Password, @Email, @Telephone,
                 @DptCode, @DelFlg, @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateAsync(User entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.EMP_USER
            SET USER_NAME = @UserName,
                EMAIL = @Email,
                TELEPHONE = @Telephone,
                DPT_CODE = @DptCode,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE USER_ID = @UserId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateStatusAsync(User entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.EMP_USER
            SET DEL_FLG = @DelFlg,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE USER_ID = @UserId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task UpdatePasswordAsync(
        string userId,
        string passwordHash,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default)
    {
        const string updateSql = """
            UPDATE dbo.EMP_USER
            SET PASSWORD = @PasswordHash,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE USER_ID = @UserId;
            """;
        const string historySql = """
            INSERT INTO dbo.CHANGE_PASSWORD_HISTORY (USER_ID, PASSWORD, LOG_DATE)
            VALUES (@UserId, @PasswordHash, @LogDate);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var updateCmd = new CommandDefinition(
            updateSql,
            new
            {
                UserId = userId,
                PasswordHash = passwordHash,
                MdfDate = auditDate,
                MdfUser = auditUser
            },
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(updateCmd);

        var historyCmd = new CommandDefinition(
            historySql,
            new
            {
                UserId = userId,
                PasswordHash = passwordHash,
                LogDate = auditDate.Date
            },
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(historyCmd);
    }

    public async Task ReplaceRolesAsync(
        string userId,
        IReadOnlyCollection<string> roleIds,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default)
    {
        const string deleteSql = "DELETE FROM dbo.MAP_USER_ROLE WHERE USER_ID = @UserId;";
        const string insertSql = """
            INSERT INTO dbo.MAP_USER_ROLE (USER_ID, ROLE_ID, CRT_DATE, CRT_USER)
            VALUES (@UserId, @RoleId, @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var deleteCmd = new CommandDefinition(
            deleteSql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        if (roleIds.Count == 0)
            return;

        var rows = roleIds.Select(roleId => new
        {
            UserId = userId,
            RoleId = roleId,
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

    private async Task AttachRolesAsync(IReadOnlyCollection<User> users, CancellationToken ct)
    {
        if (users.Count == 0)
            return;

        const string sql = """
            SELECT MUR.USER_ID AS UserId,
                   R.ROLE_ID AS RoleId,
                   R.ROLE_NAME AS RoleName,
                   R.DEL_FLG AS DelFlg,
                   R.CRT_DATE AS CrtDate,
                   R.CRT_USER AS CrtUser,
                   R.MDF_DATE AS MdfDate,
                   R.MDF_USER AS MdfUser
            FROM dbo.MAP_USER_ROLE AS MUR
            INNER JOIN dbo.DIM_ROLE AS R ON R.ROLE_ID = MUR.ROLE_ID
            WHERE MUR.USER_ID IN @UserIds
            ORDER BY R.ROLE_NAME, R.ROLE_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { UserIds = users.Select(user => user.UserId).ToArray() },
            _session.CurrentTransaction,
            cancellationToken: ct);
        var rows = await conn.QueryAsync<UserRoleRow>(cmd);
        var rolesByUser = rows
            .GroupBy(row => row.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Role>)group.Select(row => row.Role).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
            user.Roles = rolesByUser.GetValueOrDefault(user.UserId) ?? [];
    }

    private sealed class UserRoleRow
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool DelFlg { get; set; }
        public DateTime? CrtDate { get; set; }
        public string? CrtUser { get; set; }
        public DateTime? MdfDate { get; set; }
        public string? MdfUser { get; set; }

        public Role Role => new()
        {
            RoleId = RoleId,
            RoleName = RoleName,
            DelFlg = DelFlg,
            CrtDate = CrtDate,
            CrtUser = CrtUser,
            MdfDate = MdfDate,
            MdfUser = MdfUser
        };
    }
}
