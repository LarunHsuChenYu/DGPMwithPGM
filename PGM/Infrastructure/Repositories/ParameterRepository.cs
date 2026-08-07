using Dapper;
using PGM.Core.Application.Interfaces;
using PGM.Core.Domain.Entities;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

/// <summary>dbo.SET_PARAM／SET_PARAMITEM（BMW SET_ID；ParamSet SRS）。</summary>
public class ParameterRepository : IParameterRepository
{
    private readonly IDbSession _session;

    public ParameterRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Parameter>> GetAllByItemAsync(string setItem, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                SET_ITEM,
                SET_ID,
                SET_VALUE,
                SORT_ORDER,
                MEMO,
                DEL_FLG,
                CRT_DATE,
                CRT_USER,
                MDF_DATE,
                MDF_USER
            FROM dbo.[SET_PARAM]
            WHERE DEL_FLG = 0
                AND SET_ITEM = @SetItem
            ORDER BY SORT_ORDER;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        var result = await conn.QueryAsync<Parameter>(cmd);
        return result.ToList();
    }

    public async Task<IReadOnlyList<ParamItem>> GetActiveCategoriesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                SET_ITEM,
                SET_ITEM_NAME,
                MEMO,
                DEL_FLG,
                CRT_DATE,
                CRT_USER,
                MDF_DATE,
                MDF_USER
            FROM dbo.[SET_PARAMITEM]
            WHERE DEL_FLG = 0
            ORDER BY SET_ITEM;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, transaction: _session.CurrentTransaction, cancellationToken: ct);
        var result = await conn.QueryAsync<ParamItem>(cmd);
        return result.ToList();
    }

    public async Task<IReadOnlyList<Parameter>> GetActiveByItemJoinAsync(string setItem, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                B.SET_ITEM,
                B.SET_ID,
                B.SET_VALUE,
                B.SORT_ORDER,
                B.MEMO,
                B.DEL_FLG,
                B.CRT_DATE,
                B.CRT_USER,
                B.MDF_DATE,
                B.MDF_USER
            FROM dbo.[SET_PARAMITEM] AS A
            INNER JOIN dbo.[SET_PARAM] AS B
                ON A.SET_ITEM = B.SET_ITEM
            WHERE A.SET_ITEM = @SetItem
                AND A.DEL_FLG = 0
                AND B.DEL_FLG = 0
            ORDER BY B.SORT_ORDER;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        var result = await conn.QueryAsync<Parameter>(cmd);
        return result.ToList();
    }

    public async Task<Parameter?> GetByKeyAsync(string setItem, string setId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                SET_ITEM,
                SET_ID,
                SET_VALUE,
                SORT_ORDER,
                MEMO,
                DEL_FLG,
                CRT_DATE,
                CRT_USER,
                MDF_DATE,
                MDF_USER
            FROM dbo.[SET_PARAM]
            WHERE SET_ITEM = @SetItem
                AND SET_ID = @SetId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { SetItem = setItem, SetId = setId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<Parameter>(cmd);
    }

    public async Task<bool> IsCategoryActiveAsync(string setItem, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.[SET_PARAMITEM]
                WHERE SET_ITEM = @SetItem
                    AND DEL_FLG = 0
            ) THEN 1 ELSE 0 END AS BIT);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task<string?> GetCategoryNameAsync(string setItem, CancellationToken ct = default)
    {
        const string sql = """
            SELECT SET_ITEM_NAME
            FROM dbo.[SET_PARAMITEM]
            WHERE SET_ITEM = @SetItem
                AND DEL_FLG = 0;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<string?>(cmd);
    }

    public async Task<int> GetNextSortOrderAsync(string setItem, CancellationToken ct = default)
    {
        const string sql = """
            SELECT ISNULL(MAX(SORT_ORDER), 0) + 1
            FROM dbo.[SET_PARAM]
            WHERE SET_ITEM = @SetItem
                AND DEL_FLG = 0;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(cmd);
    }

    public async Task<int> AddAsync(Parameter entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.[SET_PARAM]
                (SET_ITEM, SET_ID, SET_VALUE, SORT_ORDER, MEMO, DEL_FLG, CRT_DATE, CRT_USER)
            VALUES
                (@SetItem, @SetId, @SetValue, @SortOrder, @Memo, @DelFlg, @CrtDate, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> UpdateAsync(Parameter entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.[SET_PARAM]
            SET SET_VALUE = @SetValue,
                SORT_ORDER = @SortOrder,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE SET_ITEM = @SetItem
                AND SET_ID = @SetId
                AND DEL_FLG = 0;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> ReviveAsync(Parameter entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.[SET_PARAM]
            SET SET_VALUE = @SetValue,
                SORT_ORDER = @SortOrder,
                DEL_FLG = 0,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE SET_ITEM = @SetItem
                AND SET_ID = @SetId
                AND DEL_FLG = 1;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<int> SoftDeleteAsync(Parameter entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.[SET_PARAM]
            SET DEL_FLG = 1,
                MDF_DATE = @MdfDate,
                MDF_USER = @MdfUser
            WHERE SET_ITEM = @SetItem
                AND SET_ID = @SetId
                AND DEL_FLG = 0;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, entity, _session.CurrentTransaction, cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }
}
