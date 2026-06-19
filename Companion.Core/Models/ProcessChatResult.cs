using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ProcessChatResult(
    Guid ConversationId,
    string Reply,
    IReadOnlyList<MemoryEntry> SavedMemories,
    IReadOnlyList<TaskItem> CreatedTasks,
    IReadOnlyList<ApprovalRequest> ApprovalRequests,
    IReadOnlyList<MemoryEntry> UsedMemories);
