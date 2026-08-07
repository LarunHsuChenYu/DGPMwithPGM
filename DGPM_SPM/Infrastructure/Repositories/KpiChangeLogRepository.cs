using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>
/// KPI 異動紀錄查詢資料存取（kpi.KPI_CHANGE_LOG，重用既有資料表）。
/// ⚠ schema 為 provisional draft，待 SDS 定稿確認。
/// </summary>
public class KpiChangeLogRepository : IKpiChangeLogRepository
{
    /// <summary>KPI_CHANGE_LOG 主欄位 + JOIN KPI 數據 / 經銷商 / 指標的顯示欄位。</summary>
    private const string SelectColumns = """
        l.LOG_ID,
        l.DATA_ID,
        l.ACTION_TYPE,
        l.OLD_VALUE,
        l.NEW_VALUE,
        l.REASON,
        l.ACTION_USER,
        l.ACTION_DATE,
        d.PERIOD_YM,
        dl.DEALER_CODE,
        dl.DEALER_NAME,
        i.INDICATOR_CODE,
        i.INDICATOR_NAME,
        i.UNIT
        """;

    private const string FromJoin = """
        FROM kpi.KPI_CHANGE_LOG l
        INNER JOIN kpi.KPI_DATA d ON d.DATA_ID = l.DATA_ID
        INNER JOIN org.DEALER dl ON dl.DEALER_ID = d.DEALER_ID
        INNER JOIN kpi.KPI_INDICATOR i ON i.INDICATOR_ID = d.INDICATOR_ID
        """;

    private const string FilterWhere = """
        WHERE (@PeriodYm IS NULL OR d.PERIOD_YM = @PeriodYm)
          AND (@ActionType IS NULL OR l.ACTION_TYPE = @ActionType)
          AND (@ActionDateFrom IS NULL OR l.ACTION_DATE >= @ActionDateFrom)
          AND (@ActionDateToExclusive IS NULL OR l.ACTION_DATE < @ActionDateToExclusive)
          AND (@ActionUserPattern IS NULL OR l.ACTION_USER LIKE @ActionUserPattern)
          AND (@Keyword IS NULL
               OR dl.DEALER_CODE LIKE @KeywordPattern
               OR dl.DEALER_NAME LIKE @KeywordPattern
               OR i.INDICATOR_CODE LIKE @KeywordPattern
               OR i.INDICATOR_NAME LIKE @KeywordPattern)
        """;

    private readonly IDbSession _session;

    public KpiChangeLogRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<KpiChangeLog>> GetPagedAsync(
        KpiChangeLogFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            {FromJoin}
            {FilterWhere};

            SELECT {SelectColumns}
            {FromJoin}
            {FilterWhere}
            ORDER BY l.ACTION_DATE DESC, l.LOG_ID DESC
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.PeriodYm,
            filter.ActionType,
            ActionDateFrom = filter.ActionDateFrom?.Date,
            // 迄日含當日：以「隔日 0 點」作排除上界，避免遺漏當日時間部分
            ActionDateToExclusive = filter.ActionDateTo?.Date.AddDays(1),
            ActionUserPattern = filter.ActionUser is null ? null : $"%{EscapeLike(filter.ActionUser)}%",
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
        var datas = (await result.ReadAsync<KpiChangeLog>()).ToList();

        return new PagedResult<KpiChangeLog>
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
}
