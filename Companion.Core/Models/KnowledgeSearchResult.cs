using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record KnowledgeSearchResult(
    KnowledgeSource Source,
    KnowledgeDocument Document,
    KnowledgeChunk Chunk,
    int RelevanceScore);
