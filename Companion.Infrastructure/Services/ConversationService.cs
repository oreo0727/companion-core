using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ConversationService(CompanionDbContext dbContext, TimeProvider timeProvider) : IConversationService
{
    public async Task<IReadOnlyList<Conversation>> GetConversationsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.LastMessageUtc)
            .ThenByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetConversationAsync(
        Guid userProfileId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.Id == conversationId,
                cancellationToken);
    }

    public async Task<Conversation> GetOrCreateDefaultConversationAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.LastMessageUtc)
            .ThenByDescending(x => x.UpdatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = "Companion Core Conversation",
            CreatedUtc = now,
            UpdatedUtc = now,
            LastMessageUtc = now,
            ActiveTopic = "Getting started"
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    public async Task<Message> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");

        var normalizedContent = content.Trim();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = normalizedContent,
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson,
            TokensEstimate = EstimateTokens(normalizedContent),
            CreatedUtc = now
        };

        dbContext.Messages.Add(message);

        conversation.UpdatedUtc = now;
        conversation.LastMessageUtc = now;

        if (role == MessageRole.User)
        {
            conversation.ActiveTopic = BuildActiveTopic(normalizedContent);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<IReadOnlyList<Message>> GetRecentMessagesAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var recentMessages = await dbContext.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(Math.Max(count, 1))
            .ToListAsync(cancellationToken);

        recentMessages.Reverse();
        return recentMessages;
    }

    private static int EstimateTokens(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return content
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string BuildActiveTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Conversation";
        }

        var compact = string.Join(
            ' ',
            content.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));

        return compact.Length <= 80 ? compact : $"{compact[..77]}...";
    }
}
