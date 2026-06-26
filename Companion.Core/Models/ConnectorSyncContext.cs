using System.Text.Json;
using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ConnectorSyncContext(
    Guid UserProfileId,
    ConnectorConnection Connection,
    JsonElement? Payload,
    CancellationToken CancellationToken);
