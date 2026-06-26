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

    public async Task<LocalEmailImportResult> ImportLocalEmailAsync(
        Guid userProfileId,
        LocalEmailImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var definition = await dbContext.ConnectorDefinitions
            .FirstOrDefaultAsync(
                x => x.Provider == ConnectorProviders.LocalEmail && x.Enabled,
                cancellationToken)
            ?? throw new KeyNotFoundException("LocalEmail connector definition was not found.");

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
            messages = command.Messages.Select(x => new
            {
                x.ExternalId,
                x.Subject,
                x.FromName,
                x.FromAddress,
                x.ToAddresses,
                x.Preview,
                x.Body,
                x.ReceivedUtc,
                x.IsRead,
                x.HasAttachments,
                x.IsAnswered
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
        return new LocalEmailImportResult(connection, syncRun, syncRun.ItemsSynced);
    }

    public async Task<LocalHomeImportResult> ImportLocalHomeAsync(
        Guid userProfileId,
        LocalHomeImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var definition = await dbContext.ConnectorDefinitions
            .FirstOrDefaultAsync(
                x => x.Provider == ConnectorProviders.LocalHome && x.Enabled,
                cancellationToken)
            ?? throw new KeyNotFoundException("LocalHome connector definition was not found.");

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
            devices = command.Devices.Select(x => new
            {
                externalId = x.ExternalId,
                name = x.Name,
                deviceType = x.DeviceType,
                state = x.State,
                room = x.Room,
                capabilitiesJson = x.CapabilitiesJson,
                lastSeenUtc = x.LastSeenUtc
            }),
            sensors = command.Sensors.Select(x => new
            {
                externalId = x.ExternalId,
                name = x.Name,
                sensorType = x.SensorType,
                value = x.Value,
                unit = x.Unit,
                room = x.Room,
                observedUtc = x.ObservedUtc
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
        var devicesSynced = await dbContext.HomeDeviceSnapshots.CountAsync(
            x => x.ConnectorConnectionId == connection.Id,
            cancellationToken);
        var sensorsSynced = await dbContext.HomeSensorSnapshots.CountAsync(
            x => x.ConnectorConnectionId == connection.Id,
            cancellationToken);

        return new LocalHomeImportResult(connection, syncRun, devicesSynced, sensorsSynced);
    }

    public async Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default)
    {
        return await SyncAsync(userProfileId, connectorConnectionId, payloadJson: null, cancellationToken);
    }

    public async Task<ConnectorSyncRun> SyncAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        var connection = await dbContext.ConnectorConnections
            .Include(x => x.ConnectorDefinition)
            .FirstOrDefaultAsync(
                x => x.Id == connectorConnectionId && x.UserProfileId == userProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Connector connection '{connectorConnectionId}' was not found.");

        return await ExecuteSyncAsync(userProfileId, connection, payloadJson, cancellationToken);
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

    public async Task<IReadOnlyList<EmailMessageSnapshot>> GetRecentEmailMessagesAsync(
        Guid userProfileId,
        int daysBack = 14,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var since = now.AddDays(-Math.Clamp(daysBack, 1, 90));
        var take = Math.Clamp(limit, 1, 100);

        var messages = await dbContext.EmailMessageSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId && x.ReceivedUtc >= since)
            .ToListAsync(cancellationToken);

        var ordered = messages
            .OrderByDescending(EmailImportanceScore)
            .ThenByDescending(x => x.ReceivedUtc)
            .ThenBy(x => x.Subject)
            .Take(take)
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.EmailMessagesViewed,
                nameof(EmailMessageSnapshot),
                ordered.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Viewed {ordered.Count} recent email message snapshot(s).",
                cancellationToken);
        }

        return ordered;
    }

    public async Task<IReadOnlyList<EmailMessageSnapshot>> SearchEmailMessagesAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        var take = Math.Clamp(limit, 1, 100);

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return await GetRecentEmailMessagesAsync(userProfileId, 14, take, audit, cancellationToken);
        }

        var terms = normalizedQuery
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var messages = await dbContext.EmailMessageSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        var results = messages
            .Select(x => new
            {
                Message = x,
                TermScore = terms.Sum(term => EmailTermScore(x, term)),
                ImportanceScore = EmailImportanceScore(x)
            })
            .Where(x => x.TermScore > 0)
            .OrderByDescending(x => x.TermScore + x.ImportanceScore)
            .ThenByDescending(x => x.Message.ReceivedUtc)
            .Take(take)
            .Select(x => x.Message)
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.EmailSearchPerformed,
                nameof(EmailMessageSnapshot),
                results.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Searched email snapshots for '{normalizedQuery}' and found {results.Count} result(s).",
                cancellationToken);
        }

        return results;
    }

    public async Task<IReadOnlyList<FileDocumentSnapshot>> GetRecentFileDocumentsAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        var documents = await dbContext.FileDocumentSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.ModifiedUtc ?? x.UpdatedUtc)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.FileDocumentsViewed,
                nameof(FileDocumentSnapshot),
                documents.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Viewed {documents.Count} file document snapshot(s).",
                cancellationToken);
        }

        return documents;
    }

    public async Task<IReadOnlyList<HomeDeviceSnapshot>> GetHomeDevicesAsync(
        Guid userProfileId,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.HomeDeviceSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Room)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.HomeDevicesViewed,
                nameof(HomeDeviceSnapshot),
                devices.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Viewed {devices.Count} home device snapshot(s).",
                cancellationToken);
        }

        return devices;
    }

    public async Task<IReadOnlyList<HomeSensorSnapshot>> GetHomeSensorsAsync(
        Guid userProfileId,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var sensors = await dbContext.HomeSensorSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Room)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.HomeSensorsViewed,
                nameof(HomeSensorSnapshot),
                sensors.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Viewed {sensors.Count} home sensor snapshot(s).",
                cancellationToken);
        }

        return sensors;
    }

    public async Task<HomeActionResult> ExecuteHomeActionAsync(
        Guid userProfileId,
        string provider,
        string target,
        string action,
        string? parametersJson,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim();
        var normalizedTarget = target.Trim();
        var normalizedAction = action.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProvider) ||
            string.IsNullOrWhiteSpace(normalizedTarget) ||
            string.IsNullOrWhiteSpace(normalizedAction))
        {
            throw new InvalidOperationException("Home actions require provider, target, and action values.");
        }

        var summary = $"Approved home action '{normalizedAction}' for '{normalizedTarget}' on provider '{normalizedProvider}' recorded as a dry run.";
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.HomeActionExecuted,
            "HomeAutomationAction",
            normalizedTarget,
            $"{summary} Parameters={parametersJson ?? "{}"}",
            cancellationToken);

        return new HomeActionResult(true, normalizedProvider, normalizedAction, normalizedTarget, summary);
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

    private static int EmailImportanceScore(EmailMessageSnapshot snapshot)
    {
        var text = $"{snapshot.Subject} {snapshot.Preview} {snapshot.Body}";
        var score = 0;

        if (!snapshot.IsRead)
        {
            score += 8;
        }

        if (ContainsAny(text, ["urgent", "asap", "important", "action required", "immediately"]))
        {
            score += 10;
        }

        if (ContainsAny(text, ["bill", "payment", "invoice", "due", "deadline", "overdue"]))
        {
            score += 8;
        }

        if (snapshot.HasAttachments)
        {
            score += 3;
        }

        if (!snapshot.IsAnswered)
        {
            score += 4;
        }

        return score;
    }

    private static int EmailTermScore(EmailMessageSnapshot snapshot, string term)
    {
        var score = 0;

        if (ContainsTerm(snapshot.Subject, term))
        {
            score += 10;
        }

        if (ContainsTerm(snapshot.FromName, term) || ContainsTerm(snapshot.FromAddress, term))
        {
            score += 6;
        }

        if (ContainsTerm(snapshot.Preview, term))
        {
            score += 4;
        }

        if (ContainsTerm(snapshot.Body, term))
        {
            score += 2;
        }

        return score;
    }

    private static bool ContainsAny(string text, IReadOnlyList<string> terms)
    {
        return terms.Any(term => ContainsTerm(text, term));
    }

    private static bool ContainsTerm(string? text, string term)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
