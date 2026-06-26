using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record LocalEmailImportResult(
    ConnectorConnection Connection,
    ConnectorSyncRun SyncRun,
    int MessagesImported);
