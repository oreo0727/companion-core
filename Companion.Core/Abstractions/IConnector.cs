using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IConnector
{
    string Name { get; }

    string Provider { get; }

    ConnectorRiskLevel RiskLevel { get; }

    Task<ConnectorTestResult> TestConnectionAsync(ConnectorSyncContext context);

    Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context);
}
