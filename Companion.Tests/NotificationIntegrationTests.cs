using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class NotificationIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task ManualReminder_CreatesNotification_AndCanBeRead()
    {
        using var authenticatedClient = await CreateRegisteredClientAsync();
        var title = $"Manual reminder {Guid.NewGuid():N}";

        using var createResponse = await authenticatedClient.PostAsJsonAsync("/api/reminders", new
        {
            title,
            description = "Remember this during integration testing.",
            dueUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        createResponse.EnsureSuccessStatusCode();

        using var remindersResponse = await authenticatedClient.GetAsync("/api/reminders");
        remindersResponse.EnsureSuccessStatusCode();
        using var remindersDocument = JsonDocument.Parse(await remindersResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            remindersDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);

        await ProcessRemindersAsync();

        using var notificationsResponse = await authenticatedClient.GetAsync("/api/notifications");
        notificationsResponse.EnsureSuccessStatusCode();
        using var notificationsDocument = JsonDocument.Parse(await notificationsResponse.Content.ReadAsStringAsync());
        var notification = notificationsDocument.RootElement.EnumerateArray()
            .Single(x => x.GetProperty("title").GetString() == title);
        var notificationId = notification.GetProperty("id").GetGuid();
        Assert.Equal("Unread", notification.GetProperty("status").GetString());

        using var readResponse = await authenticatedClient.PostAsync($"/api/notifications/{notificationId}/read", null);
        readResponse.EnsureSuccessStatusCode();
        using var readDocument = JsonDocument.Parse(await readResponse.Content.ReadAsStringAsync());
        Assert.Equal("Read", readDocument.RootElement.GetProperty("status").GetString());

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ReminderCreated", eventTypes);
        Assert.Contains("NotificationRead", eventTypes);
    }

    [Fact]
    public async Task WorkerProcessing_CreatesTaskApprovalAndCalendarNotifications()
    {
        using var authenticatedClient = await CreateRegisteredClientAsync();
        var taskTitle = $"Overdue task {Guid.NewGuid():N}";
        var calendarTitle = $"Upcoming event {Guid.NewGuid():N}";

        using var taskResponse = await authenticatedClient.PostAsJsonAsync("/api/tasks", new
        {
            title = taskTitle,
            description = "Task due reminder coverage.",
            dueDateUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        taskResponse.EnsureSuccessStatusCode();

        using var approvalResponse = await authenticatedClient.PostAsJsonAsync("/api/approvals", new
        {
            type = "IntegrationApproval",
            reason = "Approval pending reminder coverage.",
            payload = "{}",
            riskLevel = "Medium"
        });
        approvalResponse.EnsureSuccessStatusCode();

        using var calendarImport = await authenticatedClient.PostAsJsonAsync("/api/connectors/local-calendar/import", new
        {
            displayName = $"Reminder Calendar {Guid.NewGuid():N}",
            events = new[]
            {
                new
                {
                    externalId = $"evt-{Guid.NewGuid():N}",
                    title = calendarTitle,
                    description = "Calendar reminder coverage.",
                    location = "Room 1",
                    startUtc = DateTime.UtcNow.AddMinutes(30),
                    endUtc = DateTime.UtcNow.AddMinutes(60),
                    isAllDay = false
                }
            }
        });
        calendarImport.EnsureSuccessStatusCode();

        await ProcessRemindersAsync();

        using var notificationsResponse = await authenticatedClient.GetAsync("/api/notifications");
        notificationsResponse.EnsureSuccessStatusCode();
        using var notificationsDocument = JsonDocument.Parse(await notificationsResponse.Content.ReadAsStringAsync());
        var titles = notificationsDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("title").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains(titles, x => x!.Contains(taskTitle, StringComparison.Ordinal));
        Assert.Contains(titles, x => x!.Contains("Approval pending", StringComparison.Ordinal));
        Assert.Contains(titles, x => x!.Contains(calendarTitle, StringComparison.Ordinal));

        using var briefingResponse = await authenticatedClient.GetAsync("/api/companion/briefing");
        briefingResponse.EnsureSuccessStatusCode();
        using var briefingDocument = JsonDocument.Parse(await briefingResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            briefingDocument.RootElement.GetProperty("overdueTasks").EnumerateArray(),
            x => x.GetProperty("title").GetString() == taskTitle);
        Assert.True(briefingDocument.RootElement.GetProperty("upcomingReminders").GetArrayLength() >= 0);

        using var dashboardResponse = await authenticatedClient.GetAsync("/api/companion/dashboard");
        dashboardResponse.EnsureSuccessStatusCode();
        using var dashboardDocument = JsonDocument.Parse(await dashboardResponse.Content.ReadAsStringAsync());
        Assert.True(dashboardDocument.RootElement.GetProperty("unreadNotifications").GetInt32() >= 3);
    }

    [Fact]
    public async Task ReminderAndNotificationTools_Work()
    {
        using var authenticatedClient = await CreateRegisteredClientAsync();
        var title = $"Tool reminder {Guid.NewGuid():N}";
        var tools = await GetToolsAsync(authenticatedClient);
        var createReminderToolId = tools["CreateReminder"];
        var listNotificationsToolId = tools["ListNotifications"];

        using var createToolResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{createReminderToolId}/execute", new
        {
            input = new
            {
                title,
                description = "Created by tool.",
                dueUtc = DateTime.UtcNow.AddMinutes(-1)
            }
        });
        createToolResponse.EnsureSuccessStatusCode();
        using var createToolDocument = JsonDocument.Parse(await createToolResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", createToolDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());

        await ProcessRemindersAsync();

        using var listToolResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{listNotificationsToolId}/execute", new
        {
            input = new
            {
                includeRead = false
            }
        });
        listToolResponse.EnsureSuccessStatusCode();
        using var listToolDocument = JsonDocument.Parse(await listToolResponse.Content.ReadAsStringAsync());
        using var outputDocument = JsonDocument.Parse(listToolDocument.RootElement.GetProperty("execution").GetProperty("outputJson").GetString()!);
        Assert.Contains(
            outputDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notificationService.ProcessDueRemindersAsync();
    }

    private async Task<HttpClient> CreateRegisteredClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"notifications-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";
        await RegisterAsync(client, email, password, "Notification Test");
        var token = await LoginAsync(client, email, password);

        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.Dispose();
        return authenticatedClient;
    }

    private static async Task<Dictionary<string, Guid>> GetToolsAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/tools");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("name").GetString()!,
                x => x.GetProperty("id").GetGuid(),
                StringComparer.Ordinal);
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
