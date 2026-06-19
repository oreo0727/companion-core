using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/agent-runs")]
public class AgentRunsController(IAgentRuntime agentRuntime) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentRunResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentRunResponse>>> GetAgentRuns(CancellationToken cancellationToken)
    {
        var agentRuns = await agentRuntime.GetRunsAsync(cancellationToken);
        return Ok(agentRuns.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AgentRunResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<AgentRunResponse>> CreateAgentRun(
        [FromBody] CreateAgentRunRequest request,
        CancellationToken cancellationToken)
    {
        var agentRun = await agentRuntime.QueueRunAsync(
            new QueueAgentRunCommand(
                request.AgentName,
                request.Input,
                request.UserProfileId ?? CompanionDefaults.LocalUserProfileId,
                request.ConversationId,
                request.MetadataJson),
            cancellationToken);

        return Accepted($"/api/agent-runs/{agentRun.Id}", agentRun.ToResponse());
    }
}

public sealed class CreateAgentRunRequest
{
    [Required]
    [MaxLength(200)]
    public string AgentName { get; init; } = string.Empty;

    [Required]
    [MaxLength(20000)]
    public string Input { get; init; } = string.Empty;

    public Guid? UserProfileId { get; init; }

    public Guid? ConversationId { get; init; }

    public string? MetadataJson { get; init; }
}
