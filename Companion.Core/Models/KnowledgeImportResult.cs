using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record KnowledgeImportResult(
    KnowledgeSource Source,
    KnowledgeDocument Document,
    IReadOnlyList<KnowledgeChunk> Chunks,
    int SourceDocumentCount,
    int SourceChunkCount);
