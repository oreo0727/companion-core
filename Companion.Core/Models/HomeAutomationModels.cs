using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record LocalHomeImportCommand(
    string DisplayName,
    IReadOnlyList<LocalHomeImportDevice> Devices,
    IReadOnlyList<LocalHomeImportSensor> Sensors);

public sealed record LocalHomeImportDevice(
    string? ExternalId,
    string Name,
    string DeviceType,
    string State,
    string? Room,
    string? CapabilitiesJson,
    DateTime? LastSeenUtc);

public sealed record LocalHomeImportSensor(
    string? ExternalId,
    string Name,
    string SensorType,
    string Value,
    string? Unit,
    string? Room,
    DateTime? ObservedUtc);

public sealed record LocalHomeImportResult(
    ConnectorConnection Connection,
    ConnectorSyncRun SyncRun,
    int DevicesSynced,
    int SensorsSynced);

public sealed record HomeActionResult(
    bool Executed,
    string Provider,
    string Action,
    string Target,
    string Summary);
