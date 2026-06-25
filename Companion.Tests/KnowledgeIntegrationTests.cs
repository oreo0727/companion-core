using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Companion.Tests;

public sealed class KnowledgeIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task ImportDocument_CreatesSourceDocumentAndChunks_AndAudit()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var title = $"Engineering Notes {Guid.NewGuid():N}";
        var sourceName = $"Notebook {Guid.NewGuid():N}";
        var content = BuildLargeMarkdownContent("retrieval boundary", 6);

        using var response = await authenticatedClient.PostAsJsonAsync("/api/knowledge/import", new
        {
            sourceName,
            sourceType = "Manual",
            sourceDescription = "Imported from the Phase 7 integration test.",
            title,
            content,
            mimeType = "text/markdown"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("chunkCount").GetInt32() >= 2);
        var sourceId = document.RootElement.GetProperty("source").GetProperty("id").GetGuid();
        var importedDocumentId = document.RootElement.GetProperty("document").GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
            var source = await dbContext.KnowledgeSources
                .Include(x => x.Documents)
                .SingleAsync(x => x.Id == sourceId);
            var chunks = await dbContext.KnowledgeChunks
                .Where(x => x.KnowledgeDocumentId == importedDocumentId)
                .OrderBy(x => x.ChunkIndex)
                .ToListAsync();

            Assert.Equal(sourceName, source.Name);
            Assert.Contains(source.Documents, x => x.Id == importedDocumentId && x.Title == title);
            Assert.True(chunks.Count >= 2);
            Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.MetadataJson)));
        }

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            auditDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("eventType").GetString() == "KnowledgeDocumentImported" &&
                 x.GetProperty("description").GetString()!.Contains(title, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_Works_AndAudits()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var uniqueTerm = $"semantic-lighthouse-{Guid.NewGuid():N}";
        await ImportKnowledgeAsync(
            authenticatedClient,
            $"Search Source {Guid.NewGuid():N}",
            "Manual",
            $"Search Doc {Guid.NewGuid():N}",
            $"This note explains the {uniqueTerm} retrieval approach for Companion.");

        using var response = await authenticatedClient.GetAsync($"/api/knowledge/search?query={Uri.EscapeDataString(uniqueTerm)}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            document.RootElement.EnumerateArray(),
            x => x.GetProperty("content").GetString()!.Contains(uniqueTerm, StringComparison.Ordinal));

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            auditDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("eventType").GetString() == "KnowledgeSearchPerformed" &&
                 x.GetProperty("description").GetString()!.Contains(uniqueTerm, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ToolCanQueryKnowledge()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var uniqueTerm = $"tool-query-{Guid.NewGuid():N}";
        await ImportKnowledgeAsync(
            authenticatedClient,
            $"Tool Source {Guid.NewGuid():N}",
            "Manual",
            $"Tool Doc {Guid.NewGuid():N}",
            $"This document records the phrase {uniqueTerm} for the KnowledgeSearch tool.");

        var toolId = await GetToolIdByNameAsync(authenticatedClient, "KnowledgeSearch");
        using var response = await authenticatedClient.PostAsJsonAsync($"/api/tools/{toolId}/execute", new
        {
            input = new
            {
                query = uniqueTerm
            }
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Completed", document.RootElement.GetProperty("execution").GetProperty("status").GetString());

        using var outputDocument = JsonDocument.Parse(document.RootElement.GetProperty("execution").GetProperty("outputJson").GetString()!);
        Assert.Contains(
            outputDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("excerpt").GetString()!.Contains(uniqueTerm, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ContextBuilder_IncludesRelevantKnowledge()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();
        var userEmail = "local.user@companion-core.local";
        var uniqueTerm = $"context-layer-{Guid.NewGuid():N}";
        await ImportKnowledgeAsync(
            authenticatedClient,
            $"Context Source {Guid.NewGuid():N}",
            "Manual",
            $"Context Doc {Guid.NewGuid():N}",
            $"Reference material for the {uniqueTerm} workflow and retrieval path.");

        Guid userProfileId;
        Guid conversationId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
            userProfileId = await dbContext.UserProfiles
                .Where(x => x.Email == userEmail)
                .Select(x => x.Id)
                .SingleAsync();

            conversationId = Guid.NewGuid();
            dbContext.Conversations.Add(new Conversation
            {
                Id = conversationId,
                UserProfileId = userProfileId,
                Title = "Knowledge Context Test",
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                LastMessageUtc = DateTime.UtcNow,
                ActiveTopic = uniqueTerm
            });
            dbContext.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = Companion.Core.Enums.MessageRole.User,
                Content = $"Please use the {uniqueTerm} reference when replying.",
                CreatedUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var contextBuilder = scope.ServiceProvider.GetRequiredService<IContextBuilder>();
            var context = await contextBuilder.BuildContextAsync(userProfileId, conversationId);

            Assert.Contains(
                context.RelevantKnowledge,
                x => x.Chunk.Content.Contains(uniqueTerm, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task UserACannotAccessUserBKnowledge()
    {
        using var client = factory.CreateClient();

        var userAEmail = $"knowledge-a-{Guid.NewGuid():N}@example.com";
        var userBEmail = $"knowledge-b-{Guid.NewGuid():N}@example.com";
        const string password = "Companion123";

        await RegisterAsync(client, userAEmail, password, "Knowledge User A");
        await RegisterAsync(client, userBEmail, password, "Knowledge User B");

        var tokenA = await LoginAsync(client, userAEmail, password);
        var tokenB = await LoginAsync(client, userBEmail, password);

        using var userAClient = factory.CreateClient();
        using var userBClient = factory.CreateClient();
        userAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        userBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var uniqueTerm = $"private-knowledge-{Guid.NewGuid():N}";
        var sourceName = $"Private Source {Guid.NewGuid():N}";
        await ImportKnowledgeAsync(
            userBClient,
            sourceName,
            "Manual",
            $"Private Doc {Guid.NewGuid():N}",
            $"Only user B should retrieve the {uniqueTerm} note.");

        using var userASearchResponse = await userAClient.GetAsync($"/api/knowledge/search?query={Uri.EscapeDataString(uniqueTerm)}");
        userASearchResponse.EnsureSuccessStatusCode();
        using var userASearchDocument = JsonDocument.Parse(await userASearchResponse.Content.ReadAsStringAsync());
        Assert.Empty(userASearchDocument.RootElement.EnumerateArray());

        using var userASourcesResponse = await userAClient.GetAsync("/api/knowledge/sources");
        userASourcesResponse.EnsureSuccessStatusCode();
        using var userASourcesDocument = JsonDocument.Parse(await userASourcesResponse.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            userASourcesDocument.RootElement.EnumerateArray(),
            x => x.GetProperty("name").GetString() == sourceName);
    }

    private async Task<HttpClient> CreateSeedAdminClientAsync()
    {
        var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.Dispose();
        return authenticatedClient;
    }

    private static async Task ImportKnowledgeAsync(
        HttpClient client,
        string sourceName,
        string sourceType,
        string title,
        string content,
        string mimeType = "text/plain")
    {
        using var response = await client.PostAsJsonAsync("/api/knowledge/import", new
        {
            sourceName,
            sourceType,
            title,
            content,
            mimeType
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

    private static async Task<string> LoginSeedAdminAsync(HttpClient client)
    {
        return await LoginAsync(client, "local.user@companion-core.local", "CompanionDev123!");
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

    private static string BuildLargeMarkdownContent(string term, int sections)
    {
        var builder = new StringBuilder();

        for (var index = 1; index <= sections; index++)
        {
            builder.AppendLine($"## Section {index}");
            builder.AppendLine($"This section covers the {term} policy details for section {index}.");
            builder.AppendLine(new string('x', 220));
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
