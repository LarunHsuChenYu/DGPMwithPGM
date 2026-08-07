using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>SQL 複製自 QMS ParameterRepository.GetAllByItem</summary>
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
                SET_TYPE,
                SET_VALUE,
                SORT_ORDER,
                MEMO,
                DEL_FLG,
                CRT_DATE,
                CRT_USER,
                MDF_DATE,
                MDF_USER
            FROM SET_PARAM
            WHERE DEL_FLG = 0
                AND SET_ITEM = @SetItem
            ORDER BY SORT_ORDER
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(sql, new { SetItem = setItem }, _session.CurrentTransaction, cancellationToken: ct);
        var result = await conn.QueryAsync<Parameter>(cmd);
        return result.ToList();
    }
}
