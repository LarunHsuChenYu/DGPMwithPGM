namespace DGPM_SPM.Integration.Tests;

/// <summary>
/// 需真實 DB 的整合測試。未設定連線字串時於探索階段 Skip，預設 <c>dotnet test</c> 不會因此變紅。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDbFactAttribute : FactAttribute
{
    public RequiresDbFactAttribute()
    {
        if (!IntegrationTestSettings.HasConnectionString)
            Skip = IntegrationTestSettings.DbSkipReason;
    }
}
