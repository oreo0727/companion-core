using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ProcessChatResult(
    Guid ConversationId,
    string Reply,
    IReadOnlyList<MemoryEntry> SavedMemories,
    IReadOnlyList<TaskItem> CreatedTasks,
    IReadOnlyList<ApprovalRequest> ApprovalRequests,
    IReadOnlyList<OpenLoop> CreatedOpenLoops,
    IReadOnlyList<GoalSuggestion> GoalSuggestions,
    IReadOnlyList<ProjectSuggestion> ProjectSuggestions,
    IReadOnlyList<CompanionInsight> Insights,
    IReadOnlyList<MemoryEntry> UsedMemories);
