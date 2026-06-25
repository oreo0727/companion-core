namespace Companion.Core.Models;

public sealed record ExtractionCandidates(
    IReadOnlyList<MemorySuggestionCandidate> MemorySuggestions,
    IReadOnlyList<GoalSuggestionCandidate> GoalSuggestions,
    IReadOnlyList<ProjectSuggestionCandidate> ProjectSuggestions,
    IReadOnlyList<TaskSuggestionCandidate> TaskSuggestions);
