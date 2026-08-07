using System.Data;
using PGM.Core.Application.Interfaces;
using PGM.Infrastructure.Persistence;

namespace PGM.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbSession _session;

    public UnitOfWork(
        IDbSession session,
        IUserRepository users,
        IRoleRepository roles,
        IMenuRepository menus,
        IFunctionRepository functions,
        IParameterRepository parameters,
        IAuthenticationLogRepository authenticationLogs)
    {
        _session = session;
        Users = users;
        Roles = roles;
        Menus = menus;
        Functions = functions;
        Parameters = parameters;
        AuthenticationLogs = authenticationLogs;
    }

    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }
    public IMenuRepository Menus { get; }
    public IFunctionRepository Functions { get; }
    public IParameterRepository Parameters { get; }
    public IAuthenticationLogRepository AuthenticationLogs { get; }

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
