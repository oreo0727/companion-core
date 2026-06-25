using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/tools")]
[Authorize]
public class ToolsController(IToolExecutor toolExecutor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ToolDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ToolDefinitionResponse>>> GetTools(CancellationToken cancellationToken)
    {
        var tools = await toolExecutor.GetDefinitionsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(tools.Select(x => x.ToResponse()));
    }

    [HttpGet("executions")]
    [ProducesResponseType(typeof(IEnumerable<ToolExecutionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ToolExecutionResponse>>> GetExecutions(CancellationToken cancellationToken)
    {
        var executions = await toolExecutor.GetExecutionsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(executions.Select(x => x.ToResponse()));
    }

    [HttpPost("{id:guid}/execute")]
    [ProducesResponseType(typeof(ToolDispatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ToolDispatchResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolDispatchResponse>> ExecuteTool(
        Guid id,
        [FromBody] ExecuteToolRequest request,
        CancellationToken cancellationToken)
    {
        var definition = await toolExecutor.GetDefinitionAsync(id, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        try
        {
            var dispatch = await toolExecutor.ExecuteAsync(
                User.GetRequiredUserProfileId(),
                definition.Name,
                request.Input.ValueKind == JsonValueKind.Undefined
                    ? "{}"
                    : JsonSerializer.Serialize(request.Input),
                cancellationToken: cancellationToken);

            return dispatch.ExecutedImmediately
                ? Ok(dispatch.ToResponse())
                : Accepted($"/api/tools/executions/{dispatch.Execution.Id}", dispatch.ToResponse());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class ExecuteToolRequest
{
    public JsonElement Input { get; init; }
}
