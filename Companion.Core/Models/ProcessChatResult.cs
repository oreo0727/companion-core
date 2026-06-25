using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ProcessChatResult(
    Guid ConversationId,
    string Reply,
    IReadOnlyList<MemoryEntry> UsedMemories,
    IReadOnlyList<CompanionInsight> GeneratedInsights,
    IReadOnlyList<MemorySuggestion> MemorySuggestions,
    IReadOnlyList<GoalSuggestion> GoalSuggestions,
    IReadOnlyList<ProjectSuggestion> ProjectSuggestions,
    IReadOnlyList<TaskSuggestion> TaskSuggestions,
    IReadOnlyList<ApprovalRequest> ApprovalRequests,
    IReadOnlyList<OpenLoop> CreatedOpenLoops,
    string? Provider,
    string? Model,
    bool UsedFallback);
