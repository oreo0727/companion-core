namespace Companion.Core.Models;

public sealed record ChunkedKnowledgeSegment(
    string Content,
    int ChunkIndex,
    string MetadataJson);
