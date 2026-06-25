using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Enums;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController(IGoalService goalService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GoalResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GoalResponse>>> GetGoals(CancellationToken cancellationToken)
    {
        var goals = await goalService.GetGoalsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(goals.Select(x => x.ToResponse()));
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IEnumerable<GoalSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GoalSuggestionResponse>>> GetGoalSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await goalService.GetGoalSuggestionsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(suggestions.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<GoalResponse>> CreateGoal(
        [FromBody] CreateGoalRequest request,
        CancellationToken cancellationToken)
    {
        var goal = await goalService.CreateGoalAsync(
            User.GetRequiredUserProfileId(),
            new CreateGoalCommand(
                request.Title,
                request.Description,
                request.Priority ?? PlanningPriority.Normal,
                request.TargetDateUtc,
                request.Status ?? GoalStatus.Active),
            cancellationToken);

        return Created($"/api/goals/{goal.Id}", goal.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalResponse>> UpdateGoal(
        Guid id,
        [FromBody] UpdateGoalRequest request,
        CancellationToken cancellationToken)
    {
        var goal = await goalService.UpdateGoalAsync(
            User.GetRequiredUserProfileId(),
            id,
            new UpdateGoalCommand(
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.TargetDateUtc),
            cancellationToken);

        return goal is null ? NotFound() : Ok(goal.ToResponse());
    }

    [HttpPost("suggestions/{id:guid}/approve")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalResponse>> ApproveSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var goal = await goalService.ApproveSuggestionAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return goal is null ? NotFound() : Ok(goal.ToResponse());
    }

    [HttpPost("suggestions/{id:guid}/reject")]
    [ProducesResponseType(typeof(GoalSuggestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalSuggestionResponse>> RejectSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await goalService.RejectSuggestionAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return suggestion is null ? NotFound() : Ok(suggestion.ToResponse());
    }
}

public sealed class CreateGoalRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public GoalStatus? Status { get; init; }

    public PlanningPriority? Priority { get; init; }

    public DateTime? TargetDateUtc { get; init; }
}

public sealed class UpdateGoalRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public GoalStatus? Status { get; init; }

    public PlanningPriority? Priority { get; init; }

    public DateTime? TargetDateUtc { get; init; }
}
