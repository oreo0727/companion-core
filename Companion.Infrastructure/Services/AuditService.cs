using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class AuditService(
    CompanionDbContext dbContext,
    TimeProvider timeProvider) : IAuditService
{
    public async Task<IReadOnlyList<AuditEvent>> GetEventsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditEvent> WriteEventAsync(
        Guid? userProfileId,
        string eventType,
        string entityType,
        string entityId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            EventType = eventType.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId.Trim(),
            Description = description.Trim(),
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return auditEvent;
    }
}
