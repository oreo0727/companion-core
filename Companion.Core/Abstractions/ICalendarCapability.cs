using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ICalendarCapability
{
    Task<IReadOnlyList<CalendarEventSnapshot>> GetUpcomingEventsAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<CalendarCapabilitySummary> GetSummaryAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default);
}
