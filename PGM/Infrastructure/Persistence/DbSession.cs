using System.Data;
using System.Data.Common;

namespace PGM.Infrastructure.Persistence;

public class DbSession : IDbSession, IAsyncDisposable
{
    private readonly IDbConnectionFactory _factory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbSession(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public bool HasActiveTransaction => _transaction is not null;

    public DbTransaction? CurrentTransaction => _transaction;

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _connection ??= _factory.CreateConnection();

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        return _connection;
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active. Nested transactions are not supported.");
        }

        var conn = await GetOpenConnectionAsync(ct);
        _transaction = await conn.BeginTransactionAsync(isolationLevel, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return; // 已回滾或從未開啟，直接略過（等冪）

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // 若請求結束時仍有未 commit 的 transaction，強制回滾避免資料半死不活
        if (_transaction is not null)
        {
            try { await _transaction.RollbackAsync(); } catch { /* 已無法拋出到呼叫端 */ }
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _disposed = true;
    }
}
