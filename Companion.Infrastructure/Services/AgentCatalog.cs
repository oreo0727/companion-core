using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public sealed class AgentCatalog(CompanionDbContext dbContext) : IAgentCatalog
{
    public async Task<IReadOnlyList<AgentDefinition>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AgentDefinitions
            .AsNoTracking()
            .Where(x => x.Enabled)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentDefinition?> GetAgentAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await dbContext.AgentDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Enabled &&
                    (x.Name == normalizedName || x.DisplayName == normalizedName),
                cancellationToken);
    }
}
