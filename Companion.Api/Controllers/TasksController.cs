using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskItemResponse>>> GetTasks(CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetTasksAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(tasks.Select(x => x.ToResponse()));
    }

    [HttpGet("open")]
    [ProducesResponseType(typeof(IEnumerable<TaskItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskItemResponse>>> GetOpenTasks(CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetOpenTasksAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(tasks.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskItemResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskItemResponse>> CreateTask(
        [FromBody] CreateTaskItemRequest request,
        CancellationToken cancellationToken)
    {
        var taskItem = await taskService.CreateTaskAsync(
            CompanionDefaults.LocalUserProfileId,
            new CreateTaskItemCommand(
                request.Title,
                request.Description,
                request.Priority ?? TaskItemPriority.Normal,
                request.DueDateUtc,
                request.SourceMessageId,
                request.Status ?? TaskItemStatus.Todo),
            cancellationToken);

        return Created($"/api/tasks/{taskItem.Id}", taskItem.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemResponse>> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskItemRequest request,
        CancellationToken cancellationToken)
    {
        var taskItem = await taskService.UpdateTaskAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            new UpdateTaskItemCommand(
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.DueDateUtc),
            cancellationToken);

        return taskItem is null ? NotFound() : Ok(taskItem.ToResponse());
    }
}

public sealed class CreateTaskItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public TaskItemStatus? Status { get; init; }

    public TaskItemPriority? Priority { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public Guid? SourceMessageId { get; init; }
}

public sealed class UpdateTaskItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public TaskItemStatus? Status { get; init; }

    public TaskItemPriority? Priority { get; init; }

    public DateTime? DueDateUtc { get; init; }
}
