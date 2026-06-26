using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IDesktopConnector
{
    Task<DesktopActionResult> ReadFileAsync(string path, int? maxBytes, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> WriteFileAsync(string path, string content, bool overwrite, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> LaunchApplicationAsync(string command, string? arguments, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> CaptureScreenshotAsync(CancellationToken cancellationToken = default);

    Task<DesktopActionResult> GetClipboardTextAsync(CancellationToken cancellationToken = default);

    Task<DesktopActionResult> SetClipboardTextAsync(string text, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> RunTerminalCommandAsync(string command, string? workingDirectory, int timeoutSeconds, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> SendKeyboardAsync(string sequence, CancellationToken cancellationToken = default);

    Task<DesktopActionResult> MoveMouseAsync(int x, int y, bool click, CancellationToken cancellationToken = default);
}
