using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class ConnectorIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task ConnectorDiscovery_Works()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var response = await authenticatedClient.GetAsync("/api/connectors");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Contains(
            document.RootElement.EnumerateArray(),
            x => x.GetProperty("definition").GetProperty("provider").GetString() == "LocalCalendar");
        Assert.Contains(
            document.RootElement.EnumerateArray(),
            x => x.GetProperty("definition").GetProperty("provider").GetString() == "LocalEmail");
    }

    [Fact]
    public async Task LocalCalendarImport_Works_AndSyncRunIsRecorded()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var displayName = $"Calendar {Guid.NewGuid():N}";
        var title = $"Planning Review {Guid.NewGuid():N}";

        using var response = await authenticatedClient.PostAsJsonAsync("/api/connectors/local-calendar/import", new
        {
            displayName,
            events = new[]
            {
                new
                {
                    externalId = $"evt-{Guid.NewGuid():N}",
                    title,
                    description = "Quarterly planning review.",
                    location = "Room 100",
                    startUtc = DateTime.UtcNow.AddHours(2),
                    endUtc = DateTime.UtcNow.AddHours(3),
                    isAllDay = false
                }
            }
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var connectionId = document.RootElement.GetProperty("connection").GetProperty("id").GetGuid();
        var syncRunId = document.RootElement.GetProperty("syncRun").GetProperty("id").GetGuid();
        Assert.Equal(1, document.RootElement.GetProperty("eventsImported").GetInt32());

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
            Assert.True(await dbContext.ConnectorConnections.AnyAsync(x => x.Id == connectionId));
            Assert.True(await dbContext.ConnectorSyncRuns.AnyAsync(x => x.Id == syncRunId && x.Status == ConnectorSyncRunStatus.Completed));
            Assert.True(await dbContext.CalendarEventSnapshots.AnyAsync(x => x.ConnectorConnectionId == connectionId && x.Title == title));
        }

        using var syncResponse = await authenticatedClient.PostAsync($"/api/connectors/{connectionId}/sync", null);
        syncResponse.EnsureSuccessStatusCode();
        using var syncDocument = JsonDocument.Parse(await syncResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", syncDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpcomingEvents_AppearInBriefing_AndCalendarEndpoint()
    {
        using var client = factory.CreateClient();
        var email = $"briefing-calendar-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";
        await RegisterAsync(client, email, password, "Briefing Calendar User");
        var token = await LoginAsync(client, email, password);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var title = $"Board Meeting {Guid.NewGuid():N}";
        await ImportCalendarAsync(
            authenticatedClient,
            $"Briefing Calendar {Guid.NewGuid():N}",
            new[]
            {
                new
                {
                    externalId = $"evt-{Guid.NewGuid():N}",
                    title,
                    description = "Board check-in",
                    location = "",
                    startUtc = DateTime.UtcNow.AddHours(4),
                    endUtc = DateTime.UtcNow.AddHours(5),
                    isAllDay = false
                }
            });

        using var briefingResponse = await authenticatedClient.GetAsync("/api/companion/briefing");
        briefingResponse.EnsureSuccessStatusCode();
        using var briefingDocument = JsonDocument.Parse(await briefingResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            briefingDocument.RootElement.GetProperty("upcomingCalendarEvents").EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);

        using var calendarResponse = await authenticatedClient.GetAsync("/api/calendar/events");
        calendarResponse.EnsureSuccessStatusCode();
        using var calendarDocument = JsonDocument.Parse(await calendarResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            calendarDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);

        var insightMessages = briefingDocument.RootElement.GetProperty("chiefOfStaffInsights")
            .EnumerateArray()
            .Select(x => x.GetProperty("message").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        Assert.Contains(insightMessages, x => x!.Contains("does not have a location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CalendarEventsTool_ReturnsEvents_AndAudit()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var title = $"Launch Deadline {Guid.NewGuid():N}";
        await ImportCalendarAsync(
            authenticatedClient,
            $"Tool Calendar {Guid.NewGuid():N}",
            new[]
            {
                new
                {
                    externalId = $"evt-{Guid.NewGuid():N}",
                    title,
                    description = "Hard launch deadline.",
                    location = "HQ",
                    startUtc = DateTime.UtcNow.AddHours(1),
                    endUtc = DateTime.UtcNow.AddHours(2),
                    isAllDay = false
                }
            });

        var toolId = await GetToolIdByNameAsync(authenticatedClient, "CalendarEvents");
        using var response = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new
            {
                daysAhead = 7
            }
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Completed", document.RootElement.GetProperty("execution").GetProperty("status").GetString());

        using var outputDocument = JsonDocument.Parse(document.RootElement.GetProperty("execution").GetProperty("outputJson").GetString()!);
        Assert.Contains(
            outputDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ConnectorConnected", eventTypes);
        Assert.Contains("ConnectorSyncStarted", eventTypes);
        Assert.Contains("ConnectorSyncCompleted", eventTypes);
        Assert.Contains("CalendarEventsViewed", eventTypes);
    }

    [Fact]
    public async Task UserIsolation_Works_ForConnectors_AndCalendarEvents()
    {
        using var client = factory.CreateClient();
        var userAEmail = $"calendar-a-{Guid.NewGuid():N}@example.com";
        var userBEmail = $"calendar-b-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, userAEmail, password, "Calendar User A");
        await RegisterAsync(client, userBEmail, password, "Calendar User B");

        var tokenA = await LoginAsync(client, userAEmail, password);
        var tokenB = await LoginAsync(client, userBEmail, password);

        using var userAClient = factory.CreateClient();
        using var userBClient = factory.CreateClient();
        userAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        userBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var displayName = $"Private Calendar {Guid.NewGuid():N}";
        var title = $"Private Event {Guid.NewGuid():N}";
        await ImportCalendarAsync(
            userBClient,
            displayName,
            new[]
            {
                new
                {
                    externalId = $"evt-{Guid.NewGuid():N}",
                    title,
                    description = "Only user B should see this event.",
                    location = "Secret",
                    startUtc = DateTime.UtcNow.AddHours(6),
                    endUtc = DateTime.UtcNow.AddHours(7),
                    isAllDay = false
                }
            });

        using var userAConnectors = await userAClient.GetAsync("/api/connectors");
        userAConnectors.EnsureSuccessStatusCode();
        using var userAConnectorsDocument = JsonDocument.Parse(await userAConnectors.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userAConnectorsDocument.RootElement.EnumerateArray()
                .SelectMany(x => x.GetProperty("connections").EnumerateArray()),
            x => x.GetProperty("displayName").GetString() == displayName);

        using var userACalendar = await userAClient.GetAsync("/api/calendar/events");
        userACalendar.EnsureSuccessStatusCode();
        using var userACalendarDocument = JsonDocument.Parse(await userACalendar.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userACalendarDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);
    }

    [Fact]
    public async Task LocalEmailImport_Search_Briefing_AndTool_Work()
    {
        using var client = factory.CreateClient();
        var email = $"briefing-email-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";
        await RegisterAsync(client, email, password, "Briefing Email User");
        var token = await LoginAsync(client, email, password);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subject = $"Urgent invoice deadline {Guid.NewGuid():N}";
        using var importResponse = await authenticatedClient.PostAsJsonAsync("/api/connectors/local-email/import", new
        {
            displayName = $"Local Email {Guid.NewGuid():N}",
            messages = new[]
            {
                new
                {
                    externalId = $"msg-{Guid.NewGuid():N}",
                    subject,
                    fromName = "Billing Team",
                    fromAddress = "billing@example.com",
                    toAddresses = new[] { email },
                    preview = "Urgent payment due tomorrow. Invoice attached.",
                    body = "Please review the attached invoice before the payment deadline.",
                    receivedUtc = DateTime.UtcNow.AddHours(-2),
                    isRead = false,
                    hasAttachments = true,
                    isAnswered = false
                }
            }
        });

        importResponse.EnsureSuccessStatusCode();
        using var importDocument = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, importDocument.RootElement.GetProperty("messagesImported").GetInt32());

        using var messagesResponse = await authenticatedClient.GetAsync("/api/email/messages");
        messagesResponse.EnsureSuccessStatusCode();
        using var messagesDocument = JsonDocument.Parse(await messagesResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            messagesDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("subject").GetString() == subject &&
                 x.GetProperty("hasAttachments").GetBoolean() &&
                 !x.GetProperty("isAnswered").GetBoolean());

        using var searchResponse = await authenticatedClient.GetAsync("/api/email/search?query=invoice");
        searchResponse.EnsureSuccessStatusCode();
        using var searchDocument = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            searchDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("subject").GetString() == subject);

        using var briefingResponse = await authenticatedClient.GetAsync("/api/companion/briefing");
        briefingResponse.EnsureSuccessStatusCode();
        using var briefingDocument = JsonDocument.Parse(await briefingResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            briefingDocument.RootElement.GetProperty("importantRecentEmails").EnumerateArray(),
            x => x.GetProperty("subject").GetString() == subject);

        var insightMessages = briefingDocument.RootElement.GetProperty("chiefOfStaffInsights")
            .EnumerateArray()
            .Select(x => x.GetProperty("message").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        Assert.Contains(insightMessages, x => x!.Contains("urgent", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(insightMessages, x => x!.Contains("bill, payment, or deadline", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(insightMessages, x => x!.Contains("attachment", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(insightMessages, x => x!.Contains("unanswered", StringComparison.OrdinalIgnoreCase));

        var toolId = await GetToolIdByNameAsync(authenticatedClient, "EmailSearch");
        using var toolResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new
            {
                query = "deadline",
                limit = 5
            }
        });

        toolResponse.EnsureSuccessStatusCode();
        using var toolDocument = JsonDocument.Parse(await toolResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", toolDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());
        using var outputDocument = JsonDocument.Parse(toolDocument.RootElement.GetProperty("execution").GetProperty("outputJson").GetString()!);
        Assert.Contains(
            outputDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("subject").GetString() == subject);

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EmailMessagesViewed", eventTypes);
        Assert.Contains("EmailSearchPerformed", eventTypes);
    }

    [Fact]
    public async Task UserIsolation_Works_ForEmailSnapshots()
    {
        using var client = factory.CreateClient();
        var userAEmail = $"email-a-{Guid.NewGuid():N}@example.com";
        var userBEmail = $"email-b-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, userAEmail, password, "Email User A");
        await RegisterAsync(client, userBEmail, password, "Email User B");

        var tokenA = await LoginAsync(client, userAEmail, password);
        var tokenB = await LoginAsync(client, userBEmail, password);

        using var userAClient = factory.CreateClient();
        using var userBClient = factory.CreateClient();
        userAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        userBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var displayName = $"Private Email {Guid.NewGuid():N}";
        var subject = $"Private Message {Guid.NewGuid():N}";
        await ImportEmailAsync(
            userBClient,
            displayName,
            new[]
            {
                new
                {
                    externalId = $"msg-{Guid.NewGuid():N}",
                    subject,
                    fromName = "Private Sender",
                    fromAddress = "private@example.com",
                    toAddresses = new[] { userBEmail },
                    preview = "Only user B should see this message.",
                    body = "Private email body.",
                    receivedUtc = DateTime.UtcNow.AddHours(-1),
                    isRead = false,
                    hasAttachments = false,
                    isAnswered = false
                }
            });

        using var userAConnectors = await userAClient.GetAsync("/api/connectors");
        userAConnectors.EnsureSuccessStatusCode();
        using var userAConnectorsDocument = JsonDocument.Parse(await userAConnectors.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userAConnectorsDocument.RootElement.EnumerateArray()
                .SelectMany(x => x.GetProperty("connections").EnumerateArray()),
            x => x.GetProperty("displayName").GetString() == displayName);

        using var userAEmailMessages = await userAClient.GetAsync("/api/email/messages");
        userAEmailMessages.EnsureSuccessStatusCode();
        using var userAEmailDocument = JsonDocument.Parse(await userAEmailMessages.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userAEmailDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("subject").GetString() == subject);
    }

    private async Task<HttpClient> CreateSeedAdminClientAsync()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "local.user@companion-core.local", "CompanionDev123!");
        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.Dispose();
        return authenticatedClient;
    }

    private static async Task ImportCalendarAsync(HttpClient client, string displayName, object[] events)
    {
        using var response = await client.PostAsJsonAsync("/api/connectors/local-calendar/import", new
        {
            displayName,
            events
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task ImportEmailAsync(HttpClient client, string displayName, object[] messages)
    {
        using var response = await client.PostAsJsonAsync("/api/connectors/local-email/import", new
        {
            displayName,
            messages
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> GetToolIdByNameAsync(HttpClient client, string toolName)
    {
        using var response = await client.GetAsync("/api/tools");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateArray()
            .Where(x => x.GetProperty("name").GetString() == toolName)
            .Select(x => x.GetProperty("id").GetGuid())
            .Single();
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
