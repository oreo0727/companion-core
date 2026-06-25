using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IKnowledgeImporter
{
    Task<KnowledgeImportResult> ImportAsync(
        Guid userProfileId,
        ImportKnowledgeDocumentCommand document,
        CancellationToken cancellationToken = default);
}
