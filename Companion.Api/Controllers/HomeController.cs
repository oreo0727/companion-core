using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/home")]
[Authorize]
public class HomeController(IConnectorSyncService connectorSyncService) : ControllerBase
{
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IEnumerable<HomeDeviceSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HomeDeviceSnapshotResponse>>> GetDevices(CancellationToken cancellationToken)
    {
        var devices = await connectorSyncService.GetHomeDevicesAsync(
            User.GetRequiredUserProfileId(),
            cancellationToken: cancellationToken);

        return Ok(devices.Select(x => x.ToResponse()));
    }

    [HttpGet("sensors")]
    [ProducesResponseType(typeof(IEnumerable<HomeSensorSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HomeSensorSnapshotResponse>>> GetSensors(CancellationToken cancellationToken)
    {
        var sensors = await connectorSyncService.GetHomeSensorsAsync(
            User.GetRequiredUserProfileId(),
            cancellationToken: cancellationToken);

        return Ok(sensors.Select(x => x.ToResponse()));
    }
}
