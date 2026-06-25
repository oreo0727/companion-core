using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IContextBuilder
{
    Task<CompanionContext> BuildContextAsync(
        Guid userProfileId,
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
