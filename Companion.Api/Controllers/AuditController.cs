using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditEventResponse>>> GetAuditEvents(CancellationToken cancellationToken)
    {
        var events = await auditService.GetEventsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(events.Select(x => x.ToResponse()));
    }
}
