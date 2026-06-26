namespace Companion.Core.Abstractions;

public interface IConnectorRegistry
{
    void RegisterConnector(IConnector connector);

    IConnector? GetConnector(string provider);

    IReadOnlyList<IConnector> GetAvailableConnectors();
}
