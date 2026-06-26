using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record LocalCalendarImportResult(
    ConnectorConnection Connection,
    ConnectorSyncRun SyncRun,
    int EventsImported);
