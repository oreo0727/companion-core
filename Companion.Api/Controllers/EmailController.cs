using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/email")]
[Authorize]
public class EmailController(IConnectorSyncService connectorSyncService) : ControllerBase
{
    [HttpGet("messages")]
    [ProducesResponseType(typeof(IEnumerable<EmailMessageSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmailMessageSnapshotResponse>>> GetRecentMessages(
        [FromQuery] int daysBack = 14,
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var messages = await connectorSyncService.GetRecentEmailMessagesAsync(
            User.GetRequiredUserProfileId(),
            Math.Clamp(daysBack, 1, 90),
            Math.Clamp(limit, 1, 100),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(messages.Select(x => x.ToResponse()));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<EmailMessageSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmailMessageSnapshotResponse>>> SearchMessages(
        [FromQuery] string query = "",
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var messages = await connectorSyncService.SearchEmailMessagesAsync(
            User.GetRequiredUserProfileId(),
            query,
            Math.Clamp(limit, 1, 100),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(messages.Select(x => x.ToResponse()));
    }
}
