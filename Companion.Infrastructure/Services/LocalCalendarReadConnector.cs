using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class LocalCalendarReadConnector(
    CompanionDbContext dbContext,
    TimeProvider timeProvider) : ICalendarReadConnector
{
    public string Name => "Local Calendar";

    public string Provider => ConnectorProviders.LocalCalendar;

    public ConnectorRiskLevel RiskLevel => ConnectorRiskLevel.Low;

    public Task<ConnectorTestResult> TestConnectionAsync(ConnectorSyncContext context)
    {
        return Task.FromResult(new ConnectorTestResult(true, null));
    }

    public async Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context)
    {
        if (context.Payload is null ||
            !TryGetPropertyCaseInsensitive(context.Payload.Value, "events", out var eventsElement) ||
            eventsElement.ValueKind != JsonValueKind.Array)
        {
            var existingCount = await dbContext.CalendarEventSnapshots
                .CountAsync(x => x.ConnectorConnectionId == context.Connection.Id, context.CancellationToken);
            return new ConnectorSyncResult(existingCount, "No import payload was supplied; existing local calendar snapshots were left unchanged.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var imported = 0;

        foreach (var item in eventsElement.EnumerateArray())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var title = TryGetPropertyCaseInsensitive(item, "title", out var titleElement)
                ? titleElement.GetString()?.Trim()
                : null;
            var startUtc = TryGetPropertyCaseInsensitive(item, "startUtc", out var startElement) && startElement.TryGetDateTime(out var parsedStartUtc)
                ? parsedStartUtc
                : (DateTime?)null;
            var endUtc = TryGetPropertyCaseInsensitive(item, "endUtc", out var endElement) && endElement.TryGetDateTime(out var parsedEndUtc)
                ? parsedEndUtc
                : (DateTime?)null;

            if (string.IsNullOrWhiteSpace(title) || startUtc is null || endUtc is null || endUtc < startUtc)
            {
                continue;
            }

            var externalId = TryGetPropertyCaseInsensitive(item, "externalId", out var externalIdElement)
                ? externalIdElement.GetString()?.Trim()
                : null;
            externalId = string.IsNullOrWhiteSpace(externalId)
                ? $"{title}-{startUtc:O}"
                : externalId;

            var snapshot = await dbContext.CalendarEventSnapshots
                .FirstOrDefaultAsync(
                    x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                    context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new CalendarEventSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };

                dbContext.CalendarEventSnapshots.Add(snapshot);
            }

            snapshot.Title = title;
            snapshot.Description = TryGetPropertyCaseInsensitive(item, "description", out var descriptionElement)
                ? descriptionElement.GetString()?.Trim()
                : null;
            snapshot.Location = TryGetPropertyCaseInsensitive(item, "location", out var locationElement)
                ? locationElement.GetString()?.Trim()
                : null;
            snapshot.StartUtc = startUtc.Value;
            snapshot.EndUtc = endUtc.Value;
            snapshot.IsAllDay = TryGetPropertyCaseInsensitive(item, "isAllDay", out var allDayElement) && allDayElement.ValueKind == JsonValueKind.True;
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Imported or updated {imported} calendar event snapshot(s).");
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
