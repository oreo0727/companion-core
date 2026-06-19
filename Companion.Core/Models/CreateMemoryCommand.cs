namespace Companion.Core.Models;

public sealed record CreateMemoryCommand(
    string Type,
    string Summary,
    string Content,
    string Source,
    int Importance,
    string Sensitivity,
    decimal Confidence = 0.90m,
    DateTime? ExpiresUtc = null);
