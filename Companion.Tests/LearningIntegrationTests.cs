using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Companion.Tests;

public sealed class LearningIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task LearningProfile_AggregatesBehavioralSignals()
    {
        using var client = factory.CreateClient();
        var token = await LoginSeedAdminAsync(client);
        using var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var chatResponse = await authenticatedClient.PostAsJsonAsync("/api/chat", new
        {
            message = $"Remember that learning integration {Guid.NewGuid():N} prefers concise updates and I need to follow up tomorrow."
        });
        chatResponse.EnsureSuccessStatusCode();
        using var chatDocument = JsonDocument.Parse(await chatResponse.Content.ReadAsStringAsync());
        var conversationId = chatDocument.RootElement.GetProperty("conversationId").GetGuid();

        using var suggestionsResponse = await authenticatedClient.GetAsync("/api/suggestions");
        suggestionsResponse.EnsureSuccessStatusCode();
        using var suggestionsDocument = JsonDocument.Parse(await suggestionsResponse.Content.ReadAsStringAsync());
        var suggestionId = suggestionsDocument.RootElement.EnumerateArray()
            .First(x => x.GetProperty("status").GetString() == "Pending")
            .GetProperty("id")
            .GetGuid();

        using var ignoreResponse = await authenticatedClient.PostAsync($"/api/suggestions/{suggestionId}/ignore", null);
        ignoreResponse.EnsureSuccessStatusCode();

        using var ratingResponse = await authenticatedClient.PostAsJsonAsync("/api/learning/ratings", new
        {
            conversationId,
            rating = 5,
            comment = "Helpful and concise."
        });
        ratingResponse.EnsureSuccessStatusCode();

        var briefingToolId = await GetToolIdByNameAsync(authenticatedClient, "GetBriefing");
        using var toolResponse = await authenticatedClient.PostAsJsonAsync($"/api/tools/{briefingToolId}/execute", new
        {
            input = new { }
        });
        toolResponse.EnsureSuccessStatusCode();

        using var profileResponse = await authenticatedClient.GetAsync("/api/learning/profile");
        profileResponse.EnsureSuccessStatusCode();
        using var profileDocument = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        var profile = profileDocument.RootElement;

        Assert.True(profile.GetProperty("ignoredSuggestions").GetInt32() >= 1);
        Assert.True(profile.GetProperty("toolUsageCount").GetInt32() >= 1);
        Assert.True(profile.GetProperty("conversationRatingCount").GetInt32() >= 1);
        Assert.True(profile.GetProperty("averageConversationRating").GetDecimal() >= 5m);

        using var eventsResponse = await authenticatedClient.GetAsync("/api/learning/events");
        eventsResponse.EnsureSuccessStatusCode();
        using var eventsDocument = JsonDocument.Parse(await eventsResponse.Content.ReadAsStringAsync());
        Assert.Contains(eventsDocument.RootElement.EnumerateArray(), x => x.GetProperty("eventType").GetString() == "SuggestionIgnored");
        Assert.Contains(eventsDocument.RootElement.EnumerateArray(), x => x.GetProperty("eventType").GetString() == "ConversationRated");
        Assert.Contains(eventsDocument.RootElement.EnumerateArray(), x => x.GetProperty("eventType").GetString() == "ToolUsed");
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
