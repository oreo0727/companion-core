namespace Companion.Core.Models;

public sealed record CreateProjectSuggestionCommand(
    string Title,
    string? Description,
    int MentionCount);
