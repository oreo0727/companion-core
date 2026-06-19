using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/companion")]
public class CompanionController(
    IChiefOfStaffService chiefOfStaffService) : ControllerBase
{
    [HttpGet("briefing")]
    [ProducesResponseType(typeof(CompanionBriefingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionBriefingResponse>> GetBriefing(CancellationToken cancellationToken)
    {
        var briefing = await chiefOfStaffService.GetBriefingAsync(
            CompanionDefaults.LocalUserProfileId,
            cancellationToken);

        return Ok(briefing.ToResponse());
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CompanionDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await chiefOfStaffService.GetDashboardAsync(
            CompanionDefaults.LocalUserProfileId,
            cancellationToken);

        return Ok(dashboard.ToResponse());
    }
}
