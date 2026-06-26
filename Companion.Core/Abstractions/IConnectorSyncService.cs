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

    Task<LocalEmailImportResult> ImportLocalEmailAsync(
        Guid userProfileId,
        LocalEmailImportCommand command,
        CancellationToken cancellationToken = default);

    Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default);

    Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        string? payloadJson,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarEventSnapshot>> GetUpcomingCalendarEventsAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailMessageSnapshot>> GetRecentEmailMessagesAsync(
        Guid userProfileId,
        int daysBack = 14,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailMessageSnapshot>> SearchEmailMessagesAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileDocumentSnapshot>> GetRecentFileDocumentsAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);
}
