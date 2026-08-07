using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>kpi.KPI_USER_DATA_SCOPE 資料存取（KPI 資料權限，provisional draft）。</summary>
public class KpiUserDataScopeRepository : IKpiUserDataScopeRepository
{
    private readonly IDbSession _session;

    public KpiUserDataScopeRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<KpiUserDataScope>> GetByUserIdAsync(
        string userId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT S.SCOPE_ID,
                   S.USER_ID,
                   S.SCOPE_TYPE,
                   S.REGION_ID,
                   S.DEALER_ID,
                   S.CRT_DATE,
                   S.CRT_USER,
                   R.REGION_CODE,
                   R.REGION_NAME,
                   D.DEALER_CODE,
                   D.DEALER_NAME
            FROM kpi.KPI_USER_DATA_SCOPE AS S
            LEFT JOIN org.REGION AS R ON R.REGION_ID = S.REGION_ID
            LEFT JOIN org.DEALER AS D ON D.DEALER_ID = S.DEALER_ID
            WHERE S.USER_ID = @UserId
            ORDER BY S.SCOPE_TYPE, R.REGION_CODE, D.DEALER_CODE
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<KpiUserDataScope>(cmd)).ToList();
    }

    public async Task<IReadOnlyList<int>> GetExistingRegionIdsAsync(
        IReadOnlyCollection<int> regionIds,
        CancellationToken ct = default)
    {
        if (regionIds.Count == 0)
            return [];

        const string sql = """
            SELECT REGION_ID
            FROM org.REGION
            WHERE REGION_ID IN @RegionIds
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { RegionIds = regionIds },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<int>(cmd)).ToList();
    }

    public async Task<IReadOnlyList<int>> GetExistingDealerIdsAsync(
        IReadOnlyCollection<int> dealerIds,
        CancellationToken ct = default)
    {
        if (dealerIds.Count == 0)
            return [];

        const string sql = """
            SELECT DEALER_ID
            FROM org.DEALER
            WHERE DEALER_ID IN @DealerIds
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { DealerIds = dealerIds },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<int>(cmd)).ToList();
    }

    public async Task ReplaceByUserIdAsync(
        string userId,
        IReadOnlyCollection<KpiUserDataScope> scopes,
        CancellationToken ct = default)
    {
        const string deleteSql = "DELETE FROM kpi.KPI_USER_DATA_SCOPE WHERE USER_ID = @UserId;";
        const string insertSql = """
            INSERT INTO kpi.KPI_USER_DATA_SCOPE
                (USER_ID, SCOPE_TYPE, REGION_ID, DEALER_ID, CRT_USER)
            VALUES
                (@UserId, @ScopeType, @RegionId, @DealerId, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var deleteCmd = new CommandDefinition(
            deleteSql,
            new { UserId = userId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        if (scopes.Count == 0)
            return;

        var insertCmd = new CommandDefinition(
            insertSql,
            scopes,
            _session.CurrentTransaction,
            cancellationToken: ct);
        await conn.ExecuteAsync(insertCmd);
    }
}
