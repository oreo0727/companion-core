using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IGoalService
{
    Task<IReadOnlyList<Goal>> GetGoalsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goal>> GetActiveGoalsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<Goal> CreateGoalAsync(
        Guid userProfileId,
        CreateGoalCommand command,
        CancellationToken cancellationToken = default);

    Task<Goal?> UpdateGoalAsync(
        Guid userProfileId,
        Guid goalId,
        UpdateGoalCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoalSuggestion>> GetGoalSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<GoalSuggestion?> CaptureGoalSuggestionAsync(
        Guid userProfileId,
        CreateGoalSuggestionCommand command,
        CancellationToken cancellationToken = default);

    Task<Goal?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    Task<GoalSuggestion?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);
}
