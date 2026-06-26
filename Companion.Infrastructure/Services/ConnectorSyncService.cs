using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ConnectorSyncService(
    CompanionDbContext dbContext,
    IConnectorRegistry connectorRegistry,
    IAuditService auditService,
    TimeProvider timeProvider) : IConnectorSyncService
{
    public async Task<IReadOnlyList<ConnectorCatalogEntry>> GetCatalogAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var definitions = await dbContext.ConnectorDefinitions
            .AsNoTracking()
            .Where(x => x.Enabled)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var connections = await dbContext.ConnectorConnections
            .AsNoTracking()
            .Include(x => x.ConnectorDefinition)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);

        return definitions
            .Select(definition => new ConnectorCatalogEntry(
                definition,
                connections.Where(x => x.ConnectorDefinitionId == definition.Id).ToList()))
            .ToList();
    }

    public async Task<LocalCalendarImportResult> ImportLocalCalendarAsync(
        Guid userProfileId,
        LocalCalendarImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var definition = await dbContext.ConnectorDefinitions
            .FirstOrDefaultAsync(
                x => x.Provider == ConnectorProviders.LocalCalendar && x.Enabled,
                cancellationToken)
            ?? throw new KeyNotFoundException("LocalCalendar connector definition was not found.");

        var normalizedDisplayName = command.DisplayName.Trim();
        var connection = await dbContext.ConnectorConnections
            .Include(x => x.ConnectorDefinition)
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.ConnectorDefinitionId == definition.Id &&
                     x.DisplayName == normalizedDisplayName,
                cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var isNewConnection = false;

        if (connection is null)
        {
            connection = new ConnectorConnection
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ConnectorDefinitionId = definition.Id,
                DisplayName = normalizedDisplayName,
                Status = ConnectorConnectionStatus.Connected,
                CreatedUtc = now,
                UpdatedUtc = now,
                ConnectorDefinition = definition
            };
            dbContext.ConnectorConnections.Add(connection);
            await dbContext.SaveChangesAsync(cancellationToken);
            isNewConnection = true;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            events = command.Events.Select(x => new
            {
                x.ExternalId,
                x.Title,
                x.Description,
                x.Location,
                x.StartUtc,
                x.EndUtc,
                x.IsAllDay
            })
        });

        if (isNewConnection)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.ConnectorConnected,
                nameof(ConnectorConnection),
                connection.Id.ToString(),
                $"Connected '{connection.DisplayName}' through the {definition.Provider} connector.",
                cancellationToken);
        }

        var syncRun = await ExecuteSyncAsync(userProfileId, connection, payloadJson, cancellationToken);
        return new LocalCalendarImportResult(connection, syncRun, syncRun.ItemsSynced);
    }

    public async Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default)
    {
        var connection = await dbContext.ConnectorConnections
            .Include(x => x.ConnectorDefinition)
            .FirstOrDefaultAsync(
                x => x.Id == connectorConnectionId && x.UserProfileId == userProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Connector connection '{connectorConnectionId}' was not found.");

        return await ExecuteSyncAsync(userProfileId, connection, payloadJson: null, cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarEventSnapshot>> GetUpcomingCalendarEventsAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var horizon = now.Date.AddDays(Math.Clamp(daysAhead, 1, 30) + 1);

        var events = await dbContext.CalendarEventSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.EndUtc >= now &&
                x.StartUtc < horizon)
            .ToListAsync(cancellationToken);

        var ordered = events
            .OrderBy(x => RankEventWindow(x, now))
            .ThenBy(x => x.StartUtc)
            .ThenBy(x => x.Title)
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.CalendarEventsViewed,
                nameof(CalendarEventSnapshot),
                ordered.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Viewed {ordered.Count} upcoming calendar event(s).",
                cancellationToken);
        }

        return ordered;
    }

    private async Task<ConnectorSyncRun> ExecuteSyncAsync(
        Guid userProfileId,
        ConnectorConnection connection,
        string? payloadJson,
        CancellationToken cancellationToken)
    {
        var connector = connectorRegistry.GetConnector(connection.ConnectorDefinition?.Provider ?? string.Empty)
            ?? throw new InvalidOperationException($"Connector '{connection.ConnectorDefinition?.Provider}' is not registered.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var syncRun = new ConnectorSyncRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ConnectorConnectionId = connection.Id,
            Status = ConnectorSyncRunStatus.Running,
            StartedUtc = now,
            ItemsSynced = 0
        };

        dbContext.ConnectorSyncRuns.Add(syncRun);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.ConnectorSyncStarted,
            nameof(ConnectorSyncRun),
            syncRun.Id.ToString(),
            $"Started sync for connector '{connection.DisplayName}'.",
            cancellationToken);

        try
        {
            JsonElement? payload = null;
            if (!string.IsNullOrWhiteSpace(payloadJson))
            {
                using var document = JsonDocument.Parse(payloadJson);
                payload = document.RootElement.Clone();
            }

            var context = new ConnectorSyncContext(userProfileId, connection, payload, cancellationToken);
            var testResult = await connector.TestConnectionAsync(context);
            if (!testResult.Succeeded)
            {
                throw new InvalidOperationException(testResult.Error ?? "Connector test failed.");
            }

            var result = await connector.SyncAsync(context);
            syncRun.Status = ConnectorSyncRunStatus.Completed;
            syncRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            syncRun.ItemsSynced = result.ItemsSynced;
            syncRun.Error = null;
            connection.Status = ConnectorConnectionStatus.Connected;
            connection.UpdatedUtc = syncRun.CompletedUtc.Value;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.ConnectorSyncCompleted,
                nameof(ConnectorSyncRun),
                syncRun.Id.ToString(),
                $"Completed sync for connector '{connection.DisplayName}' with {result.ItemsSynced} item(s) synced.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            syncRun.Status = ConnectorSyncRunStatus.Failed;
            syncRun.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            syncRun.Error = ex.Message;
            connection.Status = ConnectorConnectionStatus.NeedsAttention;
            connection.UpdatedUtc = syncRun.CompletedUtc.Value;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.ConnectorSyncCompleted,
                nameof(ConnectorSyncRun),
                syncRun.Id.ToString(),
                $"Connector sync failed for '{connection.DisplayName}': {ex.Message}",
                cancellationToken);
        }

        return syncRun;
    }

    private static int RankEventWindow(CalendarEventSnapshot snapshot, DateTime now)
    {
        if (snapshot.StartUtc.Date == now.Date)
        {
            return 0;
        }

        if (snapshot.StartUtc.Date == now.Date.AddDays(1))
        {
            return 1;
        }

        return 2;
    }
}
