namespace Companion.Core.Models;

public sealed record AiCompletionRequest(
    IReadOnlyList<AiMessage> Messages,
    double? Temperature = null,
    int? MaxTokens = null,
    bool ExpectJson = false);
