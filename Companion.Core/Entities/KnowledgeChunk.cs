namespace Companion.Core.Entities;

public class KnowledgeChunk
{
    public Guid Id { get; set; }

    public Guid KnowledgeDocumentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public DateTime CreatedUtc { get; set; }

    public KnowledgeDocument? KnowledgeDocument { get; set; }
}
