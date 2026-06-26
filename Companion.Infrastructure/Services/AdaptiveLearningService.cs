using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public sealed class AdaptiveLearningService(
    CompanionDbContext dbContext,
    IAuditService auditService,
    TimeProvider timeProvider) : IAdaptiveLearningService
{
    public async Task<LearningEvent> RecordEventAsync(
        Guid userProfileId,
        RecordLearningEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var learningEvent = new LearningEvent
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            EventType = command.EventType.Trim(),
            SourceType = command.SourceType.Trim(),
            SourceId = command.SourceId.Trim(),
            Signal = command.Signal.Trim(),
            Weight = Math.Clamp(command.Weight, -10m, 10m),
            MetadataJson = NormalizeJson(command.MetadataJson),
            CreatedUtc = now
        };

        dbContext.LearningEvents.Add(learningEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.LearningEventRecorded,
            nameof(LearningEvent),
            learningEvent.Id.ToString(),
            $"{learningEvent.EventType}: {learningEvent.Signal}",
            cancellationToken);

        return learningEvent;
    }

    public async Task<ConversationRating> RateConversationAsync(
        Guid userProfileId,
        ConversationRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await dbContext.Conversations.AnyAsync(
            x => x.Id == command.ConversationId && x.UserProfileId == userProfileId,
            cancellationToken);
        if (!conversationExists)
        {
            throw new KeyNotFoundException($"Conversation '{command.ConversationId}' was not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rating = new ConversationRating
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ConversationId = command.ConversationId,
            Rating = Math.Clamp(command.Rating, 1, 5),
            Comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim(),
            CreatedUtc = now
        };

        dbContext.ConversationRatings.Add(rating);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecordEventAsync(
            userProfileId,
            new RecordLearningEventCommand(
                LearningEventTypes.ConversationRated,
                nameof(Conversation),
                command.ConversationId.ToString(),
                $"Conversation rated {rating.Rating}/5.",
                rating.Rating,
                JsonSerializer.Serialize(new { rating.Comment })),
            cancellationToken);

        return rating;
    }

    public async Task<LearningProfile> GetProfileAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var events = await dbContext.LearningEvents
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);
        var ratings = await dbContext.ConversationRatings
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new LearningProfile(
            userProfileId,
            Count(events, LearningEventTypes.SuggestionAccepted),
            Count(events, LearningEventTypes.SuggestionRejected),
            Count(events, LearningEventTypes.SuggestionIgnored),
            Count(events, LearningEventTypes.ToolUsed),
            Count(events, LearningEventTypes.ToolFailed),
            ratings.Count,
            ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(x => x.Rating), 2),
            Count(events, LearningEventTypes.GoalCompleted),
            Count(events, LearningEventTypes.ProjectCompleted),
            Count(events, LearningEventTypes.PreferenceEvolved),
            events
                .GroupBy(x => x.EventType)
                .OrderByDescending(x => x.Sum(e => e.Weight))
                .ThenBy(x => x.Key)
                .Take(5)
                .Select(x => $"{x.Key}:{x.Sum(e => e.Weight):0.##}")
                .ToList(),
            now);
    }

    public async Task<IReadOnlyList<LearningEvent>> GetEventsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LearningEvents
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    private static int Count(IReadOnlyList<LearningEvent> events, string eventType)
    {
        return events.Count(x => x.EventType == eventType);
    }

    private static string? NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }
}
