using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/agent-runs")]
[Authorize]
public class AgentRunsController(IAgentRuntime agentRuntime) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentRunResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentRunResponse>>> GetAgentRuns(CancellationToken cancellationToken)
    {
        var agentRuns = await agentRuntime.GetRunsAsync(User.GetRequiredUserProfileId(), cancellationToken);
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
                User.GetRequiredUserProfileId(),
                request.ConversationId,
                request.MetadataJson,
                request.ParentAgentRunId,
                request.DelegationReason),
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

    public Guid? ParentAgentRunId { get; init; }

    [MaxLength(1000)]
    public string? DelegationReason { get; init; }
}
