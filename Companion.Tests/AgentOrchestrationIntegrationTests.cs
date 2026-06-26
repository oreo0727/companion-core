using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class AgentOrchestrationIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task AgentCatalog_IsDiscoverable()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await authenticatedClient.GetAsync("/api/agents");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = document.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ChiefOfStaff", names);
        Assert.Contains("Planner", names);
        Assert.Contains("Research", names);
        Assert.Contains("Coder", names);
        Assert.Contains("Writer", names);
        Assert.Contains("Travel", names);
        Assert.Contains("Finance", names);
        Assert.Contains("Health", names);
        Assert.Contains("Home", names);
    }

    [Fact]
    public async Task ChiefOfStaff_DelegatesToSpecialistAgentRuns()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var queueResponse = await authenticatedClient.PostAsJsonAsync("/api/agent-runs", new
        {
            agentName = "ChiefOfStaff",
            input = $"Plan a code task, check home sensors, and track the invoice budget {Guid.NewGuid():N}."
        });
        queueResponse.EnsureSuccessStatusCode();
        using var queueDocument = JsonDocument.Parse(await queueResponse.Content.ReadAsStringAsync());
        var parentRunId = queueDocument.RootElement.GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var runtime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
            Assert.True(await runtime.ProcessPendingRunsAsync() >= 1);
            Assert.True(await runtime.ProcessPendingRunsAsync() >= 1);
        }

        using var runsResponse = await authenticatedClient.GetAsync("/api/agent-runs");
        runsResponse.EnsureSuccessStatusCode();
        using var runsDocument = JsonDocument.Parse(await runsResponse.Content.ReadAsStringAsync());
        var runs = runsDocument.RootElement.EnumerateArray().ToList();

        Assert.Contains(
            runs,
            x => x.GetProperty("id").GetGuid() == parentRunId &&
                x.GetProperty("status").GetString() == "Completed" &&
                x.GetProperty("agentDefinitionId").ValueKind == JsonValueKind.String);
        Assert.Contains(
            runs,
            x => x.GetProperty("parentAgentRunId").ValueKind == JsonValueKind.String &&
                x.GetProperty("parentAgentRunId").GetGuid() == parentRunId &&
                x.GetProperty("agentName").GetString() == "Coder" &&
                x.GetProperty("status").GetString() == "Completed");
        Assert.Contains(
            runs,
            x => x.GetProperty("parentAgentRunId").ValueKind == JsonValueKind.String &&
                x.GetProperty("parentAgentRunId").GetGuid() == parentRunId &&
                x.GetProperty("agentName").GetString() == "Home");
        Assert.Contains(
            runs,
            x => x.GetProperty("parentAgentRunId").ValueKind == JsonValueKind.String &&
                x.GetProperty("parentAgentRunId").GetGuid() == parentRunId &&
                x.GetProperty("agentName").GetString() == "Finance");

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Contains(auditDocument.RootElement.EnumerateArray(), x => x.GetProperty("eventType").GetString() == "AgentRunDelegated");
        Assert.Contains(auditDocument.RootElement.EnumerateArray(), x => x.GetProperty("eventType").GetString() == "AgentRunCompleted");
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
