namespace Companion.Core.Models;

public sealed record ConnectorTestResult(
    bool Succeeded,
    string? Error);
