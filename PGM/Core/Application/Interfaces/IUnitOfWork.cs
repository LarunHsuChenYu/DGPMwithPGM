using System.Data;

namespace PGM.Core.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IMenuRepository Menus { get; }
    IFunctionRepository Functions { get; }
    IParameterRepository Parameters { get; }
    IAuthenticationLogRepository AuthenticationLogs { get; }

    bool HasActiveTransaction { get; }

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
