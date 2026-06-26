using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/learning")]
[Authorize]
public class LearningController(IAdaptiveLearningService learningService) : ControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType(typeof(LearningProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LearningProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await learningService.GetProfileAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(profile.ToResponse());
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(IEnumerable<LearningEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LearningEventResponse>>> GetEvents(CancellationToken cancellationToken)
    {
        var events = await learningService.GetEventsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(events.Select(x => x.ToResponse()));
    }

    [HttpPost("events")]
    [ProducesResponseType(typeof(LearningEventResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LearningEventResponse>> RecordEvent(
        [FromBody] RecordLearningEventRequest request,
        CancellationToken cancellationToken)
    {
        var learningEvent = await learningService.RecordEventAsync(
            User.GetRequiredUserProfileId(),
            new RecordLearningEventCommand(
                request.EventType,
                request.SourceType,
                request.SourceId,
                request.Signal,
                request.Weight,
                request.MetadataJson),
            cancellationToken);

        return Created($"/api/learning/events/{learningEvent.Id}", learningEvent.ToResponse());
    }

    [HttpPost("ratings")]
    [ProducesResponseType(typeof(ConversationRatingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationRatingResponse>> RateConversation(
        [FromBody] ConversationRatingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rating = await learningService.RateConversationAsync(
                User.GetRequiredUserProfileId(),
                new ConversationRatingCommand(request.ConversationId, request.Rating, request.Comment),
                cancellationToken);

            return Created($"/api/learning/ratings/{rating.Id}", rating.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class RecordLearningEventRequest
{
    [Required]
    [MaxLength(100)]
    public string EventType { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SourceType { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SourceId { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Signal { get; init; } = string.Empty;

    public decimal Weight { get; init; } = 1m;

    public string? MetadataJson { get; init; }
}

public sealed class ConversationRatingRequest
{
    public Guid ConversationId { get; init; }

    [Range(1, 5)]
    public int Rating { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}
