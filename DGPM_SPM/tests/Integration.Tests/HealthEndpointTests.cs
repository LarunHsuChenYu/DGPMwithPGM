using System.Net;
using System.Text.Json;

namespace DGPM_SPM.Integration.Tests;

/// <summary>
/// 需真實 SQL Server 的端到端煙霧。未設定連線字串時以 <see cref="RequiresDbFactAttribute"/> Skip。
/// </summary>
public class HealthEndpointTests
{
    [RequiresDbFact]
    public async Task GetHealth_WhenDbAvailable_Returns200Healthy()
    {
        await using var factory = new SpmApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Health");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("data", out var data).ShouldBeTrue($"回應應含 data：{json}");
        data.TryGetProperty("status", out var status).ShouldBeTrue($"data 應含 status：{json}");
        status.GetString().ShouldBe("Healthy");

        data.TryGetProperty("database", out var database).ShouldBeTrue(json);
        database.TryGetProperty("connected", out var connected).ShouldBeTrue(json);
        connected.GetBoolean().ShouldBeTrue();
    }
}
