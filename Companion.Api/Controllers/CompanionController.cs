using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/companion")]
[Authorize]
public class CompanionController(
    IChiefOfStaffService chiefOfStaffService) : ControllerBase
{
    [HttpGet("briefing")]
    [ProducesResponseType(typeof(CompanionBriefingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionBriefingResponse>> GetBriefing(CancellationToken cancellationToken)
    {
        var briefing = await chiefOfStaffService.GetBriefingAsync(
            User.GetRequiredUserProfileId(),
            cancellationToken);

        return Ok(briefing.ToResponse());
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CompanionDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await chiefOfStaffService.GetDashboardAsync(
            User.GetRequiredUserProfileId(),
            cancellationToken);

        return Ok(dashboard.ToResponse());
    }
}
