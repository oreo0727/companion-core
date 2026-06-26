using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class NotificationService(
    CompanionDbContext dbContext,
    IAuditService auditService,
    TimeProvider timeProvider) : INotificationService
{
    private const string ManualReminder = "ManualReminder";
    private const string TaskDue = "TaskDue";
    private const string ApprovalPending = "ApprovalPending";
    private const string CalendarEvent = "CalendarEvent";

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(
        Guid userProfileId,
        bool includeRead = false,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId &&
                        (includeRead || x.Status == NotificationStatus.Unread))
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> MarkReadAsync(
        Guid userProfileId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == notificationId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (notification is null)
        {
            return null;
        }

        if (notification.Status != NotificationStatus.Read)
        {
            notification.Status = NotificationStatus.Read;
            notification.ReadUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.NotificationRead,
                nameof(Notification),
                notification.Id.ToString(),
                $"Read notification '{notification.Title}'.",
                cancellationToken);
        }

        return notification;
    }

    public async Task<Reminder> CreateReminderAsync(
        Guid userProfileId,
        CreateReminderCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            DueUtc = command.DueUtc.ToUniversalTime(),
            Status = ReminderStatus.Scheduled,
            SourceType = string.IsNullOrWhiteSpace(command.SourceType) ? ManualReminder : command.SourceType.Trim(),
            SourceId = string.IsNullOrWhiteSpace(command.SourceId) ? null : command.SourceId.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        dbContext.Reminders.Add(reminder);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.ReminderCreated,
            nameof(Reminder),
            reminder.Id.ToString(),
            $"Created reminder '{reminder.Title}' for {reminder.DueUtc:u}.",
            cancellationToken);

        return reminder;
    }

    public async Task<IReadOnlyList<Reminder>> GetRemindersAsync(
        Guid userProfileId,
        bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Reminders
            .AsNoTracking()
            .Include(x => x.Notification)
            .Where(x => x.UserProfileId == userProfileId &&
                        (includeCompleted || x.Status == ReminderStatus.Scheduled))
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetUpcomingRemindersAsync(
        Guid userProfileId,
        int daysAhead = 7,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var horizon = now.AddDays(Math.Clamp(daysAhead, 1, 30));

        return await dbContext.Reminders
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status == ReminderStatus.Scheduled &&
                x.DueUtc <= horizon)
            .OrderBy(x => x.DueUtc)
            .Take(10)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await EnsureTaskDueRemindersAsync(now, cancellationToken);
        await EnsureApprovalPendingRemindersAsync(now, cancellationToken);
        await EnsureCalendarEventRemindersAsync(now, cancellationToken);

        var dueReminders = await dbContext.Reminders
            .Where(x => x.Status == ReminderStatus.Scheduled && x.DueUtc <= now)
            .OrderBy(x => x.DueUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var reminder in dueReminders)
        {
            var notification = await CreateNotificationAsync(
                reminder.UserProfileId,
                reminder.SourceType,
                reminder.Title,
                reminder.Description ?? reminder.Title,
                ResolveSeverity(reminder.SourceType),
                reminder.SourceType,
                reminder.SourceId ?? reminder.Id.ToString(),
                cancellationToken);

            reminder.NotificationId = notification.Id;
            reminder.Status = ReminderStatus.Completed;
            reminder.CompletedUtc = now;
            reminder.UpdatedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return dueReminders.Count;
    }

    private async Task EnsureTaskDueRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var tasks = await dbContext.TaskItems
            .AsNoTracking()
            .Where(x =>
                x.DueDateUtc != null &&
                x.DueDateUtc <= now.AddDays(1) &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var leadMinutes = await GetLeadTimeMinutesAsync(task.UserProfileId, TaskDue, 1440, cancellationToken);
            var dueUtc = task.DueDateUtc!.Value.ToUniversalTime().AddMinutes(-leadMinutes);
            await EnsureSourceReminderAsync(
                task.UserProfileId,
                TaskDue,
                task.Id.ToString(),
                $"Task due: {task.Title}",
                task.DueDateUtc <= now
                    ? $"Task '{task.Title}' is overdue."
                    : $"Task '{task.Title}' is due by {task.DueDateUtc:u}.",
                dueUtc,
                cancellationToken);
        }
    }

    private async Task EnsureApprovalPendingRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var approvals = await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.UserProfileId != null && x.Status == ApprovalRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var approval in approvals)
        {
            var userProfileId = approval.UserProfileId!.Value;
            var leadMinutes = await GetLeadTimeMinutesAsync(userProfileId, ApprovalPending, 0, cancellationToken);
            await EnsureSourceReminderAsync(
                userProfileId,
                ApprovalPending,
                approval.Id.ToString(),
                $"Approval pending: {approval.Type}",
                approval.Reason,
                approval.CreatedUtc.AddMinutes(Math.Max(leadMinutes, 0)),
                cancellationToken);
        }
    }

    private async Task EnsureCalendarEventRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var events = await dbContext.CalendarEventSnapshots
            .AsNoTracking()
            .Where(x => x.StartUtc >= now && x.StartUtc <= now.AddDays(1))
            .ToListAsync(cancellationToken);

        foreach (var calendarEvent in events)
        {
            var leadMinutes = await GetLeadTimeMinutesAsync(calendarEvent.UserProfileId, CalendarEvent, 60, cancellationToken);
            await EnsureSourceReminderAsync(
                calendarEvent.UserProfileId,
                CalendarEvent,
                calendarEvent.Id.ToString(),
                $"Calendar event soon: {calendarEvent.Title}",
                string.IsNullOrWhiteSpace(calendarEvent.Location)
                    ? $"Calendar event '{calendarEvent.Title}' starts at {calendarEvent.StartUtc:u}."
                    : $"Calendar event '{calendarEvent.Title}' starts at {calendarEvent.StartUtc:u} in {calendarEvent.Location}.",
                calendarEvent.StartUtc.AddMinutes(-leadMinutes),
                cancellationToken);
        }
    }

    private async Task EnsureSourceReminderAsync(
        Guid userProfileId,
        string sourceType,
        string sourceId,
        string title,
        string description,
        DateTime dueUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Reminders
            .AnyAsync(
                x => x.UserProfileId == userProfileId &&
                     x.SourceType == sourceType &&
                     x.SourceId == sourceId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.Reminders.Add(new Reminder
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = title,
            Description = description,
            DueUtc = dueUtc,
            Status = ReminderStatus.Scheduled,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedUtc = now,
            UpdatedUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Notification> CreateNotificationAsync(
        Guid userProfileId,
        string type,
        string title,
        string body,
        NotificationSeverity severity,
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Notifications
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.Type == type &&
                     x.EntityType == entityType &&
                     x.EntityId == entityId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Type = type,
            Title = title,
            Body = body,
            Severity = severity,
            Status = NotificationStatus.Unread,
            EntityType = entityType,
            EntityId = entityId,
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    private async Task<int> GetLeadTimeMinutesAsync(
        Guid userProfileId,
        string preferenceType,
        int fallback,
        CancellationToken cancellationToken)
    {
        var preference = await dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.PreferenceType == preferenceType &&
                     x.InAppEnabled,
                cancellationToken);

        return preference?.LeadTimeMinutes ?? fallback;
    }

    private static NotificationSeverity ResolveSeverity(string sourceType)
    {
        return sourceType switch
        {
            TaskDue => NotificationSeverity.Warning,
            ApprovalPending => NotificationSeverity.Warning,
            CalendarEvent => NotificationSeverity.Info,
            _ => NotificationSeverity.Info
        };
    }
}
