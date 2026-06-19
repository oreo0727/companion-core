using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/memories")]
public class MemoriesController(IMemoryService memoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MemoryEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MemoryEntryResponse>>> GetMemories(CancellationToken cancellationToken)
    {
        var memories = await memoryService.GetMemoriesAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(memories.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MemoryEntryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MemoryEntryResponse>> CreateMemory(
        [FromBody] CreateMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var memory = await memoryService.CreateMemoryAsync(
            CompanionDefaults.LocalUserProfileId,
            new CreateMemoryCommand(
                request.Type,
                request.Summary,
                request.Content,
                request.Source,
                request.Importance,
                request.Sensitivity,
                request.Confidence,
                request.ExpiresUtc),
            cancellationToken);

        return Created($"/api/memories/{memory.Id}", memory.ToResponse());
    }

    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(typeof(MemoryEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemoryEntryResponse>> ArchiveMemory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var memory = await memoryService.ArchiveMemoryAsync(id, cancellationToken);
        return memory is null ? NotFound() : Ok(memory.ToResponse());
    }
}

public sealed class CreateMemoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Type { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; init; } = string.Empty;

    [Required]
    [MaxLength(20000)]
    public string Content { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "1")]
    public decimal Confidence { get; init; }

    [Required]
    [MaxLength(200)]
    public string Source { get; init; } = string.Empty;

    [Range(1, 5)]
    public int Importance { get; init; } = 3;

    [Required]
    [MaxLength(32)]
    public string Sensitivity { get; init; } = "Normal";

    public DateTime? ExpiresUtc { get; init; }
}
