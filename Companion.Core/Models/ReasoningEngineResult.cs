namespace Companion.Core.Models;

public sealed record ReasoningEngineResult(
    CompanionContext Context,
    string Reply,
    IReadOnlyList<CompanionInsight> Insights,
    IReadOnlyList<string> Recommendations,
    AiCompletionResult? Completion,
    string? Provider,
    string? Model,
    bool UsedFallback,
    string? FailureReason);
