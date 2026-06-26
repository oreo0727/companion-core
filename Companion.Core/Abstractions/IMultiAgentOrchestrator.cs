using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IMultiAgentOrchestrator
{
    Task<AgentRun> ExecuteAsync(AgentRun agentRun, CancellationToken cancellationToken = default);
}
