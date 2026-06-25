using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(IAgentRuntime agentRuntime) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SendChatMessageResponse>> SendMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await agentRuntime.ProcessChatAsync(
                User.GetRequiredUserProfileId(),
                request.Message,
                request.ConversationId,
                cancellationToken);

            return Ok(new SendChatMessageResponse(
                result.ConversationId,
                result.Reply,
                result.UsedMemories.Select(x => x.ToResponse()).ToList(),
                result.GeneratedInsights.Select(x => x.ToResponse()).ToList(),
                result.MemorySuggestions.Select(x => x.ToResponse()).ToList(),
                result.GoalSuggestions.Select(x => x.ToResponse()).ToList(),
                result.ProjectSuggestions.Select(x => x.ToResponse()).ToList(),
                result.TaskSuggestions.Select(x => x.ToResponse()).ToList(),
                result.ApprovalRequests.Select(x => x.ToResponse()).ToList(),
                result.CreatedOpenLoops.Select(x => x.ToResponse()).ToList(),
                result.ToolExecutions.Select(x => x.ToResponse()).ToList(),
                result.Provider,
                result.Model,
                result.UsedFallback));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class SendChatMessageRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Message { get; init; } = string.Empty;

    public Guid? ConversationId { get; init; }
}
