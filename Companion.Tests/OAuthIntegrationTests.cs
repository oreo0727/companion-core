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
        Assert.Contains(
            connectorsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("definition").GetProperty("provider").GetString() == "GooglePeople" &&
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

    [Fact]
    public async Task ProductionReadConnectors_SyncIntoSnapshots()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N");

        var googleCalendarConnectionId = await ConnectOAuthAsync(authenticatedClient, "Google", "GoogleCalendar", $"Google Calendar {suffix}");
        var googleDriveConnectionId = await ConnectOAuthAsync(authenticatedClient, "Google", "GoogleDrive", $"Google Drive {suffix}");
        var gmailConnectionId = await ConnectOAuthAsync(authenticatedClient, "Google", "Gmail", $"Gmail {suffix}");
        var googlePeopleConnectionId = await ConnectOAuthAsync(authenticatedClient, "Google", "GooglePeople", $"Google People {suffix}");
        var microsoftCalendarConnectionId = await ConnectOAuthAsync(authenticatedClient, "Microsoft", "MicrosoftCalendar", $"Microsoft Calendar {suffix}");
        var oneDriveConnectionId = await ConnectOAuthAsync(authenticatedClient, "Microsoft", "OneDrive", $"OneDrive {suffix}");
        var outlookConnectionId = await ConnectOAuthAsync(authenticatedClient, "Microsoft", "OutlookMail", $"Outlook {suffix}");

        var googleCalendarTitle = $"Google planning {suffix}";
        await SyncAsync(authenticatedClient, googleCalendarConnectionId, new
        {
            items = new[]
            {
                new
                {
                    id = $"gcal-{suffix}",
                    summary = googleCalendarTitle,
                    description = "Google calendar read sync.",
                    location = "Online",
                    start = new { dateTime = DateTime.UtcNow.AddHours(2) },
                    end = new { dateTime = DateTime.UtcNow.AddHours(3) }
                }
            }
        });

        var microsoftCalendarTitle = $"Microsoft planning {suffix}";
        await SyncAsync(authenticatedClient, microsoftCalendarConnectionId, new
        {
            value = new[]
            {
                new
                {
                    id = $"mcal-{suffix}",
                    subject = microsoftCalendarTitle,
                    bodyPreview = "Microsoft calendar read sync.",
                    location = new { displayName = "Teams" },
                    start = new { dateTime = DateTime.UtcNow.AddHours(4) },
                    end = new { dateTime = DateTime.UtcNow.AddHours(5) }
                }
            }
        });

        var gmailSubject = $"Gmail bill {suffix}";
        await SyncAsync(authenticatedClient, gmailConnectionId, new
        {
            messages = new[]
            {
                new
                {
                    id = $"gmail-{suffix}",
                    subject = gmailSubject,
                    fromAddress = "billing@example.com",
                    fromName = "Billing",
                    toAddresses = "local.user@companion-core.local",
                    preview = "Payment deadline.",
                    body = "Invoice attached.",
                    receivedUtc = DateTime.UtcNow.AddHours(-1),
                    isRead = false,
                    hasAttachments = true,
                    isAnswered = false
                }
            }
        });

        var outlookSubject = $"Outlook followup {suffix}";
        await SyncAsync(authenticatedClient, outlookConnectionId, new
        {
            value = new[]
            {
                new
                {
                    id = $"outlook-{suffix}",
                    subject = outlookSubject,
                    from = new { emailAddress = new { name = "Client", address = "client@example.com" } },
                    toAddresses = "local.user@companion-core.local",
                    bodyPreview = "Can you respond?",
                    receivedDateTime = DateTime.UtcNow.AddHours(-2),
                    isRead = false,
                    hasAttachments = false,
                    isAnswered = false
                }
            }
        });

        var googleDriveName = $"Drive Spec {suffix}.md";
        await SyncAsync(authenticatedClient, googleDriveConnectionId, new
        {
            files = new[]
            {
                new
                {
                    id = $"drive-{suffix}",
                    name = googleDriveName,
                    mimeType = "text/markdown",
                    webViewLink = "https://drive.example/spec",
                    modifiedTime = DateTime.UtcNow.AddMinutes(-30),
                    description = "Drive document preview."
                }
            }
        });

        var contactName = $"Ada Lovelace {suffix}";
        await SyncAsync(authenticatedClient, googlePeopleConnectionId, new
        {
            connections = new[]
            {
                new
                {
                    resourceName = $"people/{suffix}",
                    names = new[] { new { displayName = contactName } },
                    emailAddresses = new[] { new { value = $"ada-{suffix}@example.com" } },
                    phoneNumbers = new[] { new { value = "555-0100" } },
                    organizations = new[] { new { name = "Analytical Engines" } },
                    photos = new[] { new { url = "https://people.example/photo.jpg" } }
                }
            }
        });

        var oneDriveName = $"OneDrive Notes {suffix}.docx";
        await SyncAsync(authenticatedClient, oneDriveConnectionId, new
        {
            value = new[]
            {
                new
                {
                    id = $"onedrive-{suffix}",
                    name = oneDriveName,
                    file = new { mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                    webUrl = "https://onedrive.example/notes",
                    lastModifiedDateTime = DateTime.UtcNow.AddMinutes(-20)
                }
            }
        });

        using var calendarResponse = await authenticatedClient.GetAsync("/api/calendar/events?daysAhead=7");
        calendarResponse.EnsureSuccessStatusCode();
        using var calendarDocument = JsonDocument.Parse(await calendarResponse.Content.ReadAsStringAsync());
        Assert.Contains(calendarDocument.RootElement.EnumerateArray(), x => x.GetProperty("title").GetString() == googleCalendarTitle);
        Assert.Contains(calendarDocument.RootElement.EnumerateArray(), x => x.GetProperty("title").GetString() == microsoftCalendarTitle);

        using var emailResponse = await authenticatedClient.GetAsync("/api/email/search?query=bill");
        emailResponse.EnsureSuccessStatusCode();
        using var emailDocument = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync());
        Assert.Contains(emailDocument.RootElement.EnumerateArray(), x => x.GetProperty("subject").GetString() == gmailSubject);

        using var fileResponse = await authenticatedClient.GetAsync("/api/files/documents?limit=50");
        fileResponse.EnsureSuccessStatusCode();
        using var fileDocument = JsonDocument.Parse(await fileResponse.Content.ReadAsStringAsync());
        Assert.Contains(fileDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == googleDriveName);
        Assert.Contains(fileDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == oneDriveName);

        using var fileSearchResponse = await authenticatedClient.GetAsync($"/api/files/search?query={Uri.EscapeDataString("Drive Spec")}");
        fileSearchResponse.EnsureSuccessStatusCode();
        using var fileSearchDocument = JsonDocument.Parse(await fileSearchResponse.Content.ReadAsStringAsync());
        Assert.Contains(fileSearchDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == googleDriveName);

        using var contactsResponse = await authenticatedClient.GetAsync($"/api/contacts/search?query={Uri.EscapeDataString("Ada")}");
        contactsResponse.EnsureSuccessStatusCode();
        using var contactsDocument = JsonDocument.Parse(await contactsResponse.Content.ReadAsStringAsync());
        Assert.Contains(contactsDocument.RootElement.EnumerateArray(), x => x.GetProperty("displayName").GetString() == contactName);

        using var toolsResponse = await authenticatedClient.GetAsync("/api/tools");
        toolsResponse.EnsureSuccessStatusCode();
        using var toolsDocument = JsonDocument.Parse(await toolsResponse.Content.ReadAsStringAsync());
        var toolNames = toolsDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("GetCalendarEvents", toolNames);
        Assert.Contains("FindFreeTime", toolNames);
        Assert.Contains("SearchEmail", toolNames);
        Assert.Contains("ReadEmail", toolNames);
        Assert.Contains("CreateDraftEmail", toolNames);
        Assert.Contains("SearchDrive", toolNames);
        Assert.Contains("ReadDocument", toolNames);
        Assert.Contains("FindContact", toolNames);
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

    private static async Task<Guid> ConnectOAuthAsync(
        HttpClient authenticatedClient,
        string provider,
        string connectorProvider,
        string displayName)
    {
        using var authorizeResponse = await authenticatedClient.PostAsJsonAsync($"/api/oauth/{provider}/authorize", new
        {
            connectorProvider,
            displayName,
            redirectUri = "http://localhost:3000/connectors/oauth/callback",
            scopes = ScopesFor(provider, connectorProvider)
        });
        authorizeResponse.EnsureSuccessStatusCode();
        using var authorizeDocument = JsonDocument.Parse(await authorizeResponse.Content.ReadAsStringAsync());
        var state = authorizeDocument.RootElement.GetProperty("state").GetString();

        using var callbackResponse = await authenticatedClient.PostAsJsonAsync($"/api/oauth/{provider}/callback", new
        {
            state,
            code = $"code-{Guid.NewGuid():N}",
            accessToken = $"access-{Guid.NewGuid():N}",
            refreshToken = $"refresh-{Guid.NewGuid():N}",
            expiresUtc = DateTime.UtcNow.AddHours(1),
            subject = $"{provider}-{connectorProvider}-{Guid.NewGuid():N}",
            displayName,
            scopes = ScopesFor(provider, connectorProvider)
        });
        callbackResponse.EnsureSuccessStatusCode();
        using var callbackDocument = JsonDocument.Parse(await callbackResponse.Content.ReadAsStringAsync());
        return callbackDocument.RootElement.GetProperty("connectionId").GetGuid();
    }

    private static string[] ScopesFor(string provider, string connectorProvider)
    {
        if (provider == "Google")
        {
            return connectorProvider switch
            {
                "GoogleCalendar" => ["openid", "email", "profile", "https://www.googleapis.com/auth/calendar.readonly"],
                "Gmail" => ["openid", "email", "profile", "https://www.googleapis.com/auth/gmail.readonly", "https://www.googleapis.com/auth/gmail.compose"],
                "GoogleDrive" => ["openid", "email", "profile", "https://www.googleapis.com/auth/drive.readonly"],
                "GooglePeople" => ["openid", "email", "profile", "https://www.googleapis.com/auth/contacts.readonly"],
                _ => ["openid", "email", "profile"]
            };
        }

        return ["openid", "profile"];
    }

    private static async Task SyncAsync(HttpClient authenticatedClient, Guid connectionId, object payload)
    {
        using var response = await authenticatedClient.PostAsJsonAsync($"/api/connectors/{connectionId}/sync", payload);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Completed", document.RootElement.GetProperty("status").GetString());
        Assert.True(document.RootElement.GetProperty("itemsSynced").GetInt32() >= 1);
    }
}
