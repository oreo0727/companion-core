using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public abstract class DesktopToolBase(IDesktopConnector desktopConnector) : ITool
{
    protected IDesktopConnector DesktopConnector { get; } = desktopConnector;

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract ToolRiskLevel RiskLevel { get; }

    public abstract Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context);

    protected static string RequiredString(ToolExecutionContext context, string propertyName)
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

    protected static string? OptionalString(ToolExecutionContext context, string propertyName)
    {
        return context.Input.TryGetProperty(propertyName, out var element)
            ? element.GetString()
            : null;
    }

    protected static bool OptionalBool(ToolExecutionContext context, string propertyName, bool defaultValue = false)
    {
        return context.Input.TryGetProperty(propertyName, out var element) && element.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
            ? element.GetBoolean()
            : defaultValue;
    }

    protected static int OptionalInt(ToolExecutionContext context, string propertyName, int defaultValue)
    {
        return context.Input.TryGetProperty(propertyName, out var element) && element.TryGetInt32(out var value)
            ? value
            : defaultValue;
    }

    protected static ToolExecutionResult ToToolResult(DesktopActionResult result)
    {
        return new ToolExecutionResult(
            new
            {
                result.Succeeded,
                result.Summary,
                result.Data
            },
            result.Summary);
    }
}

public sealed class DesktopReadFileTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopReadFile;
    public override string Description => "Read a file from the configured desktop automation root after approval.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.Medium;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var result = await DesktopConnector.ReadFileAsync(
            RequiredString(context, "path"),
            OptionalInt(context, "maxBytes", 32768),
            context.CancellationToken);

        return ToToolResult(result);
    }
}

public sealed class DesktopWriteFileTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopWriteFile;
    public override string Description => "Write a file inside the configured desktop automation root after approval.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var result = await DesktopConnector.WriteFileAsync(
            RequiredString(context, "path"),
            RequiredString(context, "content"),
            OptionalBool(context, "overwrite"),
            context.CancellationToken);

        return ToToolResult(result);
    }
}

public sealed class DesktopLaunchApplicationTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopLaunchApplication;
    public override string Description => "Launch a local desktop application after approval.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.LaunchApplicationAsync(
            RequiredString(context, "command"),
            OptionalString(context, "arguments"),
            context.CancellationToken));
    }
}

public sealed class DesktopCaptureScreenshotTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopCaptureScreenshot;
    public override string Description => "Capture a screenshot when host screenshot tooling is available.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.CaptureScreenshotAsync(context.CancellationToken));
    }
}

public sealed class DesktopGetClipboardTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopGetClipboard;
    public override string Description => "Read current clipboard text after approval.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.Medium;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.GetClipboardTextAsync(context.CancellationToken));
    }
}

public sealed class DesktopSetClipboardTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopSetClipboard;
    public override string Description => "Set clipboard text after approval.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.SetClipboardTextAsync(
            RequiredString(context, "text"),
            context.CancellationToken));
    }
}

public sealed class DesktopRunTerminalTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopRunTerminal;
    public override string Description => "Run a terminal command after approval and host configuration.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.RunTerminalCommandAsync(
            RequiredString(context, "command"),
            OptionalString(context, "workingDirectory"),
            OptionalInt(context, "timeoutSeconds", 30),
            context.CancellationToken));
    }
}

public sealed class DesktopSendKeyboardTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopSendKeyboard;
    public override string Description => "Send keyboard input after approval and host configuration.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.SendKeyboardAsync(
            RequiredString(context, "sequence"),
            context.CancellationToken));
    }
}

public sealed class DesktopMoveMouseTool(IDesktopConnector desktopConnector) : DesktopToolBase(desktopConnector)
{
    public override string Name => ToolNames.DesktopMoveMouse;
    public override string Description => "Move or click the mouse after approval and host configuration.";
    public override ToolRiskLevel RiskLevel => ToolRiskLevel.High;

    public override async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        return ToToolResult(await DesktopConnector.MoveMouseAsync(
            OptionalInt(context, "x", 0),
            OptionalInt(context, "y", 0),
            OptionalBool(context, "click"),
            context.CancellationToken));
    }
}
