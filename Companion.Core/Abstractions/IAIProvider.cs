using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IAIProvider
{
    string ProviderName { get; }

    Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default);
}
