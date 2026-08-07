using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DGPM_SPM.Infrastructure.Persistence;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        // Production／IIS 不會載入 User Secrets；缺漏或空白都會在第一次 DI 解析時失敗（勿用空字串蒙混）。
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. " +
                "Development: dotnet user-secrets set. Production/IIS: set ConnectionStrings__DefaultConnection.");
        }

        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);

    public (string Server, string Database) GetTargetInfo()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        return (
            string.IsNullOrWhiteSpace(builder.DataSource) ? "(unknown)" : builder.DataSource,
            string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(unknown)" : builder.InitialCatalog);
    }
}
