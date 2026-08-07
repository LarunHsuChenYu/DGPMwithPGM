namespace PGM.Integration.Tests;

/// <summary>
/// 整合測試啟用條件。預設 <c>dotnet test</c> 在未設定連線字串時略過需 DB 的案例，避免 CI／本機全紅。
/// </summary>
public static class IntegrationTestSettings
{
    /// <summary>
    /// 讀取順序：
    /// 1. 環境變數 <c>ConnectionStrings__DefaultConnection</c>（與 Api／IIS 一致）
    /// 2. 環境變數 <c>PGM_INTEGRATION_CONNECTION</c>（僅測試用別名）
    /// </summary>
    public static string? ConnectionString =>
        FirstNonEmpty(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            Environment.GetEnvironmentVariable("PGM_INTEGRATION_CONNECTION"));

    public static bool HasConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);

    public const string DbSkipReason =
        "未設定 ConnectionStrings__DefaultConnection（或 PGM_INTEGRATION_CONNECTION）；" +
        "略過需真實 SQL Server 的整合測試。啟用方式見 tests/Integration.Tests/README.md。";

    /// <summary>測試宿主用的 JWT（僅整合測試注入，非正式金鑰）。</summary>
    public const string TestJwtSecret = "dgpm-spm-integration-test-secret-key-32c!";

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
