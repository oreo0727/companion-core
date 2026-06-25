using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IAgentRuntime
{
    Task<IReadOnlyList<AgentRun>> GetRunsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<AgentRun> QueueRunAsync(QueueAgentRunCommand command, CancellationToken cancellationToken = default);

    Task<ProcessChatResult> ProcessChatAsync(
        Guid userProfileId,
        string message,
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    Task<int> ProcessPendingRunsAsync(CancellationToken cancellationToken = default);
}
