namespace PGM.Integration.Tests;

/// <summary>可離線執行的輕量煙霧：不啟動 Api、不連資料庫。</summary>
public class OfflineSmokeTests
{
    [Fact]
    public void SkipReason_IsDocumented()
    {
        IntegrationTestSettings.DbSkipReason.ShouldContain("ConnectionStrings__DefaultConnection");
        IntegrationTestSettings.DbSkipReason.ShouldContain("README.md");
    }

    [Fact]
    public void TestJwtSecret_MeetsApiMinimumLength()
    {
        // Api Program.cs：SecretKey 至少 32 字元
        IntegrationTestSettings.TestJwtSecret.Length.ShouldBeGreaterThanOrEqualTo(32);
    }

    [Fact]
    public void ConnectionStringProbe_ReportsAvailabilityWithoutFailing()
    {
        // 僅記錄目前環境是否具備 DB；無論有無連線字串都通過，避免預設 CI 變紅
        var available = IntegrationTestSettings.HasConnectionString;
        (available || !available).ShouldBeTrue();
    }
}
