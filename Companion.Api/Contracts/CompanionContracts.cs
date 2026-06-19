using Companion.Core.Entities;
using Companion.Core.Enums;

namespace Companion.Api.Contracts;

public sealed record ConversationResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DateTime LastMessageUtc,
    string? ActiveTopic);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    MessageRole Role,
    string Content,
    DateTime CreatedUtc,
    string? MetadataJson,
    int? TokensEstimate);

public sealed record MemoryEntryResponse(
    Guid Id,
    Guid UserProfileId,
    string Type,
    string Summary,
    string Content,
    decimal Confidence,
    string Source,
    DateTime CreatedUtc,
    DateTime? LastReferencedUtc,
    int Importance,
    string Sensitivity,
    DateTime? ExpiresUtc,
    bool IsArchived);

public sealed record TaskItemResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDateUtc,
    DateTime CreatedUtc,
    Guid? SourceMessageId,
    DateTime? CompletedUtc);

public sealed record ApprovalRequestResponse(
    Guid Id,
    Guid? UserProfileId,
    Guid? ConversationId,
    Guid? SourceMessageId,
    string Type,
    string Reason,
    string Payload,
    string RiskLevel,
    ApprovalRequestStatus Status,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc);

public sealed record AgentRunResponse(
    Guid Id,
    Guid? UserProfileId,
    Guid? ConversationId,
    string AgentName,
    AgentRunStatus Status,
    string Input,
    string? Output,
    string? Error,
    string? MetadataJson,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc);

public sealed record SendChatMessageResponse(
    string Reply,
    Guid ConversationId,
    IReadOnlyList<MemoryEntryResponse> SavedMemories,
    IReadOnlyList<TaskItemResponse> CreatedTasks,
    IReadOnlyList<ApprovalRequestResponse> ApprovalRequests,
    IReadOnlyList<MemoryEntryResponse> UsedMemories);

public sealed record CompanionBriefingResponse(
    IReadOnlyList<TaskItemResponse> OpenTasks,
    IReadOnlyList<ApprovalRequestResponse> PendingApprovals,
    IReadOnlyList<MemoryEntryResponse> RecentMemories,
    IReadOnlyList<MessageResponse> RecentMessages);

public static class CompanionApiMappings
{
    public static ConversationResponse ToResponse(this Conversation conversation)
    {
        return new ConversationResponse(
            conversation.Id,
            conversation.UserProfileId,
            conversation.Title,
            conversation.CreatedUtc,
            conversation.UpdatedUtc,
            conversation.LastMessageUtc,
            conversation.ActiveTopic);
    }

    public static MessageResponse ToResponse(this Message message)
    {
        return new MessageResponse(
            message.Id,
            message.ConversationId,
            message.Role,
            message.Content,
            message.CreatedUtc,
            message.MetadataJson,
            message.TokensEstimate);
    }

    public static MemoryEntryResponse ToResponse(this MemoryEntry memoryEntry)
    {
        return new MemoryEntryResponse(
            memoryEntry.Id,
            memoryEntry.UserProfileId,
            memoryEntry.Type,
            memoryEntry.Summary,
            memoryEntry.Content,
            memoryEntry.Confidence,
            memoryEntry.Source,
            memoryEntry.CreatedUtc,
            memoryEntry.LastReferencedUtc,
            memoryEntry.Importance,
            memoryEntry.Sensitivity,
            memoryEntry.ExpiresUtc,
            memoryEntry.IsArchived);
    }

    public static TaskItemResponse ToResponse(this TaskItem taskItem)
    {
        return new TaskItemResponse(
            taskItem.Id,
            taskItem.UserProfileId,
            taskItem.Title,
            taskItem.Description,
            taskItem.Status,
            taskItem.Priority,
            taskItem.DueDateUtc,
            taskItem.CreatedUtc,
            taskItem.SourceMessageId,
            taskItem.CompletedUtc);
    }

    public static ApprovalRequestResponse ToResponse(this ApprovalRequest approvalRequest)
    {
        return new ApprovalRequestResponse(
            approvalRequest.Id,
            approvalRequest.UserProfileId,
            approvalRequest.ConversationId,
            approvalRequest.SourceMessageId,
            approvalRequest.Type,
            approvalRequest.Reason,
            approvalRequest.Payload,
            approvalRequest.RiskLevel,
            approvalRequest.Status,
            approvalRequest.CreatedUtc,
            approvalRequest.ReviewedUtc);
    }

    public static AgentRunResponse ToResponse(this AgentRun agentRun)
    {
        return new AgentRunResponse(
            agentRun.Id,
            agentRun.UserProfileId,
            agentRun.ConversationId,
            agentRun.AgentName,
            agentRun.Status,
            agentRun.Input,
            agentRun.Output,
            agentRun.Error,
            agentRun.MetadataJson,
            agentRun.CreatedUtc,
            agentRun.StartedUtc,
            agentRun.CompletedUtc);
    }
}
