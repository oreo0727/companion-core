using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record SuggestionActionResult(
    SuggestionRecord Suggestion,
    string MaterializedEntityType,
    Guid MaterializedEntityId,
    SuggestionKind Kind);
