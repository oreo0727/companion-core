namespace Companion.Core.Models;

public sealed record QueueAgentRunCommand(
    string AgentName,
    string Input,
    Guid? UserProfileId = null,
    Guid? ConversationId = null,
    string? MetadataJson = null);
