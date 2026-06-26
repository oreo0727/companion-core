using System.ComponentModel.DataAnnotations;
using System.Text;
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

    [HttpPost("local-email/import")]
    [ProducesResponseType(typeof(LocalEmailImportResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LocalEmailImportResponse>> ImportLocalEmail(
        [FromBody] LocalEmailImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await connectorSyncService.ImportLocalEmailAsync(
            User.GetRequiredUserProfileId(),
            new LocalEmailImportCommand(
                request.DisplayName,
                request.Messages.Select(x => new LocalEmailImportMessage(
                    x.ExternalId,
                    x.Subject,
                    x.FromName,
                    x.FromAddress,
                    x.ToAddresses,
                    x.Preview,
                    x.Body,
                    x.ReceivedUtc,
                    x.IsRead,
                    x.HasAttachments,
                    x.IsAnswered)).ToList()),
            cancellationToken);

        return Created($"/api/connectors/{result.Connection.Id}", result.ToResponse());
    }

    [HttpPost("local-home/import")]
    [ProducesResponseType(typeof(LocalHomeImportResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LocalHomeImportResponse>> ImportLocalHome(
        [FromBody] LocalHomeImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await connectorSyncService.ImportLocalHomeAsync(
            User.GetRequiredUserProfileId(),
            new LocalHomeImportCommand(
                request.DisplayName,
                request.Devices.Select(x => new LocalHomeImportDevice(
                    x.ExternalId,
                    x.Name,
                    x.DeviceType,
                    x.State,
                    x.Room,
                    x.CapabilitiesJson,
                    x.LastSeenUtc)).ToList(),
                request.Sensors.Select(x => new LocalHomeImportSensor(
                    x.ExternalId,
                    x.Name,
                    x.SensorType,
                    x.Value,
                    x.Unit,
                    x.Room,
                    x.ObservedUtc)).ToList()),
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
            string? payloadJson = null;
            if (Request.ContentLength > 0)
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                payloadJson = await reader.ReadToEndAsync(cancellationToken);
            }

            var syncRun = await connectorSyncService.SyncAsync(User.GetRequiredUserProfileId(), id, payloadJson, cancellationToken);
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

public sealed class LocalEmailImportRequest
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<LocalEmailImportMessageRequest> Messages { get; init; } = [];
}

public sealed class LocalEmailImportMessageRequest
{
    [MaxLength(300)]
    public string? ExternalId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? FromName { get; init; }

    [Required]
    [MaxLength(500)]
    public string FromAddress { get; init; } = string.Empty;

    public IReadOnlyList<string> ToAddresses { get; init; } = [];

    [MaxLength(1000)]
    public string? Preview { get; init; }

    [MaxLength(12000)]
    public string? Body { get; init; }

    public DateTime ReceivedUtc { get; init; }

    public bool IsRead { get; init; }

    public bool HasAttachments { get; init; }

    public bool IsAnswered { get; init; }
}

public sealed class LocalHomeImportRequest
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<LocalHomeDeviceRequest> Devices { get; init; } = [];

    public IReadOnlyList<LocalHomeSensorRequest> Sensors { get; init; } = [];
}

public sealed class LocalHomeDeviceRequest
{
    [MaxLength(300)]
    public string? ExternalId { get; init; }

    [Required]
    [MaxLength(300)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DeviceType { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string State { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? Room { get; init; }

    [MaxLength(4000)]
    public string? CapabilitiesJson { get; init; }

    public DateTime? LastSeenUtc { get; init; }
}

public sealed class LocalHomeSensorRequest
{
    [MaxLength(300)]
    public string? ExternalId { get; init; }

    [Required]
    [MaxLength(300)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SensorType { get; init; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Value { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? Unit { get; init; }

    [MaxLength(200)]
    public string? Room { get; init; }

    public DateTime? ObservedUtc { get; init; }
}
