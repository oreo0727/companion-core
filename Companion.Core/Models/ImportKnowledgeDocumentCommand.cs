namespace Companion.Core.Models;

public sealed record ImportKnowledgeDocumentCommand(
    Guid? KnowledgeSourceId,
    string? SourceName,
    string? SourceType,
    string? SourceDescription,
    string Title,
    string Content,
    string MimeType);
