using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class MemoryService(
    CompanionDbContext dbContext,
    IAuditService auditService,
    TimeProvider timeProvider) : IMemoryService
{
    private static readonly HashSet<string> SearchStopWords =
    [
        "a",
        "an",
        "and",
        "for",
        "from",
        "have",
        "i",
        "me",
        "my",
        "need",
        "note",
        "now",
        "please",
        "remember",
        "remind",
        "send",
        "task",
        "that",
        "the",
        "this",
        "to",
        "todo",
        "with",
        "you",
        "your"
    ];

    public async Task<IReadOnlyList<MemoryEntry>> GetMemoriesAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MemoryEntries
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.IsArchived)
            .ThenByDescending(x => x.LastReferencedUtc ?? x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryEntry>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(normalizedQuery) || limit <= 0)
        {
            return [];
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var candidates = await dbContext.MemoryEntries
            .Where(x =>
                x.UserProfileId == userProfileId &&
                !x.IsArchived &&
                (x.ExpiresUtc == null || x.ExpiresUtc > now))
            .ToListAsync(cancellationToken);

        var normalizedQueryLower = normalizedQuery.ToLowerInvariant();
        var terms = ExtractSearchTerms(normalizedQuery);
        var matches = candidates
            .Select(memoryEntry => new
            {
                MemoryEntry = memoryEntry,
                Score = CalculateMatchScore(memoryEntry, normalizedQueryLower, terms)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.MemoryEntry.Importance)
            .ThenByDescending(x => x.MemoryEntry.LastReferencedUtc ?? x.MemoryEntry.CreatedUtc)
            .Take(limit)
            .Select(x => x.MemoryEntry)
            .ToList();

        if (matches.Count == 0)
        {
            return matches;
        }

        foreach (var match in matches)
        {
            match.LastReferencedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return matches;
    }

    public async Task<MemoryEntry> CreateMemoryAsync(
        Guid userProfileId,
        CreateMemoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var memoryEntry = new MemoryEntry
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Type = command.Type.Trim(),
            Summary = command.Summary.Trim(),
            Content = command.Content.Trim(),
            Confidence = Math.Clamp(command.Confidence, 0m, 1m),
            Source = command.Source.Trim(),
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime,
            Importance = Math.Clamp(command.Importance, 1, 5),
            Sensitivity = string.IsNullOrWhiteSpace(command.Sensitivity) ? "Normal" : command.Sensitivity.Trim(),
            ExpiresUtc = command.ExpiresUtc,
            IsArchived = false
        };

        dbContext.MemoryEntries.Add(memoryEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.MemoryCreated,
            nameof(MemoryEntry),
            memoryEntry.Id.ToString(),
            $"Created memory '{memoryEntry.Summary}'.",
            cancellationToken);

        return memoryEntry;
    }

    public async Task<MemoryEntry?> ArchiveMemoryAsync(
        Guid userProfileId,
        Guid memoryEntryId,
        CancellationToken cancellationToken = default)
    {
        var memoryEntry = await dbContext.MemoryEntries
            .FirstOrDefaultAsync(
                x => x.Id == memoryEntryId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (memoryEntry is null)
        {
            return null;
        }

        memoryEntry.IsArchived = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return memoryEntry;
    }

    private static string[] ExtractSearchTerms(string query)
    {
        return query
            .ToLowerInvariant()
            .Split([
                ' ',
                '\r',
                '\n',
                '\t',
                '.',
                ',',
                '!',
                '?',
                ';',
                ':',
                '(',
                ')'
            ], StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length >= 3 && !SearchStopWords.Contains(term))
            .Distinct()
            .ToArray();
    }

    private static int CalculateMatchScore(
        MemoryEntry memoryEntry,
        string normalizedQueryLower,
        IReadOnlyCollection<string> terms)
    {
        var haystack = $"{memoryEntry.Summary} {memoryEntry.Content}".ToLowerInvariant();
        var score = 0;
        var termMatches = terms.Count(term => haystack.Contains(term, StringComparison.Ordinal));

        if (haystack.Contains(normalizedQueryLower, StringComparison.Ordinal))
        {
            score += 5;
        }

        if (score == 0 && termMatches == 0)
        {
            return 0;
        }

        score += termMatches;
        score += memoryEntry.Importance;

        return score;
    }
}
