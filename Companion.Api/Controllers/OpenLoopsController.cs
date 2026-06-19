using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/open-loops")]
public class OpenLoopsController(IOpenLoopService openLoopService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OpenLoopResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OpenLoopResponse>>> GetOpenLoops(CancellationToken cancellationToken)
    {
        var openLoops = await openLoopService.GetOpenLoopsAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(openLoops.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OpenLoopResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OpenLoopResponse>> CreateOpenLoop(
        [FromBody] CreateOpenLoopRequest request,
        CancellationToken cancellationToken)
    {
        var openLoop = await openLoopService.CreateOpenLoopAsync(
            CompanionDefaults.LocalUserProfileId,
            new CreateOpenLoopCommand(
                request.Title,
                request.Description,
                request.Status ?? OpenLoopStatus.Open),
            cancellationToken);

        return Created($"/api/open-loops/{openLoop.Id}", openLoop.ToResponse());
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(OpenLoopResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpenLoopResponse>> CloseOpenLoop(Guid id, CancellationToken cancellationToken)
    {
        var openLoop = await openLoopService.CloseOpenLoopAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            cancellationToken);

        return openLoop is null ? NotFound() : Ok(openLoop.ToResponse());
    }
}

public sealed class CreateOpenLoopRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public OpenLoopStatus? Status { get; init; }
}
