using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public sealed class HomeStatusTool(IConnectorSyncService connectorSyncService) : ITool
{
    public string Name => ToolNames.HomeStatus;
    public string Description => "List current home device and sensor snapshots.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var devices = await connectorSyncService.GetHomeDevicesAsync(context.UserProfileId, audit: false, context.CancellationToken);
        var sensors = await connectorSyncService.GetHomeSensorsAsync(context.UserProfileId, audit: false, context.CancellationToken);
        var output = new
        {
            Devices = devices.Select(x => new { x.Id, x.Name, x.DeviceType, x.State, x.Room }).ToList(),
            Sensors = sensors.Select(x => new { x.Id, x.Name, x.SensorType, x.Value, x.Unit, x.Room }).ToList()
        };

        return new ToolExecutionResult(output, $"Found {devices.Count} home device(s) and {sensors.Count} sensor(s).");
    }
}

public sealed class HomeExecuteActionTool(IConnectorSyncService connectorSyncService) : ITool
{
    public string Name => ToolNames.HomeExecuteAction;
    public string Description => "Execute an approved home automation action.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var provider = RequiredString(context, "provider");
        var target = RequiredString(context, "target");
        var action = RequiredString(context, "action");
        var parametersJson = context.Input.TryGetProperty("parameters", out var parameters)
            ? parameters.ToString()
            : null;

        var result = await connectorSyncService.ExecuteHomeActionAsync(
            context.UserProfileId,
            provider,
            target,
            action,
            parametersJson,
            context.CancellationToken);

        return new ToolExecutionResult(result, result.Summary);
    }

    private static string RequiredString(ToolExecutionContext context, string propertyName)
    {
        var value = context.Input.TryGetProperty(propertyName, out var element)
            ? element.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required.");
        }

        return value;
    }
}
