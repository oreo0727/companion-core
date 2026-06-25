using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record CompanionContext(
    Guid UserProfileId,
    Guid ConversationId,
    string? ActiveTopic,
    IReadOnlyList<Message> RecentMessages,
    IReadOnlyList<MemoryEntry> RelevantMemories,
    IReadOnlyList<Goal> ActiveGoals,
    IReadOnlyList<Project> ActiveProjects,
    IReadOnlyList<KnowledgeSearchResult> RelevantKnowledge,
    IReadOnlyList<TaskItem> OpenTasks,
    IReadOnlyList<OpenLoop> OpenLoops,
    IReadOnlyList<ApprovalRequest> PendingApprovals,
    IReadOnlyList<CompanionInsight> ChiefOfStaffInsights);
