using System.Data;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IParameterRepository Parameters { get; }
    IExchangeRateRepository ExchangeRates { get; }
    IRegionRepository Regions { get; }
    IDealerRepository Dealers { get; }
    IKpiIndicatorRepository KpiIndicators { get; }
    IKpiUserDataScopeRepository KpiUserDataScopes { get; }
    IKpiImportRepository KpiImports { get; }
    IKpiDataRepository KpiDatas { get; }
    IKpiChangeLogRepository KpiChangeLogs { get; }

    bool HasActiveTransaction { get; }

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
