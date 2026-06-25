namespace Companion.Core.Models;

public sealed record MemorySuggestionCandidate(
    string Type,
    string Summary,
    string Content,
    decimal Confidence,
    string Source,
    int Importance,
    string Sensitivity);
