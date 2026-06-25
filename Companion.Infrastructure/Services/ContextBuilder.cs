using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ContextBuilder(
    CompanionDbContext dbContext,
    IChiefOfStaffService chiefOfStaffService,
    IKnowledgeSearchService knowledgeSearchService,
    TimeProvider timeProvider) : IContextBuilder
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
        "it",
        "me",
        "my",
        "of",
        "on",
        "please",
        "that",
        "the",
        "this",
        "to",
        "we",
        "with",
        "you"
    ];

    public async Task<CompanionContext> BuildContextAsync(
        Guid userProfileId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == conversationId && x.UserProfileId == userProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");

        var recentMessages = await dbContext.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(12)
            .ToListAsync(cancellationToken);

        recentMessages.Reverse();

        var relevantMemories = await SelectRelevantMemoriesAsync(userProfileId, recentMessages, conversation.ActiveTopic, now, cancellationToken);
        var relevantKnowledge = await SelectRelevantKnowledgeAsync(userProfileId, recentMessages, conversation.ActiveTopic, cancellationToken);
        var openTasks = await dbContext.TaskItems
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Cancelled)
            .OrderBy(x => x.DueDateUtc)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.CreatedUtc)
            .Take(8)
            .ToListAsync(cancellationToken);
        var activeGoals = await dbContext.Goals
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != GoalStatus.Completed &&
                x.Status != GoalStatus.Cancelled)
            .OrderBy(x => x.TargetDateUtc)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .Take(6)
            .ToListAsync(cancellationToken);
        var activeProjects = await dbContext.Projects
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != ProjectStatus.Completed &&
                x.Status != ProjectStatus.Archived)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .Take(6)
            .ToListAsync(cancellationToken);
        var openLoops = await dbContext.OpenLoops
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != OpenLoopStatus.Closed)
            .OrderByDescending(x => x.Status == OpenLoopStatus.Waiting)
            .ThenByDescending(x => x.CreatedUtc)
            .Take(6)
            .ToListAsync(cancellationToken);
        var pendingApprovals = await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status == ApprovalRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(5)
            .ToListAsync(cancellationToken);
        var chiefOfStaffInsights = (await chiefOfStaffService.GetDashboardAsync(userProfileId, cancellationToken))
            .TopInsights
            .Take(4)
            .ToList();

        return new CompanionContext(
            userProfileId,
            conversationId,
            conversation.ActiveTopic,
            recentMessages,
            relevantMemories,
            activeGoals,
            activeProjects,
            relevantKnowledge,
            openTasks,
            openLoops,
            pendingApprovals,
            chiefOfStaffInsights);
    }

    private async Task<IReadOnlyList<MemoryEntry>> SelectRelevantMemoriesAsync(
        Guid userProfileId,
        IReadOnlyList<Message> recentMessages,
        string? activeTopic,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var memories = await dbContext.MemoryEntries
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                !x.IsArchived &&
                (x.ExpiresUtc == null || x.ExpiresUtc > now))
            .ToListAsync(cancellationToken);

        if (memories.Count == 0)
        {
            return [];
        }

        var searchText = string.Join(
            ' ',
            recentMessages
                .Where(x => x.Role == MessageRole.User)
                .Select(x => x.Content)
                .Append(activeTopic ?? string.Empty));
        var normalizedSearchText = searchText.Trim().ToLowerInvariant();
        var terms = searchText
            .ToLowerInvariant()
            .Split([' ', '\r', '\n', '\t', '.', ',', '!', '?', ';', ':', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 3 && !SearchStopWords.Contains(x))
            .Distinct()
            .ToArray();

        return memories
            .Select(x => new
            {
                Memory = x,
                Score = CalculateMemoryScore(x, normalizedSearchText, terms)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Memory.Importance)
            .ThenByDescending(x => x.Memory.LastReferencedUtc ?? x.Memory.CreatedUtc)
            .Take(6)
            .Select(x => x.Memory)
            .ToList();
    }

    private static int CalculateMemoryScore(MemoryEntry memory, string normalizedSearchText, IReadOnlyCollection<string> terms)
    {
        var haystack = $"{memory.Summary} {memory.Content}".ToLowerInvariant();
        var score = memory.Importance;

        if (!string.IsNullOrWhiteSpace(normalizedSearchText) &&
            haystack.Contains(normalizedSearchText, StringComparison.Ordinal))
        {
            score += 5;
        }

        score += terms.Count(term => haystack.Contains(term, StringComparison.Ordinal));
        return score;
    }

    private async Task<IReadOnlyList<KnowledgeSearchResult>> SelectRelevantKnowledgeAsync(
        Guid userProfileId,
        IReadOnlyList<Message> recentMessages,
        string? activeTopic,
        CancellationToken cancellationToken)
    {
        var query = string.Join(
            ' ',
            recentMessages
                .Where(x => x.Role == MessageRole.User)
                .Select(x => x.Content)
                .Append(activeTopic ?? string.Empty))
            .Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return await knowledgeSearchService.SearchAsync(
            userProfileId,
            query,
            limit: 4,
            audit: false,
            cancellationToken: cancellationToken);
    }
}
