using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Companion.Tests;

public sealed class OperatingSystemIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task OperatingSystem_GeneratesRoutineAndSchedulesAgentRun()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var routineResponse = await authenticatedClient.PostAsJsonAsync("/api/os/routines/MorningStartup/generate", new { });
        routineResponse.EnsureSuccessStatusCode();
        using var routineDocument = JsonDocument.Parse(await routineResponse.Content.ReadAsStringAsync());
        var routine = routineDocument.RootElement.GetProperty("run");

        Assert.Equal("MorningStartup", routine.GetProperty("routineType").GetString());
        Assert.Equal("Scheduled", routine.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, routine.GetProperty("scheduledAgentRunId").GetGuid());
        Assert.Contains("open task", routine.GetProperty("summary").GetString(), StringComparison.OrdinalIgnoreCase);

        using var optimizeResponse = await authenticatedClient.PostAsync("/api/os/context/optimize", null);
        optimizeResponse.EnsureSuccessStatusCode();
        using var optimizeDocument = JsonDocument.Parse(await optimizeResponse.Content.ReadAsStringAsync());
        Assert.Equal("ContextOptimization", optimizeDocument.RootElement.GetProperty("run").GetProperty("routineType").GetString());

        using var runsResponse = await authenticatedClient.GetAsync("/api/os/runs");
        runsResponse.EnsureSuccessStatusCode();
        using var runsDocument = JsonDocument.Parse(await runsResponse.Content.ReadAsStringAsync());
        Assert.Contains(runsDocument.RootElement.EnumerateArray(), x => x.GetProperty("routineType").GetString() == "MorningStartup");
        Assert.Contains(runsDocument.RootElement.EnumerateArray(), x => x.GetProperty("routineType").GetString() == "ContextOptimization");

        using var agentRunsResponse = await authenticatedClient.GetAsync("/api/agent-runs");
        agentRunsResponse.EnsureSuccessStatusCode();
        using var agentRunsDocument = JsonDocument.Parse(await agentRunsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            agentRunsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == routine.GetProperty("scheduledAgentRunId").GetGuid() &&
                (x.GetProperty("status").GetString() == "Pending" || x.GetProperty("status").GetString() == "Completed"));
    }

    private static async Task<string> LoginSeedAdminAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "local.user@companion-core.local",
            password = "CompanionDev123!"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }
}
