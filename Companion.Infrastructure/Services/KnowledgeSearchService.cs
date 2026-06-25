using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class KnowledgeSearchService(
    CompanionDbContext dbContext,
    IAuditService auditService) : IKnowledgeSearchService
{
    private static readonly HashSet<string> SearchStopWords =
    [
        "a",
        "an",
        "and",
        "for",
        "from",
        "in",
        "is",
        "it",
        "of",
        "on",
        "or",
        "the",
        "to",
        "with"
    ];

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 8,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery) || limit <= 0)
        {
            return [];
        }

        var candidates = await dbContext.KnowledgeChunks
            .AsNoTracking()
            .Include(x => x.KnowledgeDocument)
            .ThenInclude(document => document!.KnowledgeSource)
            .Where(x => x.KnowledgeDocument!.KnowledgeSource!.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        var normalizedQueryLower = normalizedQuery.ToLowerInvariant();
        var terms = ExtractSearchTerms(normalizedQuery);
        var results = candidates
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = CalculateMatchScore(chunk, normalizedQueryLower, terms)
            })
            .Where(x => x.Score > 0 &&
                        x.Chunk.KnowledgeDocument is not null &&
                        x.Chunk.KnowledgeDocument.KnowledgeSource is not null)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.KnowledgeDocument!.Title)
            .ThenBy(x => x.Chunk.ChunkIndex)
            .Take(limit)
            .Select(x => new KnowledgeSearchResult(
                x.Chunk.KnowledgeDocument!.KnowledgeSource!,
                x.Chunk.KnowledgeDocument,
                x.Chunk,
                x.Score))
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.KnowledgeSearchPerformed,
                nameof(KnowledgeChunk),
                results.FirstOrDefault()?.Chunk.Id.ToString() ?? string.Empty,
                $"Performed knowledge search for '{normalizedQuery}' and found {results.Count} match(es).",
                cancellationToken);
        }

        return results;
    }

    public async Task<IReadOnlyList<KnowledgeSourceSummary>> GetSourcesAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.KnowledgeSources
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new KnowledgeSourceSummary(
                x.Id,
                x.UserProfileId,
                x.Name,
                x.Type,
                x.Description,
                x.CreatedUtc,
                x.Documents.Count,
                x.Documents.SelectMany(d => d.Chunks).Count()))
            .ToListAsync(cancellationToken);
    }

    private static string[] ExtractSearchTerms(string query)
    {
        return query
            .ToLowerInvariant()
            .Split([' ', '\r', '\n', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length >= 2 && !SearchStopWords.Contains(term))
            .Distinct()
            .ToArray();
    }

    private static int CalculateMatchScore(
        KnowledgeChunk chunk,
        string normalizedQueryLower,
        IReadOnlyCollection<string> terms)
    {
        var document = chunk.KnowledgeDocument;
        var source = document?.KnowledgeSource;
        var haystack = $"{source?.Name} {document?.Title} {chunk.Content}".ToLowerInvariant();
        var score = 0;

        if (haystack.Contains(normalizedQueryLower, StringComparison.Ordinal))
        {
            score += 8;
        }

        score += terms.Count(term => haystack.Contains(term, StringComparison.Ordinal));

        if (document is not null &&
            document.Title.Contains(normalizedQueryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (source is not null &&
            source.Name.Contains(normalizedQueryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        return score;
    }
}
