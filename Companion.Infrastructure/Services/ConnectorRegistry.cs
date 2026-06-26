using Companion.Core.Abstractions;

namespace Companion.Infrastructure.Services;

public class ConnectorRegistry : IConnectorRegistry
{
    private readonly Dictionary<string, IConnector> connectors;

    public ConnectorRegistry(IEnumerable<IConnector> connectors)
    {
        this.connectors = new Dictionary<string, IConnector>(StringComparer.OrdinalIgnoreCase);

        foreach (var connector in connectors)
        {
            RegisterConnector(connector);
        }
    }

    public void RegisterConnector(IConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        connectors[connector.Provider] = connector;
    }

    public IConnector? GetConnector(string provider)
    {
        return connectors.GetValueOrDefault(provider);
    }

    public IReadOnlyList<IConnector> GetAvailableConnectors()
    {
        return connectors.Values
            .OrderBy(x => x.Provider, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
