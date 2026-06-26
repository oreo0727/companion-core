using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/connectors")]
[Authorize]
public class ConnectorsController(IConnectorSyncService connectorSyncService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConnectorCatalogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConnectorCatalogEntryResponse>>> GetConnectors(CancellationToken cancellationToken)
    {
        var catalog = await connectorSyncService.GetCatalogAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(catalog.Select(x => x.ToResponse()));
    }

    [HttpPost("local-calendar/import")]
    [ProducesResponseType(typeof(LocalCalendarImportResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LocalCalendarImportResponse>> ImportLocalCalendar(
        [FromBody] LocalCalendarImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await connectorSyncService.ImportLocalCalendarAsync(
            User.GetRequiredUserProfileId(),
            new LocalCalendarImportCommand(
                request.DisplayName,
                request.Events.Select(x => new LocalCalendarImportEvent(
                    x.ExternalId,
                    x.Title,
                    x.Description,
                    x.Location,
                    x.StartUtc,
                    x.EndUtc,
                    x.IsAllDay)).ToList()),
            cancellationToken);

        return Created($"/api/connectors/{result.Connection.Id}", result.ToResponse());
    }

    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(typeof(ConnectorSyncRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConnectorSyncRunResponse>> SyncConnector(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var syncRun = await connectorSyncService.SyncAsync(User.GetRequiredUserProfileId(), id, cancellationToken);
            return Ok(syncRun.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class LocalCalendarImportRequest
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<LocalCalendarImportEventRequest> Events { get; init; } = [];
}

public sealed class LocalCalendarImportEventRequest
{
    [MaxLength(300)]
    public string? ExternalId { get; init; }

    [Required]
    [MaxLength(300)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    [MaxLength(500)]
    public string? Location { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public bool IsAllDay { get; init; }
}
