using Companion.Core.Entities;
using Companion.Core.Enums;

namespace Companion.Core.Abstractions;

public interface IConversationService
{
    Task<IReadOnlyList<Conversation>> GetConversationsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationAsync(
        Guid userProfileId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<Conversation> GetOrCreateDefaultConversationAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<Message> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetRecentMessagesAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default);
}
