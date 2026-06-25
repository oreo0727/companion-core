using System.Text.Json;
using System.Text.Json.Serialization;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public class OllamaProvider(
    HttpClient httpClient,
    IAiProviderConfigurationService configurationService) : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => AiProviderNames.Ollama;

    public async Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = await configurationService.GetConfigurationAsync(ProviderName, cancellationToken)
            ?? throw new InvalidOperationException("Ollama configuration was not found.");

        var payload = new
        {
            model = configuration.Model,
            stream = false,
            messages = request.Messages.Select(x => new { role = x.Role, content = x.Content }),
            options = new
            {
                temperature = request.Temperature ?? (double)configuration.Temperature,
                num_predict = request.MaxTokens ?? configuration.MaxTokens
            }
        };

        using var httpRequest = AiProviderExecution.CreateJsonRequest(
            HttpMethod.Post,
            configuration.ApiBaseUrl,
            "api/chat",
            payload,
            JsonOptions);

        var executionResult = await AiProviderExecution.SendAsync(
            httpClient,
            httpRequest,
            ProviderName,
            configuration.TimeoutSeconds,
            cancellationToken);

        using var document = JsonDocument.Parse(executionResult.Body);
        var root = document.RootElement;
        var content = root.TryGetProperty("message", out var messageElement) &&
                      messageElement.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString() ?? string.Empty
            : string.Empty;
        var usage = new AiUsage(
            root.TryGetProperty("prompt_eval_count", out var promptTokens) ? promptTokens.GetInt32() : 0,
            root.TryGetProperty("eval_count", out var completionTokens) ? completionTokens.GetInt32() : 0);

        return new AiCompletionResult(
            content,
            ProviderName,
            configuration.Model,
            usage,
            executionResult.LatencyMs,
            root.TryGetProperty("done_reason", out var finishReason) ? finishReason.GetString() : null);
    }
}
