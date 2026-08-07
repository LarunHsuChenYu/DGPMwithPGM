using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

public class KpiIndicatorRepository : IKpiIndicatorRepository
{
    private const string SelectColumns = """
        INDICATOR_ID,
        INDICATOR_CODE,
        INDICATOR_NAME,
        UNIT,
        DATA_TYPE,
        DECIMAL_PLACES,
        SORT_ORDER,
        STATUS,
        MEMO,
        CRT_DATE,
        CRT_USER,
        MDF_DATE,
        MDF_USER
        """;

    private readonly IDbSession _session;

    public KpiIndicatorRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<KpiIndicator>> GetPagedAsync(
        KpiIndicatorFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM kpi.KPI_INDICATOR
            WHERE (@Keyword IS NULL OR INDICATOR_CODE LIKE @KeywordPattern OR INDICATOR_NAME LIKE @KeywordPattern)
              AND (@DataType IS NULL OR DATA_TYPE = @DataType)
              AND (@Status IS NULL OR STATUS = @Status);

            SELECT {SelectColumns}
            FROM kpi.KPI_INDICATOR
            WHERE (@Keyword IS NULL OR INDICATOR_CODE LIKE @KeywordPattern OR INDICATOR_NAME LIKE @KeywordPattern)
              AND (@DataType IS NULL OR DATA_TYPE = @DataType)
              AND (@Status IS NULL OR STATUS = @Status)
            ORDER BY SORT_ORDER, INDICATOR_CODE
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.Keyword,
            KeywordPattern = filter.Keyword is null ? null : $"%{EscapeLike(filter.Keyword)}%",
            filter.DataType,
            filter.Status,
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
        var datas = (await result.ReadAsync<KpiIndicator>()).ToList();

        return new PagedResult<KpiIndicator>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<KpiIndicator?> GetByIdAsync(int indicatorId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM kpi.KPI_INDICATOR
            WHERE INDICATOR_ID = @IndicatorId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { IndicatorId = indicatorId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<KpiIndicator>(cmd);
    }

    public async Task<bool> ExistsByCodeAsync(
        string indicatorCode,
        int? excludeIndicatorId = null,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM kpi.KPI_INDICATOR
                WHERE INDICATOR_CODE = @IndicatorCode
                  AND (@ExcludeIndicatorId IS NULL OR INDICATOR_ID <> @ExcludeIndicatorId)
            )
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { IndicatorCode = indicatorCode, ExcludeIndicatorId = excludeIndicatorId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleAsync<bool>(cmd);
    }

    public async Task<KpiIndicator> AddAsync(KpiIndicator entity, CancellationToken ct = default)
    {
        var sql = $"""
            INSERT INTO kpi.KPI_INDICATOR
                (INDICATOR_CODE, INDICATOR_NAME, UNIT, DATA_TYPE, DECIMAL_PLACES, SORT_ORDER, STATUS, MEMO, CRT_USER)
            OUTPUT {OutputColumns()}
            VALUES
                (@IndicatorCode, @IndicatorName, @Unit, @DataType, @DecimalPlaces, @SortOrder, @Status, @Memo, @CrtUser)
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleAsync<KpiIndicator>(cmd);
    }

    public async Task<KpiIndicator?> UpdateAsync(KpiIndicator entity, CancellationToken ct = default)
    {
        var sql = $"""
            UPDATE kpi.KPI_INDICATOR
            SET INDICATOR_CODE = @IndicatorCode,
                INDICATOR_NAME = @IndicatorName,
                UNIT = @Unit,
                DATA_TYPE = @DataType,
                DECIMAL_PLACES = @DecimalPlaces,
                SORT_ORDER = @SortOrder,
                MEMO = @Memo,
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @MdfUser
            OUTPUT {OutputColumns()}
            WHERE INDICATOR_ID = @IndicatorId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<KpiIndicator>(cmd);
    }

    public async Task<KpiIndicator?> SetStatusAsync(
        int indicatorId,
        string status,
        string modifiedBy,
        CancellationToken ct = default)
    {
        var sql = $"""
            UPDATE kpi.KPI_INDICATOR
            SET STATUS = @Status,
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @ModifiedBy
            OUTPUT {OutputColumns()}
            WHERE INDICATOR_ID = @IndicatorId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { IndicatorId = indicatorId, Status = status, ModifiedBy = modifiedBy },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<KpiIndicator>(cmd);
    }

    public async Task<IReadOnlyList<KpiIndicator>> GetActiveByCodesAsync(
        IReadOnlyCollection<string> indicatorCodes,
        CancellationToken ct = default)
    {
        if (indicatorCodes.Count == 0)
            return [];

        var sql = $"""
            SELECT {SelectColumns}
            FROM kpi.KPI_INDICATOR
            WHERE STATUS = 'A'
              AND INDICATOR_CODE IN @IndicatorCodes;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { IndicatorCodes = indicatorCodes },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<KpiIndicator>(cmd)).ToList();
    }

    /// <summary>LIKE 萬用字元跳脫，避免使用者輸入 % / _ / [ 影響查詢結果。</summary>
    private static string EscapeLike(string value)
        => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private static string OutputColumns()
        => string.Join(
            ", ",
            SelectColumns.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(column => $"INSERTED.{column}"));
}
