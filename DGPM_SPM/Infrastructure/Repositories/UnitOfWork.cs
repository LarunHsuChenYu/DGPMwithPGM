using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Infrastructure.Persistence;

namespace DGPM_SPM.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbSession _session;

    public UnitOfWork(
        IDbSession session,
        IUserRepository users,
        IParameterRepository parameters,
        IExchangeRateRepository exchangeRates,
        IRegionRepository regions,
        IDealerRepository dealers,
        IKpiIndicatorRepository kpiIndicators,
        IKpiUserDataScopeRepository kpiUserDataScopes,
        IKpiImportRepository kpiImports,
        IKpiDataRepository kpiDatas,
        IKpiChangeLogRepository kpiChangeLogs)
    {
        _session = session;
        Users = users;
        Parameters = parameters;
        ExchangeRates = exchangeRates;
        Regions = regions;
        Dealers = dealers;
        KpiIndicators = kpiIndicators;
        KpiUserDataScopes = kpiUserDataScopes;
        KpiImports = kpiImports;
        KpiDatas = kpiDatas;
        KpiChangeLogs = kpiChangeLogs;
    }

    public IUserRepository Users { get; }
    public IParameterRepository Parameters { get; }
    public IExchangeRateRepository ExchangeRates { get; }
    public IRegionRepository Regions { get; }
    public IDealerRepository Dealers { get; }
    public IKpiIndicatorRepository KpiIndicators { get; }
    public IKpiUserDataScopeRepository KpiUserDataScopes { get; }
    public IKpiImportRepository KpiImports { get; }
    public IKpiDataRepository KpiDatas { get; }
    public IKpiChangeLogRepository KpiChangeLogs { get; }

    public bool HasActiveTransaction => _session.HasActiveTransaction;

    public Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
        => _session.BeginTransactionAsync(isolationLevel, ct);

    public Task CommitAsync(CancellationToken ct = default) => _session.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default) => _session.RollbackAsync(ct);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
