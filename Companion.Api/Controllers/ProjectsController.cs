using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetProjectsAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(projects.Select(x => x.ToResponse()));
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IEnumerable<ProjectSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectSuggestionResponse>>> GetProjectSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await projectService.GetProjectSuggestionsAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(suggestions.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateProjectAsync(
            CompanionDefaults.LocalUserProfileId,
            new CreateProjectCommand(
                request.Title,
                request.Description,
                request.Priority ?? PlanningPriority.Normal,
                request.Status ?? ProjectStatus.Active),
            cancellationToken);

        return Created($"/api/projects/{project.Id}", project.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.UpdateProjectAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            new UpdateProjectCommand(
                request.Title,
                request.Description,
                request.Status,
                request.Priority),
            cancellationToken);

        return project is null ? NotFound() : Ok(project.ToResponse());
    }

    [HttpPost("suggestions/{id:guid}/approve")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> ApproveSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectService.ApproveSuggestionAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            cancellationToken);

        return project is null ? NotFound() : Ok(project.ToResponse());
    }

    [HttpPost("suggestions/{id:guid}/reject")]
    [ProducesResponseType(typeof(ProjectSuggestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectSuggestionResponse>> RejectSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await projectService.RejectSuggestionAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            cancellationToken);

        return suggestion is null ? NotFound() : Ok(suggestion.ToResponse());
    }
}

public sealed class CreateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public ProjectStatus? Status { get; init; }

    public PlanningPriority? Priority { get; init; }
}

public sealed class UpdateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public ProjectStatus? Status { get; init; }

    public PlanningPriority? Priority { get; init; }
}
