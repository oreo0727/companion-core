using System.Diagnostics;
using System.Text;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Companion.Infrastructure.Services;

public class LocalDesktopConnector(IConfiguration configuration) : IDesktopConnector
{
    private const int DefaultMaxReadBytes = 32 * 1024;
    private const int MaxReadBytes = 256 * 1024;
    private string clipboardBuffer = string.Empty;

    public async Task<DesktopActionResult> ReadFileAsync(string path, int? maxBytes, CancellationToken cancellationToken = default)
    {
        var resolvedPath = ResolvePath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("DesktopReadFile could not find the requested file.", resolvedPath);
        }

        var byteLimit = Math.Clamp(maxBytes ?? DefaultMaxReadBytes, 1, MaxReadBytes);
        await using var stream = File.OpenRead(resolvedPath);
        var buffer = new byte[Math.Min(byteLimit, (int)Math.Min(stream.Length, byteLimit))];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        var content = Encoding.UTF8.GetString(buffer, 0, read);

        return new DesktopActionResult(
            true,
            $"Read {read} byte(s) from '{resolvedPath}'.",
            new
            {
                Path = resolvedPath,
                BytesRead = read,
                Truncated = stream.Length > read,
                Content = content
            });
    }

    public async Task<DesktopActionResult> WriteFileAsync(string path, string content, bool overwrite, CancellationToken cancellationToken = default)
    {
        var resolvedPath = ResolvePath(path);
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(resolvedPath) && !overwrite)
        {
            throw new IOException("DesktopWriteFile target already exists and overwrite was not requested.");
        }

        await File.WriteAllTextAsync(resolvedPath, content, Encoding.UTF8, cancellationToken);
        return new DesktopActionResult(
            true,
            $"Wrote {Encoding.UTF8.GetByteCount(content)} byte(s) to '{resolvedPath}'.",
            new
            {
                Path = resolvedPath,
                BytesWritten = Encoding.UTF8.GetByteCount(content)
            });
    }

    public async Task<DesktopActionResult> LaunchApplicationAsync(string command, string? arguments, CancellationToken cancellationToken = default)
    {
        if (!GetConfiguredBool("DesktopAutomation:AllowProcessLaunch"))
        {
            return new DesktopActionResult(
                true,
                "LaunchApplication was approved but process launch is disabled; recorded a dry run.",
                new { Command = command, Arguments = arguments, DryRun = true });
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments ?? string.Empty,
            UseShellExecute = false
        });

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        return new DesktopActionResult(
            process is not null,
            process is null ? "Application launch failed." : $"Launched '{command}' with process id {process.Id}.",
            new { Command = command, Arguments = arguments, ProcessId = process?.Id });
    }

    public async Task<DesktopActionResult> CaptureScreenshotAsync(CancellationToken cancellationToken = default)
    {
        var hasDisplay = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")) ||
                         !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
        var screenshotTool = FindExecutable("gnome-screenshot") ?? FindExecutable("import");
        if (!hasDisplay || screenshotTool is null)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                false,
                "Screenshot capture is unavailable on this host session.",
                new { Available = false, HasDisplay = hasDisplay, ToolFound = screenshotTool is not null });
        }

        var screenshotDirectory = Path.Combine(GetAllowedRoot(), "screenshots");
        Directory.CreateDirectory(screenshotDirectory);
        var outputPath = Path.Combine(screenshotDirectory, $"companion-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        var arguments = Path.GetFileName(screenshotTool).Equals("import", StringComparison.OrdinalIgnoreCase)
            ? $"-window root \"{outputPath}\""
            : $"-f \"{outputPath}\"";

        var result = await RunProcessAsync(screenshotTool, arguments, GetAllowedRoot(), 10, cancellationToken);
        return new DesktopActionResult(
            result.ExitCode == 0 && File.Exists(outputPath),
            result.ExitCode == 0 ? $"Captured screenshot to '{outputPath}'." : "Screenshot command failed.",
            new { Path = outputPath, result.ExitCode, result.StdOut, result.StdErr });
    }

    public async Task<DesktopActionResult> GetClipboardTextAsync(CancellationToken cancellationToken = default)
    {
        var pasteTool = FindExecutable("wl-paste") ?? FindExecutable("xclip");
        if (pasteTool is null)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                true,
                "Clipboard tooling is unavailable; returned the connector buffer.",
                new { Text = clipboardBuffer, Source = "ConnectorBuffer" });
        }

        var arguments = Path.GetFileName(pasteTool).Equals("xclip", StringComparison.OrdinalIgnoreCase)
            ? "-selection clipboard -o"
            : string.Empty;
        var result = await RunProcessAsync(pasteTool, arguments, GetAllowedRoot(), 5, cancellationToken);
        return new DesktopActionResult(
            result.ExitCode == 0,
            result.ExitCode == 0 ? "Read clipboard text." : "Clipboard read failed.",
            new { Text = result.StdOut, result.ExitCode, result.StdErr });
    }

    public async Task<DesktopActionResult> SetClipboardTextAsync(string text, CancellationToken cancellationToken = default)
    {
        clipboardBuffer = text;
        var copyTool = FindExecutable("wl-copy") ?? FindExecutable("xclip");
        if (copyTool is null)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                true,
                "Clipboard tooling is unavailable; stored text in connector buffer.",
                new { Characters = text.Length, Target = "ConnectorBuffer" });
        }

        var arguments = Path.GetFileName(copyTool).Equals("xclip", StringComparison.OrdinalIgnoreCase)
            ? "-selection clipboard"
            : string.Empty;
        var result = await RunProcessAsync(copyTool, arguments, GetAllowedRoot(), 5, cancellationToken, text);
        return new DesktopActionResult(
            result.ExitCode == 0,
            result.ExitCode == 0 ? "Updated clipboard text." : "Clipboard write failed.",
            new { Characters = text.Length, result.ExitCode, result.StdErr });
    }

    public async Task<DesktopActionResult> RunTerminalCommandAsync(string command, string? workingDirectory, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        var resolvedWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? GetAllowedRoot()
            : ResolvePath(workingDirectory);

        if (!Directory.Exists(resolvedWorkingDirectory))
        {
            throw new DirectoryNotFoundException($"Terminal working directory was not found: {resolvedWorkingDirectory}");
        }

        if (!GetConfiguredBool("DesktopAutomation:AllowTerminalExecution"))
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                true,
                "Terminal command was approved but terminal execution is disabled; recorded a dry run.",
                new { Command = command, WorkingDirectory = resolvedWorkingDirectory, DryRun = true });
        }

        var result = await RunProcessAsync("/bin/bash", $"-lc \"{EscapeBashArgument(command)}\"", resolvedWorkingDirectory, timeoutSeconds, cancellationToken);
        return new DesktopActionResult(
            result.ExitCode == 0,
            result.ExitCode == 0 ? "Terminal command completed." : "Terminal command failed.",
            new
            {
                Command = command,
                WorkingDirectory = resolvedWorkingDirectory,
                result.ExitCode,
                StdOut = Truncate(result.StdOut, 4000),
                StdErr = Truncate(result.StdErr, 4000)
            });
    }

    public async Task<DesktopActionResult> SendKeyboardAsync(string sequence, CancellationToken cancellationToken = default)
    {
        if (!GetConfiguredBool("DesktopAutomation:AllowInputAutomation") || FindExecutable("xdotool") is not { } xdotool)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                true,
                "Keyboard automation was approved but host input automation is unavailable or disabled; recorded a dry run.",
                new { Sequence = sequence, DryRun = true });
        }

        var result = await RunProcessAsync(xdotool, $"key {sequence}", GetAllowedRoot(), 5, cancellationToken);
        return new DesktopActionResult(result.ExitCode == 0, "Keyboard automation attempted.", result);
    }

    public async Task<DesktopActionResult> MoveMouseAsync(int x, int y, bool click, CancellationToken cancellationToken = default)
    {
        if (!GetConfiguredBool("DesktopAutomation:AllowInputAutomation") || FindExecutable("xdotool") is not { } xdotool)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new DesktopActionResult(
                true,
                "Mouse automation was approved but host input automation is unavailable or disabled; recorded a dry run.",
                new { X = x, Y = y, Click = click, DryRun = true });
        }

        var command = click ? $"mousemove {x} {y} click 1" : $"mousemove {x} {y}";
        var result = await RunProcessAsync(xdotool, command, GetAllowedRoot(), 5, cancellationToken);
        return new DesktopActionResult(result.ExitCode == 0, "Mouse automation attempted.", result);
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("A desktop file path is required.");
        }

        var allowedRoot = GetAllowedRoot();
        var resolved = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(allowedRoot, path));
        if (!resolved.StartsWith(allowedRoot, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Path '{resolved}' is outside the configured desktop automation root.");
        }

        return resolved;
    }

    private string GetAllowedRoot()
    {
        var configuredRoot = configuration["DesktopAutomation:AllowedRoot"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Path.GetTempPath(), "companion-desktop")
            : configuredRoot;
        var resolved = Path.GetFullPath(root);
        Directory.CreateDirectory(resolved);
        return resolved.EndsWith(Path.DirectorySeparatorChar)
            ? resolved
            : resolved + Path.DirectorySeparatorChar;
    }

    private bool GetConfiguredBool(string key)
    {
        return bool.TryParse(configuration[key], out var value) && value;
    }

    private static async Task<ProcessRunResult> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken,
        string? standardInput = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 120)));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = standardInput is not null,
                UseShellExecute = false
            }
        };

        process.Start();
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput.AsMemory(), timeout.Token);
            process.StandardInput.Close();
        }

        var stdOut = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stdErr = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);
        return new ProcessRunResult(process.ExitCode, await stdOut, await stdErr);
    }

    private static string? FindExecutable(string name)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return paths
            .Select(path => Path.Combine(path, name))
            .FirstOrDefault(File.Exists);
    }

    private static string EscapeBashArgument(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record ProcessRunResult(int ExitCode, string StdOut, string StdErr);
}
