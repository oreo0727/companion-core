using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public sealed class CalendarCapability(IConnectorSyncService connectorSyncService) : ICalendarCapability
{
    public Task<IReadOnlyList<CalendarEventSnapshot>> GetUpcomingEventsAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        return connectorSyncService.GetUpcomingCalendarEventsAsync(userProfileId, daysAhead, audit, cancellationToken);
    }

    public async Task<CalendarCapabilitySummary> GetSummaryAsync(
        Guid userProfileId,
        int daysAhead = 7,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var events = await GetUpcomingEventsAsync(userProfileId, daysAhead, audit, cancellationToken);
        var ordered = events.OrderBy(x => x.StartUtc).ThenBy(x => x.EndUtc).ToList();

        return new CalendarCapabilitySummary(
            ordered,
            BuildFreeTimeBlocks(ordered),
            BuildConflictSummaries(ordered),
            ordered
                .Where(x => string.IsNullOrWhiteSpace(x.Location) && !x.IsAllDay)
                .Select(x => x.Title)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static IReadOnlyList<FreeTimeBlock> BuildFreeTimeBlocks(IReadOnlyList<CalendarEventSnapshot> events)
    {
        var blocks = new List<FreeTimeBlock>();
        foreach (var group in events.Where(x => !x.IsAllDay).GroupBy(x => x.StartUtc.Date).OrderBy(x => x.Key))
        {
            var cursor = group.Key.AddHours(8);
            var workEnd = group.Key.AddHours(18);
            foreach (var item in group.OrderBy(x => x.StartUtc))
            {
                if (item.StartUtc > cursor)
                {
                    AddBlock(blocks, cursor, item.StartUtc);
                }

                if (item.EndUtc > cursor)
                {
                    cursor = item.EndUtc;
                }
            }

            if (cursor < workEnd)
            {
                AddBlock(blocks, cursor, workEnd);
            }
        }

        return blocks
            .OrderByDescending(x => x.DurationMinutes)
            .ThenBy(x => x.StartUtc)
            .Take(12)
            .ToList();
    }

    private static void AddBlock(List<FreeTimeBlock> blocks, DateTime startUtc, DateTime endUtc)
    {
        var duration = (int)(endUtc - startUtc).TotalMinutes;
        if (duration >= 30)
        {
            blocks.Add(new FreeTimeBlock(startUtc, endUtc, duration));
        }
    }

    private static IReadOnlyList<string> BuildConflictSummaries(IReadOnlyList<CalendarEventSnapshot> events)
    {
        var conflicts = new List<string>();
        var timedEvents = events.Where(x => !x.IsAllDay).OrderBy(x => x.StartUtc).ToList();
        for (var i = 0; i < timedEvents.Count - 1; i++)
        {
            var current = timedEvents[i];
            var next = timedEvents[i + 1];
            if (current.EndUtc > next.StartUtc)
            {
                conflicts.Add($"{current.Title} overlaps {next.Title}.");
            }
        }

        return conflicts.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
    }
}

public sealed class EmailCapability(
    IConnectorSyncService connectorSyncService,
    CompanionDbContext dbContext,
    IAuditService auditService) : IEmailCapability
{
    public Task<IReadOnlyList<EmailMessageSnapshot>> GetImportantRecentAsync(
        Guid userProfileId,
        int daysBack = 14,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        return connectorSyncService.GetRecentEmailMessagesAsync(userProfileId, daysBack, limit, audit, cancellationToken);
    }

    public Task<IReadOnlyList<EmailMessageSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        return connectorSyncService.SearchEmailMessagesAsync(userProfileId, query, limit, audit, cancellationToken);
    }

    public async Task<EmailMessageSnapshot?> ReadAsync(
        Guid userProfileId,
        Guid messageId,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.EmailMessageSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .FirstOrDefaultAsync(x => x.Id == messageId && x.UserProfileId == userProfileId, cancellationToken);

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.EmailMessagesViewed,
                nameof(EmailMessageSnapshot),
                message?.Id.ToString() ?? string.Empty,
                message is null ? "Attempted to read an email snapshot that was not found." : $"Read email snapshot '{message.Subject}'.",
                cancellationToken);
        }

        return message;
    }

    public async Task<EmailDraftResult> CreateDraftAsync(
        Guid userProfileId,
        EmailDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.EmailDraftRequested,
            "EmailDraft",
            request.To,
            $"Draft email requested for '{request.To}' with subject '{request.Subject}'. Approval is required before provider write.",
            cancellationToken);

        return new EmailDraftResult(true, "Draft creation requires approval before any provider write occurs.");
    }
}

public sealed class FileCapability(
    IConnectorSyncService connectorSyncService,
    CompanionDbContext dbContext,
    IAuditService auditService) : IFileCapability
{
    public Task<IReadOnlyList<FileDocumentSnapshot>> GetRecentAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        return connectorSyncService.GetRecentFileDocumentsAsync(userProfileId, limit, audit, cancellationToken);
    }

    public async Task<IReadOnlyList<FileDocumentSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        var take = Math.Clamp(limit, 1, 100);
        var documents = await dbContext.FileDocumentSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        var results = string.IsNullOrWhiteSpace(normalizedQuery)
            ? documents
            : documents
                .Where(x =>
                    ContainsTerm(x.Name, normalizedQuery) ||
                    ContainsTerm(x.MimeType, normalizedQuery) ||
                    ContainsTerm(x.PreviewText, normalizedQuery))
                .ToList();

        results = results
            .OrderByDescending(x => x.ModifiedUtc ?? x.UpdatedUtc)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.FileSearchPerformed,
                nameof(FileDocumentSnapshot),
                results.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                $"Searched file snapshots for '{normalizedQuery}' and found {results.Count} result(s).",
                cancellationToken);
        }

        return results;
    }

    public async Task<FileDocumentSnapshot?> ReadMetadataAsync(
        Guid userProfileId,
        Guid documentId,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.FileDocumentSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.UserProfileId == userProfileId, cancellationToken);

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.FileDocumentsViewed,
                nameof(FileDocumentSnapshot),
                document?.Id.ToString() ?? string.Empty,
                document is null ? "Attempted to read a file snapshot that was not found." : $"Read file snapshot '{document.Name}'.",
                cancellationToken);
        }

        return document;
    }

    private static bool ContainsTerm(string? text, string term)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PeopleCapability(
    CompanionDbContext dbContext,
    IAuditService auditService) : IPeopleCapability
{
    public async Task<IReadOnlyList<ContactSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        var take = Math.Clamp(limit, 1, 100);
        var contacts = await dbContext.ContactSnapshots
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        var results = string.IsNullOrWhiteSpace(normalizedQuery)
            ? contacts
            : contacts
                .Where(x =>
                    ContainsTerm(x.DisplayName, normalizedQuery) ||
                    ContainsTerm(x.Email, normalizedQuery) ||
                    ContainsTerm(x.Phone, normalizedQuery) ||
                    ContainsTerm(x.Organization, normalizedQuery))
                .ToList();

        results = results
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .Take(take)
            .ToList();

        if (audit)
        {
            await auditService.WriteEventAsync(
                userProfileId,
                string.IsNullOrWhiteSpace(normalizedQuery) ? AuditEventTypes.ContactsViewed : AuditEventTypes.ContactSearchPerformed,
                nameof(ContactSnapshot),
                results.FirstOrDefault()?.Id.ToString() ?? string.Empty,
                string.IsNullOrWhiteSpace(normalizedQuery)
                    ? $"Viewed {results.Count} contact snapshot(s)."
                    : $"Searched contact snapshots for '{normalizedQuery}' and found {results.Count} result(s).",
                cancellationToken);
        }

        return results;
    }

    public Task<IReadOnlyList<ContactSnapshot>> GetRelevantContactsAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(userProfileId, string.Empty, limit, audit, cancellationToken);
    }

    private static bool ContainsTerm(string? text, string term)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
