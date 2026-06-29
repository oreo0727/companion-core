using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public sealed class GetCalendarEventsTool(ICalendarCapability calendarCapability) : ITool
{
    public string Name => ToolNames.GetCalendarEvents;
    public string Description => "Retrieve upcoming meetings through the calendar capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var daysAhead = ReadInt(context, "daysAhead", 7, 1, 30);
        var events = await calendarCapability.GetUpcomingEventsAsync(context.UserProfileId, daysAhead, audit: true, context.CancellationToken);
        var output = events.Select(x => new
        {
            id = x.Id,
            x.Title,
            x.Description,
            x.Location,
            x.StartUtc,
            x.EndUtc,
            x.IsAllDay,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} upcoming calendar event(s).");
    }

    private static int ReadInt(ToolExecutionContext context, string name, int fallback, int min, int max)
    {
        return context.Input.TryGetProperty(name, out var element) && element.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, min, max)
            : fallback;
    }
}

public sealed class FindFreeTimeTool(ICalendarCapability calendarCapability) : ITool
{
    public string Name => ToolNames.FindFreeTime;
    public string Description => "Find open calendar focus blocks and conflicts through the calendar capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var daysAhead = context.Input.TryGetProperty("daysAhead", out var daysAheadElement) && daysAheadElement.TryGetInt32(out var parsedDaysAhead)
            ? Math.Clamp(parsedDaysAhead, 1, 30)
            : 7;
        var summary = await calendarCapability.GetSummaryAsync(context.UserProfileId, daysAhead, audit: true, context.CancellationToken);

        var output = new
        {
            freeTime = summary.FreeTime,
            conflicts = summary.Conflicts,
            missingLocationEvents = summary.MissingLocationEvents,
            eventCount = summary.Events.Count
        };

        return new ToolExecutionResult(output, $"Found {summary.FreeTime.Count} free block(s), {summary.Conflicts.Count} conflict(s), and {summary.MissingLocationEvents.Count} event(s) missing location.");
    }
}

public sealed class SearchEmailTool(IEmailCapability emailCapability) : ITool
{
    public string Name => ToolNames.SearchEmail;
    public string Description => "Search read-only email snapshots through the email capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = ReadString(context, "query");
        var limit = ReadInt(context, "limit", 10, 1, 100);
        var messages = string.IsNullOrWhiteSpace(query)
            ? await emailCapability.GetImportantRecentAsync(context.UserProfileId, 14, limit, audit: true, context.CancellationToken)
            : await emailCapability.SearchAsync(context.UserProfileId, query, limit, audit: true, context.CancellationToken);

        var output = messages.Select(x => new
        {
            id = x.Id,
            x.Subject,
            x.FromName,
            x.FromAddress,
            x.Preview,
            x.ReceivedUtc,
            x.IsRead,
            x.HasAttachments,
            x.IsAnswered,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} email message snapshot(s).");
    }

    private static string ReadString(ToolExecutionContext context, string name)
    {
        return context.Input.TryGetProperty(name, out var element) ? element.GetString()?.Trim() ?? string.Empty : string.Empty;
    }

    private static int ReadInt(ToolExecutionContext context, string name, int fallback, int min, int max)
    {
        return context.Input.TryGetProperty(name, out var element) && element.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, min, max)
            : fallback;
    }
}

public sealed class ReadEmailTool(IEmailCapability emailCapability) : ITool
{
    public string Name => ToolNames.ReadEmail;
    public string Description => "Read a single email snapshot through the email capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var id = ReadGuid(context, "id");
        var message = await emailCapability.ReadAsync(context.UserProfileId, id, audit: true, context.CancellationToken)
            ?? throw new KeyNotFoundException($"Email message '{id}' was not found.");

        return new ToolExecutionResult(new
        {
            id = message.Id,
            message.Subject,
            message.FromName,
            message.FromAddress,
            message.ToAddresses,
            message.Preview,
            message.Body,
            message.ReceivedUtc,
            message.IsRead,
            message.HasAttachments,
            message.IsAnswered,
            connector = message.ConnectorConnection?.DisplayName
        }, $"Read email snapshot '{message.Subject}'.");
    }

    private static Guid ReadGuid(ToolExecutionContext context, string name)
    {
        var value = context.Input.TryGetProperty(name, out var element) ? element.GetString() : null;
        return Guid.TryParse(value, out var id) ? id : throw new InvalidOperationException($"'{name}' must be a valid id.");
    }
}

public sealed class CreateDraftEmailTool(IEmailCapability emailCapability) : ITool
{
    public string Name => ToolNames.CreateDraftEmail;
    public string Description => "Create a draft email after approval; sending is not supported.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Medium;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var request = new EmailDraftRequest(
            ReadRequiredString(context, "to"),
            ReadRequiredString(context, "subject"),
            ReadRequiredString(context, "body"));
        var result = await emailCapability.CreateDraftAsync(context.UserProfileId, request, context.CancellationToken);

        return new ToolExecutionResult(result, result.Summary);
    }

    private static string ReadRequiredString(ToolExecutionContext context, string name)
    {
        var value = context.Input.TryGetProperty(name, out var element) ? element.GetString()?.Trim() : null;
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"'{name}' is required.") : value;
    }
}

public sealed class SearchDriveTool(IFileCapability fileCapability) : ITool
{
    public string Name => ToolNames.SearchDrive;
    public string Description => "Search file snapshots through the file capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = context.Input.TryGetProperty("query", out var queryElement) ? queryElement.GetString()?.Trim() ?? string.Empty : string.Empty;
        var limit = context.Input.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var parsedLimit)
            ? Math.Clamp(parsedLimit, 1, 100)
            : 10;
        var documents = await fileCapability.SearchAsync(context.UserProfileId, query, limit, audit: true, context.CancellationToken);

        var output = documents.Select(x => new
        {
            id = x.Id,
            x.Name,
            x.MimeType,
            x.WebUrl,
            x.PreviewText,
            x.ModifiedUtc,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} file document snapshot(s).");
    }
}

public sealed class ReadDocumentTool(IFileCapability fileCapability) : ITool
{
    public string Name => ToolNames.ReadDocument;
    public string Description => "Read file metadata and preview text through the file capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var value = context.Input.TryGetProperty("id", out var element) ? element.GetString() : null;
        var id = Guid.TryParse(value, out var parsed) ? parsed : throw new InvalidOperationException("'id' must be a valid document id.");
        var document = await fileCapability.ReadMetadataAsync(context.UserProfileId, id, audit: true, context.CancellationToken)
            ?? throw new KeyNotFoundException($"Document '{id}' was not found.");

        return new ToolExecutionResult(new
        {
            id = document.Id,
            document.Name,
            document.MimeType,
            document.WebUrl,
            document.PreviewText,
            document.ModifiedUtc,
            connector = document.ConnectorConnection?.DisplayName
        }, $"Read document snapshot '{document.Name}'.");
    }
}

public sealed class FindContactTool(IPeopleCapability peopleCapability) : ITool
{
    public string Name => ToolNames.FindContact;
    public string Description => "Find contacts through the people capability.";
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = context.Input.TryGetProperty("query", out var queryElement) ? queryElement.GetString()?.Trim() ?? string.Empty : string.Empty;
        var limit = context.Input.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var parsedLimit)
            ? Math.Clamp(parsedLimit, 1, 100)
            : 10;
        var contacts = await peopleCapability.SearchAsync(context.UserProfileId, query, limit, audit: true, context.CancellationToken);

        var output = contacts.Select(x => new
        {
            id = x.Id,
            x.DisplayName,
            x.Email,
            x.Phone,
            x.Organization,
            x.BirthdayUtc,
            x.PhotoUrl,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} contact snapshot(s).");
    }
}
