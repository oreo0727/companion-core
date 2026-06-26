using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Companion.Tests;

public sealed class VoiceIntegrationTests(PostgresTestApiFactory factory) : IClassFixture<PostgresTestApiFactory>
{
    [Fact]
    public async Task VoiceSessionLifecycle_Works()
    {
        using var authenticatedClient = await CreateSeedAdminClientAsync();

        using var providersResponse = await authenticatedClient.GetAsync("/api/voice/providers");
        providersResponse.EnsureSuccessStatusCode();
        using var providersDocument = JsonDocument.Parse(await providersResponse.Content.ReadAsStringAsync());
        Assert.Contains(providersDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == "OpenAI" && x.GetProperty("providerType").GetString() == "SpeechToText");
        Assert.Contains(providersDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == "LocalWhisper");
        Assert.Contains(providersDocument.RootElement.EnumerateArray(), x => x.GetProperty("name").GetString() == "LocalPiper");

        using var wakeResponse = await authenticatedClient.PostAsJsonAsync("/api/voice/wake", new
        {
            wakePhrase = "Companion",
            speechToTextProvider = "LocalWhisper",
            textToSpeechProvider = "LocalPiper"
        });
        wakeResponse.EnsureSuccessStatusCode();
        using var wakeDocument = JsonDocument.Parse(await wakeResponse.Content.ReadAsStringAsync());
        Assert.True(wakeDocument.RootElement.GetProperty("isWakeSession").GetBoolean());

        using var startResponse = await authenticatedClient.PostAsJsonAsync("/api/voice/sessions", new
        {
            speechToTextProvider = "LocalWhisper",
            textToSpeechProvider = "LocalPiper"
        });
        startResponse.EnsureSuccessStatusCode();
        using var startDocument = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        var sessionId = startDocument.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Listening", startDocument.RootElement.GetProperty("status").GetString());

        using var transcribeResponse = await authenticatedClient.PostAsJsonAsync($"/api/voice/sessions/{sessionId}/transcribe", new
        {
            simulatedTranscript = "What is my current briefing?",
            language = "en-US"
        });
        transcribeResponse.EnsureSuccessStatusCode();
        using var transcribeDocument = JsonDocument.Parse(await transcribeResponse.Content.ReadAsStringAsync());
        Assert.Equal("What is my current briefing?", transcribeDocument.RootElement.GetProperty("transcript").GetString());

        using var converseResponse = await authenticatedClient.PostAsJsonAsync($"/api/voice/sessions/{sessionId}/conversation", new
        {
            simulatedTranscript = "Tell me one thing that needs attention.",
            language = "en-US"
        });
        converseResponse.EnsureSuccessStatusCode();
        using var converseDocument = JsonDocument.Parse(await converseResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(converseDocument.RootElement.GetProperty("reply").GetString()));
        Assert.True(converseDocument.RootElement.GetProperty("streamChunks").GetArrayLength() >= 1);
        Assert.False(string.IsNullOrWhiteSpace(converseDocument.RootElement.GetProperty("speech").GetProperty("audioContentBase64").GetString()));

        using var speakResponse = await authenticatedClient.PostAsJsonAsync($"/api/voice/sessions/{sessionId}/speak", new
        {
            text = "This is a direct speech response.",
            voice = "calm",
            format = "text/plain;base64"
        });
        speakResponse.EnsureSuccessStatusCode();

        using var interruptResponse = await authenticatedClient.PostAsJsonAsync($"/api/voice/sessions/{sessionId}/interrupt", new
        {
            reason = "User started speaking"
        });
        interruptResponse.EnsureSuccessStatusCode();
        using var interruptDocument = JsonDocument.Parse(await interruptResponse.Content.ReadAsStringAsync());
        Assert.Equal("Interrupted", interruptDocument.RootElement.GetProperty("status").GetString());

        using var historyResponse = await authenticatedClient.GetAsync($"/api/voice/sessions/{sessionId}/history");
        historyResponse.EnsureSuccessStatusCode();
        using var historyDocument = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        var types = historyDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("type").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Transcription", types);
        Assert.Contains("UserUtterance", types);
        Assert.Contains("AssistantSpeech", types);
        Assert.Contains("Interruption", types);

        using var auditResponse = await authenticatedClient.GetAsync("/api/audit");
        auditResponse.EnsureSuccessStatusCode();
        using var auditDocument = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        var eventTypes = auditDocument.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("eventType").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("VoiceSessionStarted", eventTypes);
        Assert.Contains("VoiceTranscribed", eventTypes);
        Assert.Contains("VoiceSpoken", eventTypes);
        Assert.Contains("VoiceInterrupted", eventTypes);
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
