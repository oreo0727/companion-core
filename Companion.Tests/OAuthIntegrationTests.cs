using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class OAuthIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task OAuthProviderDiscovery_AndConsentLifecycle_Work()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var providersResponse = await authenticatedClient.GetAsync("/api/oauth/providers");
        providersResponse.EnsureSuccessStatusCode();
        using var providersDocument = JsonDocument.Parse(await providersResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            providersDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("provider").GetString() == "Google");
        Assert.Contains(
            providersDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("provider").GetString() == "Microsoft");

        using var connectorsResponse = await authenticatedClient.GetAsync("/api/connectors");
        connectorsResponse.EnsureSuccessStatusCode();
        using var connectorsDocument = JsonDocument.Parse(await connectorsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            connectorsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("definition").GetProperty("provider").GetString() == "GoogleCalendar" &&
                 x.GetProperty("definition").GetProperty("supportsOAuth").GetBoolean());

        using var authorizeResponse = await authenticatedClient.PostAsJsonAsync("/api/oauth/Google/authorize", new
        {
            connectorProvider = "GoogleCalendar",
            displayName = "Primary Google Calendar",
            redirectUri = "http://localhost:3000/connectors/oauth/callback",
            scopes = new[] { "https://www.googleapis.com/auth/calendar.readonly", "openid" }
        });
        authorizeResponse.EnsureSuccessStatusCode();
        using var authorizeDocument = JsonDocument.Parse(await authorizeResponse.Content.ReadAsStringAsync());
        var state = authorizeDocument.RootElement.GetProperty("state").GetString();
        var authorizationUrl = authorizeDocument.RootElement.GetProperty("authorizationUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(state));
        Assert.Contains("code_challenge=", authorizationUrl);
        Assert.Contains("state=", authorizationUrl);

        using var callbackResponse = await authenticatedClient.PostAsJsonAsync("/api/oauth/Google/callback", new
        {
            state,
            code = "callback-code",
            accessToken = "plain-access-token",
            refreshToken = "plain-refresh-token",
            expiresUtc = DateTime.UtcNow.AddHours(1),
            subject = "google-user-1",
            displayName = "Primary Google Calendar",
            scopes = new[] { "openid", "https://www.googleapis.com/auth/calendar.readonly" }
        });
        callbackResponse.EnsureSuccessStatusCode();
        using var callbackDocument = JsonDocument.Parse(await callbackResponse.Content.ReadAsStringAsync());
        var connectionId = callbackDocument.RootElement.GetProperty("connectionId").GetGuid();
        Assert.Equal("Connected", callbackDocument.RootElement.GetProperty("status").GetString());

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
            var connection = await dbContext.ConnectorConnections.SingleAsync(x => x.Id == connectionId);
            Assert.NotEqual("plain-access-token", connection.AccessTokenEncrypted);
            Assert.NotEqual("plain-refresh-token", connection.RefreshTokenEncrypted);
            Assert.True(await dbContext.OAuthConsentGrants.AnyAsync(x =>
                x.ConnectorConnectionId == connectionId &&
                x.Subject == "google-user-1" &&
                x.RevokedUtc == null));
        }

        using var connectionsResponse = await authenticatedClient.GetAsync("/api/oauth/connections");
        connectionsResponse.EnsureSuccessStatusCode();
        using var connectionsDocument = JsonDocument.Parse(await connectionsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            connectionsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("connectionId").GetGuid() == connectionId);

        using var disconnectResponse = await authenticatedClient.DeleteAsync($"/api/oauth/connections/{connectionId}");
        disconnectResponse.EnsureSuccessStatusCode();
        using var disconnectDocument = JsonDocument.Parse(await disconnectResponse.Content.ReadAsStringAsync());
        Assert.Equal("Disconnected", disconnectDocument.RootElement.GetProperty("status").GetString());
        Assert.True(disconnectDocument.RootElement.TryGetProperty("revokedUtc", out var revokedUtc) && revokedUtc.ValueKind == JsonValueKind.String);

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("OAuthAuthorizationStarted", eventTypes);
        Assert.Contains("OAuthConsentGranted", eventTypes);
        Assert.Contains("OAuthConsentRevoked", eventTypes);
    }

    private async Task<HttpClient> CreateSeedAdminClientAsync()
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "local.user@companion-core.local",
            password = "CompanionDev123!"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Expected seeded admin access token.");

        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.Dispose();
        return authenticatedClient;
    }
}
