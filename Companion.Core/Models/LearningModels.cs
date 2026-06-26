namespace Companion.Core.Models;

public sealed record RecordLearningEventCommand(
    string EventType,
    string SourceType,
    string SourceId,
    string Signal,
    decimal Weight,
    string? MetadataJson = null);

public sealed record ConversationRatingCommand(
    Guid ConversationId,
    int Rating,
    string? Comment);

public sealed record LearningProfile(
    Guid UserProfileId,
    int AcceptedSuggestions,
    int RejectedSuggestions,
    int IgnoredSuggestions,
    int ToolUsageCount,
    int FailedToolUsageCount,
    int ConversationRatingCount,
    decimal AverageConversationRating,
    int CompletedGoals,
    int CompletedProjects,
    int PreferenceEvolutionEvents,
    IReadOnlyList<string> StrongSignals,
    DateTime GeneratedUtc);
