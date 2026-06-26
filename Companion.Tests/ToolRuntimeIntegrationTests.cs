using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class ToolRuntimeIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task ToolDiscovery_Works()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await authenticatedClient.GetAsync("/api/tools");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var toolNames = document.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("MemorySearch", toolNames);
        Assert.Contains("CreateTask", toolNames);
        Assert.Contains("GetBriefing", toolNames);
        Assert.Contains("DesktopCaptureScreenshot", toolNames);
        Assert.Contains("DesktopWriteFile", toolNames);
    }

    [Fact]
    public async Task UserCanExecutePermittedTool()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var toolId = await GetToolIdByNameAsync(authenticatedClient, "GetBriefing");
        using var executeResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new { }
        });

        executeResponse.EnsureSuccessStatusCode();
        using var executeDocument = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", executeDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            auditDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("eventType").GetString() == "ToolExecutionCompleted" &&
                 x.GetProperty("description").GetString()!.Contains("Tool=GetBriefing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnauthorizedTool_IsBlocked()
    {
        using var client = factory.CreateClient();
        var email = $"blocked-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, email, password, "Blocked Tool User");
        var token = await LoginAsync(client, email, password);

        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var toolId = await GetToolIdByNameAsync(authenticatedClient, "GetBriefing");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
            var userProfileId = await dbContext.UserProfiles
                .Where(x => x.Email == email)
                .Select(x => x.Id)
                .SingleAsync();
            var permission = await dbContext.ToolPermissions
                .SingleAsync(x => x.UserProfileId == userProfileId && x.ToolDefinitionId == toolId);
            permission.Allowed = false;
            await dbContext.SaveChangesAsync();
        }

        using var response = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new { }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApprovalRequiredTool_CreatesApproval_AndExecutesAfterApproval()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var toolId = await GetToolIdByNameAsync(authenticatedClient, "CreateTask");

        var title = $"Phase6 Task {Guid.NewGuid():N}";
        using var executeResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new
            {
                title,
                description = "Created through the tool runtime approval path."
            }
        });

        Assert.Equal(HttpStatusCode.Accepted, executeResponse.StatusCode);
        using var executeDocument = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
        var approvalRequestId = executeDocument.RootElement.GetProperty("approvalRequestId").GetGuid();
        var executionId = executeDocument.RootElement.GetProperty("execution").GetProperty("id").GetGuid();
        Assert.Equal("AwaitingApproval", executeDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());

        using var approveResponse = await authenticatedClient.PostAsync($"/api/approvals/{approvalRequestId}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        using var executionsResponse = await authenticatedClient.GetAsync("/api/tools/executions");
        executionsResponse.EnsureSuccessStatusCode();
        using var executionsDocument = JsonDocument.Parse(await executionsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            executionsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == executionId &&
                 x.GetProperty("status").GetString() == "Completed");

        using var tasksResponse = await authenticatedClient.GetAsync("/api/tasks");
        tasksResponse.EnsureSuccessStatusCode();
        using var tasksDocument = JsonDocument.Parse(await tasksResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            tasksDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("title").GetString() == title);
    }

    [Fact]
    public async Task FailedTool_CapturesError()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var toolId = await GetToolIdByNameAsync(authenticatedClient, "MemorySearch");

        using var executeResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new { }
        });

        executeResponse.EnsureSuccessStatusCode();
        using var executeDocument = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Failed", executeDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());
        Assert.Contains("query", executeDocument.RootElement.GetProperty("execution").GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            auditDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("eventType").GetString() == "ToolExecutionFailed" &&
                 x.GetProperty("description").GetString()!.Contains("Tool=MemorySearch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DesktopLowRiskTool_ExecutesImmediately()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var toolId = await GetToolIdByNameAsync(authenticatedClient, "DesktopCaptureScreenshot");

        using var executeResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new { }
        });

        executeResponse.EnsureSuccessStatusCode();
        using var executeDocument = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", executeDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());
        Assert.True(executeDocument.RootElement.GetProperty("executedImmediately").GetBoolean());
    }

    [Fact]
    public async Task DesktopHighRiskTool_RequiresApproval_AndWritesInsideAutomationRoot()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var toolId = await GetToolIdByNameAsync(authenticatedClient, "DesktopWriteFile");
        var fileName = $"phase16-{Guid.NewGuid():N}.txt";

        using var executeResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new
            {
                path = fileName,
                content = "desktop automation approval path",
                overwrite = true
            }
        });

        Assert.Equal(HttpStatusCode.Accepted, executeResponse.StatusCode);
        using var executeDocument = JsonDocument.Parse(await executeResponse.Content.ReadAsStringAsync());
        var approvalRequestId = executeDocument.RootElement.GetProperty("approvalRequestId").GetGuid();
        var executionId = executeDocument.RootElement.GetProperty("execution").GetProperty("id").GetGuid();
        Assert.Equal("AwaitingApproval", executeDocument.RootElement.GetProperty("execution").GetProperty("status").GetString());

        using var approveResponse = await authenticatedClient.PostAsync($"/api/approvals/{approvalRequestId}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        using var executionsResponse = await authenticatedClient.GetAsync("/api/tools/executions");
        executionsResponse.EnsureSuccessStatusCode();
        using var executionsDocument = JsonDocument.Parse(await executionsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            executionsDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == executionId &&
                 x.GetProperty("status").GetString() == "Completed");

        var writtenPath = Path.Combine(Path.GetTempPath(), "companion-desktop", fileName);
        Assert.True(File.Exists(writtenPath));
        Assert.Equal("desktop automation approval path", await File.ReadAllTextAsync(writtenPath));
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

    private static async Task<string> LoginSeedAdminAsync(HttpClient client)
    {
        return await LoginAsync(client, "local.user@companion-core.local", "CompanionDev123!");
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
