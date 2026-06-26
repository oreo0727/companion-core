using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IConnectorSyncService connectorSyncService) : ControllerBase
{
    [HttpGet("documents")]
    [ProducesResponseType(typeof(IEnumerable<FileDocumentSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FileDocumentSnapshotResponse>>> GetDocuments(
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var documents = await connectorSyncService.GetRecentFileDocumentsAsync(
            User.GetRequiredUserProfileId(),
            limit,
            audit: true,
            cancellationToken);

        return Ok(documents.Select(x => x.ToResponse()));
    }
}
