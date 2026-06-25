using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IChunkingService
{
    Task<IReadOnlyList<ChunkedKnowledgeSegment>> ChunkAsync(
        string content,
        string title,
        string mimeType,
        CancellationToken cancellationToken = default);
}
