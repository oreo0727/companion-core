using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(
        Guid userProfileId,
        bool includeRead = false,
        CancellationToken cancellationToken = default);

    Task<Notification?> MarkReadAsync(
        Guid userProfileId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<Reminder> CreateReminderAsync(
        Guid userProfileId,
        CreateReminderCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> GetRemindersAsync(
        Guid userProfileId,
        bool includeCompleted = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> GetUpcomingRemindersAsync(
        Guid userProfileId,
        int daysAhead = 7,
        CancellationToken cancellationToken = default);

    Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
