namespace Companion.Core.Models;

public sealed record ConnectorSyncResult(
    int ItemsSynced,
    string? Summary);
