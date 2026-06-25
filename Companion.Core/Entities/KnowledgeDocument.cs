namespace Companion.Core.Entities;

public class KnowledgeDocument
{
    public Guid Id { get; set; }

    public Guid KnowledgeSourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public KnowledgeSource? KnowledgeSource { get; set; }

    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
}
