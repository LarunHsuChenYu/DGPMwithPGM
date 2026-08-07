using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>
/// KPI 數據覆核資料存取（kpi.KPI_DATA + kpi.KPI_CHANGE_LOG）。
/// ⚠ schema 為 provisional draft，待 SDS 定稿確認。
/// </summary>
public class KpiDataRepository : IKpiDataRepository
{
    /// <summary>KPI_DATA 主欄位 + JOIN 經銷商 / 指標的顯示欄位。</summary>
    private const string SelectColumns = """
        d.DATA_ID,
        d.DEALER_ID,
        d.INDICATOR_ID,
        d.PERIOD_YM,
        d.KPI_VALUE,
        d.BATCH_ID,
        d.REVIEW_STATUS,
        d.REVIEW_USER,
        d.REVIEW_DATE,
        d.CRT_DATE,
        d.CRT_USER,
        d.MDF_DATE,
        d.MDF_USER,
        dl.DEALER_CODE,
        dl.DEALER_NAME,
        i.INDICATOR_CODE,
        i.INDICATOR_NAME,
        i.UNIT
        """;

    private const string FromJoin = """
        FROM kpi.KPI_DATA d
        INNER JOIN org.DEALER dl ON dl.DEALER_ID = d.DEALER_ID
        INNER JOIN kpi.KPI_INDICATOR i ON i.INDICATOR_ID = d.INDICATOR_ID
        """;

    private const string FilterWhere = """
        WHERE (@PeriodYm IS NULL OR d.PERIOD_YM = @PeriodYm)
          AND (@ReviewStatus IS NULL OR d.REVIEW_STATUS = @ReviewStatus)
          AND (@Keyword IS NULL
               OR dl.DEALER_CODE LIKE @KeywordPattern
               OR dl.DEALER_NAME LIKE @KeywordPattern
               OR i.INDICATOR_CODE LIKE @KeywordPattern
               OR i.INDICATOR_NAME LIKE @KeywordPattern)
        """;

    private readonly IDbSession _session;

    public KpiDataRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<KpiData>> GetPagedAsync(
        KpiDataFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            {FromJoin}
            {FilterWhere};

            SELECT {SelectColumns}
            {FromJoin}
            {FilterWhere}
            ORDER BY d.PERIOD_YM DESC, dl.DEALER_CODE, i.SORT_ORDER, i.INDICATOR_CODE
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.PeriodYm,
            filter.ReviewStatus,
            filter.Keyword,
            KeywordPattern = filter.Keyword is null ? null : $"%{EscapeLike(filter.Keyword)}%",
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
        var datas = (await result.ReadAsync<KpiData>()).ToList();

        return new PagedResult<KpiData>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<KpiData?> GetByIdAsync(long dataId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            {FromJoin}
            WHERE d.DATA_ID = @DataId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DataId = dataId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<KpiData>(cmd);
    }

    public async Task<int> UpdateReviewStatusAsync(
        long dataId,
        string reviewStatus,
        string reviewUser,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE kpi.KPI_DATA
            SET REVIEW_STATUS = @ReviewStatus,
                REVIEW_USER = @ReviewUser,
                REVIEW_DATE = SYSDATETIME(),
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @ReviewUser
            WHERE DATA_ID = @DataId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DataId = dataId, ReviewStatus = reviewStatus, ReviewUser = reviewUser },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<long> AddChangeLogAsync(KpiChangeLog entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO kpi.KPI_CHANGE_LOG
                (DATA_ID, ACTION_TYPE, OLD_VALUE, NEW_VALUE, REASON, ACTION_USER)
            OUTPUT INSERTED.LOG_ID
            VALUES
                (@DataId, @ActionType, @OldValue, @NewValue, @Reason, @ActionUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<long>(cmd);
    }

    /// <summary>LIKE 萬用字元跳脫，避免使用者輸入 % / _ / [ 影響查詢結果。</summary>
    private static string EscapeLike(string value)
        => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
