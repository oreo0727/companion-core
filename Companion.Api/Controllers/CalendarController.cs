using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController(IConnectorSyncService connectorSyncService) : ControllerBase
{
    [HttpGet("events")]
    [ProducesResponseType(typeof(IEnumerable<CalendarEventSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CalendarEventSnapshotResponse>>> GetUpcomingEvents(
        [FromQuery] int daysAhead = 7,
        CancellationToken cancellationToken = default)
    {
        var events = await connectorSyncService.GetUpcomingCalendarEventsAsync(
            User.GetRequiredUserProfileId(),
            Math.Clamp(daysAhead, 1, 30),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(events.Select(x => x.ToResponse()));
    }
}
