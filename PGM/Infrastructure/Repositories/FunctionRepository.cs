using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>系統功能維護（dbo.SET_FUNCTION）。列表僅顯示未軟刪；刪除為軟刪。</summary>
public class FunctionRepository : IFunctionRepository
{
    private const string SelectColumns = """
        F.FUNCTION_ID   AS Fun_ID,
        F.FUNCTION_NAME AS Fun_Name,
        F.PARENT_ID     AS Parent_ID,
        P.FUNCTION_NAME AS Parent_Name,
        F.ACTION_TYPE   AS Action_Type,
        F.FUNCTION_URL  AS Url_Path,
        F.ICON          AS Icon,
        CAST(F.SORT_ID AS decimal(18,2)) AS Sort_Order,
        F.IS_MENU       AS Is_Menu,
        F.IS_ENABLED    AS Is_Enabled,
        F.FUN_DESC      AS Fun_Desc,
        F.SYSTEM_CODE   AS System_Code,
        CASE WHEN F.DEL_FLG = 0 THEN 'N' ELSE 'Y' END AS Del_YN,
        F.CRT_USER      AS Cre_Person,
        F.CRT_DATE      AS Cre_Date,
        ISNULL(F.MDF_USER, F.CRT_USER) AS Chg_Person,
        ISNULL(F.MDF_DATE, F.CRT_DATE) AS Chg_Date
        """;

    private readonly IDbSession _session;

    public FunctionRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<SysFun>> GetPagedAsync(
        FunctionFilter filter,
        CancellationToken ct = default)
    {
        var pagingSql = filter.PageSize == FilterBase.NoPagingPageSize
            ? string.Empty
            : """
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var sql = $"""
            SELECT COUNT(1)
            FROM dbo.[SET_FUNCTION] AS F
            WHERE F.DEL_FLG = 0
              AND (@Keyword IS NULL OR F.FUNCTION_ID LIKE @KeywordPattern OR F.FUNCTION_NAME LIKE @KeywordPattern)
              AND (@ParentId IS NULL OR F.PARENT_ID = @ParentId)
              AND (@ActionType IS NULL OR F.ACTION_TYPE = @ActionType);

            SELECT {SelectColumns}
            FROM dbo.[SET_FUNCTION] AS F
            LEFT JOIN dbo.[SET_FUNCTION] AS P ON P.FUNCTION_ID = F.PARENT_ID
            WHERE F.DEL_FLG = 0
              AND (@Keyword IS NULL OR F.FUNCTION_ID LIKE @KeywordPattern OR F.FUNCTION_NAME LIKE @KeywordPattern)
              AND (@ParentId IS NULL OR F.PARENT_ID = @ParentId)
              AND (@ActionType IS NULL OR F.ACTION_TYPE = @ActionType)
            ORDER BY F.SORT_ID, F.FUNCTION_ID
            {pagingSql};
            """;

        var parameters = new
        {
            filter.Keyword,
            KeywordPattern = filter.Keyword is null ? null : $"%{EscapeLike(filter.Keyword)}%",
            filter.ParentId,
            filter.ActionType,
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
        var datas = (await result.ReadAsync<SysFun>()).ToList();

        return new PagedResult<SysFun>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.PageSize == FilterBase.NoPagingPageSize ? 1 : filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<SysFun?> GetByFunIdAsync(string funId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.[SET_FUNCTION] AS F
            LEFT JOIN dbo.[SET_FUNCTION] AS P ON P.FUNCTION_ID = F.PARENT_ID
            WHERE F.FUNCTION_ID = @FunId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { FunId = funId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<SysFun>(cmd);
    }

    public async Task<IReadOnlyList<SysFun>> GetModuleOptionsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT FUNCTION_ID AS Fun_ID, FUNCTION_NAME AS Fun_Name, CAST(SORT_ID AS decimal(18,2)) AS Sort_Order
            FROM dbo.[SET_FUNCTION]
            WHERE DEL_FLG = 0 AND ACTION_TYPE = 'M'
            ORDER BY SORT_ID, FUNCTION_ID;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            transaction: _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<SysFun>(cmd)).ToList();
    }

    public async Task<IReadOnlyList<SysFun>> GetActiveOptionsAsync(
        string? excludeFunId,
        CancellationToken ct = default)
    {
        const string sql = """
            WITH ExcludedFunctions AS
            (
                SELECT FUNCTION_ID AS Fun_ID
                FROM dbo.[SET_FUNCTION]
                WHERE @ExcludeFunId IS NOT NULL AND FUNCTION_ID = @ExcludeFunId
                UNION ALL
                SELECT F.FUNCTION_ID
                FROM dbo.[SET_FUNCTION] AS F
                INNER JOIN ExcludedFunctions AS E ON E.Fun_ID = F.PARENT_ID
                WHERE F.DEL_FLG = 0
            )
            SELECT F.FUNCTION_ID AS Fun_ID,
                   F.FUNCTION_NAME AS Fun_Name,
                   CAST(F.SORT_ID AS decimal(18,2)) AS Sort_Order,
                   F.ACTION_TYPE AS Action_Type
            FROM dbo.[SET_FUNCTION] AS F
            WHERE F.DEL_FLG = 0
              AND NOT EXISTS
                  (SELECT 1 FROM ExcludedFunctions AS E WHERE E.Fun_ID = F.FUNCTION_ID)
            ORDER BY F.SORT_ID, F.FUNCTION_ID
            OPTION (MAXRECURSION 100);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { ExcludeFunId = excludeFunId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<SysFun>(cmd)).ToList();
    }

    public Task<bool> ExistsFunIdAsync(string funId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.[SET_FUNCTION] WHERE FUNCTION_ID = @FunId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        return ExecuteScalarAsync<bool>(sql, new { FunId = funId }, ct);
    }

    public Task<bool> IsDescendantAsync(
        string funId,
        string candidateFunId,
        CancellationToken ct = default)
    {
        const string sql = """
            WITH Descendants AS
            (
                SELECT FUNCTION_ID AS Fun_ID
                FROM dbo.[SET_FUNCTION]
                WHERE PARENT_ID = @FunId AND DEL_FLG = 0
                UNION ALL
                SELECT F.FUNCTION_ID
                FROM dbo.[SET_FUNCTION] AS F
                INNER JOIN Descendants AS D ON D.Fun_ID = F.PARENT_ID
                WHERE F.DEL_FLG = 0
            )
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM Descendants WHERE Fun_ID = @CandidateFunId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            OPTION (MAXRECURSION 100);
            """;

        return ExecuteScalarAsync<bool>(
            sql,
            new { FunId = funId, CandidateFunId = candidateFunId },
            ct);
    }

    public Task<bool> HasActiveChildrenAsync(string funId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.[SET_FUNCTION]
                WHERE PARENT_ID = @FunId AND DEL_FLG = 0
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        return ExecuteScalarAsync<bool>(sql, new { FunId = funId }, ct);
    }

    public Task<int> AddAsync(SysFun entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.[SET_FUNCTION]
            (
                FUNCTION_ID, FUNCTION_NAME, PARENT_ID, PARENT_NAME, ACTION_TYPE, FUNCTION_URL, ICON,
                SORT_ID, IS_MENU, IS_ENABLED, FUN_DESC, DEL_FLG, SYSTEM_CODE,
                CRT_USER, CRT_DATE, MDF_USER, MDF_DATE
            )
            VALUES
            (
                @FunId, @FunName, @ParentId, @ParentName, @ActionType, @UrlPath, @Icon,
                CAST(@SortOrder AS smallint), @IsMenu, @IsEnabled, @FunDesc,
                CASE WHEN @DelYn = 'Y' THEN 1 ELSE 0 END,
                ISNULL(NULLIF(@SystemCode, ''), 'PGM'),
                @CrePerson, @CreDate, @ChgPerson, @ChgDate
            );
            """;
        return ExecuteAsync(sql, entity, ct);
    }

    public Task<int> UpdateAsync(SysFun entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.[SET_FUNCTION]
            SET FUNCTION_NAME = @FunName,
                PARENT_ID = @ParentId,
                ACTION_TYPE = @ActionType,
                FUNCTION_URL = @UrlPath,
                SORT_ID = CAST(@SortOrder AS smallint),
                IS_MENU = @IsMenu,
                IS_ENABLED = @IsEnabled,
                FUN_DESC = @FunDesc,
                SYSTEM_CODE = ISNULL(NULLIF(@SystemCode, ''), SYSTEM_CODE),
                MDF_USER = @ChgPerson,
                MDF_DATE = @ChgDate
            WHERE FUNCTION_ID = @FunId AND DEL_FLG = 0;
            """;
        return ExecuteAsync(sql, entity, ct);
    }

    public Task<int> SoftDeleteAsync(SysFun entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.[SET_FUNCTION]
            SET DEL_FLG = 1,
                IS_MENU = 'N',
                MDF_USER = @ChgPerson,
                MDF_DATE = @ChgDate
            WHERE FUNCTION_ID = @FunId AND DEL_FLG = 0;
            """;
        return ExecuteAsync(sql, entity, ct);
    }

    private static string EscapeLike(string value)
        => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private async Task<T> ExecuteScalarAsync<T>(string sql, object parameters, CancellationToken ct)
    {
        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            parameters,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.ExecuteScalarAsync<T>(cmd))!;
    }

    private async Task<int> ExecuteAsync(string sql, object parameters, CancellationToken ct)
    {
        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            parameters,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }
}
