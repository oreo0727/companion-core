namespace Companion.Core.Models;

public sealed record ToolExecutionResult(
    object? Output,
    string Summary);
