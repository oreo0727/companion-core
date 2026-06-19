using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/chat")]
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
                CompanionDefaults.LocalUserProfileId,
                request.Message,
                request.ConversationId,
                cancellationToken);

            return Ok(new SendChatMessageResponse(
                result.Reply,
                result.ConversationId,
                result.SavedMemories.Select(x => x.ToResponse()).ToList(),
                result.CreatedTasks.Select(x => x.ToResponse()).ToList(),
                result.ApprovalRequests.Select(x => x.ToResponse()).ToList(),
                result.UsedMemories.Select(x => x.ToResponse()).ToList()));
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
