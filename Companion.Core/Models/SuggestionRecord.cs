using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record SuggestionRecord(
    Guid Id,
    SuggestionKind Kind,
    string Title,
    string? Description,
    SuggestionStatus Status,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc,
    string? Detail = null,
    string? Meta = null);
