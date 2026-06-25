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

public sealed record MemorySuggestionResponse(
    Guid Id,
    Guid UserProfileId,
    string Type,
    string Summary,
    string Content,
    decimal Confidence,
    string Source,
    int Importance,
    string Sensitivity,
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

public sealed record TaskSuggestionResponse(
    Guid Id,
    Guid UserProfileId,
    string Title,
    string? Description,
    TaskItemPriority Priority,
    DateTime? DueDateUtc,
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
    string? Provider,
    string? Model,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
    long? LatencyMs,
    bool FallbackUsed,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc);

public sealed record ToolDefinitionResponse(
    Guid Id,
    string Name,
    string Description,
    string Category,
    ToolRiskLevel RiskLevel,
    bool RequiresApproval,
    bool Enabled,
    DateTime CreatedUtc);

public sealed record ToolExecutionResponse(
    Guid Id,
    Guid UserProfileId,
    Guid ToolDefinitionId,
    Guid? AgentRunId,
    ToolExecutionStatus Status,
    string InputJson,
    string? OutputJson,
    string? Error,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? ToolName,
    string? ToolDescription);

public sealed record ToolDispatchResponse(
    ToolExecutionResponse Execution,
    Guid? ApprovalRequestId,
    bool ExecutedImmediately);

public sealed record KnowledgeSourceResponse(
    Guid Id,
    Guid UserProfileId,
    string Name,
    string Type,
    string? Description,
    DateTime CreatedUtc,
    int DocumentCount,
    int ChunkCount);

public sealed record KnowledgeDocumentResponse(
    Guid Id,
    Guid KnowledgeSourceId,
    string Title,
    string Content,
    string MimeType,
    DateTime CreatedUtc);

public sealed record KnowledgeImportResponse(
    KnowledgeSourceResponse Source,
    KnowledgeDocumentResponse Document,
    int ChunkCount);

public sealed record KnowledgeSearchResultResponse(
    Guid SourceId,
    string SourceName,
    Guid DocumentId,
    string DocumentTitle,
    Guid ChunkId,
    int ChunkIndex,
    string Content,
    string MetadataJson,
    int RelevanceScore);

public sealed record CompanionInsightResponse(
    string Category,
    string Message,
    int Priority);

public sealed record SendChatMessageResponse(
    Guid ConversationId,
    string Reply,
    IReadOnlyList<MemoryEntryResponse> UsedMemories,
    IReadOnlyList<CompanionInsightResponse> GeneratedInsights,
    IReadOnlyList<MemorySuggestionResponse> MemorySuggestions,
    IReadOnlyList<GoalSuggestionResponse> GoalSuggestions,
    IReadOnlyList<ProjectSuggestionResponse> ProjectSuggestions,
    IReadOnlyList<TaskSuggestionResponse> TaskSuggestions,
    IReadOnlyList<ApprovalRequestResponse> ApprovalRequests,
    IReadOnlyList<OpenLoopResponse> CreatedOpenLoops,
    IReadOnlyList<ToolExecutionResponse> ToolExecutions,
    string? Provider,
    string? Model,
    bool UsedFallback);

public sealed record AiProviderConfigurationResponse(
    Guid Id,
    string Provider,
    string Model,
    string ApiBaseUrl,
    bool IsEnabled,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    bool HasApiKey,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record SuggestionRecordResponse(
    Guid Id,
    SuggestionKind Kind,
    string Title,
    string? Description,
    SuggestionStatus Status,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc,
    string? Detail,
    string? Meta);

public sealed record SuggestionActionResponse(
    SuggestionRecordResponse Suggestion,
    string MaterializedEntityType,
    Guid MaterializedEntityId,
    SuggestionKind Kind);

public sealed record UserPreferenceResponse(
    Guid Id,
    Guid UserProfileId,
    string PreferenceType,
    string Value,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record AuditEventResponse(
    Guid Id,
    Guid? UserProfileId,
    string EventType,
    string EntityType,
    string EntityId,
    string Description,
    DateTime CreatedUtc);

public sealed record AuthUserProfileResponse(
    Guid UserId,
    Guid UserProfileId,
    string Email,
    string DisplayName,
    DateTime CreatedUtc,
    DateTime? LastLoginUtc,
    IReadOnlyList<string> Roles);

public sealed record UserCapabilitiesResponse(
    bool CanManageOwnData,
    bool CanQueueAgentRuns,
    bool CanReviewApprovals,
    bool CanManageAiSettings,
    bool CanViewAuditTrail);

public sealed record CurrentUserResponse(
    AuthUserProfileResponse Profile,
    IReadOnlyList<UserPreferenceResponse> Preferences,
    UserCapabilitiesResponse Capabilities);

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTime ExpiresUtc,
    CurrentUserResponse Me);

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

    public static MemorySuggestionResponse ToResponse(this MemorySuggestion memorySuggestion)
    {
        return new MemorySuggestionResponse(
            memorySuggestion.Id,
            memorySuggestion.UserProfileId,
            memorySuggestion.Type,
            memorySuggestion.Summary,
            memorySuggestion.Content,
            memorySuggestion.Confidence,
            memorySuggestion.Source,
            memorySuggestion.Importance,
            memorySuggestion.Sensitivity,
            memorySuggestion.Status,
            memorySuggestion.CreatedUtc,
            memorySuggestion.ReviewedUtc);
    }

    public static TaskSuggestionResponse ToResponse(this TaskSuggestion taskSuggestion)
    {
        return new TaskSuggestionResponse(
            taskSuggestion.Id,
            taskSuggestion.UserProfileId,
            taskSuggestion.Title,
            taskSuggestion.Description,
            taskSuggestion.Priority,
            taskSuggestion.DueDateUtc,
            taskSuggestion.Status,
            taskSuggestion.CreatedUtc,
            taskSuggestion.ReviewedUtc);
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
            agentRun.Provider,
            agentRun.Model,
            agentRun.PromptTokens,
            agentRun.CompletionTokens,
            agentRun.TotalTokens,
            agentRun.LatencyMs,
            agentRun.FallbackUsed,
            agentRun.CreatedUtc,
            agentRun.StartedUtc,
            agentRun.CompletedUtc);
    }

    public static ToolDefinitionResponse ToResponse(this ToolDefinition definition)
    {
        return new ToolDefinitionResponse(
            definition.Id,
            definition.Name,
            definition.Description,
            definition.Category,
            definition.RiskLevel,
            definition.RequiresApproval,
            definition.Enabled,
            definition.CreatedUtc);
    }

    public static ToolExecutionResponse ToResponse(this ToolExecution execution)
    {
        return new ToolExecutionResponse(
            execution.Id,
            execution.UserProfileId,
            execution.ToolDefinitionId,
            execution.AgentRunId,
            execution.Status,
            execution.InputJson,
            execution.OutputJson,
            execution.Error,
            execution.StartedUtc,
            execution.CompletedUtc,
            execution.ToolDefinition?.Name,
            execution.ToolDefinition?.Description);
    }

    public static ToolDispatchResponse ToResponse(this ToolDispatchResult result)
    {
        result.Execution.ToolDefinition ??= result.Definition;

        return new ToolDispatchResponse(
            result.Execution.ToResponse(),
            result.ApprovalRequest?.Id,
            result.ExecutedImmediately);
    }

    public static KnowledgeSourceResponse ToResponse(this KnowledgeSourceSummary source)
    {
        return new KnowledgeSourceResponse(
            source.Id,
            source.UserProfileId,
            source.Name,
            source.Type,
            source.Description,
            source.CreatedUtc,
            source.DocumentCount,
            source.ChunkCount);
    }

    public static KnowledgeDocumentResponse ToResponse(this KnowledgeDocument document)
    {
        return new KnowledgeDocumentResponse(
            document.Id,
            document.KnowledgeSourceId,
            document.Title,
            document.Content,
            document.MimeType,
            document.CreatedUtc);
    }

    public static KnowledgeImportResponse ToResponse(this KnowledgeImportResult result)
    {
        return new KnowledgeImportResponse(
            new KnowledgeSourceSummary(
                result.Source.Id,
                result.Source.UserProfileId,
                result.Source.Name,
                result.Source.Type,
                result.Source.Description,
                result.Source.CreatedUtc,
                result.SourceDocumentCount,
                result.SourceChunkCount).ToResponse(),
            result.Document.ToResponse(),
            result.Chunks.Count);
    }

    public static KnowledgeSearchResultResponse ToResponse(this KnowledgeSearchResult result)
    {
        return new KnowledgeSearchResultResponse(
            result.Source.Id,
            result.Source.Name,
            result.Document.Id,
            result.Document.Title,
            result.Chunk.Id,
            result.Chunk.ChunkIndex,
            result.Chunk.Content,
            result.Chunk.MetadataJson,
            result.RelevanceScore);
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

    public static AiProviderConfigurationResponse ToResponse(this AiProviderConfigurationSummary configuration)
    {
        return new AiProviderConfigurationResponse(
            configuration.Id,
            configuration.Provider,
            configuration.Model,
            configuration.ApiBaseUrl,
            configuration.IsEnabled,
            configuration.Temperature,
            configuration.MaxTokens,
            configuration.TimeoutSeconds,
            configuration.HasApiKey,
            configuration.CreatedUtc,
            configuration.UpdatedUtc);
    }

    public static SuggestionRecordResponse ToResponse(this SuggestionRecord suggestion)
    {
        return new SuggestionRecordResponse(
            suggestion.Id,
            suggestion.Kind,
            suggestion.Title,
            suggestion.Description,
            suggestion.Status,
            suggestion.CreatedUtc,
            suggestion.ReviewedUtc,
            suggestion.Detail,
            suggestion.Meta);
    }

    public static SuggestionActionResponse ToResponse(this SuggestionActionResult result)
    {
        return new SuggestionActionResponse(
            result.Suggestion.ToResponse(),
            result.MaterializedEntityType,
            result.MaterializedEntityId,
            result.Kind);
    }

    public static UserPreferenceResponse ToResponse(this UserPreference preference)
    {
        return new UserPreferenceResponse(
            preference.Id,
            preference.UserProfileId,
            preference.PreferenceType,
            preference.Value,
            preference.CreatedUtc,
            preference.UpdatedUtc);
    }

    public static AuditEventResponse ToResponse(this AuditEvent auditEvent)
    {
        return new AuditEventResponse(
            auditEvent.Id,
            auditEvent.UserProfileId,
            auditEvent.EventType,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.Description,
            auditEvent.CreatedUtc);
    }
}
