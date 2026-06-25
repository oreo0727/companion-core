using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IMemoryExtractionService
{
    Task<ExtractionCandidates> ExtractAsync(
        Guid userProfileId,
        Guid conversationId,
        string userMessage,
        string assistantResponse,
        CancellationToken cancellationToken = default);
}
