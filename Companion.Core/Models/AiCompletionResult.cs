namespace Companion.Core.Models;

public sealed record AiCompletionResult(
    string Content,
    string Provider,
    string Model,
    AiUsage Usage,
    long LatencyMs,
    string? FinishReason = null);
