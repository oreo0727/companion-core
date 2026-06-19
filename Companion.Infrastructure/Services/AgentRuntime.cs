using System.Text.Json;
using Companion.Core.Abstractions;
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
    IMemoryService memoryService,
    ITaskService taskService,
    IApprovalService approvalService,
    IChiefOfStaffService chiefOfStaffService,
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

    public async Task<IReadOnlyList<AgentRun>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AgentRuns
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentRun> QueueRunAsync(
        QueueAgentRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var agentRun = new AgentRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = command.UserProfileId,
            ConversationId = command.ConversationId,
            AgentName = command.AgentName.Trim(),
            Status = AgentRunStatus.Pending,
            Input = command.Input.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(command.MetadataJson) ? null : command.MetadataJson.Trim(),
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.AgentRuns.Add(agentRun);
        await dbContext.SaveChangesAsync(cancellationToken);

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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var conversation = conversationId is { } providedConversationId
            ? await conversationService.GetConversationAsync(userProfileId, providedConversationId, cancellationToken)
                ?? throw new KeyNotFoundException($"Conversation '{providedConversationId}' was not found.")
            : await conversationService.GetOrCreateDefaultConversationAsync(userProfileId, cancellationToken);

        var userMessage = await conversationService.AddMessageAsync(
            conversation.Id,
            MessageRole.User,
            normalizedMessage,
            JsonSerializer.Serialize(new
            {
                kind = "user-message",
                source = "api.chat",
                phase = "brain-spine",
                conversationProvided = conversationId is not null
            }),
            cancellationToken);

        var recentMessages = await conversationService.GetRecentMessagesAsync(
            conversation.Id,
            count: 8,
            cancellationToken);

        var usedMemories = await memoryService.SearchAsync(
            userProfileId,
            normalizedMessage,
            limit: 5,
            cancellationToken);

        var savedMemories = new List<MemoryEntry>();
        if (ShouldCreateMemory(normalizedMessage))
        {
            savedMemories.Add(await memoryService.CreateMemoryAsync(
                userProfileId,
                new CreateMemoryCommand(
                    DetermineMemoryType(normalizedMessage),
                    BuildMemorySummary(normalizedMessage),
                    normalizedMessage,
                    "Chat",
                    DetermineMemoryImportance(normalizedMessage),
                    DetermineSensitivity(normalizedMessage),
                    DetermineMemoryConfidence(normalizedMessage)),
                cancellationToken));
        }

        var createdTasks = new List<TaskItem>();
        if (ShouldCreateTask(normalizedMessage))
        {
            createdTasks.Add(await taskService.CreateTaskAsync(
                userProfileId,
                new CreateTaskItemCommand(
                    ExtractTaskTitle(normalizedMessage),
                    normalizedMessage,
                    DetermineTaskPriority(normalizedMessage),
                    DetermineDueDate(normalizedMessage),
                    userMessage.Id),
                cancellationToken));
        }

        var approvalRequests = new List<ApprovalRequest>();
        foreach (var approvalCommand in BuildApprovalCommands(userProfileId, conversation.Id, userMessage.Id, normalizedMessage))
        {
            approvalRequests.Add(await approvalService.CreateApprovalAsync(approvalCommand, cancellationToken));
        }

        var planningAnalysis = await chiefOfStaffService.AnalyzeMessageAsync(
            userProfileId,
            userMessage,
            cancellationToken);

        var reply = BuildAssistantReply(
            recentMessages.Count,
            usedMemories.Count,
            savedMemories.Count,
            createdTasks.Count,
            approvalRequests.Count,
            planningAnalysis.CreatedOpenLoops.Count,
            planningAnalysis.GoalSuggestions.Count,
            planningAnalysis.ProjectSuggestions.Count,
            planningAnalysis.Insights.Count);

        await conversationService.AddMessageAsync(
            conversation.Id,
            MessageRole.Companion,
            reply,
            JsonSerializer.Serialize(new
            {
                kind = "assistant-message",
                source = "deterministic-placeholder",
                phase = "brain-spine",
                recentMessageCount = recentMessages.Count,
                usedMemoryIds = usedMemories.Select(x => x.Id),
                savedMemoryIds = savedMemories.Select(x => x.Id),
                createdTaskIds = createdTasks.Select(x => x.Id),
                approvalRequestIds = approvalRequests.Select(x => x.Id),
                openLoopIds = planningAnalysis.CreatedOpenLoops.Select(x => x.Id),
                goalSuggestionIds = planningAnalysis.GoalSuggestions.Select(x => x.Id),
                projectSuggestionIds = planningAnalysis.ProjectSuggestions.Select(x => x.Id),
                insightCategories = planningAnalysis.Insights.Select(x => x.Category)
            }),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProcessChatResult(
            conversation.Id,
            reply,
            savedMemories,
            createdTasks,
            approvalRequests,
            planningAnalysis.CreatedOpenLoops,
            planningAnalysis.GoalSuggestions,
            planningAnalysis.ProjectSuggestions,
            planningAnalysis.Insights,
            usedMemories);
    }

    public async Task<int> ProcessPendingRunsAsync(CancellationToken cancellationToken = default)
    {
        var pendingRuns = await dbContext.AgentRuns
            .Where(x => x.Status == AgentRunStatus.Pending)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var pendingRun in pendingRuns)
        {
            pendingRun.Status = AgentRunStatus.Running;
            pendingRun.StartedUtc ??= timeProvider.GetUtcNow().UtcDateTime;
            pendingRun.Error = null;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await SimulateRunAsync(pendingRun, cancellationToken);

                pendingRun.Status = AgentRunStatus.Completed;
                pendingRun.Output = $"Placeholder execution completed for agent '{pendingRun.AgentName}'.";
                pendingRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
                pendingRun.Error = null;
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
                pendingRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return pendingRuns.Count;
    }

    private static bool ShouldCreateMemory(string message)
    {
        return ContainsAny(message, "remember", "from now on", "note that");
    }

    private static bool ShouldCreateTask(string message)
    {
        return ContainsAny(message, "remind me", "todo", "task", "i need to");
    }

    private static bool ContainsAny(string message, params string[] markers)
    {
        return markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string DetermineMemoryType(string message)
    {
        return ContainsAny(message, "prefer", "from now on") ? "Preference" : "Note";
    }

    private static string BuildMemorySummary(string message)
    {
        var summary = RemoveLeadingMarker(
            message,
            "remember that",
            "remember",
            "from now on",
            "note that");

        summary = summary.Trim().Trim('.', '!', '?');
        summary = summary.StartsWith("to ", StringComparison.OrdinalIgnoreCase) ? summary[3..] : summary;
        summary = string.IsNullOrWhiteSpace(summary) ? message.Trim() : summary;

        return summary.Length <= 140 ? Capitalize(summary) : $"{Capitalize(summary[..137].Trim())}...";
    }

    private static int DetermineMemoryImportance(string message)
    {
        return ContainsAny(message, "always", "important") ? 5 : 4;
    }

    private static string DetermineSensitivity(string message)
    {
        return ContainsAny(message, "secret", "private", "password", "credential") ? "High" : "Normal";
    }

    private static decimal DetermineMemoryConfidence(string message)
    {
        return ContainsAny(message, "from now on", "remember") ? 0.96m : 0.90m;
    }

    private static string ExtractTaskTitle(string message)
    {
        var title = RemoveLeadingMarker(
            message,
            "remind me to",
            "remind me",
            "todo",
            "task",
            "i need to");

        title = title.Trim().Trim(':', '-', '.', '!', '?');
        title = title.StartsWith("to ", StringComparison.OrdinalIgnoreCase) ? title[3..] : title;
        title = string.IsNullOrWhiteSpace(title) ? "Follow up on chat request" : title;

        title = Capitalize(title);
        return title.Length <= 200 ? title : $"{title[..197].Trim()}...";
    }

    private TaskItemPriority DetermineTaskPriority(string message)
    {
        if (ContainsAny(message, "critical"))
        {
            return TaskItemPriority.Critical;
        }

        if (ContainsAny(message, "urgent", "asap", "important"))
        {
            return TaskItemPriority.High;
        }

        return TaskItemPriority.Normal;
    }

    private DateTime? DetermineDueDate(string message)
    {
        if (!message.Contains("tomorrow", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return now.Date.AddDays(1).AddHours(17);
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

    private static string BuildAssistantReply(
        int recentMessageCount,
        int usedMemoryCount,
        int savedMemoryCount,
        int createdTaskCount,
        int approvalRequestCount,
        int openLoopCount,
        int goalSuggestionCount,
        int projectSuggestionCount,
        int insightCount)
    {
        var clauses = new List<string>
        {
            "stored your message in the conversation history"
        };

        if (recentMessageCount > 1)
        {
            clauses.Add("kept the recent conversation context in view");
        }

        if (usedMemoryCount > 0)
        {
            clauses.Add($"recalled {FormatCount(usedMemoryCount, "relevant memory", "relevant memories")}");
        }

        if (savedMemoryCount > 0)
        {
            clauses.Add(savedMemoryCount == 1 ? "saved that as a memory" : $"saved {savedMemoryCount} new memories");
        }

        if (createdTaskCount > 0)
        {
            clauses.Add(createdTaskCount == 1 ? "created one task" : $"created {createdTaskCount} tasks");
        }

        if (approvalRequestCount > 0)
        {
            clauses.Add(
                approvalRequestCount == 1
                    ? "flagged one action for approval"
                    : $"flagged {approvalRequestCount} actions for approval");
        }

        if (openLoopCount > 0)
        {
            clauses.Add(
                openLoopCount == 1
                    ? "captured one open loop"
                    : $"captured {openLoopCount} open loops");
        }

        if (goalSuggestionCount > 0)
        {
            clauses.Add(
                goalSuggestionCount == 1
                    ? "suggested one goal"
                    : $"suggested {goalSuggestionCount} goals");
        }

        if (projectSuggestionCount > 0)
        {
            clauses.Add(
                projectSuggestionCount == 1
                    ? "suggested one project"
                    : $"suggested {projectSuggestionCount} projects");
        }

        if (insightCount > 0)
        {
            clauses.Add(
                insightCount == 1
                    ? "surfaced one planning insight"
                    : $"surfaced {insightCount} planning insights");
        }

        return $"I {JoinClauses(clauses)}.";
    }

    private static string JoinClauses(IReadOnlyList<string> clauses)
    {
        return clauses.Count switch
        {
            0 => "processed your message",
            1 => clauses[0],
            2 => $"{clauses[0]} and {clauses[1]}",
            _ => $"{string.Join(", ", clauses.Take(clauses.Count - 1))}, and {clauses[^1]}"
        };
    }

    private static string FormatCount(int count, string singular, string plural)
    {
        return count == 1 ? $"1 {singular}" : $"{count} {plural}";
    }

    private static string RemoveLeadingMarker(string message, params string[] markers)
    {
        foreach (var marker in markers.OrderByDescending(x => x.Length))
        {
            if (!message.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return message[marker.Length..];
        }

        return message;
    }

    private static string Capitalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static async Task SimulateRunAsync(AgentRun pendingRun, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);

        if (pendingRun.Input.Contains("force-fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Agent run was instructed to fail for testing.");
        }
    }
}
