using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ISuggestionService
{
    Task<IReadOnlyList<MemorySuggestion>> CaptureMemorySuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<MemorySuggestionCandidate> candidates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskSuggestion>> CaptureTaskSuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<TaskSuggestionCandidate> candidates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SuggestionRecord>> GetSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<SuggestionActionResult?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    Task<SuggestionRecord?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    Task<SuggestionRecord?> MarkSuggestionIgnoredAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);
}
