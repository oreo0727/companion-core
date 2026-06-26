using System.Diagnostics;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Companion.Infrastructure.Services;

public class AgentRuntime(
    CompanionDbContext dbContext,
    IConversationService conversationService,
    IReasoningEngine reasoningEngine,
    IMemoryExtractionService memoryExtractionService,
    ISuggestionService suggestionService,
    IGoalService goalService,
    IProjectService projectService,
    IOpenLoopService openLoopService,
    IApprovalService approvalService,
    IToolExecutor toolExecutor,
    IAgentCatalog agentCatalog,
    IMultiAgentOrchestrator multiAgentOrchestrator,
    IAuditService auditService,
    TimeProvider timeProvider,
    ILogger<AgentRuntime> logger) : IAgentRuntime
{
    private static readonly (string Keyword, string Type, string RiskLevel)[] ApprovalRules =
    [
        ("send", "SendAction", "High"),
        ("delete", "DeleteAction", "High"),
        ("purchase", "PurchaseAction", "High"),
        ("schedule", "ScheduleAction", "Medium")
    ];

    public async Task<IReadOnlyList<AgentRun>> GetRunsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AgentRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentRun> QueueRunAsync(
        QueueAgentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var agentDefinition = await agentCatalog.GetAgentAsync(command.AgentName, cancellationToken);
        var normalizedAgentName = agentDefinition?.Name ?? command.AgentName.Trim();
        var agentRun = new AgentRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = command.UserProfileId,
            ConversationId = command.ConversationId,
            AgentDefinitionId = agentDefinition?.Id,
            ParentAgentRunId = command.ParentAgentRunId,
            AgentName = normalizedAgentName,
            DelegationReason = string.IsNullOrWhiteSpace(command.DelegationReason) ? null : command.DelegationReason.Trim(),
            Status = AgentRunStatus.Pending,
            Input = command.Input.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(command.MetadataJson) ? null : command.MetadataJson.Trim(),
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.AgentRuns.Add(agentRun);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (command.UserProfileId is { } userProfileId)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.AgentRunQueued,
                nameof(AgentRun),
                agentRun.Id.ToString(),
                $"Queued {agentRun.AgentName} agent run.",
                cancellationToken);
        }

        return agentRun;
    }

    public async Task<ProcessChatResult> ProcessChatAsync(
        Guid userProfileId,
        string message,
        Guid? conversationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedMessage = message.Trim();

        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var chatStopwatch = Stopwatch.StartNew();
        var agentRun = new AgentRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ConversationId = conversationId,
            AgentName = "ChiefOfStaff.ChatV2",
            Status = AgentRunStatus.Running,
            Input = normalizedMessage,
            FallbackUsed = false,
            CreatedUtc = now,
            StartedUtc = now,
            MetadataJson = JsonSerializer.Serialize(new
            {
                kind = "chat-v2",
                conversationProvided = conversationId is not null
            })
        };

        dbContext.AgentRuns.Add(agentRun);
        await dbContext.SaveChangesAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var conversation = conversationId is { } providedConversationId
                ? await conversationService.GetConversationAsync(userProfileId, providedConversationId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Conversation '{providedConversationId}' was not found.")
                : await conversationService.GetOrCreateDefaultConversationAsync(userProfileId, cancellationToken);

            agentRun.ConversationId = conversation.Id;

            var userMessage = await conversationService.AddMessageAsync(
                conversation.Id,
                MessageRole.User,
                normalizedMessage,
                JsonSerializer.Serialize(new
                {
                    kind = "user-message",
                    source = "api.chat",
                    phase = "phase-4",
                    conversationProvided = conversationId is not null
                }),
                cancellationToken);

            var reasoningResult = await reasoningEngine.GenerateReplyAsync(
                userProfileId,
                conversation.Id,
                cancellationToken);
            var extractionCandidates = await memoryExtractionService.ExtractAsync(
                userProfileId,
                conversation.Id,
                normalizedMessage,
                reasoningResult.Reply,
                cancellationToken);

            var memorySuggestions = await suggestionService.CaptureMemorySuggestionsAsync(
                userProfileId,
                extractionCandidates.MemorySuggestions,
                cancellationToken);
            var goalSuggestions = await CaptureGoalSuggestionsAsync(
                userProfileId,
                extractionCandidates.GoalSuggestions,
                cancellationToken);
            var projectSuggestions = await CaptureProjectSuggestionsAsync(
                userProfileId,
                extractionCandidates.ProjectSuggestions,
                cancellationToken);
            var taskSuggestions = await suggestionService.CaptureTaskSuggestionsAsync(
                userProfileId,
                extractionCandidates.TaskSuggestions,
                cancellationToken);
            var approvalRequests = await CaptureApprovalRequestsAsync(
                userProfileId,
                conversation.Id,
                userMessage.Id,
                normalizedMessage,
                cancellationToken);
            var createdOpenLoops = await CaptureOpenLoopsAsync(
                userProfileId,
                normalizedMessage,
                cancellationToken);
            var toolDispatchResults = await ExecuteToolRequestsAsync(
                userProfileId,
                conversation.Id,
                userMessage.Id,
                agentRun.Id,
                reasoningResult.ToolRequests,
                cancellationToken);
            var toolExecutions = toolDispatchResults
                .Select(x => x.Execution)
                .ToList();
            var toolApprovalRequests = toolDispatchResults
                .Select(x => x.ApprovalRequest)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();
            var allApprovalRequests = approvalRequests
                .Concat(toolApprovalRequests)
                .ToList();

            var assistantMessage = await conversationService.AddMessageAsync(
                conversation.Id,
                MessageRole.Companion,
                reasoningResult.Reply,
                JsonSerializer.Serialize(new
                {
                    kind = "assistant-message",
                    source = reasoningResult.Provider ?? "fallback",
                    phase = "phase-4",
                    usedFallback = reasoningResult.UsedFallback,
                    provider = reasoningResult.Provider,
                    model = reasoningResult.Model,
                    failureReason = reasoningResult.FailureReason,
                    usedMemoryIds = reasoningResult.Context.RelevantMemories.Select(x => x.Id),
                    insightCategories = reasoningResult.Insights.Select(x => x.Category),
                    memorySuggestionIds = memorySuggestions.Select(x => x.Id),
                    goalSuggestionIds = goalSuggestions.Select(x => x.Id),
                    projectSuggestionIds = projectSuggestions.Select(x => x.Id),
                    taskSuggestionIds = taskSuggestions.Select(x => x.Id),
                    approvalRequestIds = allApprovalRequests.Select(x => x.Id),
                    openLoopIds = createdOpenLoops.Select(x => x.Id),
                    toolExecutionIds = toolExecutions.Select(x => x.Id)
                }),
                cancellationToken);

            agentRun.Status = AgentRunStatus.Completed;
            agentRun.Output = reasoningResult.Reply;
            agentRun.Error = reasoningResult.UsedFallback ? reasoningResult.FailureReason : null;
            agentRun.Provider = reasoningResult.Provider;
            agentRun.Model = reasoningResult.Model;
            agentRun.PromptTokens = reasoningResult.Completion?.Usage.PromptTokens;
            agentRun.CompletionTokens = reasoningResult.Completion?.Usage.CompletionTokens;
            agentRun.TotalTokens = reasoningResult.Completion?.Usage.TotalTokens;
            agentRun.LatencyMs = reasoningResult.Completion?.LatencyMs ?? chatStopwatch.ElapsedMilliseconds;
            agentRun.FallbackUsed = reasoningResult.UsedFallback;
            agentRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            agentRun.MetadataJson = JsonSerializer.Serialize(new
            {
                kind = "chat-v2",
                conversationId = conversation.Id,
                userMessageId = userMessage.Id,
                assistantMessageId = assistantMessage.Id,
                provider = reasoningResult.Provider,
                model = reasoningResult.Model,
                usedFallback = reasoningResult.UsedFallback,
                failureReason = reasoningResult.FailureReason,
                context = new
                {
                    recentMessages = reasoningResult.Context.RecentMessages.Count,
                    memories = reasoningResult.Context.RelevantMemories.Count,
                    goals = reasoningResult.Context.ActiveGoals.Count,
                    projects = reasoningResult.Context.ActiveProjects.Count,
                    openLoops = reasoningResult.Context.OpenLoops.Count,
                    approvals = reasoningResult.Context.PendingApprovals.Count
                },
                suggestions = new
                {
                    memories = memorySuggestions.Count,
                    goals = goalSuggestions.Count,
                    projects = projectSuggestions.Count,
                    tasks = taskSuggestions.Count
                },
                tools = new
                {
                    requested = reasoningResult.ToolRequests.Count,
                    executed = toolExecutions.Count(x => x.Status == ToolExecutionStatus.Completed),
                    awaitingApproval = toolExecutions.Count(x => x.Status == ToolExecutionStatus.AwaitingApproval),
                    failed = toolExecutions.Count(x => x.Status == ToolExecutionStatus.Failed)
                }
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ProcessChatResult(
                conversation.Id,
                reasoningResult.Reply,
                reasoningResult.Context.RelevantMemories,
                reasoningResult.Insights,
                memorySuggestions,
                goalSuggestions,
                projectSuggestions,
                taskSuggestions,
                allApprovalRequests,
                createdOpenLoops,
                toolExecutions,
                reasoningResult.Provider,
                reasoningResult.Model,
                reasoningResult.UsedFallback);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            agentRun.Status = AgentRunStatus.Failed;
            agentRun.Error = "Chat processing was canceled.";
            agentRun.FallbackUsed = false;
            agentRun.LatencyMs ??= chatStopwatch.ElapsedMilliseconds;
            agentRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            logger.LogError(ex, "Chat processing failed for conversation {ConversationId}.", conversationId);
            agentRun.Status = AgentRunStatus.Failed;
            agentRun.Error = ex.Message;
            agentRun.FallbackUsed = false;
            agentRun.LatencyMs ??= chatStopwatch.ElapsedMilliseconds;
            agentRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> ProcessPendingRunsAsync(CancellationToken cancellationToken = default)
    {
        var pendingRuns = await dbContext.AgentRuns
            .Where(x => x.Status == AgentRunStatus.Pending)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var pendingRun in pendingRuns)
        {
            var runStopwatch = Stopwatch.StartNew();
            pendingRun.Status = AgentRunStatus.Running;
            pendingRun.StartedUtc ??= timeProvider.GetUtcNow().UtcDateTime;
            pendingRun.Error = null;
            pendingRun.FallbackUsed = false;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await multiAgentOrchestrator.ExecuteAsync(pendingRun, cancellationToken);

                pendingRun.Status = AgentRunStatus.Completed;
                pendingRun.LatencyMs = runStopwatch.ElapsedMilliseconds;
                pendingRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
                pendingRun.Error = null;
                if (pendingRun.UserProfileId is { } completedUserProfileId)
                {
                    await auditService.WriteEventAsync(
                        completedUserProfileId,
                        AuditEventTypes.AgentRunCompleted,
                        nameof(AgentRun),
                        pendingRun.Id.ToString(),
                        $"Completed {pendingRun.AgentName} agent run.",
                        cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent run {AgentRunId} failed while processing.", pendingRun.Id);
                pendingRun.Status = AgentRunStatus.Failed;
                pendingRun.Error = ex.Message;
                pendingRun.LatencyMs = runStopwatch.ElapsedMilliseconds;
                pendingRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
                if (pendingRun.UserProfileId is { } failedUserProfileId)
                {
                    await auditService.WriteEventAsync(
                        failedUserProfileId,
                        AuditEventTypes.AgentRunFailed,
                        nameof(AgentRun),
                        pendingRun.Id.ToString(),
                        $"{pendingRun.AgentName} agent run failed: {ex.Message}",
                        cancellationToken);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return pendingRuns.Count;
    }

    private async Task<IReadOnlyList<GoalSuggestion>> CaptureGoalSuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<GoalSuggestionCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<GoalSuggestion>();

        foreach (var candidate in candidates)
        {
            var suggestion = await goalService.CaptureGoalSuggestionAsync(
                userProfileId,
                new CreateGoalSuggestionCommand(candidate.Title, candidate.Description),
                cancellationToken);

            if (suggestion is not null)
            {
                suggestions.Add(suggestion);
            }
        }

        return suggestions;
    }

    private async Task<IReadOnlyList<ProjectSuggestion>> CaptureProjectSuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<ProjectSuggestionCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<ProjectSuggestion>();

        foreach (var candidate in candidates)
        {
            var suggestion = await projectService.CaptureProjectSuggestionAsync(
                userProfileId,
                new CreateProjectSuggestionCommand(candidate.Title, candidate.Description, candidate.MentionCount),
                cancellationToken);

            if (suggestion is not null)
            {
                suggestions.Add(suggestion);
            }
        }

        return suggestions;
    }

    private async Task<IReadOnlyList<ApprovalRequest>> CaptureApprovalRequestsAsync(
        Guid userProfileId,
        Guid conversationId,
        Guid sourceMessageId,
        string message,
        CancellationToken cancellationToken)
    {
        var approvals = new List<ApprovalRequest>();

        foreach (var approvalCommand in BuildApprovalCommands(userProfileId, conversationId, sourceMessageId, message))
        {
            approvals.Add(await approvalService.CreateApprovalAsync(approvalCommand, cancellationToken));
        }

        return approvals;
    }

    private async Task<IReadOnlyList<OpenLoop>> CaptureOpenLoopsAsync(
        Guid userProfileId,
        string message,
        CancellationToken cancellationToken)
    {
        if (!TryBuildOpenLoop(message, out var command))
        {
            return [];
        }

        var openLoop = await openLoopService.CaptureOpenLoopAsync(userProfileId, command, cancellationToken);
        return openLoop is null ? [] : [openLoop];
    }

    private static bool TryBuildOpenLoop(string message, out CreateOpenLoopCommand command)
    {
        command = default!;

        if (ContainsAny(message, "waiting on", "still need to", "follow up with", "haven't heard back"))
        {
            command = new CreateOpenLoopCommand(
                BuildTitle(message, "waiting on", "still need to", "follow up with", "haven't heard back"),
                message.Trim(),
                message.Contains("waiting on", StringComparison.OrdinalIgnoreCase)
                    ? OpenLoopStatus.Waiting
                    : OpenLoopStatus.Open);
            return true;
        }

        return false;
    }

    private static bool ContainsAny(string message, params string[] markers)
    {
        return markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildTitle(string message, params string[] markers)
    {
        foreach (var marker in markers)
        {
            var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var title = message[(index + marker.Length)..]
                .Trim()
                .Trim(':', '-', '.', '!', '?');

            if (title.StartsWith("to ", StringComparison.OrdinalIgnoreCase))
            {
                title = title[3..];
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                break;
            }

            title = string.Concat(char.ToUpperInvariant(title[0]), title[1..]);
            return title.Length <= 200 ? title : $"{title[..197].Trim()}...";
        }

        return "Follow up on outstanding item";
    }

    private static IReadOnlyList<CreateApprovalRequestCommand> BuildApprovalCommands(
        Guid userProfileId,
        Guid conversationId,
        Guid sourceMessageId,
        string message)
    {
        var commands = new List<CreateApprovalRequestCommand>();

        foreach (var rule in ApprovalRules)
        {
            if (!message.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            commands.Add(new CreateApprovalRequestCommand(
                userProfileId,
                conversationId,
                sourceMessageId,
                rule.Type,
                $"Chat message requested a '{rule.Keyword}' action that requires approval.",
                JsonSerializer.Serialize(new
                {
                    action = rule.Keyword,
                    requestedMessage = message
                }),
                rule.RiskLevel));
        }

        return commands;
    }

    private async Task<IReadOnlyList<ToolDispatchResult>> ExecuteToolRequestsAsync(
        Guid userProfileId,
        Guid conversationId,
        Guid sourceMessageId,
        Guid agentRunId,
        IReadOnlyList<ToolRequest> toolRequests,
        CancellationToken cancellationToken)
    {
        if (toolRequests.Count == 0)
        {
            return [];
        }

        var results = new List<ToolDispatchResult>();

        foreach (var toolRequest in toolRequests)
        {
            try
            {
                var result = await toolExecutor.ExecuteAsync(
                    userProfileId,
                    toolRequest.Tool,
                    toolRequest.InputJson,
                    agentRunId,
                    conversationId,
                    sourceMessageId,
                    cancellationToken);
                results.Add(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Tool request '{ToolName}' was ignored during chat processing for agent run {AgentRunId}.",
                    toolRequest.Tool,
                    agentRunId);
            }
        }

        return results;
    }
}
