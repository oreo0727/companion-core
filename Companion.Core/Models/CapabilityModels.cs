using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record FreeTimeBlock(
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes);

public sealed record CalendarCapabilitySummary(
    IReadOnlyList<CalendarEventSnapshot> Events,
    IReadOnlyList<FreeTimeBlock> FreeTime,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> MissingLocationEvents);

public sealed record EmailDraftRequest(
    string To,
    string Subject,
    string Body);

public sealed record EmailDraftResult(
    bool RequiresApproval,
    string Summary);
