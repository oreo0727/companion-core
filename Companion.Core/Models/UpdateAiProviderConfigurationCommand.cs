namespace Companion.Core.Models;

public sealed record UpdateAiProviderConfigurationCommand(
    string Provider,
    string Model,
    string ApiBaseUrl,
    string? ApiKey,
    bool IsEnabled,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds);
