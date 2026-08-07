using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DGPM_SPM.Integration.Tests;

/// <summary>
/// 以 <see cref="WebApplicationFactory{TEntryPoint}"/> 啟動真實 Api 宿主。
/// 需已設定連線字串；JWT 與 env 由記憶體設定覆寫，避免依賴本機 User Secrets。
/// </summary>
public sealed class SpmApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = IntegrationTestSettings.ConnectionString
            ?? throw new InvalidOperationException(
                "SpmApiFactory 需要 ConnectionStrings__DefaultConnection。");

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = IntegrationTestSettings.TestJwtSecret,
                ["JwtSettings:Issuer"] = "DGPM_SPM.Api",
                ["JwtSettings:Audience"] = "DGPM_SPM.Api",
                ["JwtSettings:ExpirationHours"] = "24",
                ["JwtSettings:AccessTokenMinutes"] = "240",
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["env:name"] = "sit"
            });
        });
    }
}
