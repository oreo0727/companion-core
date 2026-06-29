using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Companion.Tests;

public sealed class OperationsIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task SetupStatus_IsAvailableBeforeAuthentication()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/setup/status");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.False(document.RootElement.GetProperty("isFirstRun").GetBoolean());
        Assert.True(document.RootElement.GetProperty("userCount").GetInt32() >= 1);
        Assert.Equal("local.user@companion-core.local", document.RootElement.GetProperty("seededLocalAdminEmail").GetString());
        Assert.NotEmpty(document.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task OperationalEndpoints_ReturnDailyUseStatus()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var healthResponse = await authenticatedClient.GetAsync("/api/system/health");
        healthResponse.EnsureSuccessStatusCode();
        using var healthDocument = JsonDocument.Parse(await healthResponse.Content.ReadAsStringAsync());
        Assert.True(healthDocument.RootElement.GetProperty("databaseOk").GetBoolean());

        using var diagnosticsResponse = await authenticatedClient.GetAsync("/api/system/diagnostics");
        diagnosticsResponse.EnsureSuccessStatusCode();
        using var diagnosticsDocument = JsonDocument.Parse(await diagnosticsResponse.Content.ReadAsStringAsync());
        Assert.True(diagnosticsDocument.RootElement.GetProperty("counts").GetProperty("memories").GetInt32() >= 1);
        Assert.NotEmpty(diagnosticsDocument.RootElement.GetProperty("providers").EnumerateArray());
        Assert.NotEmpty(diagnosticsDocument.RootElement.GetProperty("connectors").EnumerateArray());

        using var logsResponse = await authenticatedClient.GetAsync("/api/system/logs");
        logsResponse.EnsureSuccessStatusCode();

        using var smokeResponse = await authenticatedClient.GetAsync("/api/system/smoke-test/status");
        smokeResponse.EnsureSuccessStatusCode();
        using var smokeDocument = JsonDocument.Parse(await smokeResponse.Content.ReadAsStringAsync());
        Assert.Equal("ReadyToRun", smokeDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ProviderAndConnectorTests_ReturnClearStatus()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var providerResponse = await authenticatedClient.PostAsync("/api/settings/ai/Ollama/test", null);
        providerResponse.EnsureSuccessStatusCode();
        using var providerDocument = JsonDocument.Parse(await providerResponse.Content.ReadAsStringAsync());
        Assert.Equal("Ollama", providerDocument.RootElement.GetProperty("provider").GetString());
        Assert.Contains(providerDocument.RootElement.GetProperty("status").GetString(), new[] { "Succeeded", "Failed" });

        using var connectorResponse = await authenticatedClient.PostAsJsonAsync("/api/connectors/LocalCalendar/test", new { });
        connectorResponse.EnsureSuccessStatusCode();
        using var connectorDocument = JsonDocument.Parse(await connectorResponse.Content.ReadAsStringAsync());
        Assert.Equal("LocalCalendar", connectorDocument.RootElement.GetProperty("provider").GetString());
        Assert.Equal("Succeeded", connectorDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ChatFallback_RespondsNaturallyToGreeting()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var response = await authenticatedClient.PostAsJsonAsync("/api/chat", new
        {
            message = "hello"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var reply = document.RootElement.GetProperty("reply").GetString() ?? string.Empty;

        Assert.True(document.RootElement.GetProperty("usedFallback").GetBoolean());
        Assert.Contains("Hi", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("next step plan", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BackupExportAndImport_WorkAcrossUsers()
    {
        using var client = factory.CreateClient();
        var sourceEmail = $"backup-source-{Guid.NewGuid():N}@example.com";
        var targetEmail = $"backup-target-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";
        var summary = $"Backup memory {Guid.NewGuid():N}";

        await RegisterAsync(client, sourceEmail, password, "Backup Source");
        await RegisterAsync(client, targetEmail, password, "Backup Target");

        using var sourceClient = factory.CreateClient();
        sourceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, sourceEmail, password));
        using var createMemoryResponse = await sourceClient.PostAsJsonAsync("/api/memories", new
        {
            type = "Preference",
            summary,
            content = "This memory should survive backup and restore.",
            confidence = 0.91m,
            source = "OperationsIntegrationTest",
            importance = 4,
            sensitivity = "Normal"
        });
        createMemoryResponse.EnsureSuccessStatusCode();

        using var exportResponse = await sourceClient.GetAsync("/api/system/backup/export");
        exportResponse.EnsureSuccessStatusCode();
        var backup = JsonNode.Parse(await exportResponse.Content.ReadAsStringAsync())!.AsObject();

        using var targetClient = factory.CreateClient();
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, targetEmail, password));
        using var importResponse = await targetClient.PostAsJsonAsync("/api/system/backup/import", backup);
        importResponse.EnsureSuccessStatusCode();
        using var importDocument = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        Assert.True(importDocument.RootElement.GetProperty("importedCounts").GetProperty("memories").GetInt32() >= 1);

        using var memoriesResponse = await targetClient.GetAsync("/api/memories");
        memoriesResponse.EnsureSuccessStatusCode();
        using var memoriesDocument = JsonDocument.Parse(await memoriesResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            memoriesDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("summary").GetString() == summary);
    }

    private async Task<HttpClient> CreateSeedAdminClientAsync()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(factory.CreateClient(), "local.user@companion-core.local", "CompanionDev123!"));
        return client;
    }

    private static async Task RegisterAsync(HttpClient client, string email, string password, string displayName)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            displayName
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Expected access token in login response.");
    }
}
