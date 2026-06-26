namespace Companion.Core.Models;

public sealed record CreateReminderCommand(
    string Title,
    string? Description,
    DateTime DueUtc,
    string SourceType,
    string? SourceId);
