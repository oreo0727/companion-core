using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications(
        [FromQuery] bool includeRead = false,
        CancellationToken cancellationToken = default)
    {
        var notifications = await notificationService.GetNotificationsAsync(
            User.GetRequiredUserProfileId(),
            includeRead,
            cancellationToken);

        return Ok(notifications.Select(x => x.ToResponse()));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await notificationService.MarkReadAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return notification is null ? NotFound() : Ok(notification.ToResponse());
    }
}
