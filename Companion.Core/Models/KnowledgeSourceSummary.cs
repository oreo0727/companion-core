namespace Companion.Core.Models;

public sealed record KnowledgeSourceSummary(
    Guid Id,
    Guid UserProfileId,
    string Name,
    string Type,
    string? Description,
    DateTime CreatedUtc,
    int DocumentCount,
    int ChunkCount);
