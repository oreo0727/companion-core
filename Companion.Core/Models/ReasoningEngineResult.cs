namespace Companion.Core.Models;

public sealed record ReasoningEngineResult(
    CompanionContext Context,
    string Reply,
    IReadOnlyList<CompanionInsight> Insights,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<ToolRequest> ToolRequests,
    AiCompletionResult? Completion,
    string? Provider,
    string? Model,
    bool UsedFallback,
    string? FailureReason);
