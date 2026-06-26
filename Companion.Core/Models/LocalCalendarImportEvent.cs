namespace Companion.Core.Models;

public sealed record LocalCalendarImportEvent(
    string? ExternalId,
    string Title,
    string? Description,
    string? Location,
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsAllDay);
