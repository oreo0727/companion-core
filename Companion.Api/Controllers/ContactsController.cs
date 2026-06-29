using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
public class ContactsController(IPeopleCapability peopleCapability) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContactSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContactSnapshotResponse>>> GetContacts(
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var contacts = await peopleCapability.GetRelevantContactsAsync(
            User.GetRequiredUserProfileId(),
            Math.Clamp(limit, 1, 100),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(contacts.Select(x => x.ToResponse()));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ContactSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContactSnapshotResponse>>> SearchContacts(
        [FromQuery] string query = "",
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var contacts = await peopleCapability.SearchAsync(
            User.GetRequiredUserProfileId(),
            query,
            Math.Clamp(limit, 1, 100),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(contacts.Select(x => x.ToResponse()));
    }
}
