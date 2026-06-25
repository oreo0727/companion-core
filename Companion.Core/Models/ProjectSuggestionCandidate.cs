namespace Companion.Core.Models;

public sealed record ProjectSuggestionCandidate(
    string Title,
    string? Description,
    int MentionCount);
