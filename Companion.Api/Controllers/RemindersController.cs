using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
public class RemindersController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReminderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReminderResponse>>> GetReminders(
        [FromQuery] bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        var reminders = await notificationService.GetRemindersAsync(
            User.GetRequiredUserProfileId(),
            includeCompleted,
            cancellationToken);

        return Ok(reminders.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReminderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReminderResponse>> CreateReminder(
        [FromBody] CreateReminderRequest request,
        CancellationToken cancellationToken)
    {
        var reminder = await notificationService.CreateReminderAsync(
            User.GetRequiredUserProfileId(),
            new CreateReminderCommand(
                request.Title,
                request.Description,
                request.DueUtc,
                string.IsNullOrWhiteSpace(request.SourceType) ? "ManualReminder" : request.SourceType,
                request.SourceId),
            cancellationToken);

        return Created($"/api/reminders/{reminder.Id}", reminder.ToResponse());
    }
}

public sealed class CreateReminderRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public DateTime DueUtc { get; init; }

    [MaxLength(100)]
    public string? SourceType { get; init; }

    [MaxLength(100)]
    public string? SourceId { get; init; }
}
