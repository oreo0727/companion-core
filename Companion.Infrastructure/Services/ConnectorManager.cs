using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ConnectorManager(CompanionDbContext dbContext) : IConnectorManager
{
    public async Task<IReadOnlyList<ConnectorAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ConnectorAccounts
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsProviderAvailableAsync(string provider, CancellationToken cancellationToken = default)
    {
        return dbContext.ConnectorAccounts.AnyAsync(
            x => x.Provider == provider && x.Status == ConnectorAccountStatus.Connected,
            cancellationToken);
    }
}
