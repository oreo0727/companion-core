using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IAdaptiveLearningService
{
    Task<LearningEvent> RecordEventAsync(
        Guid userProfileId,
        RecordLearningEventCommand command,
        CancellationToken cancellationToken = default);

    Task<ConversationRating> RateConversationAsync(
        Guid userProfileId,
        ConversationRatingCommand command,
        CancellationToken cancellationToken = default);

    Task<LearningProfile> GetProfileAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearningEvent>> GetEventsAsync(Guid userProfileId, CancellationToken cancellationToken = default);
}
