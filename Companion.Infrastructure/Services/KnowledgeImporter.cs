using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class KnowledgeImporter(
    CompanionDbContext dbContext,
    IChunkingService chunkingService,
    IAuditService auditService,
    TimeProvider timeProvider) : IKnowledgeImporter
{
    private static readonly HashSet<string> SupportedMimeTypes =
    [
        "text/plain",
        "text/markdown",
        "application/json"
    ];

    public async Task<KnowledgeImportResult> ImportAsync(
        Guid userProfileId,
        ImportKnowledgeDocumentCommand document,
        CancellationToken cancellationToken = default)
    {
        var mimeType = document.MimeType.Trim();
        if (!SupportedMimeTypes.Contains(mimeType))
        {
            throw new InvalidOperationException($"Unsupported mime type '{mimeType}'.");
        }

        var source = await ResolveSourceAsync(userProfileId, document, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var knowledgeDocument = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            KnowledgeSourceId = source.Id,
            Title = document.Title.Trim(),
            Content = document.Content.Trim(),
            MimeType = mimeType,
            CreatedUtc = now
        };

        var chunkDrafts = await chunkingService.ChunkAsync(
            knowledgeDocument.Content,
            knowledgeDocument.Title,
            knowledgeDocument.MimeType,
            cancellationToken);

        var chunks = chunkDrafts
            .Select(x => new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                KnowledgeDocumentId = knowledgeDocument.Id,
                Content = x.Content,
                ChunkIndex = x.ChunkIndex,
                MetadataJson = x.MetadataJson,
                CreatedUtc = now
            })
            .ToList();

        dbContext.KnowledgeDocuments.Add(knowledgeDocument);
        if (chunks.Count > 0)
        {
            dbContext.KnowledgeChunks.AddRange(chunks);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.KnowledgeDocumentImported,
            nameof(KnowledgeDocument),
            knowledgeDocument.Id.ToString(),
            $"Imported knowledge document '{knowledgeDocument.Title}' into source '{source.Name}' with {chunks.Count} chunk(s).",
            cancellationToken);

        knowledgeDocument.KnowledgeSource = source;
        var sourceDocumentCount = await dbContext.KnowledgeDocuments
            .CountAsync(x => x.KnowledgeSourceId == source.Id, cancellationToken);
        var sourceChunkCount = await dbContext.KnowledgeChunks
            .CountAsync(x => x.KnowledgeDocument!.KnowledgeSourceId == source.Id, cancellationToken);

        return new KnowledgeImportResult(source, knowledgeDocument, chunks, sourceDocumentCount, sourceChunkCount);
    }

    private async Task<KnowledgeSource> ResolveSourceAsync(
        Guid userProfileId,
        ImportKnowledgeDocumentCommand document,
        CancellationToken cancellationToken)
    {
        if (document.KnowledgeSourceId.HasValue)
        {
            return await dbContext.KnowledgeSources
                .FirstOrDefaultAsync(
                    x => x.Id == document.KnowledgeSourceId.Value && x.UserProfileId == userProfileId,
                    cancellationToken)
                ?? throw new KeyNotFoundException($"Knowledge source '{document.KnowledgeSourceId.Value}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(document.SourceName) || string.IsNullOrWhiteSpace(document.SourceType))
        {
            throw new InvalidOperationException("A new knowledge import requires 'sourceName' and 'sourceType' when no sourceId is provided.");
        }

        var normalizedName = document.SourceName.Trim();
        var normalizedType = document.SourceType.Trim();
        var existing = await dbContext.KnowledgeSources
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.Name == normalizedName &&
                     x.Type == normalizedType,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var source = new KnowledgeSource
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Name = normalizedName,
            Type = normalizedType,
            Description = string.IsNullOrWhiteSpace(document.SourceDescription) ? null : document.SourceDescription.Trim(),
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.KnowledgeSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return source;
    }
}
