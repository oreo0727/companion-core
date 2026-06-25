using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IToolExecutor
{
    Task<ToolDefinition?> GetDefinitionAsync(Guid toolDefinitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolDefinition>> GetDefinitionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolExecution>> GetExecutionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<ToolDispatchResult> ExecuteAsync(
        Guid userProfileId,
        string toolName,
        string inputJson,
        Guid? agentRunId = null,
        Guid? conversationId = null,
        Guid? sourceMessageId = null,
        CancellationToken cancellationToken = default);

    Task<ToolExecution?> ExecuteApprovedAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<ToolExecution?> RejectApprovedAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);
}
