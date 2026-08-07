using Dapper;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Domain.Entities;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

/// <summary>
/// KPI 匯入資料存取（kpi.KPI_IMPORT_BATCH / kpi.KPI_DATA / kpi.KPI_CHANGE_LOG）。
/// ⚠ schema 為 provisional draft，待 SDS 定稿確認。
/// </summary>
public class KpiImportRepository : IKpiImportRepository
{
    private const string BatchColumns = """
        BATCH_ID,
        FILE_NAME,
        PERIOD_YM,
        IMPORT_STATUS,
        TOTAL_ROWS,
        SUCCESS_ROWS,
        FAIL_ROWS,
        ERROR_MESSAGE,
        IMPORT_USER,
        IMPORT_START,
        IMPORT_END
        """;

    private readonly IDbSession _session;

    public KpiImportRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<long> AddBatchAsync(KpiImportBatch entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO kpi.KPI_IMPORT_BATCH
                (FILE_NAME, PERIOD_YM, IMPORT_STATUS, TOTAL_ROWS, IMPORT_USER)
            OUTPUT INSERTED.BATCH_ID
            VALUES
                (@FileName, @PeriodYm, @ImportStatus, @TotalRows, @ImportUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<long>(cmd);
    }

    public async Task<int> UpdateBatchResultAsync(KpiImportBatch entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE kpi.KPI_IMPORT_BATCH
            SET IMPORT_STATUS = @ImportStatus,
                TOTAL_ROWS = @TotalRows,
                SUCCESS_ROWS = @SuccessRows,
                FAIL_ROWS = @FailRows,
                ERROR_MESSAGE = @ErrorMessage,
                IMPORT_END = SYSDATETIME()
            WHERE BATCH_ID = @BatchId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteAsync(cmd);
    }

    public async Task<KpiImportBatch?> GetBatchByIdAsync(long batchId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {BatchColumns}
            FROM kpi.KPI_IMPORT_BATCH
            WHERE BATCH_ID = @BatchId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { BatchId = batchId },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<KpiImportBatch>(cmd);
    }

    public async Task<PagedResult<KpiImportBatch>> GetBatchPagedAsync(
        KpiImportBatchFilter filter,
        CancellationToken ct = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM kpi.KPI_IMPORT_BATCH
            WHERE (@PeriodYm IS NULL OR PERIOD_YM = @PeriodYm)
              AND (@ImportStatus IS NULL OR IMPORT_STATUS = @ImportStatus);

            SELECT {BatchColumns}
            FROM kpi.KPI_IMPORT_BATCH
            WHERE (@PeriodYm IS NULL OR PERIOD_YM = @PeriodYm)
              AND (@ImportStatus IS NULL OR IMPORT_STATUS = @ImportStatus)
            ORDER BY IMPORT_START DESC, BATCH_ID DESC
            OFFSET @RowSkip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.PeriodYm,
            filter.ImportStatus,
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
        var datas = (await result.ReadAsync<KpiImportBatch>()).ToList();

        return new PagedResult<KpiImportBatch>
        {
            Datas = datas,
            TotalRow = totalRow,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyList<KpiData>> GetDataByPeriodAsync(
        string periodYm,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT DATA_ID,
                   DEALER_ID,
                   INDICATOR_ID,
                   PERIOD_YM,
                   KPI_VALUE,
                   BATCH_ID,
                   REVIEW_STATUS,
                   REVIEW_USER,
                   REVIEW_DATE,
                   CRT_DATE,
                   CRT_USER,
                   MDF_DATE,
                   MDF_USER
            FROM kpi.KPI_DATA
            WHERE PERIOD_YM = @PeriodYm;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            new { PeriodYm = periodYm },
            _session.CurrentTransaction,
            cancellationToken: ct);
        return (await conn.QueryAsync<KpiData>(cmd)).ToList();
    }

    public async Task<long> AddDataAsync(KpiData entity, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO kpi.KPI_DATA
                (DEALER_ID, INDICATOR_ID, PERIOD_YM, KPI_VALUE, BATCH_ID, REVIEW_STATUS, CRT_USER)
            OUTPUT INSERTED.DATA_ID
            VALUES
                (@DealerId, @IndicatorId, @PeriodYm, @KpiValue, @BatchId, @ReviewStatus, @CrtUser);
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
            _session.CurrentTransaction,
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<long>(cmd);
    }

    public async Task<int> UpdateDataValueAsync(KpiData entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE kpi.KPI_DATA
            SET KPI_VALUE = @KpiValue,
                BATCH_ID = @BatchId,
                REVIEW_STATUS = @ReviewStatus,
                MDF_DATE = SYSDATETIME(),
                MDF_USER = @MdfUser
            WHERE DATA_ID = @DataId;
            """;

        var conn = await _session.GetOpenConnectionAsync(ct);
        var cmd = new CommandDefinition(
            sql,
            entity,
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
}
