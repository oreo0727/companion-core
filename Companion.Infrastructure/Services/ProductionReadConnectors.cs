using System.Net.Http.Headers;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public abstract class OAuthReadConnectorBase(
    HttpClient httpClient,
    CompanionDbContext dbContext,
    IOAuthTokenProtector tokenProtector,
    TimeProvider timeProvider) : IConnector
{
    public abstract string Name { get; }

    public abstract string Provider { get; }

    public ConnectorRiskLevel RiskLevel => ConnectorRiskLevel.Low;

    protected abstract string Endpoint { get; }

    public Task<ConnectorTestResult> TestConnectionAsync(ConnectorSyncContext context)
    {
        if (context.Payload is not null)
        {
            return Task.FromResult(new ConnectorTestResult(true, null));
        }

        return Task.FromResult(
            tokenProtector.Unprotect(context.Connection.AccessTokenEncrypted) is null
                ? new ConnectorTestResult(false, "OAuth access token is missing or could not be decrypted.")
                : new ConnectorTestResult(true, null));
    }

    public virtual async Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context)
    {
        using var document = context.Payload is null
            ? await FetchProviderDocumentAsync(context)
            : JsonDocument.Parse(context.Payload.Value.GetRawText());

        return await SyncDocumentAsync(context, document.RootElement);
    }

    protected abstract Task<ConnectorSyncResult> SyncDocumentAsync(ConnectorSyncContext context, JsonElement root);

    protected DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    protected CompanionDbContext DbContext => dbContext;

    protected async Task<JsonDocument> FetchProviderDocumentAsync(ConnectorSyncContext context)
    {
        return await FetchProviderDocumentAsync(context, Endpoint);
    }

    protected async Task<JsonDocument> FetchProviderDocumentAsync(ConnectorSyncContext context, string endpoint)
    {
        var accessToken = tokenProtector.Unprotect(context.Connection.AccessTokenEncrypted)
            ?? throw new InvalidOperationException("OAuth access token is missing or could not be decrypted.");

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, context.CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(context.CancellationToken);
            throw new HttpRequestException(
                $"Provider request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {TrimProviderError(responseText)}",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(context.CancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: context.CancellationToken);
    }

    protected static IEnumerable<JsonElement> ReadArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyCaseInsensitive(root, name, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray();
            }
        }

        return root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : [];
    }

    protected static string? ReadString(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (TryReadPath(element, path, out var value))
            {
                var text = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        return null;
    }

    protected static bool ReadBool(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryReadPath(element, path, out var value))
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    protected static DateTime? ReadUtc(JsonElement element, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryReadPath(element, path, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String && value.TryGetDateTime(out var parsed))
            {
                return parsed.ToUniversalTime();
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixMs))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
            }
        }

        return null;
    }

    private static bool TryReadPath(JsonElement element, string path, out JsonElement value)
    {
        value = element;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (value.ValueKind == JsonValueKind.Array && int.TryParse(part, out var index))
            {
                if (index < 0 || index >= value.GetArrayLength())
                {
                    value = default;
                    return false;
                }

                value = value[index];
                continue;
            }

            if (!TryGetPropertyCaseInsensitive(value, part, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string TrimProviderError(string value)
    {
        var compact = value.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 500 ? compact : string.Concat(compact.AsSpan(0, 500), "...");
    }
}

public abstract class CalendarOAuthReadConnector(
    HttpClient httpClient,
    CompanionDbContext dbContext,
    IOAuthTokenProtector tokenProtector,
    TimeProvider timeProvider) : OAuthReadConnectorBase(httpClient, dbContext, tokenProtector, timeProvider), ICalendarReadConnector
{
    protected override async Task<ConnectorSyncResult> SyncDocumentAsync(ConnectorSyncContext context, JsonElement root)
    {
        var now = UtcNow;
        var imported = 0;

        foreach (var item in ReadArray(root, "events", "items", "value"))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var title = ReadString(item, "title", "summary", "subject");
            var startUtc = ReadUtc(item, "startUtc", "start.dateTime", "start.date", "start");
            var endUtc = ReadUtc(item, "endUtc", "end.dateTime", "end.date", "end");
            if (string.IsNullOrWhiteSpace(title) || startUtc is null || endUtc is null || endUtc < startUtc)
            {
                continue;
            }

            var externalId = ReadString(item, "externalId", "id", "iCalUId") ?? $"{title}-{startUtc:O}";
            var snapshot = await DbContext.CalendarEventSnapshots.FirstOrDefaultAsync(
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
                DbContext.CalendarEventSnapshots.Add(snapshot);
            }

            snapshot.Title = title;
            snapshot.Description = ReadString(item, "description", "bodyPreview", "body.content");
            snapshot.Location = ReadString(item, "location", "location.displayName");
            snapshot.StartUtc = startUtc.Value;
            snapshot.EndUtc = endUtc.Value;
            snapshot.IsAllDay = ReadBool(item, "isAllDay");
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await DbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Synced {imported} calendar event snapshot(s).");
    }
}

public abstract class EmailOAuthReadConnector(
    HttpClient httpClient,
    CompanionDbContext dbContext,
    IOAuthTokenProtector tokenProtector,
    TimeProvider timeProvider) : OAuthReadConnectorBase(httpClient, dbContext, tokenProtector, timeProvider), IEmailReadConnector
{
    protected override async Task<ConnectorSyncResult> SyncDocumentAsync(ConnectorSyncContext context, JsonElement root)
    {
        var now = UtcNow;
        var imported = 0;

        foreach (var item in ReadArray(root, "messages", "items", "value"))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var subject = ReadString(item, "subject", "snippet");
            var fromAddress = ReadString(item, "fromAddress", "from.emailAddress.address", "sender.emailAddress.address", "from");
            var receivedUtc = ReadUtc(item, "receivedUtc", "receivedDateTime", "internalDate", "date");
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(fromAddress) || receivedUtc is null)
            {
                continue;
            }

            var externalId = ReadString(item, "externalId", "id", "internetMessageId") ?? $"{fromAddress}-{subject}-{receivedUtc:O}";
            var snapshot = await DbContext.EmailMessageSnapshots.FirstOrDefaultAsync(
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
                DbContext.EmailMessageSnapshots.Add(snapshot);
            }

            snapshot.Subject = subject;
            snapshot.FromName = ReadString(item, "fromName", "from.emailAddress.name", "sender.emailAddress.name");
            snapshot.FromAddress = fromAddress;
            snapshot.ToAddresses = ReadString(item, "toAddresses", "toRecipients");
            snapshot.Preview = ReadString(item, "preview", "snippet", "bodyPreview");
            snapshot.Body = ReadString(item, "body", "body.content");
            snapshot.ReceivedUtc = receivedUtc.Value;
            snapshot.IsRead = ReadBool(item, "isRead");
            snapshot.HasAttachments = ReadBool(item, "hasAttachments");
            snapshot.IsAnswered = ReadBool(item, "isAnswered");
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await DbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Synced {imported} email message snapshot(s).");
    }
}

public abstract class FileOAuthReadConnector(
    HttpClient httpClient,
    CompanionDbContext dbContext,
    IOAuthTokenProtector tokenProtector,
    TimeProvider timeProvider) : OAuthReadConnectorBase(httpClient, dbContext, tokenProtector, timeProvider), IFileReadConnector
{
    protected override async Task<ConnectorSyncResult> SyncDocumentAsync(ConnectorSyncContext context, JsonElement root)
    {
        var now = UtcNow;
        var imported = 0;

        foreach (var item in ReadArray(root, "files", "items", "value"))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var name = ReadString(item, "name", "title");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var externalId = ReadString(item, "externalId", "id") ?? name;
            var snapshot = await DbContext.FileDocumentSnapshots.FirstOrDefaultAsync(
                x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new FileDocumentSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };
                DbContext.FileDocumentSnapshots.Add(snapshot);
            }

            snapshot.Name = name;
            snapshot.MimeType = ReadString(item, "mimeType", "file.mimeType");
            snapshot.WebUrl = ReadString(item, "webUrl", "webViewLink");
            snapshot.PreviewText = ReadString(item, "previewText", "description");
            snapshot.ModifiedUtc = ReadUtc(item, "modifiedUtc", "modifiedTime", "lastModifiedDateTime");
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await DbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Synced {imported} file document snapshot(s).");
    }
}

public abstract class PeopleOAuthReadConnector(
    HttpClient httpClient,
    CompanionDbContext dbContext,
    IOAuthTokenProtector tokenProtector,
    TimeProvider timeProvider) : OAuthReadConnectorBase(httpClient, dbContext, tokenProtector, timeProvider)
{
    protected override async Task<ConnectorSyncResult> SyncDocumentAsync(ConnectorSyncContext context, JsonElement root)
    {
        var now = UtcNow;
        var imported = 0;

        foreach (var item in ReadArray(root, "contacts", "connections", "people"))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var displayName = ReadString(item, "displayName", "names.0.displayName", "names.0.unstructuredName", "name");
            var email = ReadString(item, "email", "emailAddresses.0.value");
            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var externalId = ReadString(item, "externalId", "resourceName", "id") ?? email ?? displayName!;
            var snapshot = await DbContext.ContactSnapshots.FirstOrDefaultAsync(
                x => x.ConnectorConnectionId == context.Connection.Id && x.ExternalId == externalId,
                context.CancellationToken);

            if (snapshot is null)
            {
                snapshot = new ContactSnapshot
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = context.UserProfileId,
                    ConnectorConnectionId = context.Connection.Id,
                    ExternalId = externalId,
                    CreatedUtc = now
                };
                DbContext.ContactSnapshots.Add(snapshot);
            }

            snapshot.DisplayName = displayName ?? email ?? "Unnamed contact";
            snapshot.Email = email;
            snapshot.Phone = ReadString(item, "phone", "phoneNumbers.0.value");
            snapshot.Organization = ReadString(item, "organization", "organizations.0.name", "organizations.0.title");
            snapshot.BirthdayUtc = ReadUtc(item, "birthdayUtc", "birthdays.0.date");
            snapshot.PhotoUrl = ReadString(item, "photoUrl", "photos.0.url");
            snapshot.UpdatedUtc = now;
            imported++;
        }

        await DbContext.SaveChangesAsync(context.CancellationToken);
        return new ConnectorSyncResult(imported, $"Synced {imported} contact snapshot(s).");
    }
}

public sealed class GoogleCalendarReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : CalendarOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Google Calendar";
    public override string Provider => ConnectorProviders.GoogleCalendar;
    protected override string Endpoint => "https://www.googleapis.com/calendar/v3/calendars/primary/events?singleEvents=true&orderBy=startTime";
}

public sealed class GmailReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : EmailOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Gmail";
    public override string Provider => ConnectorProviders.Gmail;
    protected override string Endpoint => "https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=25&q=newer_than:30d";

    public override async Task<ConnectorSyncResult> SyncAsync(ConnectorSyncContext context)
    {
        if (context.Payload is not null)
        {
            return await base.SyncAsync(context);
        }

        using var listDocument = await FetchProviderDocumentAsync(context);
        var messages = new List<JsonElement>();

        foreach (var item in ReadArray(listDocument.RootElement, "messages"))
        {
            var id = ReadString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var detailUrl =
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{Uri.EscapeDataString(id)}?format=metadata&metadataHeaders=Subject&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Date";
            using var detailDocument = await FetchProviderDocumentAsync(context, detailUrl);
            messages.Add(ToEmailSnapshotJson(detailDocument.RootElement).RootElement.Clone());
        }

        using var normalizedDocument = JsonDocument.Parse(JsonSerializer.Serialize(new { messages }));
        return await SyncDocumentAsync(context, normalizedDocument.RootElement);
    }

    private static JsonDocument ToEmailSnapshotJson(JsonElement message)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (message.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("headers", out var headerArray) &&
            headerArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var header in headerArray.EnumerateArray())
            {
                var name = ReadString(header, "name");
                var value = ReadString(header, "value");
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                {
                    headers[name] = value;
                }
            }
        }

        var internalDate = ReadString(message, "internalDate");
        DateTime? receivedUtc = null;
        if (!string.IsNullOrWhiteSpace(internalDate) && long.TryParse(internalDate, out var unixMs))
        {
            receivedUtc = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
        }

        var from = headers.GetValueOrDefault("From") ?? string.Empty;
        var parsedFrom = ParseAddress(from);
        var labelIds = message.TryGetProperty("labelIds", out var labels) && labels.ValueKind == JsonValueKind.Array
            ? labels.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        return JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            externalId = ReadString(message, "id"),
            subject = headers.GetValueOrDefault("Subject") ?? "(No subject)",
            fromName = parsedFrom.Name,
            fromAddress = parsedFrom.Address,
            toAddresses = headers.GetValueOrDefault("To"),
            preview = ReadString(message, "snippet"),
            receivedUtc,
            isRead = !labelIds.Contains("UNREAD"),
            hasAttachments = false,
            isAnswered = labelIds.Contains("SENT")
        }));
    }

    private static (string? Name, string Address) ParseAddress(string value)
    {
        var trimmed = value.Trim();
        var start = trimmed.LastIndexOf('<');
        var end = trimmed.LastIndexOf('>');
        if (start >= 0 && end > start)
        {
            var name = trimmed[..start].Trim().Trim('"');
            var address = trimmed[(start + 1)..end].Trim();
            return (string.IsNullOrWhiteSpace(name) ? null : name, address);
        }

        return (null, trimmed);
    }
}

public sealed class GoogleDriveReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : FileOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Google Drive";
    public override string Provider => ConnectorProviders.GoogleDrive;
    protected override string Endpoint => "https://www.googleapis.com/drive/v3/files?fields=files(id,name,mimeType,webViewLink,modifiedTime,description)";
}

public sealed class GooglePeopleReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : PeopleOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Google People";
    public override string Provider => ConnectorProviders.GooglePeople;
    protected override string Endpoint => "https://people.googleapis.com/v1/people/me/connections?personFields=names,emailAddresses,phoneNumbers,organizations,birthdays,photos";
}

public sealed class MicrosoftCalendarReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : CalendarOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Microsoft Calendar";
    public override string Provider => ConnectorProviders.MicrosoftCalendar;
    protected override string Endpoint => "https://graph.microsoft.com/v1.0/me/events";
}

public sealed class OutlookMailReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : EmailOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "Outlook Mail";
    public override string Provider => ConnectorProviders.OutlookMail;
    protected override string Endpoint => "https://graph.microsoft.com/v1.0/me/messages";
}

public sealed class OneDriveReadConnector(HttpClient httpClient, CompanionDbContext dbContext, IOAuthTokenProtector tokenProtector, TimeProvider timeProvider)
    : FileOAuthReadConnector(httpClient, dbContext, tokenProtector, timeProvider)
{
    public override string Name => "OneDrive";
    public override string Provider => ConnectorProviders.OneDrive;
    protected override string Endpoint => "https://graph.microsoft.com/v1.0/me/drive/root/children";
}
