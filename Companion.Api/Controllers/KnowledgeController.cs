using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
[Authorize]
public class KnowledgeController(
    IKnowledgeImporter knowledgeImporter,
    IKnowledgeSearchService knowledgeSearchService) : ControllerBase
{
    [HttpPost("import")]
    [ProducesResponseType(typeof(KnowledgeImportResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<KnowledgeImportResponse>> ImportDocument(
        [FromBody] ImportKnowledgeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeImporter.ImportAsync(
            User.GetRequiredUserProfileId(),
            new ImportKnowledgeDocumentCommand(
                request.SourceId,
                request.SourceName,
                request.SourceType,
                request.SourceDescription,
                request.Title,
                request.Content,
                request.MimeType),
            cancellationToken);

        return Created($"/api/knowledge/sources/{result.Source.Id}", result.ToResponse());
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeSearchResultResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgeSearchResultResponse>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var results = await knowledgeSearchService.SearchAsync(
            User.GetRequiredUserProfileId(),
            query,
            Math.Clamp(limit, 1, 20),
            audit: true,
            cancellationToken: cancellationToken);

        return Ok(results.Select(x => x.ToResponse()));
    }

    [HttpGet("sources")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeSourceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgeSourceResponse>>> GetSources(CancellationToken cancellationToken)
    {
        var sources = await knowledgeSearchService.GetSourcesAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(sources.Select(x => x.ToResponse()));
    }
}

public sealed class ImportKnowledgeDocumentRequest
{
    public Guid? SourceId { get; init; }

    [MaxLength(200)]
    public string? SourceName { get; init; }

    [MaxLength(100)]
    public string? SourceType { get; init; }

    [MaxLength(2000)]
    public string? SourceDescription { get; init; }

    [Required]
    [MaxLength(300)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(100000)]
    public string Content { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string MimeType { get; init; } = string.Empty;
}
