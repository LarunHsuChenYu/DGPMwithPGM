using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>Add / UpdateLogout SQL 複製自 QMS AuthenticationLogRepository；GetPaged 為本專案查詢擴充。</summary>
public class AuthenticationLogRepository : IAuthenticationLogRepository
{
    /// <summary>查詢刻意不含 IDENTITY_CONTENT（登入身分內容，不回傳敏感資訊）。</summary>
    private const string SelectColumns = """
        GUID,
        USER_ID,
        IP,
        LOGIN_TYPE,
        AUTH_STATUS,
        LOGIN_TIME,
        LOGOUT_TIME
        """;

    private const string FilterWhere = """
        WHERE (@KeywordPattern IS NULL OR USER_ID LIKE @KeywordPattern)
          AND (@LoginDateFrom IS NULL OR LOGIN_TIME >= @LoginDateFrom)
          AND (@LoginDateToExclusive IS NULL OR LOGIN_TIME < @LoginDateToExclusive)
          AND (@AuthStatus IS NULL OR AUTH_STATUS = @AuthStatus)
        """;

    private readonly IDbSession _session;

    public AuthenticationLogRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<AuthenticationLog>> GetPagedAsync(
        AuthenticationLogFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM AUTHENTICATION_LOG
            {FilterWhere};

            SELECT {SelectColumns}
            FROM AUTHENTICATION_LOG
            {FilterWhere}
            ORDER BY LOGIN_TIME DESC, GUID
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            KeywordPattern = filter.Keyword is null ? null : $"%{EscapeLike(filter.Keyword)}%",
            LoginDateFrom = filter.LoginDateFrom?.Date,
            // 迄日含當日：以「隔日 0 點」作排除上界，避免遺漏當日時間部分
            LoginDateToExclusive = filter.LoginDateTo?.Date.AddDays(1),
            filter.AuthStatus,
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
        var datas = (await result.ReadAsync<AuthenticationLog>()).ToList();

        return new PagedResult<AuthenticationLog>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <summary>LIKE 萬用字元跳脫，避免使用者輸入 % / _ / [ 影響查詢結果。</summary>
    private static string EscapeLike(string value)
        => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    public async Task<int> AddAsync(AuthenticationLog entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO AUTHENTICATION_LOG (
                GUID,
                USER_ID,
                IDENTITY_CONTENT,
                IP,
                LOGIN_TYPE,
                AUTH_STATUS,
                LOGIN_TIME,
                LOGOUT_TIME
            ) VALUES (
                @Guid,
                @UserId,
                @IdentityContent,
                @Ip,
                @LoginType,
                @AuthStatus,
                @LoginTime,
                @LogoutTime
            )
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateLogoutAsync(AuthenticationLog entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE AUTHENTICATION_LOG
            SET LOGOUT_TIME = @LogoutTime,
                AUTH_STATUS = @AuthStatus
            WHERE GUID = @Guid
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }
}
