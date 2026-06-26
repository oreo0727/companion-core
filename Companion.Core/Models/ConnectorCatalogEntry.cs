using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ConnectorCatalogEntry(
    ConnectorDefinition Definition,
    IReadOnlyList<ConnectorConnection> Connections);
