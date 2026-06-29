using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IFileCapability fileCapability) : ControllerBase
{
    [HttpGet("documents")]
    [ProducesResponseType(typeof(IEnumerable<FileDocumentSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FileDocumentSnapshotResponse>>> GetDocuments(
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var documents = await fileCapability.GetRecentAsync(
            User.GetRequiredUserProfileId(),
            limit,
            audit: true,
            cancellationToken);

        return Ok(documents.Select(x => x.ToResponse()));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<FileDocumentSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FileDocumentSnapshotResponse>>> SearchDocuments(
        [FromQuery] string query = "",
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var documents = await fileCapability.SearchAsync(
            User.GetRequiredUserProfileId(),
            query,
            Math.Clamp(limit, 1, 100),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(documents.Select(x => x.ToResponse()));
    }

    [HttpGet("documents/{id:guid}")]
    [ProducesResponseType(typeof(FileDocumentSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDocumentSnapshotResponse>> GetDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = await fileCapability.ReadMetadataAsync(
            User.GetRequiredUserProfileId(),
            id,
            audit: true,
            cancellationToken: cancellationToken);

        return document is null ? NotFound() : Ok(document.ToResponse());
    }
}
