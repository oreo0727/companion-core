namespace Companion.Core.Models;

public sealed record CreateGoalSuggestionCommand(
    string Title,
    string? Description);
