using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/os")]
[Authorize]
public class OperatingSystemController(ICompanionOperatingSystemService operatingSystemService) : ControllerBase
{
    [HttpGet("runs")]
    [ProducesResponseType(typeof(IEnumerable<OperatingSystemRunResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OperatingSystemRunResponse>>> GetRuns(CancellationToken cancellationToken)
    {
        var runs = await operatingSystemService.GetRunsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(runs.Select(x => x.ToResponse()));
    }

    [HttpPost("routines/{routineType}/generate")]
    [ProducesResponseType(typeof(OperatingSystemRunResultResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperatingSystemRunResultResponse>> GenerateRoutine(
        string routineType,
        [FromBody] GenerateOperatingSystemRunRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await operatingSystemService.GenerateRunAsync(
            User.GetRequiredUserProfileId(),
            new GenerateOperatingSystemRunCommand(
                string.IsNullOrWhiteSpace(routineType) ? OperatingSystemRoutineTypes.DailyBriefing : routineType,
                request?.PeriodStartUtc,
                request?.PeriodEndUtc),
            cancellationToken);

        return Created($"/api/os/runs/{result.Run.Id}", result.ToResponse());
    }

    [HttpPost("context/optimize")]
    [ProducesResponseType(typeof(OperatingSystemRunResultResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperatingSystemRunResultResponse>> OptimizeContext(CancellationToken cancellationToken)
    {
        var result = await operatingSystemService.OptimizeContextAsync(
            User.GetRequiredUserProfileId(),
            cancellationToken);

        return Created($"/api/os/runs/{result.Run.Id}", result.ToResponse());
    }
}

public sealed class GenerateOperatingSystemRunRequest
{
    public DateTime? PeriodStartUtc { get; init; }

    public DateTime? PeriodEndUtc { get; init; }
}
