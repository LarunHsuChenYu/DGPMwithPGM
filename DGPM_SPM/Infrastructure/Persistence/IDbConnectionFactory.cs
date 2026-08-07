using System.Data.Common;

namespace DGPM_SPM.Infrastructure.Persistence;

/// <summary>
/// 負責產生原始 DbConnection。獨立成 factory 是為了：
///   1. 讓 DbSession 可以 lazy 建立連線
///   2. 未來要換 provider（Postgres / MySQL）只改這裡
///   3. 測試時可以 mock
/// </summary>
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();

    /// <summary>僅回傳 Data Source／Initial Catalog，供健康檢查比對環境（不含帳密）。</summary>
    (string Server, string Database) GetTargetInfo();
}
