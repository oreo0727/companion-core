using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record TaskSuggestionCandidate(
    string Title,
    string? Description,
    TaskItemPriority Priority,
    DateTime? DueDateUtc);
