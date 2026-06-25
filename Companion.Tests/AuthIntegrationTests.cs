using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Companion.Tests;

public sealed class AuthIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task Register_CreatesAccount_ReturnsTokenAndPreferences()
    {
        using var client = factory.CreateClient();
        var email = $"register-{Guid.NewGuid():N}@example.com";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Companion123",
            displayName = "Register Test"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("accessToken").GetString()));
        Assert.Equal(email, document.RootElement.GetProperty("me").GetProperty("profile").GetProperty("email").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("me").GetProperty("preferences").GetArrayLength());
    }

    [Fact]
    public async Task Login_Logout_AndJwtValidation_Work()
    {
        using var client = factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, email, password, "Login Test");
        var token = await LoginAsync(client, email, password);

        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var meResponse = await authenticatedClient.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        using var logoutResponse = await authenticatedClient.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var staleTokenResponse = await authenticatedClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleTokenResponse.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedAccess_IsBlocked()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/memories");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserACannotAccessUserBData()
    {
        using var client = factory.CreateClient();

        var userAEmail = $"user-a-{Guid.NewGuid():N}@example.com";
        var userBEmail = $"user-b-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, userAEmail, password, "User A");
        await RegisterAsync(client, userBEmail, password, "User B");

        var tokenA = await LoginAsync(client, userAEmail, password);
        var tokenB = await LoginAsync(client, userBEmail, password);

        using var userAClient = factory.CreateClient();
        using var userBClient = factory.CreateClient();
        userAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        userBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var summary = $"Private Memory {Guid.NewGuid():N}";
        using var createMemoryResponse = await userBClient.PostAsJsonAsync("/api/memories", new
        {
            type = "Preference",
            summary,
            content = "Only user B should see this.",
            confidence = 0.91m,
            source = "IntegrationTest",
            importance = 4,
            sensitivity = "Normal"
        });
        createMemoryResponse.EnsureSuccessStatusCode();
        using var createdMemory = JsonDocument.Parse(await createMemoryResponse.Content.ReadAsStringAsync());
        var memoryId = createdMemory.RootElement.GetProperty("id").GetGuid();

        using var userAMemoriesResponse = await userAClient.GetAsync("/api/memories");
        userAMemoriesResponse.EnsureSuccessStatusCode();
        using var userAMemories = JsonDocument.Parse(await userAMemoriesResponse.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userAMemories.RootElement.EnumerateArray(),
            element => string.Equals(element.GetProperty("summary").GetString(), summary, StringComparison.Ordinal));

        using var archiveAttempt = await userAClient.PutAsync($"/api/memories/{memoryId}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, archiveAttempt.StatusCode);
    }

    [Fact]
    public async Task AuditEvents_AreCreated()
    {
        using var client = factory.CreateClient();
        var email = $"audit-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, email, password, "Audit Test");
        var token = await LoginAsync(client, email, password);

        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var createMemoryResponse = await authenticatedClient.PostAsJsonAsync("/api/memories", new
        {
            type = "Preference",
            summary = $"Audit Memory {Guid.NewGuid():N}",
            content = "Track this memory creation in the audit log.",
            confidence = 0.77m,
            source = "IntegrationTest",
            importance = 3,
            sensitivity = "Normal"
        });
        createMemoryResponse.EnsureSuccessStatusCode();

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Login", eventTypes);
        Assert.Contains("MemoryCreated", eventTypes);
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

public sealed class PostgresTestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databaseName = $"companion_test_{Guid.NewGuid():N}";
    private readonly string adminConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    public string ConnectionString => $"Host=localhost;Port=5432;Database={databaseName};Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
    }

    public new async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{databaseName}'
                  AND pid <> pg_backend_pid();
                """;
            await terminate.ExecuteNonQueryAsync();
        }

        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
        await drop.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:SigningKey"] = "CompanionCoreIntegrationTestSigningKey-2026",
                ["Jwt:Issuer"] = "CompanionCore.Tests",
                ["Jwt:Audience"] = "CompanionCore.Tests.Client"
            });
        });
    }
}
