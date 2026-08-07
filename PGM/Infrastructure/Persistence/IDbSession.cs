using System.Data;
using System.Data.Common;

namespace PGM.Infrastructure.Persistence;

/// <summary>
/// 環境資料庫 Session（Scoped）。同一個 HTTP request 內：
///   - Repository 拿到的 Connection 都是同一條
///   - 若 UnitOfWork 開了 Transaction，Repository 執行 Dapper 呼叫時會共用該 transaction
/// </summary>
public interface IDbSession
{
    /// <summary>目前是否有開啟中的 transaction。</summary>
    bool HasActiveTransaction { get; }

    /// <summary>取得該 request 共用的 transaction；未開啟時為 null。</summary>
    DbTransaction? CurrentTransaction { get; }

    /// <summary>取得已 Open 的 connection（第一次呼叫時才建立與開啟）。</summary>
    Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
