using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IConnectorSyncService
{
    Task<IReadOnlyList<ConnectorCatalogEntry>> GetCatalogAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<LocalCalendarImportResult> ImportLocalCalendarAsync(
        Guid userProfileId,
        LocalCalendarImportCommand command,
        CancellationToken cancellationToken = default);

    Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarEventSnapshot>> GetUpcomingCalendarEventsAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default);
}
