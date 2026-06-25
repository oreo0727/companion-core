using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IKnowledgeSearchService
{
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 8,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSourceSummary>> GetSourcesAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);
}
