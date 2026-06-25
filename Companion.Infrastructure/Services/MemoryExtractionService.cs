using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Enums;
using Companion.Core.Models;
using Microsoft.Extensions.Logging;

namespace Companion.Infrastructure.Services;

public class MemoryExtractionService(
    IAiProviderConfigurationService configurationService,
    IEnumerable<IAIProvider> providers,
    ILogger<MemoryExtractionService> logger) : IMemoryExtractionService
{
    public async Task<ExtractionCandidates> ExtractAsync(
        Guid userProfileId,
        Guid conversationId,
        string userMessage,
        string assistantResponse,
        CancellationToken cancellationToken = default)
    {
        var providerConfiguration = await configurationService.GetEnabledConfigurationAsync(cancellationToken);

        if (providerConfiguration is null)
        {
            return BuildHeuristicCandidates(userMessage);
        }

        var provider = providers.FirstOrDefault(x =>
            string.Equals(x.ProviderName, providerConfiguration.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            return BuildHeuristicCandidates(userMessage);
        }

        try
        {
            var completion = await provider.CompleteAsync(
                new AiCompletionRequest(
                    [
                        new AiMessage(
                            "system",
                            """
                            Extract candidate memories, goals, projects, and tasks from the user's message and the assistant response.
                            Return valid JSON only.
                            Do not invent facts that were not strongly implied.
                            Suggestions are only candidates for user approval, not committed entities.
                            """),
                        new AiMessage(
                            "user",
                            BuildExtractionPrompt(userMessage, assistantResponse))
                    ],
                    (double)providerConfiguration.Temperature,
                    Math.Min(providerConfiguration.MaxTokens, 600),
                    ExpectJson: true),
                cancellationToken);

            if (TryParseExtractionCandidates(completion.Content, out var candidates))
            {
                return candidates;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Falling back to heuristic extraction after provider extraction failed.");
        }

        return BuildHeuristicCandidates(userMessage);
    }

    private static string BuildExtractionPrompt(string userMessage, string assistantResponse)
    {
        var payload = new
        {
            responseSchema = new
            {
                memorySuggestions = new[]
                {
                    new
                    {
                        type = "Preference",
                        summary = "string",
                        content = "string",
                        confidence = 0.0m,
                        source = "Conversation",
                        importance = 1,
                        sensitivity = "Normal"
                    }
                },
                goalSuggestions = new[]
                {
                    new
                    {
                        title = "string",
                        description = "string"
                    }
                },
                projectSuggestions = new[]
                {
                    new
                    {
                        title = "string",
                        description = "string",
                        mentionCount = 1
                    }
                },
                taskSuggestions = new[]
                {
                    new
                    {
                        title = "string",
                        description = "string",
                        priority = "Normal",
                        dueDateUtc = "2026-06-20T17:00:00Z"
                    }
                }
            },
            userMessage,
            assistantResponse
        };

        return JsonSerializer.Serialize(payload);
    }

    private static bool TryParseExtractionCandidates(string content, out ExtractionCandidates candidates)
    {
        candidates = new ExtractionCandidates([], [], [], []);

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

        var memorySuggestions = new List<MemorySuggestionCandidate>();
        if (root.TryGetProperty("memorySuggestions", out var memorySuggestionsElement) &&
            memorySuggestionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in memorySuggestionsElement.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var typeElement) ? typeElement.GetString()?.Trim() : null;
                var summary = item.TryGetProperty("summary", out var summaryElement) ? summaryElement.GetString()?.Trim() : null;
                var candidateContent = item.TryGetProperty("content", out var contentElement) ? contentElement.GetString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(candidateContent))
                {
                    continue;
                }

                var confidence = item.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetDecimal(out var parsedConfidence)
                    ? Math.Clamp(parsedConfidence, 0m, 1m)
                    : 0.75m;
                var source = item.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString()?.Trim() : null;
                var importance = item.TryGetProperty("importance", out var importanceElement) && importanceElement.TryGetInt32(out var parsedImportance)
                    ? Math.Clamp(parsedImportance, 1, 5)
                    : 3;
                var sensitivity = item.TryGetProperty("sensitivity", out var sensitivityElement) ? sensitivityElement.GetString()?.Trim() : null;

                memorySuggestions.Add(new MemorySuggestionCandidate(
                    type,
                    summary,
                    candidateContent,
                    confidence,
                    string.IsNullOrWhiteSpace(source) ? "Conversation" : source,
                    importance,
                    string.IsNullOrWhiteSpace(sensitivity) ? "Normal" : sensitivity));
            }
        }

        var goalSuggestions = new List<GoalSuggestionCandidate>();
        if (root.TryGetProperty("goalSuggestions", out var goalSuggestionsElement) &&
            goalSuggestionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in goalSuggestionsElement.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var titleElement) ? titleElement.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var description = item.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()?.Trim()
                    : null;
                goalSuggestions.Add(new GoalSuggestionCandidate(title, description));
            }
        }

        var projectSuggestions = new List<ProjectSuggestionCandidate>();
        if (root.TryGetProperty("projectSuggestions", out var projectSuggestionsElement) &&
            projectSuggestionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in projectSuggestionsElement.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var titleElement) ? titleElement.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var description = item.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()?.Trim()
                    : null;
                var mentionCount = item.TryGetProperty("mentionCount", out var mentionCountElement) && mentionCountElement.TryGetInt32(out var parsedMentionCount)
                    ? Math.Max(parsedMentionCount, 1)
                    : 1;
                projectSuggestions.Add(new ProjectSuggestionCandidate(title, description, mentionCount));
            }
        }

        var taskSuggestions = new List<TaskSuggestionCandidate>();
        if (root.TryGetProperty("taskSuggestions", out var taskSuggestionsElement) &&
            taskSuggestionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in taskSuggestionsElement.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var titleElement) ? titleElement.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var description = item.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()?.Trim()
                    : null;
                var priority = item.TryGetProperty("priority", out var priorityElement) &&
                               Enum.TryParse<TaskItemPriority>(priorityElement.GetString(), true, out var parsedPriority)
                    ? parsedPriority
                    : TaskItemPriority.Normal;
                var dueDateUtc = item.TryGetProperty("dueDateUtc", out var dueDateElement) &&
                                 dueDateElement.ValueKind == JsonValueKind.String &&
                                 DateTime.TryParse(dueDateElement.GetString(), out var parsedDueDate)
                    ? parsedDueDate.ToUniversalTime()
                    : (DateTime?)null;

                taskSuggestions.Add(new TaskSuggestionCandidate(title, description, priority, dueDateUtc));
            }
        }

        candidates = new ExtractionCandidates(
            memorySuggestions.DistinctBy(x => NormalizeKey($"{x.Type}:{x.Summary}:{x.Content}")).ToList(),
            goalSuggestions.DistinctBy(x => NormalizeKey(x.Title)).ToList(),
            projectSuggestions.DistinctBy(x => NormalizeKey(x.Title)).ToList(),
            taskSuggestions.DistinctBy(x => NormalizeKey(x.Title)).ToList());
        return true;
    }

    private static ExtractionCandidates BuildHeuristicCandidates(string message)
    {
        var memorySuggestions = new List<MemorySuggestionCandidate>();
        var goalSuggestions = new List<GoalSuggestionCandidate>();
        var projectSuggestions = new List<ProjectSuggestionCandidate>();
        var taskSuggestions = new List<TaskSuggestionCandidate>();

        if (ContainsAny(message, "remember", "note that", "from now on", "i prefer", "my preference"))
        {
            memorySuggestions.Add(new MemorySuggestionCandidate(
                ContainsAny(message, "prefer", "preference", "from now on") ? "Preference" : "Note",
                BuildSummary(message, "remember that", "remember", "note that", "from now on", "i prefer", "my preference"),
                message.Trim(),
                0.88m,
                "Conversation",
                ContainsAny(message, "always", "important") ? 5 : 4,
                ContainsAny(message, "private", "secret", "password") ? "High" : "Normal"));
        }

        if (ContainsAny(message, "i want to", "my goal is", "i am trying to", "i plan to"))
        {
            goalSuggestions.Add(new GoalSuggestionCandidate(
                BuildTitle(message, "i want to", "my goal is", "i am trying to", "i plan to"),
                message.Trim()));
        }

        if (ContainsAny(message, "project", "launch", "build", "ship", "roll out"))
        {
            projectSuggestions.Add(new ProjectSuggestionCandidate(
                BuildTitle(message, "project", "launch", "build", "ship", "roll out"),
                message.Trim(),
                1));
        }

        if (ContainsAny(message, "remind me", "todo", "task", "i need to", "follow up"))
        {
            taskSuggestions.Add(new TaskSuggestionCandidate(
                BuildTitle(message, "remind me to", "remind me", "todo", "task", "i need to", "follow up"),
                message.Trim(),
                DetermineTaskPriority(message),
                DetermineDueDate(message)));
        }

        return new ExtractionCandidates(
            memorySuggestions,
            goalSuggestions,
            projectSuggestions,
            taskSuggestions);
    }

    private static bool ContainsAny(string message, params string[] markers)
    {
        return markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildSummary(string message, params string[] markers)
    {
        var cleaned = RemoveLeadingMarker(message, markers)
            .Trim()
            .Trim(':', '-', '.', '!', '?');

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = message.Trim();
        }

        return cleaned.Length <= 140 ? Capitalize(cleaned) : $"{Capitalize(cleaned[..137].Trim())}...";
    }

    private static string BuildTitle(string message, params string[] markers)
    {
        var cleaned = RemoveLeadingMarker(message, markers)
            .Trim()
            .Trim(':', '-', '.', '!', '?');

        if (cleaned.StartsWith("to ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..];
        }

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = message.Trim();
        }

        cleaned = Capitalize(cleaned);
        return cleaned.Length <= 200 ? cleaned : $"{cleaned[..197].Trim()}...";
    }

    private static string RemoveLeadingMarker(string message, params string[] markers)
    {
        foreach (var marker in markers)
        {
            var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return message[(index + marker.Length)..];
            }
        }

        return message;
    }

    private static string Capitalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : string.Concat(char.ToUpperInvariant(value[0]), value[1..]);
    }

    private static TaskItemPriority DetermineTaskPriority(string message)
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

    private static DateTime? DetermineDueDate(string message)
    {
        return message.Contains("tomorrow", StringComparison.OrdinalIgnoreCase)
            ? DateTime.UtcNow.Date.AddDays(1).AddHours(17)
            : null;
    }

    private static string NormalizeKey(string value)
    {
        return string.Join(
            ' ',
            value
                .Trim()
                .ToLowerInvariant()
                .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }
}
