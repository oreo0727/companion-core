using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class LocalEmailReadConnector(
    CompanionDbContext dbContext,
    TimeProvider timeProvider) : IEmailReadConnector
{
    public string Name => "Local Email";

    public string Provider => ConnectorProviders.LocalEmail;

    public ConnectorRiskLevel RiskLevel => ConnectorRiskLevel.Low;

    public Task<ConnectorTestResult> TestConnectionAsync(ConnectorSyncContext context)
    {
        return Task.FromResult(new ConnectorTestResult(true, null));
    }

    public async Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context)
    {
        if (context.Payload is null ||
            !TryGetPropertyCaseInsensitive(context.Payload.Value, "messages", out var messagesElement) ||
            messagesElement.ValueKind != JsonValueKind.Array)
        {
            var existingCount = await dbContext.EmailMessageSnapshots
                .CountAsync(x => x.ConnectorConnectionId == context.Connection.Id, context.CancellationToken);
            return new ConnectorSyncResult(existingCount, "No import payload was supplied; existing local email snapshots were left unchanged.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var imported = 0;

        foreach (var item in messagesElement.EnumerateArray())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var subject = TryGetPropertyCaseInsensitive(item, "subject", out var subjectElement)
                ? subjectElement.GetString()?.Trim()
                : null;
            var fromAddress = TryGetPropertyCaseInsensitive(item, "fromAddress", out var fromAddressElement)
                ? fromAddressElement.GetString()?.Trim()
                : null;
            var receivedUtc = TryGetPropertyCaseInsensitive(item, "receivedUtc", out var receivedElement) && receivedElement.TryGetDateTime(out var parsedReceivedUtc)
                ? parsedReceivedUtc
                : (DateTime?)null;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(fromAddress) || receivedUtc is null)
            {
                continue;
            }

            var externalId = TryGetPropertyCaseInsensitive(item, "externalId", out var externalIdElement)
                ? externalIdElement.GetString()?.Trim()
                : null;
            externalId = string.IsNullOrWhiteSpace(externalId)
                ? $"{fromAddress}-{subject}-{receivedUtc:O}"
                : externalId;

            var snapshot = await dbContext.EmailMessageSnapshots
                .FirstOrDefaultAsync(
                    x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                    context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new EmailMessageSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };

                dbContext.EmailMessageSnapshots.Add(snapshot);
            }

            snapshot.Subject = subject;
            snapshot.FromName = TryGetPropertyCaseInsensitive(item, "fromName", out var fromNameElement)
                ? fromNameElement.GetString()?.Trim()
                : null;
            snapshot.FromAddress = fromAddress;
            snapshot.ToAddresses = ReadToAddresses(item);
            snapshot.Preview = TryGetPropertyCaseInsensitive(item, "preview", out var previewElement)
                ? previewElement.GetString()?.Trim()
                : null;
            snapshot.Body = TryGetPropertyCaseInsensitive(item, "body", out var bodyElement)
                ? bodyElement.GetString()?.Trim()
                : null;
            snapshot.ReceivedUtc = receivedUtc.Value.ToUniversalTime();
            snapshot.IsRead = TryGetPropertyCaseInsensitive(item, "isRead", out var readElement) && readElement.ValueKind == JsonValueKind.True;
            snapshot.HasAttachments = TryGetPropertyCaseInsensitive(item, "hasAttachments", out var attachmentsElement) && attachmentsElement.ValueKind == JsonValueKind.True;
            snapshot.IsAnswered = TryGetPropertyCaseInsensitive(item, "isAnswered", out var answeredElement) && answeredElement.ValueKind == JsonValueKind.True;
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Imported or updated {imported} email message snapshot(s).");
    }

    private static string? ReadToAddresses(JsonElement item)
    {
        if (!TryGetPropertyCaseInsensitive(item, "toAddresses", out var toAddressesElement))
        {
            return null;
        }

        if (toAddressesElement.ValueKind == JsonValueKind.Array)
        {
            var values = toAddressesElement
                .EnumerateArray()
                .Select(x => x.GetString()?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return values.Count == 0 ? null : string.Join(", ", values);
        }

        return toAddressesElement.GetString()?.Trim();
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
