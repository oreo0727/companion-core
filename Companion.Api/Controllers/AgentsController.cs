using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentsController(IAgentCatalog agentCatalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentDefinitionResponse>>> GetAgents(CancellationToken cancellationToken)
    {
        var agents = await agentCatalog.GetAgentsAsync(cancellationToken);
        return Ok(agents.Select(x => x.ToResponse()));
    }
}
