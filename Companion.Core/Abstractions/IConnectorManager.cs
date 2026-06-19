using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IConnectorManager
{
    Task<IReadOnlyList<ConnectorAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<bool> IsProviderAvailableAsync(string provider, CancellationToken cancellationToken = default);
}
