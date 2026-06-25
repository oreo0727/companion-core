using System.Text.Json;

namespace Companion.Core.Models;

public sealed record ToolExecutionContext(
    Guid UserProfileId,
    Guid ToolDefinitionId,
    Guid? AgentRunId,
    JsonElement Input,
    CancellationToken CancellationToken);
