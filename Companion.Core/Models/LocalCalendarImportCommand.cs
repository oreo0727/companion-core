namespace Companion.Core.Models;

public sealed record LocalCalendarImportCommand(
    string DisplayName,
    IReadOnlyList<LocalCalendarImportEvent> Events);
