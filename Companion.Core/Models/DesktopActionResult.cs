namespace Companion.Core.Models;

public sealed record DesktopActionResult(
    bool Succeeded,
    string Summary,
    object? Data = null);
