using System.Text;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;
using Microsoft.Extensions.Logging;

namespace Companion.Infrastructure.Services;

public class ChiefOfStaffReasoningEngine(
    IContextBuilder contextBuilder,
    IAiProviderConfigurationService configurationService,
    IToolRegistry toolRegistry,
    IEnumerable<IAIProvider> providers,
    ILogger<ChiefOfStaffReasoningEngine> logger) : IReasoningEngine
{
    public async Task<ReasoningEngineResult> GenerateReplyAsync(
        Guid userProfileId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildContextAsync(userProfileId, conversationId, cancellationToken);
        var providerConfiguration = await configurationService.GetEnabledConfigurationAsync(cancellationToken);

        if (providerConfiguration is null)
        {
            return BuildFallback(context, "No enabled AI provider configuration was found.");
        }

        var provider = providers.FirstOrDefault(x =>
            string.Equals(x.ProviderName, providerConfiguration.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            return BuildFallback(
                context,
                $"No provider implementation is registered for '{providerConfiguration.Provider}'.",
                provider: providerConfiguration.Provider,
                model: providerConfiguration.Model);
        }

        try
        {
            var completion = await provider.CompleteAsync(
                new AiCompletionRequest(
                    [
                        new AiMessage("system", CompanionPrompts.ChiefOfStaff),
                        new AiMessage("user", BuildReasoningPrompt(context, toolRegistry.GetAvailableTools()))
                    ],
                    (double)providerConfiguration.Temperature,
                    providerConfiguration.MaxTokens,
                    ExpectJson: true),
                cancellationToken);

            if (!TryParseCompletionPayload(
                completion.Content,
                out var reply,
                out var insights,
                out var recommendations,
                out var toolRequests))
            {
                return BuildFallback(
                    context,
                    "The selected provider returned malformed JSON.",
                    completion);
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                return BuildFallback(context, "The selected provider returned an empty response.", completion);
            }

            return new ReasoningEngineResult(
                context,
                reply,
                insights.Count > 0 ? insights : context.ChiefOfStaffInsights.Take(3).ToList(),
                recommendations,
                toolRequests,
                completion,
                completion.Provider,
                completion.Model,
                UsedFallback: false,
                FailureReason: null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Reasoning engine fell back after provider '{Provider}' failed.", provider.ProviderName);
            return BuildFallback(
                context,
                ex.Message,
                provider: providerConfiguration.Provider,
                model: providerConfiguration.Model);
        }
    }

    private static string BuildReasoningPrompt(CompanionContext context, IReadOnlyList<ITool> availableTools)
    {
        var payload = new
        {
            responseSchema = new
            {
                reply = "string",
                insights = new[]
                {
                    new
                    {
                        category = "string",
                        message = "string",
                        priority = 0
                    }
                },
                recommendations = new[] { "string" },
                toolRequests = new[]
                {
                    new
                    {
                        tool = "string",
                        input = new { }
                    }
                }
            },
            availableTools = availableTools.Select(tool => new
            {
                tool.Name,
                tool.Description,
                riskLevel = tool.RiskLevel.ToString()
            }),
            context = new
            {
                context.ActiveTopic,
                recentMessages = context.RecentMessages.Select(x => new
                {
                    role = x.Role.ToString(),
                    x.Content,
                    x.CreatedUtc
                }),
                relevantMemories = context.RelevantMemories.Select(x => new
                {
                    x.Type,
                    x.Summary,
                    x.Content,
                    x.Importance,
                    x.Sensitivity
                }),
                openTasks = context.OpenTasks.Select(x => new
                {
                    x.Title,
                    x.Description,
                    status = x.Status.ToString(),
                    priority = x.Priority.ToString(),
                    x.DueDateUtc
                }),
                activeGoals = context.ActiveGoals.Select(x => new
                {
                    x.Title,
                    x.Description,
                    status = x.Status.ToString(),
                    priority = x.Priority.ToString(),
                    x.TargetDateUtc
                }),
                activeProjects = context.ActiveProjects.Select(x => new
                {
                    x.Title,
                    x.Description,
                    status = x.Status.ToString(),
                    priority = x.Priority.ToString()
                }),
                upcomingCalendarEvents = context.UpcomingCalendarEvents.Select(x => new
                {
                    x.Title,
                    x.Description,
                    x.Location,
                    x.StartUtc,
                    x.EndUtc,
                    x.IsAllDay
                }),
                relevantKnowledge = context.RelevantKnowledge.Select(x => new
                {
                    source = x.Source.Name,
                    document = x.Document.Title,
                    x.Chunk.ChunkIndex,
                    x.Chunk.Content,
                    x.RelevanceScore
                }),
                openLoops = context.OpenLoops.Select(x => new
                {
                    x.Title,
                    x.Description,
                    status = x.Status.ToString()
                }),
                pendingApprovals = context.PendingApprovals.Select(x => new
                {
                    x.Type,
                    x.Reason,
                    x.RiskLevel,
                    x.CreatedUtc
                }),
                chiefOfStaffInsights = context.ChiefOfStaffInsights.Select(x => new
                {
                    x.Category,
                    x.Message,
                    x.Priority
                })
            },
            instructions = new[]
            {
                "Reply as Companion, a calm personal Chief Of Staff.",
                "Use the context provided and say when context is incomplete.",
                "Return valid JSON only.",
                "Keep the reply practical, concise, and specific to the user's current situation.",
                "Treat the context priority as: conversation, memories, goals, projects, calendar, knowledge, tasks, then other signals.",
                "Use the insights array for strategic observations and recommendations for suggested next actions.",
                "Only request a tool when it materially helps the user and the tool is listed in availableTools.",
                "Leave toolRequests empty when no action should be taken."
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static bool TryParseCompletionPayload(
        string content,
        out string reply,
        out List<CompanionInsight> insights,
        out List<string> recommendations,
        out List<ToolRequest> toolRequests)
    {
        reply = string.Empty;
        insights = [];
        recommendations = [];
        toolRequests = [];

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        using var document = AiJsonPayloadParser.ParseObjectDocument(content);
        if (document is null)
        {
            return false;
        }

        var root = document.RootElement;

        if (root.TryGetProperty("reply", out var replyElement))
        {
            reply = replyElement.GetString()?.Trim() ?? string.Empty;
        }

        if (root.TryGetProperty("insights", out var insightsElement) &&
            insightsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in insightsElement.EnumerateArray())
            {
                var category = item.TryGetProperty("category", out var categoryElement)
                    ? categoryElement.GetString()?.Trim()
                    : null;
                var message = item.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()?.Trim()
                    : null;
                var priority = item.TryGetProperty("priority", out var priorityElement) && priorityElement.TryGetInt32(out var parsedPriority)
                    ? parsedPriority
                    : 50;

                if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(message))
                {
                    insights.Add(new CompanionInsight(category, message, Math.Clamp(priority, 1, 100)));
                }
            }
        }

        if (root.TryGetProperty("recommendations", out var recommendationsElement) &&
            recommendationsElement.ValueKind == JsonValueKind.Array)
        {
            recommendations = recommendationsElement
                .EnumerateArray()
                .Select(x => x.GetString()?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();
        }

        if (root.TryGetProperty("toolRequests", out var toolRequestsElement) &&
            toolRequestsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in toolRequestsElement.EnumerateArray())
            {
                var toolName = item.TryGetProperty("tool", out var toolElement)
                    ? toolElement.GetString()?.Trim()
                    : null;

                if (string.IsNullOrWhiteSpace(toolName))
                {
                    continue;
                }

                var inputJson = item.TryGetProperty("input", out var inputElement)
                    ? JsonSerializer.Serialize(inputElement)
                    : "{}";

                toolRequests.Add(new ToolRequest(toolName, inputJson));
            }
        }

        return !string.IsNullOrWhiteSpace(reply);
    }

    private static ReasoningEngineResult BuildFallback(
        CompanionContext context,
        string failureReason,
        AiCompletionResult? completion = null,
        string? provider = null,
        string? model = null)
    {
        var reply = BuildFallbackReply(context);

        return new ReasoningEngineResult(
            context,
            reply,
            context.ChiefOfStaffInsights.Take(3).ToList(),
            [],
            [],
            completion,
            completion?.Provider ?? provider,
            completion?.Model ?? model,
            UsedFallback: true,
            failureReason);
    }

    private static string BuildFallbackReply(CompanionContext context)
    {
        var latestUserMessage = context.RecentMessages.LastOrDefault(x => string.Equals(x.Role.ToString(), "User", StringComparison.OrdinalIgnoreCase));
        var reply = new StringBuilder();

        reply.Append("I have the latest conversation context in view");

        if (!string.IsNullOrWhiteSpace(context.ActiveTopic))
        {
            reply.Append($", especially around {context.ActiveTopic}");
        }

        reply.Append(". ");

        if (context.OpenTasks.Count > 0)
        {
            reply.Append($"You currently have {context.OpenTasks.Count} open tasks");

            if (context.ActiveGoals.Count > 0)
            {
                reply.Append($" and {context.ActiveGoals.Count} active goals");
            }

            reply.Append(". ");
        }

        if (context.PendingApprovals.Count > 0)
        {
            reply.Append($"There are {context.PendingApprovals.Count} pending approvals that may block progress. ");
        }

        if (latestUserMessage is not null)
        {
            reply.Append("I can help you turn the latest message into a clear next step plan.");
        }
        else
        {
            reply.Append("I can help you prioritize the next best step.");
        }

        return reply.ToString().Trim();
    }
}
