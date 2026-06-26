using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IAgentCatalog
{
    Task<IReadOnlyList<AgentDefinition>> GetAgentsAsync(CancellationToken cancellationToken = default);

    Task<AgentDefinition?> GetAgentAsync(string name, CancellationToken cancellationToken = default);
}
