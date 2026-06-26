using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public abstract class HomeAutomationConnectorBase(
    CompanionDbContext dbContext,
    TimeProvider timeProvider) : IHomeAutomationConnector
{
    public abstract string Name { get; }

    public abstract string Provider { get; }

    public ConnectorRiskLevel RiskLevel => ConnectorRiskLevel.Low;

    public Task<ConnectorTestResult> TestConnectionAsync(ConnectorSyncContext context)
    {
        return Task.FromResult(new ConnectorTestResult(true, null));
    }

    public async Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context)
    {
        if (context.Payload is null)
        {
            var existing = await dbContext.HomeDeviceSnapshots.CountAsync(
                x => x.ConnectorConnectionId == context.Connection.Id,
                context.CancellationToken);
            return new ConnectorSyncResult(existing, "No home automation payload was supplied; existing snapshots were left unchanged.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var devicesSynced = await ImportDevicesAsync(context, context.Payload.Value, now);
        var sensorsSynced = await ImportSensorsAsync(context, context.Payload.Value, now);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new ConnectorSyncResult(
            devicesSynced + sensorsSynced,
            $"Imported or updated {devicesSynced} device snapshot(s) and {sensorsSynced} sensor snapshot(s).");
    }

    private async Task<int> ImportDevicesAsync(ConnectorSyncContext context, JsonElement payload, DateTime now)
    {
        if (!TryGetPropertyCaseInsensitive(payload, "devices", out var devicesElement) ||
            devicesElement.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var synced = 0;
        foreach (var item in devicesElement.EnumerateArray())
        {
            var name = GetString(item, "name");
            var deviceType = GetString(item, "deviceType") ?? GetString(item, "type");
            var state = GetString(item, "state") ?? "unknown";
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(deviceType))
            {
                continue;
            }

            var externalId = GetString(item, "externalId") ?? $"{Provider}:{deviceType}:{name}";
            var snapshot = await dbContext.HomeDeviceSnapshots.FirstOrDefaultAsync(
                x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new HomeDeviceSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };
                dbContext.HomeDeviceSnapshots.Add(snapshot);
            }

            snapshot.Name = name;
            snapshot.DeviceType = deviceType;
            snapshot.State = state;
            snapshot.Room = GetString(item, "room");
            snapshot.CapabilitiesJson = TryGetPropertyCaseInsensitive(item, "capabilities", out var capabilities)
                ? JsonSerializer.Serialize(capabilities)
                : GetString(item, "capabilitiesJson");
            snapshot.LastSeenUtc = TryGetDateTime(item, "lastSeenUtc");
            snapshot.UpdatedUtc = now;
            synced++;
        }

        return synced;
    }

    private async Task<int> ImportSensorsAsync(ConnectorSyncContext context, JsonElement payload, DateTime now)
    {
        if (!TryGetPropertyCaseInsensitive(payload, "sensors", out var sensorsElement) ||
            sensorsElement.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var synced = 0;
        foreach (var item in sensorsElement.EnumerateArray())
        {
            var name = GetString(item, "name");
            var sensorType = GetString(item, "sensorType") ?? GetString(item, "type");
            var value = GetString(item, "value");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sensorType) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var externalId = GetString(item, "externalId") ?? $"{Provider}:{sensorType}:{name}";
            var snapshot = await dbContext.HomeSensorSnapshots.FirstOrDefaultAsync(
                x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new HomeSensorSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };
                dbContext.HomeSensorSnapshots.Add(snapshot);
            }

            snapshot.Name = name;
            snapshot.SensorType = sensorType;
            snapshot.Value = value;
            snapshot.Unit = GetString(item, "unit");
            snapshot.Room = GetString(item, "room");
            snapshot.ObservedUtc = TryGetDateTime(item, "observedUtc");
            snapshot.UpdatedUtc = now;
            synced++;
        }

        return synced;
    }

    protected static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    protected static string? GetString(JsonElement element, string propertyName)
    {
        return TryGetPropertyCaseInsensitive(element, propertyName, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : value.ToString()
            : null;
    }

    protected static DateTime? TryGetDateTime(JsonElement element, string propertyName)
    {
        return TryGetPropertyCaseInsensitive(element, propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            value.TryGetDateTime(out var dateTime)
            ? dateTime.ToUniversalTime()
            : null;
    }
}

public sealed class LocalHomeAutomationConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "Local Home";
    public override string Provider => ConnectorProviders.LocalHome;
}

public sealed class HomeAssistantConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "Home Assistant";
    public override string Provider => ConnectorProviders.HomeAssistant;
}

public sealed class HueConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "Hue";
    public override string Provider => ConnectorProviders.Hue;
}

public sealed class SmartThingsConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "SmartThings";
    public override string Provider => ConnectorProviders.SmartThings;
}

public sealed class ShellyConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "Shelly";
    public override string Provider => ConnectorProviders.Shelly;
}

public sealed class ESPHomeConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "ESPHome";
    public override string Provider => ConnectorProviders.ESPHome;
}

public sealed class MqttHomeConnector(CompanionDbContext dbContext, TimeProvider timeProvider)
    : HomeAutomationConnectorBase(dbContext, timeProvider)
{
    public override string Name => "MQTT";
    public override string Provider => ConnectorProviders.Mqtt;
}
