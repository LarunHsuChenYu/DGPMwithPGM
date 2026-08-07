using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>org.DEALER 資料存取。⚠ schema 為 provisional draft，待 SDS 定稿確認。</summary>
public class DealerRepository : IDealerRepository
{
    private readonly IDbSession _session;

    public DealerRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<PagedResult<Dealer>> GetPagedAsync(
        DealerFilter filter,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM org.DEALER AS D
            WHERE (@Keyword IS NULL
                   OR D.DEALER_CODE LIKE '%' + @Keyword + '%'
                   OR D.DEALER_NAME LIKE '%' + @Keyword + '%')
              AND (@RegionId IS NULL OR D.REGION_ID = @RegionId)
              AND (@Status IS NULL OR D.STATUS = @Status);

            SELECT D.DEALER_ID,
                   D.DEALER_CODE,
                   D.DEALER_NAME,
                   D.REGION_ID,
                   R.REGION_NAME,
                   D.CURRENCY_CODE,
                   D.CONTACT_NAME,
                   D.CONTACT_EMAIL,
                   D.CONTACT_TEL,
                   D.STATUS,
                   D.MEMO,
                   D.CRT_DATE,
                   D.CRT_USER,
                   D.MDF_DATE,
                   D.MDF_USER
            FROM org.DEALER AS D
            INNER JOIN org.REGION AS R ON R.REGION_ID = D.REGION_ID
            WHERE (@Keyword IS NULL
                   OR D.DEALER_CODE LIKE '%' + @Keyword + '%'
                   OR D.DEALER_NAME LIKE '%' + @Keyword + '%')
              AND (@RegionId IS NULL OR D.REGION_ID = @RegionId)
              AND (@Status IS NULL OR D.STATUS = @Status)
            ORDER BY D.DEALER_CODE, D.DEALER_ID
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim(),
            filter.RegionId,
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
        var entities = (await result.ReadAsync<Dealer>()).ToList();

        return new PagedResult<Dealer>
        {
            Datas = entities,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Dealer?> GetByIdAsync(int dealerId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT D.DEALER_ID,
                   D.DEALER_CODE,
                   D.DEALER_NAME,
                   D.REGION_ID,
                   R.REGION_NAME,
                   D.CURRENCY_CODE,
                   D.CONTACT_NAME,
                   D.CONTACT_EMAIL,
                   D.CONTACT_TEL,
                   D.STATUS,
                   D.MEMO,
                   D.CRT_DATE,
                   D.CRT_USER,
                   D.MDF_DATE,
                   D.MDF_USER
            FROM org.DEALER AS D
            INNER JOIN org.REGION AS R ON R.REGION_ID = D.REGION_ID
            WHERE D.DEALER_ID = @DealerId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DealerId = dealerId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<Dealer>(cmd);
    }

    public async Task<bool> ExistsCodeAsync(
        string dealerCode,
        int? excludeDealerId = null,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM org.DEALER
                WHERE DEALER_CODE = @DealerCode
                  AND (@ExcludeDealerId IS NULL OR DEALER_ID <> @ExcludeDealerId)
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DealerCode = dealerCode, ExcludeDealerId = excludeDealerId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task<int> AddAsync(Dealer entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO org.DEALER
                (DEALER_CODE, DEALER_NAME, REGION_ID, CURRENCY_CODE,
                 CONTACT_NAME, CONTACT_EMAIL, CONTACT_TEL, STATUS, MEMO,
                 CRT_DATE, CRT_USER)
            OUTPUT INSERTED.DEALER_ID
            VALUES
                (@DealerCode, @DealerName, @RegionId, @CurrencyCode,
                 @ContactName, @ContactEmail, @ContactTel, @Status, @Memo,
                 @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(cmd);
    }

    public async Task<int> UpdateAsync(Dealer entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE org.DEALER
            SET DEALER_CODE = @DealerCode,
                DEALER_NAME = @DealerName,
                REGION_ID = @RegionId,
                CURRENCY_CODE = @CurrencyCode,
                CONTACT_NAME = @ContactName,
                CONTACT_EMAIL = @ContactEmail,
                CONTACT_TEL = @ContactTel,
                MEMO = @Memo,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE DEALER_ID = @DealerId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<IReadOnlyList<Dealer>> GetActiveByCodesAsync(
        IReadOnlyCollection<string> dealerCodes,
        CancellationToken ct = default)
    {
        if (dealerCodes.Count == 0)
            return [];

        const string sql = """
            SELECT DEALER_ID,
                   DEALER_CODE,
                   DEALER_NAME,
                   REGION_ID,
                   CURRENCY_CODE,
                   CONTACT_NAME,
                   CONTACT_EMAIL,
                   CONTACT_TEL,
                   STATUS,
                   MEMO,
                   CRT_DATE,
                   CRT_USER,
                   MDF_DATE,
                   MDF_USER
            FROM org.DEALER
            WHERE STATUS = 'A'
              AND DEALER_CODE IN @DealerCodes;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DealerCodes = dealerCodes },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<Dealer>(cmd)).ToList();
    }

    public async Task<int> UpdateStatusAsync(Dealer entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE org.DEALER
            SET STATUS = @Status,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE DEALER_ID = @DealerId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }
}
