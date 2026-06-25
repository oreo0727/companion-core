using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IAuditService
{
    Task<IReadOnlyList<AuditEvent>> GetEventsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<AuditEvent> WriteEventAsync(
        Guid? userProfileId,
        string eventType,
        string entityType,
        string entityId,
        string description,
        CancellationToken cancellationToken = default);
}
