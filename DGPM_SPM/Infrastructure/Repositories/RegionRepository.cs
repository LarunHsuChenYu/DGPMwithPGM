using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

public class RegionRepository : IRegionRepository
{
    private readonly IDbSession _session;

    public RegionRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Region>> GetActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT REGION_ID,
                   REGION_CODE,
                   REGION_NAME,
                   PARENT_REGION_ID,
                   SORT_ORDER,
                   STATUS
            FROM org.REGION
            WHERE STATUS = 'A'
            ORDER BY SORT_ORDER, REGION_CODE;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            transaction: _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<Region>(cmd)).ToList();
    }

    public async Task<PagedResult<Region>> GetPagedAsync(
        RegionFilter filter,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM org.REGION AS R
            WHERE (@Keyword IS NULL
                   OR R.REGION_CODE LIKE '%' + @Keyword + '%'
                   OR R.REGION_NAME LIKE '%' + @Keyword + '%')
              AND (@Status IS NULL OR R.STATUS = @Status)
              AND (@ParentRegionId IS NULL OR R.PARENT_REGION_ID = @ParentRegionId);

            SELECT R.REGION_ID,
                   R.REGION_CODE,
                   R.REGION_NAME,
                   R.PARENT_REGION_ID,
                   P.REGION_NAME AS PARENT_REGION_NAME,
                   R.SORT_ORDER,
                   R.STATUS,
                   R.CRT_DATE,
                   R.CRT_USER,
                   R.MDF_DATE,
                   R.MDF_USER
            FROM org.REGION AS R
            LEFT JOIN org.REGION AS P ON P.REGION_ID = R.PARENT_REGION_ID
            WHERE (@Keyword IS NULL
                   OR R.REGION_CODE LIKE '%' + @Keyword + '%'
                   OR R.REGION_NAME LIKE '%' + @Keyword + '%')
              AND (@Status IS NULL OR R.STATUS = @Status)
              AND (@ParentRegionId IS NULL OR R.PARENT_REGION_ID = @ParentRegionId)
            ORDER BY R.SORT_ORDER, R.REGION_CODE, R.REGION_ID
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim(),
            filter.Status,
            filter.ParentRegionId,
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
        var entities = (await result.ReadAsync<Region>()).ToList();

        return new PagedResult<Region>
        {
            Datas = entities,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Region?> GetByIdAsync(int regionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT R.REGION_ID,
                   R.REGION_CODE,
                   R.REGION_NAME,
                   R.PARENT_REGION_ID,
                   P.REGION_NAME AS PARENT_REGION_NAME,
                   R.SORT_ORDER,
                   R.STATUS,
                   R.CRT_DATE,
                   R.CRT_USER,
                   R.MDF_DATE,
                   R.MDF_USER
            FROM org.REGION AS R
            LEFT JOIN org.REGION AS P ON P.REGION_ID = R.PARENT_REGION_ID
            WHERE R.REGION_ID = @RegionId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RegionId = regionId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<Region>(cmd);
    }

    public async Task<IReadOnlyList<Region>> GetActiveOptionsAsync(
        int? excludeRegionId,
        CancellationToken ct = default)
    {
        const string sql = """
            WITH ExcludedRegions AS
            (
                SELECT REGION_ID
                FROM org.REGION
                WHERE @ExcludeRegionId IS NOT NULL AND REGION_ID = @ExcludeRegionId
                UNION ALL
                SELECT R.REGION_ID
                FROM org.REGION AS R
                INNER JOIN ExcludedRegions AS E ON E.REGION_ID = R.PARENT_REGION_ID
            )
            SELECT R.REGION_ID,
                   R.REGION_CODE,
                   R.REGION_NAME
            FROM org.REGION AS R
            WHERE R.STATUS = 'A'
              AND NOT EXISTS
                  (SELECT 1 FROM ExcludedRegions AS E WHERE E.REGION_ID = R.REGION_ID)
            ORDER BY R.SORT_ORDER, R.REGION_CODE
            OPTION (MAXRECURSION 100);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { ExcludeRegionId = excludeRegionId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<Region>(cmd)).ToList();
    }

    public async Task<bool> ExistsCodeAsync(
        string regionCode,
        int? excludeRegionId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM org.REGION
                WHERE REGION_CODE = @RegionCode
                  AND (@ExcludeRegionId IS NULL OR REGION_ID <> @ExcludeRegionId)
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            new { RegionCode = regionCode, ExcludeRegionId = excludeRegionId },
            ct);
    }

    public async Task<bool> IsDescendantAsync(
        int regionId,
        int candidateRegionId,
        CancellationToken ct = default)
    {
        const string sql = """
            WITH Descendants AS
            (
                SELECT REGION_ID
                FROM org.REGION
                WHERE PARENT_REGION_ID = @RegionId
                UNION ALL
                SELECT R.REGION_ID
                FROM org.REGION AS R
                INNER JOIN Descendants AS D ON D.REGION_ID = R.PARENT_REGION_ID
            )
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM Descendants WHERE REGION_ID = @CandidateRegionId
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            OPTION (MAXRECURSION 100);
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            new { RegionId = regionId, CandidateRegionId = candidateRegionId },
            ct);
    }

    public Task<bool> HasActiveChildrenAsync(int regionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM org.REGION
                WHERE PARENT_REGION_ID = @RegionId AND STATUS = 'A'
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        return ExecuteScalarAsync<bool>(sql, new { RegionId = regionId }, ct);
    }

    public Task<bool> HasActiveDealersAsync(int regionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM org.DEALER
                WHERE REGION_ID = @RegionId AND STATUS = 'A'
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        return ExecuteScalarAsync<bool>(sql, new { RegionId = regionId }, ct);
    }

    public async Task<int> AddAsync(Region entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO org.REGION
                (REGION_CODE, REGION_NAME, PARENT_REGION_ID, SORT_ORDER, STATUS, CRT_DATE, CRT_USER)
            OUTPUT INSERTED.REGION_ID
            VALUES
                (@RegionCode, @RegionName, @ParentRegionId, @SortOrder, @Status, @CrtDate, @CrtUser);
            """;
        return await ExecuteScalarAsync<int>(sql, entity, ct);
    }

    public Task<int> UpdateAsync(Region entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE org.REGION
            SET REGION_CODE = @RegionCode,
                REGION_NAME = @RegionName,
                PARENT_REGION_ID = @ParentRegionId,
                SORT_ORDER = @SortOrder,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE REGION_ID = @RegionId;
            """;
        return ExecuteAsync(sql, entity, ct);
    }

    public Task<int> UpdateStatusAsync(Region entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE org.REGION
            SET STATUS = @Status,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE REGION_ID = @RegionId;
            """;
        return ExecuteAsync(sql, entity, ct);
    }

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
