using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IReasoningEngine
{
    Task<ReasoningEngineResult> GenerateReplyAsync(
        Guid userProfileId,
        Guid conversationId,
        string? currentUserMessage = null,
        CancellationToken cancellationToken = default);
}
