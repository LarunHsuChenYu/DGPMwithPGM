using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private const string SelectColumns = """
        RATE_ID,
        CURRENCY_CODE,
        RATE_YM,
        RATE_VALUE,
        STATUS,
        MEMO,
        CRT_DATE,
        CRT_USER,
        MDF_DATE,
        MDF_USER
        """;

    private readonly IDbSession _session;

    public ExchangeRateRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<ExchangeRate>> GetPagedAsync(
        ExchangeRateFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM cfg.EXCHANGE_RATE
            WHERE (@CurrencyCode IS NULL OR CURRENCY_CODE = @CurrencyCode)
              AND (@RateYmFrom IS NULL OR RATE_YM >= @RateYmFrom)
              AND (@RateYmTo IS NULL OR RATE_YM <= @RateYmTo)
              AND (@Status IS NULL OR STATUS = @Status);

            SELECT {SelectColumns}
            FROM cfg.EXCHANGE_RATE
            WHERE (@CurrencyCode IS NULL OR CURRENCY_CODE = @CurrencyCode)
              AND (@RateYmFrom IS NULL OR RATE_YM >= @RateYmFrom)
              AND (@RateYmTo IS NULL OR RATE_YM <= @RateYmTo)
              AND (@Status IS NULL OR STATUS = @Status)
            ORDER BY RATE_YM DESC, CURRENCY_CODE
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.CurrencyCode,
            filter.RateYmFrom,
            filter.RateYmTo,
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
        var datas = (await result.ReadAsync<ExchangeRate>()).ToList();

        return new PagedResult<ExchangeRate>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ExchangeRate?> GetByIdAsync(int rateId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM cfg.EXCHANGE_RATE
            WHERE RATE_ID = @RateId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RateId = rateId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<ExchangeRate>(cmd);
    }

    public async Task<bool> ExistsAsync(
        string currencyCode,
        string rateYm,
        int? excludeRateId = null,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM cfg.EXCHANGE_RATE
                WHERE CURRENCY_CODE = @CurrencyCode
                  AND RATE_YM = @RateYm
                  AND (@ExcludeRateId IS NULL OR RATE_ID <> @ExcludeRateId)
            )
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { CurrencyCode = currencyCode, RateYm = rateYm, ExcludeRateId = excludeRateId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleAsync<bool>(cmd);
    }

    public async Task<ExchangeRate> AddAsync(ExchangeRate entity, CancellationToken ct = default)
    {
        var sql = $"""
            INSERT INTO cfg.EXCHANGE_RATE
                (CURRENCY_CODE, RATE_YM, RATE_VALUE, STATUS, MEMO, CRT_USER)
            OUTPUT {OutputColumns()}
            VALUES
                (@CurrencyCode, @RateYm, @RateValue, @Status, @Memo, @CrtUser)
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleAsync<ExchangeRate>(cmd);
    }

    public async Task<ExchangeRate?> UpdateAsync(ExchangeRate entity, CancellationToken ct = default)
    {
        var sql = $"""
            UPDATE cfg.EXCHANGE_RATE
            SET CURRENCY_CODE = @CurrencyCode,
                RATE_YM = @RateYm,
                RATE_VALUE = @RateValue,
                MEMO = @Memo,
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @MdfUser
            OUTPUT {OutputColumns()}
            WHERE RATE_ID = @RateId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<ExchangeRate>(cmd);
    }

    public async Task<ExchangeRate?> SetStatusAsync(
        int rateId,
        string status,
        string modifiedBy,
        CancellationToken ct = default)
    {
        var sql = $"""
            UPDATE cfg.EXCHANGE_RATE
            SET STATUS = @Status,
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @ModifiedBy
            OUTPUT {OutputColumns()}
            WHERE RATE_ID = @RateId
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RateId = rateId, Status = status, ModifiedBy = modifiedBy },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<ExchangeRate>(cmd);
    }

    private static string OutputColumns()
        => string.Join(
            ", ",
            SelectColumns.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(column => $"INSERTED.{column}"));
}
