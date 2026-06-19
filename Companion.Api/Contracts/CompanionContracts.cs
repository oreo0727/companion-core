using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;

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

public sealed record GoalResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    GoalStatus Status,
    PlanningPriority Priority,
    DateTime? TargetDateUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record ProjectResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    ProjectStatus Status,
    PlanningPriority Priority,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record OpenLoopResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    OpenLoopStatus Status,
    DateTime CreatedUtc,
    DateTime? ClosedUtc);

public sealed record GoalSuggestionResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    SuggestionStatus Status,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc);

public sealed record ProjectSuggestionResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    int MentionCount,
    SuggestionStatus Status,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc);

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

public sealed record CompanionInsightResponse(
    string Category,
    string Message,
    int Priority);

public sealed record SendChatMessageResponse(
    string Reply,
    Guid ConversationId,
    IReadOnlyList<MemoryEntryResponse> SavedMemories,
    IReadOnlyList<TaskItemResponse> CreatedTasks,
    IReadOnlyList<ApprovalRequestResponse> ApprovalRequests,
    IReadOnlyList<OpenLoopResponse> CreatedOpenLoops,
    IReadOnlyList<GoalSuggestionResponse> GoalSuggestions,
    IReadOnlyList<ProjectSuggestionResponse> ProjectSuggestions,
    IReadOnlyList<CompanionInsightResponse> Insights,
    IReadOnlyList<MemoryEntryResponse> UsedMemories);

public sealed record CompanionBriefingResponse(
    IReadOnlyList<TaskItemResponse> OpenTasks,
    IReadOnlyList<ApprovalRequestResponse> PendingApprovals,
    IReadOnlyList<MemoryEntryResponse> RecentMemories,
    IReadOnlyList<GoalResponse> Goals,
    IReadOnlyList<ProjectResponse> Projects,
    IReadOnlyList<OpenLoopResponse> OpenLoops,
    IReadOnlyList<ProjectSuggestionResponse> ProjectSuggestions,
    IReadOnlyList<GoalSuggestionResponse> GoalSuggestions,
    IReadOnlyList<CompanionInsightResponse> ChiefOfStaffInsights);

public sealed record CompanionDashboardResponse(
    int ActiveProjects,
    int ActiveGoals,
    int OpenLoops,
    int PendingApprovals,
    IReadOnlyList<CompanionInsightResponse> TopInsights);

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

    public static GoalResponse ToResponse(this Goal goal)
    {
        return new GoalResponse(
            goal.Id,
            goal.UserProfileId,
            goal.Title,
            goal.Description,
            goal.Status,
            goal.Priority,
            goal.TargetDateUtc,
            goal.CreatedUtc,
            goal.UpdatedUtc);
    }

    public static ProjectResponse ToResponse(this Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.UserProfileId,
            project.Title,
            project.Description,
            project.Status,
            project.Priority,
            project.CreatedUtc,
            project.UpdatedUtc);
    }

    public static OpenLoopResponse ToResponse(this OpenLoop openLoop)
    {
        return new OpenLoopResponse(
            openLoop.Id,
            openLoop.UserProfileId,
            openLoop.Title,
            openLoop.Description,
            openLoop.Status,
            openLoop.CreatedUtc,
            openLoop.ClosedUtc);
    }

    public static GoalSuggestionResponse ToResponse(this GoalSuggestion goalSuggestion)
    {
        return new GoalSuggestionResponse(
            goalSuggestion.Id,
            goalSuggestion.UserProfileId,
            goalSuggestion.Title,
            goalSuggestion.Description,
            goalSuggestion.Status,
            goalSuggestion.CreatedUtc,
            goalSuggestion.ReviewedUtc);
    }

    public static ProjectSuggestionResponse ToResponse(this ProjectSuggestion projectSuggestion)
    {
        return new ProjectSuggestionResponse(
            projectSuggestion.Id,
            projectSuggestion.UserProfileId,
            projectSuggestion.Title,
            projectSuggestion.Description,
            projectSuggestion.MentionCount,
            projectSuggestion.Status,
            projectSuggestion.CreatedUtc,
            projectSuggestion.ReviewedUtc);
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

    public static CompanionInsightResponse ToResponse(this CompanionInsight insight)
    {
        return new CompanionInsightResponse(
            insight.Category,
            insight.Message,
            insight.Priority);
    }

    public static CompanionBriefingResponse ToResponse(this CompanionBriefing briefing)
    {
        return new CompanionBriefingResponse(
            briefing.OpenTasks.Select(x => x.ToResponse()).ToList(),
            briefing.PendingApprovals.Select(x => x.ToResponse()).ToList(),
            briefing.RecentMemories.Select(x => x.ToResponse()).ToList(),
            briefing.Goals.Select(x => x.ToResponse()).ToList(),
            briefing.Projects.Select(x => x.ToResponse()).ToList(),
            briefing.OpenLoops.Select(x => x.ToResponse()).ToList(),
            briefing.ProjectSuggestions.Select(x => x.ToResponse()).ToList(),
            briefing.GoalSuggestions.Select(x => x.ToResponse()).ToList(),
            briefing.ChiefOfStaffInsights.Select(x => x.ToResponse()).ToList());
    }

    public static CompanionDashboardResponse ToResponse(this CompanionDashboard dashboard)
    {
        return new CompanionDashboardResponse(
            dashboard.ActiveProjects,
            dashboard.ActiveGoals,
            dashboard.OpenLoops,
            dashboard.PendingApprovals,
            dashboard.TopInsights.Select(x => x.ToResponse()).ToList());
    }
}
